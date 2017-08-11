namespace ExBuddy.OrderBotTags.Gather.Strategies
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Behaviors;
    using Buddy.Coroutines;
    using Enumerations;
    using ff14bot;
    using ff14bot.Objects;
    using Helpers;
    using Interfaces;
    using Inventory;
    using Inventory.StockManagers;
    using Logging;

    internal class BeforeGatherGpRegenStrategy : IGpRegenStrategy
    {
        public class EffectiveNodeLifespan
        {
            public EffectiveNodeLifespan(NodeHelper.NodeLifespan actualNodeLifespan, int rotationSeconds)
            {
                this.Set(actualNodeLifespan.DeSpawn - TimeSpan.FromSeconds(rotationSeconds));
            }

            private void Set(TimeSpan despawn)
            {
                this.DeSpawn = despawn;
                this.DeSpawnTicks = EorzeaTimeHelper.ConvertSecondsToTicks(Convert.ToInt32(this.DeSpawn.TotalSeconds));
            }

            public void Clamp(TimeSpan min, TimeSpan max)
            {
                this.Set(this.DeSpawn.Clamp(min, max));
            }

            public TimeSpan DeSpawn { get; protected set; }
            public int DeSpawnTicks { get; protected set; }
        }

        private const double MAX_TOUCHANDGO_WAIT = 15;

        private readonly IBeforeGatherGpRegenStrategyLogger logger;
        private readonly CordialStockManager cordialStockManager;
        protected readonly short gpPerTick;

        protected IGatheringRotation gatherRotation;
        protected GatherStrategy gatherStrategy;
        protected CordialTime cordialTime;
        protected CordialType requestedCordialType;
        protected CordialType effectiveCordialType;

        /// <summary>
        /// Instantiates a new instance of the <see cref="BeforeGatherGpRegenStrategy"/> class.
        /// </summary>
        public BeforeGatherGpRegenStrategy(CordialStockManager cordialStockManager, IBeforeGatherGpRegenStrategyLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException("logger");
            this.cordialStockManager = cordialStockManager ?? throw new ArgumentNullException("cordialStockManager");
            this.gpPerTick = CharacterResource.GetGpPerTick();
        }

        /// <summary>
        /// Logs the cordial use result
        /// </summary>
        private void LogCordialResult(InventoryItem.UseResult result)
        {
            switch (result)
            {
                case InventoryItem.UseResult.OK:
                    this.logger.CordialUsed(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq,
                        this.CordialGp,
                        this.StartingGp,
                        this.RegeneratedGp,
                        this.TargetGp
                    );
                    break;

                case InventoryItem.UseResult.CantUse:
                    this.logger.CordialCannotBeUsedByPlayer(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.DismountFailed:
                    this.logger.CordialCannotBeUsedDismountFailed(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.Mounted:
                    this.logger.CordialCannotBeUsedByMountedPlayer(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.NoInventory:
                    this.logger.CordialInventoryDepleted(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.OnCooldown:
                    this.logger.CordialOnCooldown(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.PlayerDead:
                    this.logger.CordialCannotBeUsedByDeadPlayer(
                        this.Cordial.ItemData.CurrentLocaleName,
                        this.Cordial.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.NoUse:
                case InventoryItem.UseResult.UnknownItem:
                default:
                    this.logger.CordialItemTypeInvalid(
                        this.Cordial.ItemKey.Id
                    );
                    break;
            }
        }

        /// <summary>
        /// Configures the strategy for use
        /// </summary>
        protected void Configure(GatheringPointObject node, IGatheringRotation gatherRotation, GatherStrategy gatherStrategy, CordialTime cordialTime, CordialType cordialType)
        {
            // Set gathering parameters
            this.gatherRotation = gatherRotation ?? throw new ArgumentNullException("gatherRotation");
            this.gatherStrategy = gatherStrategy;

            // Set cordial parameters
            this.cordialTime = cordialTime;
            this.requestedCordialType = this.effectiveCordialType = cordialType;

            // Override cordial type
            if (this.requestedCordialType > CordialType.None)
            {
                // Turn off cordial when there is no stock
                if (!this.cordialStockManager.HasStock())
                {
                    this.effectiveCordialType = CordialType.None;

                    this.logger.RegeneratingCordialUseDisabledNoStock();
                }

                // Turn off cordial when node is not ephemeral in TnG strategies
                if (this.gatherStrategy == GatherStrategy.TouchAndGo && !node.IsEphemeral())
                {
                    this.effectiveCordialType = CordialType.None;
                }
            }
            else
            {
                this.effectiveCordialType = this.requestedCordialType;
            }

            // Set node lifespan parameters
            this.NodeLifespan = new EffectiveNodeLifespan(NodeHelper.GetNodeLifespan(node), this.gatherRotation.Attributes.RequiredTimeInSeconds);
            if (this.gatherStrategy == GatherStrategy.TouchAndGo)
            {
                this.NodeLifespan.Clamp(TimeSpan.Zero, TimeSpan.FromSeconds(MAX_TOUCHANDGO_WAIT));
            }
            this.EffectiveTimeTillGather = this.NodeLifespan.DeSpawn;

            // Set GP parameters
            this.StartingGp = this.CurrentGp;
            this.MaxGp = ExProfileBehavior.Me.MaxGP;
            this.EffectiveGp = CharacterResource.GetEffectiveGp(this.NodeLifespan.DeSpawnTicks);

            // Set breakpoint and cordial parameters
            this.CalculateTargetAndCordialSelection();

            // Calculate regeneration parameters
            this.RegeneratedGp = (short)(this.TargetGp - this.CordialGp - this.StartingGp);
            if (this.RegeneratedGp < 0) this.RegeneratedGp = 0;
            this.EffectiveTimeToRegenerate = CharacterResource.EstimateExpectedRegenerationTime(this.RegeneratedGp);
        }

        /// <summary>
        /// Calculates the target GP needed to execute gathering strategy with cordials
        /// </summary>
        protected void CalculateTargetAndCordialSelection()
        {
            // Reset cordial
            this.Cordial = null;
            this.CordialGp = 0;

            var lastBreakpointGpValue = 0;
            var useCordial = this.effectiveCordialType > CordialType.None
                             && this.cordialTime.HasFlag(CordialTime.BeforeGather)
                             && this.cordialStockManager.GetCordialCooldown().Add(new TimeSpan(0, 0, 0, 2)) <= this.EffectiveTimeTillGather;

            foreach (var breakpointGp in this.gatherRotation.Attributes.RequiredGpBreakpoints.OrderByDescending(bp => bp))
            {
                this.TargetGp = this.BreakpointGp = (short)breakpointGp;

                // If we'll regen enough, do not select a cordial
                if (this.EffectiveGp >= this.TargetGp)
                {
                    return;
                }

                // Find a cordial if configured to allow it and there is enough time
                if (useCordial)
                {
                    var missingGp = this.TargetGp - this.EffectiveGp;
                    var bestCordial = this.cordialStockManager.GetFulfillingCordial(missingGp, this.effectiveCordialType);

                    // If we found a cordial, use this breakpoint and cordial
                    if (bestCordial != null)
                    {
                        this.Cordial = bestCordial;
                        this.CordialGp = (short)CordialStockManager.CordialDataMap[bestCordial.ItemKey].Gp;
                        return;
                    }
                }

                // Store the last breakpoint GP in case there is no way to regen
                lastBreakpointGpValue = breakpointGp;
            }

            // There is no way to regenerate the required GP
            this.BreakpointGp = (short)lastBreakpointGpValue;
            this.TargetGp = this.StartingGp;
        }

        /// <summary>
        /// Executes the coroutine which causes the player to wait for GP regeneration
        /// </summary>
        /// <returns></returns>
        protected async Task<GpRegenStrategyResult.GpRegenStrategyResultState> WaitForGpRegeneration()
        {
            // Return OK immediately if our target is met
            if (this.EffectiveTimeToRegenerate == TimeSpan.Zero)
            {
                return GpRegenStrategyResult.GpRegenStrategyResultState.OK;
            }

            // Execute the wait coroutine
            this.logger.RegeneratingGp(Convert.ToInt16((this.EffectiveTimeToRegenerate.TotalSeconds)));

            await Coroutine.Wait(
                this.EffectiveTimeTillGather,
                () =>
                {
                    var gp = this.CurrentGp;
                    return gp >= this.TargetGp || gp == this.MaxGp || Core.Player.IsDead;
                }
            );

            return GpRegenStrategyResult.GpRegenStrategyResultState.OK;
        }

        /// <summary>
        /// Executes the strategy logic in charge of regenerating GP before the gather.
        /// </summary>
        /// <returns></returns>
        public async Task<GpRegenStrategyResult> RegenerateGp(GatheringPointObject node, IGatheringRotation gatherRotation, GatherStrategy gatherStrategy, CordialTime cordialTime, CordialType cordialType)
        {
            var rtn = new GpRegenStrategyResult()
            {
                EffectiveCordialType = this.effectiveCordialType,
                OriginalCordialType = this.requestedCordialType,
            };

            // Return OK immediately if there is no node
            if (node == null)
            {
                this.logger.GatheringNodeIsGone();

                rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.NodeGone;
                return rtn;
            }

            // Configure the strategy and report to the log
            this.Configure(node, gatherRotation, gatherStrategy, cordialTime, cordialType);
            this.logger.LogReport(this);

            // Return not enough time if player has less than 3 seconds to gather
            if (this.EffectiveTimeTillGather.TotalSeconds < 3)
            {
                this.logger.GatheringNotEnoughTime();

                rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.NotEnoughTime;
                return rtn;
            }

            if (this.gatherStrategy == GatherStrategy.GatherOrCollect)
            {
                // Return not enough GP if we cannot meet the target breakpoint for the rotation
                if (this.BreakpointGp > this.TargetGp)
                {
                    this.logger.RegeneratingNotEnoughGp();

                    rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.NotEnoughGp;
                    return rtn;
                }
            }

            // Use the cordial if one was selected
            if (this.Cordial != null)
            {
                rtn.UseState = await this.Cordial.Use(
                    ExProfileBehavior.Me,
                    maxTimeout: this.EffectiveTimeTillGather,
                    dismount: true
                );

                this.LogCordialResult(rtn.UseState.Value);

                // Recalculate player's ability to gather after cordial use failure
                if (rtn.UseState != InventoryItem.UseResult.OK)
                {
                    this.effectiveCordialType = CordialType.None;
                    this.CalculateTargetAndCordialSelection();

                    this.logger.LogReport(this);

                    // Return not enough GP if we cannot meet the target breakpoint for the rotation
                    if (this.BreakpointGp > this.TargetGp)
                    {
                        this.logger.RegeneratingNotEnoughGp();

                        rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.NotEnoughGp;
                        return rtn;
                    }
                }
            }

            // Handle TouchAndGo overrides
            if (this.gatherStrategy == GatherStrategy.TouchAndGo)
            {
                // Do nothing if this is not ephemeral
                if (!node.IsEphemeral())
                {
                    this.logger.GatheringNodeSkippedNotEphemeral();

                    rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.OK;
                    return rtn;
                }

                if (this.EffectiveTimeToRegenerate.TotalSeconds > MAX_TOUCHANDGO_WAIT)
                {
                    this.logger.RegeneratingSkippedExceedsEphemeralMaxWait(
                        this.EffectiveTimeToRegenerate.TotalSeconds,
                        MAX_TOUCHANDGO_WAIT
                    );

                    rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.OK;
                    return rtn;
                }
            }

            // Return OK immediately if the rotation forcefully gathers the item
            if (gatherRotation.ShouldForceGather(node))
            {
                rtn.StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.OK;
                return rtn;
            }

            // Execute wait logic
            rtn.StrategyState = await this.WaitForGpRegeneration();

            return rtn;
        }

        /// <summary>
        /// Formats a status report string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(Localization.Localization.ExGather_BeforeGatherReport,
                this.StartingGp,
                this.CurrentGp,
                this.EffectiveGp,
                this.BreakpointGp,
                this.TargetGp,
                this.MaxGp,
                this.Cordial != null
                    ? (this.Cordial.ItemData != null)
                        ? this.Cordial.ItemData.CurrentLocaleName
                        : this.Cordial.ItemKey.Id.ToString()
                    : string.Empty,
                this.Cordial != null
                    ? this.CordialGp
                    : (short)0
            );
        }

        /// <summary>
        /// Gets the node lifespan
        /// </summary>
        public EffectiveNodeLifespan NodeLifespan { get; protected set; }

        /// <summary>
        /// Gets the seconds until we must gather
        /// </summary>
        public TimeSpan EffectiveTimeTillGather { get; protected set; }

        /// <summary>
        /// Gets the seconds to wait to hit target GP
        /// </summary>
        public TimeSpan EffectiveTimeToRegenerate { get; protected set; }

        /// <summary>
        /// Gets the player's starting GP at beginning of strategy
        /// </summary>
        public short StartingGp { get; protected set; }

        /// <summary>
        /// Gets the player's current GP
        /// </summary>
        public short CurrentGp
        {
            get { return ExProfileBehavior.Me.CurrentGP; }
        }

        /// <summary>
        /// Gets the player's max GP
        /// </summary>
        public short MaxGp { get; protected set; }

        /// <summary>
        /// Gets what the player's GP will be when gathering begins
        /// </summary>
        public short EffectiveGp { get; protected set; }

        /// <summary>
        /// Gets the gathering rotation breakpoint GP
        /// </summary>
        public short BreakpointGp { get; protected set; }

        /// <summary>
        /// Gets the GP target to regenerate to
        /// </summary>
        public short TargetGp { get; protected set; }

        /// <summary>
        /// Gets the GP that we will regenerate
        /// </summary>
        public short RegeneratedGp { get; protected set; }

        /// <summary>
        /// Gets the cordial selected for use during regeneration
        /// </summary>
        public InventoryItem Cordial { get; protected set; }

        /// <summary>
        /// Amount of GP provided by the cordial
        /// </summary>
        public short CordialGp { get; protected set; }
    }
}

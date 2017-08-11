namespace ExBuddy.OrderBotTags.Gather.Strategies
{
    using System;
    using System.Threading.Tasks;
    using Behaviors;
    using Enumerations;
    using ff14bot.Objects;
    using Interfaces;
    using Inventory;
    using Inventory.StockManagers;
    using Logging;

    internal class AfterGatherGpRegenStrategy : IGpRegenStrategy
    {
        private readonly CordialStockManager cordialStock;
        private readonly ICordialConsumerLogger logger;

        /// <summary>
        /// Instantiates a new instance of <see cref="AfterGatherGpRegenStrategy"/> class.
        /// </summary>
        public AfterGatherGpRegenStrategy(CordialStockManager cordialStock, ICordialConsumerLogger cordialConsumerLogger)
        {
            this.cordialStock = cordialStock ?? throw new ArgumentNullException("cordialStock");
            this.logger = cordialConsumerLogger ?? throw new ArgumentNullException("cordialConsumerLogger");
        }

        private void LogCordialResult(InventoryItem.UseResult result, InventoryItem cordialUsed, int startingGp)
        {
            switch (result)
            {
                case InventoryItem.UseResult.OK:
                    var cordialGp = CordialStockManager.CordialDataMap[cordialUsed.ItemKey].Gp;
                    this.logger.CordialUsed(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq,
                        cordialGp,
                        startingGp,
                        0,
                        startingGp + cordialGp
                    );
                    break;

                case InventoryItem.UseResult.CantUse:
                    this.logger.CordialCannotBeUsedByPlayer(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.DismountFailed:
                    this.logger.CordialCannotBeUsedDismountFailed(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.Mounted:
                    this.logger.CordialCannotBeUsedByMountedPlayer(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.NoInventory:
                    this.logger.CordialInventoryDepleted(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.OnCooldown:
                    this.logger.CordialOnCooldown(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.PlayerDead:
                    this.logger.CordialCannotBeUsedByDeadPlayer(
                        cordialUsed.ItemData.CurrentLocaleName,
                        cordialUsed.ItemKey.Hq
                    );
                    break;

                case InventoryItem.UseResult.NoUse:
                case InventoryItem.UseResult.UnknownItem:
                default:
                    this.logger.CordialItemTypeInvalid(
                        cordialUsed.ItemKey.Id
                    );
                    break;
            }
        }

        /// <summary>
        /// Executes the strategy logic in charge of regenerating GP after the gather.
        /// </summary>
        /// <returns></returns>
        public async Task<GpRegenStrategyResult> RegenerateGp(GatheringPointObject node, IGatheringRotation gatherRotation, GatherStrategy gatherStrategy, CordialTime cordialTime, CordialType cordialType)
        {
            var rtn = new GpRegenStrategyResult()
            {
                StrategyState = GpRegenStrategyResult.GpRegenStrategyResultState.OK,
                EffectiveCordialType = cordialType,
                OriginalCordialType = cordialType,
            };

            var useCordial = cordialTime.HasFlag(CordialTime.AfterGather)
                && this.cordialStock.HasStock()
                && this.cordialStock.GetCordialCooldown() == TimeSpan.Zero;

            var currentGp = ExProfileBehavior.Me.CurrentGP;
            var maxGp = ExProfileBehavior.Me.MaxGP;
            var missingGp = maxGp - currentGp;
            var cordial = this.cordialStock.GetBestCordial(missingGp, cordialType);

            // Return OK if there is no cordial to use
            if (cordial == null)
            {
                rtn.UseState = InventoryItem.UseResult.OK;
                return rtn;
            }

            // Use the cordial
            rtn.UseState = await cordial.Use(
                ExProfileBehavior.Me
            );

            // Log the result
            this.LogCordialResult(
                rtn.UseState ?? InventoryItem.UseResult.CantUse,
                cordial,
                currentGp
            );

            return rtn;
        }
    }
}

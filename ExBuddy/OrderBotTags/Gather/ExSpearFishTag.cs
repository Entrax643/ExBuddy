namespace ExBuddy.OrderBotTags.Gather
{
    using Attributes;
    using Behaviors;
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using Enumerations;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;
    using ff14bot.Objects;
    using ff14bot.RemoteWindows;
    using GatherSpots;
    using Helpers;
    using Interfaces;
    using Localization;
    using Objects;
    using Strategies;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using TreeSharp;
    using Windows;

    [LoggerName("ExSpearFish")]
    [XmlElement("ExSpearFish")]
    public class ExSpearFish : ExGatherTag
    {
        private static readonly object Lock = new object();

        protected static Regex SpearFishRegex = new Regex(
#if RB_CN
            @"[\u4e00-\u9fa5A-Za-z0-9·]+获得了|[\u4e00-\u9fa5]+|\ue03c",
#else
            @"You spear(?: a| an| [2-3])? (.+) measuring (\d{1,4}\.\d) ilms!",
#endif
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static Regex SpearFishGetAwayRegex = new Regex(
#if RB_CN
            @"鱼逃走了……",
#else
            @"The fish gets away\.\.\.",
#endif
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static Regex SwimmingShadowsRegex = new Regex(
#if RB_CN
            @"在某处出现了鱼影",
#else
            @"The shadow of an elusive sea creature lurks somewhere nearby!",
#endif
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static Regex FishSizeRegex = new Regex(@"(\d{1,4}\.\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static SpearResult SpearResult = new SpearResult();

        private readonly HarpoonTip _gigWindow = new HarpoonTip();

        internal SpellData CordialSpellData;

        private Func<bool> freeRangeConditionFunc;

        private bool gigSelected;

        private bool swimmingShadows;

        private bool interactedWithNode;

        private int loopCount;

        private Composite poiCoroutine;

        private int DefaultWait = 4500;

        private DateTime startTime;

        [XmlElement("Collectables")]
        private new List<Collectable> Collectables { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("GigId")]
        public int GigId { get; set; }

        [DefaultValue(1)]
        [XmlAttribute("VeteranTradeDepth")]
        public int VeteranTradeDepth { get; set; }

        [DefaultValue(1)]
        [XmlAttribute("IdenticalGigDepth")]
        public int IdenticalGigDepth { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("BountifulCatch")]
        public bool BountifulCatch { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("BountifulCatchDepth")]
        public int BountifulCatchDepth { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("BountifulCatchAfterVeteranTrade")]
        public bool BountifulCatchAfterVeteranTrade { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("Collect")]
        public bool Collect { get; set; }

        private uint CollectabilityValue { get; set; }

        [XmlElement("Items")]
        public new NamedItemCollection Items { get; set; }

        protected override Color Info => Colors.Aquamarine;

        protected override void DoReset()
        {
            loopCount = 0;
            gigSelected = false;

            if (HotSpots != null)
            {
                HotSpots.Index = 0;
            }

            ResetInternal();
        }

        protected override async Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            return HandleDeath() || HandleCondition() || await CastTruth() || await SelectGig() || HandleReset() || await MoveToHotSpot()
                   || await FindNode() || await ResetOrDone();
        }

        protected override void OnDone() { TreeHooks.Instance.RemoveHook("PoiAction", poiCoroutine); }

        protected override void OnStart()
        {
            SpellDelay = SpellDelay < 0 ? 0 : SpellDelay;
            WindowDelay = WindowDelay < 1000 ? 1000 : WindowDelay;
            SkipWindowDelay = SkipWindowDelay < 200 ? 200 : SkipWindowDelay;
            GpPerTick = CharacterResource.GetGpPerTick();

            if (Distance > 3.5f)
            {
                TreeRoot.Stop(Localization.ExGather_Distance);
            }

            if (HotSpots != null)
            {
                HotSpots.IsCyclic = Loops < 1;
            }

            // backwards compatibility
            if (GatherObjects == null && !string.IsNullOrWhiteSpace(GatherObject))
            {
                GatherObjects = new List<string> { GatherObject };
            }

            startTime = DateTime.Now;

            if (Items == null)
            {
                Items = new NamedItemCollection();
            }

            if (Collect && Collectables == null)
            {
                Collectables = new List<Collectable> { new Collectable { Name = string.Empty, Value = (int)CollectabilityValue } };
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = Items.Count > 0 ? Items.First().Name : string.Format(Localization.ExGather_Zone, WorldManager.ZoneId, ExProfileBehavior.Me.Location);
            }

            StatusText = Name;

            GamelogManager.MessageRecevied += ReceiveMessage;
            poiCoroutine = new ActionRunCoroutine(ctx => ExecutePoiLogic());
            TreeHooks.Instance.AddHook("PoiAction", poiCoroutine);
        }

        protected void ReceiveMessage(object sender, ChatEventArgs e)
        {
#if RB_CN
            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents.Contains("获得了") ||
                e.ChatLogEntry.MessageType == (MessageType)67 && e.ChatLogEntry.Contents.Contains("鱼逃走了"))
            {
                MatchSpearResult(e.ChatLogEntry.Contents);
            }

            if (e.ChatLogEntry.MessageType == (MessageType)67 && e.ChatLogEntry.Contents.Contains("鱼影"))
            {
                MatchSwimmingShadow(e.ChatLogEntry.Contents);
            }
#else
            if (e.ChatLogEntry.MessageType == (MessageType)2115 && e.ChatLogEntry.Contents.StartsWith("You spear")
                || e.ChatLogEntry.MessageType == (MessageType)67 && e.ChatLogEntry.Contents.StartsWith("The fish"))
            {
                MatchSpearResult(e.ChatLogEntry.Contents);
            }

            if (e.ChatLogEntry.MessageType == (MessageType)67 && e.ChatLogEntry.Contents.StartsWith("The shadow"))
            {
                MatchSwimmingShadow(e.ChatLogEntry.Contents);
            }
#endif
        }

        protected void MatchSpearResult(string message)
        {
            SpearResult spearResult = new SpearResult();
            Match spearFishAwayMatch = SpearFishGetAwayRegex.Match(message);
#if RB_CN
            var spearFishSizeMatch = FishSizeRegex.Match(message);
            var spearFishMatch = SpearFishRegex.Matches(message);

            if (spearFishSizeMatch.Success)
            {
                spearResult.Name = spearFishMatch[1].ToString();
                float.TryParse(spearFishSizeMatch.Groups[1].Value, out float size);
                if (spearFishMatch[2].ToString() == "\uE03C")
                {
                    spearResult.IsHighQuality = true;
                }
#else
            Match spearFishMatch = SpearFishRegex.Match(message);
            if (spearFishMatch.Success)
            {
                spearResult.Name = spearFishMatch.Groups[1].Value;
                float.TryParse(spearFishMatch.Groups[2].Value, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out float size);
                if (spearResult.Name[spearResult.Name.Length - 2] == ' ')
                {
                    spearResult.IsHighQuality = true;
                }
#endif
                spearResult.Size = size;
            }

            if (spearFishAwayMatch.Success)
            {
                spearResult.Name = "none";
            }

            SpearResult = spearResult;
        }

        protected void MatchSwimmingShadow(string message)
        {
            swimmingShadows = SwimmingShadowsRegex.Match(message).Success;
        }

        private static bool HandleDeath()
        {
            if (!ExProfileBehavior.Me.IsDead || Poi.Current.Type == PoiType.Death)
            {
                return false;
            }

            Poi.Current = new Poi(ExProfileBehavior.Me, PoiType.Death);
            return true;
        }

        private bool HandleCondition()
        {
            if (WhileFunc == null)
            {
                WhileFunc = ScriptManager.GetCondition(While);
            }

            // If statement is true, return false so we can continue the routine
            if (WhileFunc() && !(GatherStrategy == GatherStrategy.FishAndGo && swimmingShadows))
            {
                return false;
            }

            swimmingShadows = false;
            isDone = true;
            return true;
        }

        private async Task<bool> CastTruth()
        {
            if (ExProfileBehavior.Me.CurrentJob != ClassJobType.Fisher)
            {
                return false;
            }

            if (ExProfileBehavior.Me.ClassLevel < 66
                || ExProfileBehavior.Me.HasAura((uint)AbilityAura.TruthOfOceans))
            {
                return false;
            }

            return
                await
                    CastAura(
                        Ability.Truth,
                        AbilityAura.TruthOfOceans);
        }

        private async Task<bool> SelectGig()
        {
            if (ExProfileBehavior.Me.CurrentJob != ClassJobType.Fisher)
            {
                return false;
            }

            if (ExProfileBehavior.Me.ClassLevel < 61
                || gigSelected)
            {
                return false;
            }

            gigSelected = await _gigWindow.SelectGiG(GigId);

            return gigSelected;
        }

        private bool HandleReset()
        {
            if (Node == null ||
                Node.IsValid && (!FreeRange || !(Node.Location.Distance3D(ExProfileBehavior.Me.Location) > Radius)))
            {
                return false;
            }

            OnResetCachedDone();
            return true;
        }

        private async Task<bool> MoveToHotSpot()
        {
            if (HotSpots == null || HotSpots.CurrentOrDefault.WithinHotSpot2D(ExProfileBehavior.Me.Location))
            {
                return false;
            }

            string name = !string.IsNullOrWhiteSpace(HotSpots.CurrentOrDefault.Name)
                ? "[" + HotSpots.CurrentOrDefault.Name + "] "
                : string.Empty;

            StatusText = string.Format(Localization.ExGather_MoveToHotSpot, name, HotSpots.CurrentOrDefault);

            await
                HotSpots.CurrentOrDefault.XYZ.MoveTo(
                    radius: HotSpots.CurrentOrDefault.Radius * 0.75f,
                    name: HotSpots.CurrentOrDefault.Name,
                    stopCallback: MovementStopCallback);

            startTime = DateTime.Now;
            return true;
        }

        private async Task<bool> FindNode(bool retryCenterHotspot = true)
        {
            if (Node != null)
            {
                return false;
            }

            StatusText = Localization.ExGather_SearchForNodes;

            while (Behaviors.ShouldContinue)
            {
                IEnumerable<GatheringPointObject> nodes =
                    GameObjectManager.GetObjectsOfType<GatheringPointObject>().Where(gpo => gpo.CanGather).ToArray();

                if (GatherStrategy == GatherStrategy.TouchAndGo && HotSpots != null)
                {
                    if (GatherObjects != null)
                    {
                        nodes = nodes.Where(gpo => GatherObjects.Contains(gpo.EnglishName, StringComparer.InvariantCultureIgnoreCase));
                    }

                    foreach (GatheringPointObject node in
                        nodes.Where(gpo => HotSpots.CurrentOrDefault.WithinHotSpot2D(gpo.Location))
                            .OrderBy(gpo => gpo.Location.Distance2D(ExProfileBehavior.Me.Location))
                            .Skip(1))
                    {
                        if (!Blacklist.Contains(node.ObjectId, BlacklistFlags.Interact))
                        {
                            Blacklist.Add(
                                node,
                                BlacklistFlags.Interact,
                                TimeSpan.FromSeconds(18),
                                Localization.ExGather_SkipFurthestNodes);
                        }
                    }
                }

                nodes = nodes.Where(gpo => !Blacklist.Contains(gpo.ObjectId, BlacklistFlags.Interact));

                if (FreeRange)
                {
                    nodes = nodes.Where(gpo => gpo.Distance2D(ExProfileBehavior.Me.Location) < Radius);
                }
                else
                {
                    if (HotSpots != null)
                    {
                        nodes = nodes.Where(gpo => HotSpots.CurrentOrDefault.WithinHotSpot2D(gpo.Location));
                    }
                }

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (GatherObjects != null)
                {
                    Node =
                        nodes.OrderBy(
                                gpo =>
                                    GatherObjects.FindIndex(i => string.Equals(gpo.EnglishName, i, StringComparison.InvariantCultureIgnoreCase)))
                            .ThenBy(gpo => gpo.Location.Distance2D(ExProfileBehavior.Me.Location))
                            .FirstOrDefault(gpo => GatherObjects.Contains(gpo.EnglishName, StringComparer.InvariantCultureIgnoreCase));
                }
                else
                {
                    Node = nodes.OrderBy(gpo => gpo.Location.Distance2D(ExProfileBehavior.Me.Location)).FirstOrDefault();
                }

                if (Node == null)
                {
                    if (HotSpots != null)
                    {
                        Vector3 myLocation = ExProfileBehavior.Me.Location;

                        float distanceToFurthestVisibleGameObject =
                            GameObjectManager.GameObjects.Select(o => o.Location.Distance2D(myLocation))
                                .OrderByDescending(o => o)
                                .FirstOrDefault();

                        float distanceToFurthestVectorInHotspot = myLocation.Distance2D(HotSpots.CurrentOrDefault)
                                                                + HotSpots.CurrentOrDefault.Radius;

                        if (myLocation.Distance2D(HotSpots.CurrentOrDefault) > Radius && GatherStrategy == GatherStrategy.GatherOrCollect
                            && retryCenterHotspot && distanceToFurthestVisibleGameObject <= distanceToFurthestVectorInHotspot)
                        {
                            Logger.Verbose(Localization.ExGather_DistanceObject + distanceToFurthestVisibleGameObject);
                            Logger.Verbose(Localization.ExGather_DistanceHotSpot + distanceToFurthestVectorInHotspot);

                            Logger.Warn(
                                Localization.ExGather_NoAvailableNode);
                            await HotSpots.CurrentOrDefault.XYZ.MoveTo(radius: Radius, name: HotSpots.CurrentOrDefault.Name);

                            retryCenterHotspot = false;
                            await Coroutine.Yield();
                            continue;
                        }

                        if (!await ChangeHotSpot())
                        {
                            retryCenterHotspot = false;
                            await Coroutine.Yield();
                            continue;
                        }
                    }

                    if (FreeRange && !FreeRangeConditional())
                    {
                        await Coroutine.Yield();
                        isDone = true;
                        return true;
                    }

                    return true;
                }

                Blacklist.BlacklistEntry entry = Blacklist.GetEntry(Node.ObjectId);
                if (entry != null && entry.Flags.HasFlag(BlacklistFlags.Interact))
                {
                    Logger.Warn(Localization.ExGather_NodeBlacklist);

                    if (await Coroutine.Wait(entry.Length, () =>
                            entry.IsFinished || Node.Location.Distance2D(ExProfileBehavior.Me.Location) > Radius) || Core.Player.IsDead
                    )
                    {
                        if (!entry.IsFinished)
                        {
                            Node = null;
                            Logger.Info(Localization.ExGather_NodeReset);
                            return false;
                        }
                    }

                    Logger.Info(Localization.ExGather_NodeBlacklistRemoved);
                }

                Logger.Info(Localization.ExGather_NodeSet + Node);

                if (HotSpots == null)
                {
                    MovementManager.SetFacing2D(Node.Location);
                }

                if (Poi.Current.Unit != Node)
                {
                    Poi.Current = new Poi(Node, PoiType.Gather);
                }

                return true;
            }

            return true;
        }

        private async Task<bool> ChangeHotSpot()
        {
            if (SpawnTimeout > 0 && DateTime.Now < startTime.AddSeconds(SpawnTimeout))
            {
                return false;
            }

            startTime = DateTime.Now;

            if (HotSpots != null)
            {
                if (!HotSpots.Next())
                {
                    Logger.Info(Localization.ExGather_FinishedLoop, ++loopCount, Loops);

                    // If finished all loops, otherwise just incrementing loop count
                    if (loopCount == Loops)
                    {
                        isDone = true;
                        return true;
                    }

                    // If not cyclic and it is on the last index
                    if (!HotSpots.IsCyclic && HotSpots.Index == HotSpots.Count - 1)
                    {
                        HotSpots.Index = 0;
                    }
                }
            }

            await Coroutine.Wait(2000, () => !Window<Gathering>.IsOpen && !GatheringMasterpiece.IsOpen);

            return true;
        }

        private bool FreeRangeConditional()
        {
            if (freeRangeConditionFunc == null)
            {
                freeRangeConditionFunc = ScriptManager.GetCondition(FreeRangeCondition);
            }

            return freeRangeConditionFunc();
        }

        private async Task<bool> ResetOrDone()
        {
            while (ExProfileBehavior.Me.InCombat && Behaviors.ShouldContinue)
            {
                // TODO: Run away...far far away
                await Coroutine.Yield();
            }

            ResetInternal();

            return true;
        }

        private async Task<bool> ExecutePoiLogic()
        {
            if (Poi.Current.Type != PoiType.Gather)
            {
                return false;
            }

            bool result = FindGatherSpot() || await GatherSequence();

            if (!result)
            {
                Poi.Clear(Localization.ExGather_ExecutePoiLogic);
            }

            if (Poi.Current.Type == PoiType.Gather && (!Poi.Current.Unit.IsValid || !Poi.Current.Unit.IsVisible))
            {
                Poi.Clear(Localization.ExGather_NodeIsGone);
            }

            return result;
        }

        private bool FindGatherSpot()
        {
            if (GatherSpot != null)
            {
                return false;
            }

            if (GatherSpots != null && Node.Location.Distance3D(ExProfileBehavior.Me.Location) > Distance)
            {
                GatherSpot =
                    GatherSpots.OrderBy(gs => gs.NodeLocation.Distance3D(Node.Location))
                        .FirstOrDefault(gs => gs.NodeLocation.Distance3D(Node.Location) <= Distance);
            }

            // Either GatherSpots is null, the node is already in range, or there are no matches, use fallback
            if (GatherSpot == null)
            {
                if (Node == null || Node.Location == Vector3.Zero)
                {
                    ResetInternal();
                    return false;
                }

                SetFallbackGatherSpot(Node.Location, true);
            }

            Logger.Info(Localization.ExGather_GatherSpotSet + GatherSpot);

            return true;
        }

        private void SetFallbackGatherSpot(Vector3 location, bool useMesh)
        {
            switch (DefaultGatherSpotType)
            {
                // TODO: Smart stealth implementation (where any enemy within x distance and i'm not behind them, use stealth approach and set stealth location as current)
                // If flying, land in area closest to node not in sight of an enemy and stealth.
                case GatherSpotType.StealthGatherSpot:
                    GatherSpot = new StealthGatherSpot { NodeLocation = location, UseMesh = useMesh };
                    break;
                // ReSharper disable once RedundantCaseLabel
                case GatherSpotType.GatherSpot:
                default:
                    GatherSpot = new GatherSpot { NodeLocation = location, UseMesh = useMesh };
                    break;
            }
        }

        private async Task<bool> GatherSequence() { return await MoveToGatherSpot() && await SpearFish() && await AfterSpearFish() && await MoveFromGatherSpot(); }

        private async Task<bool> MoveToGatherSpot()
        {
            float distance = Poi.Current.Location.Distance3D(ExProfileBehavior.Me.Location);
            if (FreeRange)
            {
                while (distance > Distance && distance <= Radius && Behaviors.ShouldContinue)
                {
                    await Coroutine.Yield();
                    distance = Poi.Current.Location.Distance3D(ExProfileBehavior.Me.Location);
                }
            }

            if (distance <= Distance)
            {
                return true;
            }

            await GatherSpot.MoveToSpot(this);

            return false;
        }

        private async Task<bool> SpearFish()
        {
            return await InteractWithNode() && await PrepareRotation() && await ExecuteRotation()
                   && await Coroutine.Wait(DefaultWait, () => !Node.CanGather) && await WaitForGatherWindowToClose();
        }

        private async Task<bool> InteractWithNode()
        {
            StatusText = "Interacting with node";

            int attempts = 0;
            while (attempts++ < 5 && !CanDoAbility(Ability.Gig) && Behaviors.ShouldContinue && Poi.Current.Unit.IsVisible
                   && Poi.Current.Unit.IsValid)
            {
                int ticks = 0;
                while (MovementManager.IsFlying && !MovementManager.IsDiving && ticks++ < 5 && Behaviors.ShouldContinue && Poi.Current.Unit.IsVisible
                       && Poi.Current.Unit.IsValid)
                {
                    Vector3 ground = ExProfileBehavior.Me.Location.GetFloor(5);
                    if (Math.Abs(ground.Y - ExProfileBehavior.Me.Location.Y) > float.Epsilon)
                    {
                        if (Navigator.PlayerMover is IFlightEnabledPlayerMover mover && !mover.IsLanding && !mover.IsTakingOff)
                        {
                            await CommonTasks.DescendTo(ground.Y);
                        }
                    }

                    await Coroutine.Sleep(200);
                }

                Poi.Current.Unit.Interact();

                if (await Coroutine.Wait(WindowDelay, () => CanDoAbility(Ability.Gig)))
                {
                    break;
                }

                if (attempts == 1 && WindowDelay <= 3000 && await Coroutine.Wait(WindowDelay, () => CanDoAbility(Ability.Gig)))
                {
                    break;
                }

                if (FreeRange)
                {
                    Logger.Warn(Localization.ExGather_GatherWindow, attempts);
                    continue;
                }

                Logger.Warn(Localization.ExGather_GatherWindow2, attempts);
                SetFallbackGatherSpot(Node.Location, true);

                await MoveToGatherSpot();
            }

            if (!CanDoAbility(Ability.Gig))
            {
                if (!FreeRange)
                {
                    await MoveFromGatherSpot();
                }

                OnResetCachedDone();
                return false;
            }

            interactedWithNode = true;

            Logger.Verbose(
                Localization.ExGather_GatherStart,
                Node.EnglishName,
                ExProfileBehavior.Me.CurrentGP,
                ExProfileBehavior.Me.MaxGP,
                WorldManager.EorzaTime.ToShortTimeString());

            BlacklistCurrentNode();

            return true;
        }

        private void BlacklistCurrentNode()
        {
            if (Poi.Current.Type != PoiType.Gather)
            {
                return;
            }

            if (Blacklist.Contains(Poi.Current.Unit, BlacklistFlags.Interact))
            {
                return;
            }

            TimeSpan timeToBlacklist = TimeSpan.FromSeconds(60);
            Blacklist.Add(
                Poi.Current.Unit,
                BlacklistFlags.Interact,
                timeToBlacklist,
                Localization.ExGather_BlackListNode + Poi.Current.Unit);
        }

        public async Task<bool> PrepareRotation()
        {
            if (Collect)
            {
                await CastAura(Ability.CollectorsGlove, AbilityAura.CollectorsGlove);
            }
            else
            {
                if (Core.Player.HasAura((int)AbilityAura.CollectorsGlove))
                {
                    return await Cast(Ability.CollectorsGlove);
                }
            }

            return true;
        }

        public async Task<bool> ExecuteRotation()
        {
            StatusText = "SpearFishing items";

            int hits = 0;

            // Precompute some character states per node
            bool hasBountifulCatch = Actions.HasAction(Ability.BountifulCatch);

            bool hasVeteranTrade = Actions.HasAction(Ability.VeteranTrade);
            bool usedVeteranTrade = false;

            bool hasIdenticalGig = Actions.HasAction(Ability.IdenticalGig);
            bool usedIdenticalGig = false;

            // Start spearfishing
            while (await Coroutine.Wait(DefaultWait, () => Actions.CanCast(Ability.Gig) || !Node.IsValid))
            {
                if (BountifulCatch
                    && hasBountifulCatch
                    && (!BountifulCatchAfterVeteranTrade || usedVeteranTrade)
                    && (1 <= hits && hits <= BountifulCatchDepth)
                    && await Coroutine.Wait(DefaultWait, () => Actions.CanCast(Ability.BountifulCatch, 200, CostType.GPCost))
                )
                {
                    Logger.Info(Localization.ExSpearFish_UsingBountifulCatch);
                    await Cast(Ability.BountifulCatch);
                }

                await Coroutine.Sleep(DefaultWait);
                await Cast(Ability.Gig);

                while (SelectYesno.IsOpen && Behaviors.ShouldContinue)
                {
                    await HandleCollectable();
                }

                await Coroutine.Sleep(1000);

                hits++;
                Logger.Info(Localization.ExSpearFish_SpearFishing, SpearResult.FishName, SpearResult.IsHighQuality, SpearResult.Size, WorldManager.EorzaTime);

                // Prefer Identical Gig over Veteran Trade if player has it; able to collect more target fish this way
                if (!usedIdenticalGig
                    && hasIdenticalGig
                    && hits <= IdenticalGigDepth
                    && Items.Any(SpearResult.ShouldKeep)
                    && await Coroutine.Wait(DefaultWait, () => Actions.CanCast(Ability.IdenticalGig, 350, CostType.GPCost))
                )
                {
                    Logger.Info(Localization.ExSpearFish_UsingIdenticalGig, SpearResult.FishName);
                    await Cast(Ability.IdenticalGig);
                    usedIdenticalGig = true;
                }
                // If player doesn't have Identical Gig, fall back on Veteran Trade
                else if (!usedVeteranTrade
                    && (hasVeteranTrade && !hasIdenticalGig)
                    && hits <= VeteranTradeDepth
                    && !Items.Any(SpearResult.ShouldKeep)
                    && await Coroutine.Wait(DefaultWait, () => Actions.CanCast(Ability.VeteranTrade, 200, CostType.GPCost))
                )
                {
                    Logger.Info(Localization.ExSpearFish_UsingVeteranTrade, SpearResult.FishName);
                    await Cast(Ability.VeteranTrade);
                    usedVeteranTrade = true;
                }
            }

            StatusText = "SpearFishing items complete";

            return true;
        }

        private async Task<bool> HandleCollectable()
        {
            uint required = CollectabilityValue;
            string itemName = string.Empty;
            if (!string.IsNullOrWhiteSpace(Collectables.First().Name))
            {
                Item item = SelectYesno.Item;
                if (item == null
                    || !Collectables.Any(c => string.Equals(c.Name, item.EnglishName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    int ticks = 0;
                    while ((item == null
                            ||
                            !Collectables.Any(c => string.Equals(c.Name, item.EnglishName, StringComparison.InvariantCultureIgnoreCase)))
                           && ticks++ < 60 && Behaviors.ShouldContinue)
                    {
                        item = SelectYesno.Item;
                        await Coroutine.Yield();
                    }

                    if (ticks > 60)
                    {
                        required = (uint)Collectables.Select(c => c.Value).Max();
                    }
                }

                if (item != null)
                {
                    itemName = item.EnglishName;
                    Collectable collectable = Collectables.FirstOrDefault(c => string.Equals(c.Name, item.EnglishName));

                    if (collectable != null)
                    {
                        required = (uint)collectable.Value;
                    }
                }
            }

            uint value = SelectYesno.CollectabilityValue;

            if (value >= required)
            {
                Logger.Info(Localization.ExFish_Collecting, itemName, value, required);
                SelectYesno.Yes();
            }
            else
            {
                Logger.Info(Localization.ExFish_Declining, itemName, value, required);
                SelectYesno.No();
            }

            await Coroutine.Wait(3000, () => !SelectYesno.IsOpen);

            return true;
        }

        private async Task<bool> AfterSpearFish() { return await HandleCordial(); }

        private async Task<bool> HandleCordial()
        {
            if (CordialType == CordialType.None)
            {
                return false;
            }

            CordialSpellData = CordialSpellData ?? Cordial.GetSpellData();

            if (CordialSpellData == null)
            {
                CordialType = CordialType.None;
                return false;
            }

            if (!CanUseCordial(8))
            {
                return false;
            }

            int missingGp = ExProfileBehavior.Me.MaxGP - ExProfileBehavior.Me.CurrentGP;

            if (missingGp < 300)
            {
                return false;
            }

            if (missingGp >= 450 && (CordialType == CordialType.HiCordial || CordialType == CordialType.Auto))
            {
                if (await UseCordial(CordialType.HiCordial))
                {
                    return true;
                }
            }

            if (missingGp < 400 || CordialType != CordialType.Cordial && CordialType != CordialType.Auto)
            {
                return await UseCordial(CordialType.WateredCordial);
            }

            if (await UseCordial(CordialType.Cordial))
            {
                return true;
            }

            return await UseCordial(CordialType.WateredCordial);
        }

        internal bool CanUseCordial(ushort withinSeconds = 5)
        {
            return CordialSpellData.Cooldown.TotalSeconds < withinSeconds
                   && (CordialType == CordialType.WateredCordial && Cordial.HasWateredCordials()
                       || CordialType == CordialType.Cordial && (Cordial.HasWateredCordials() || Cordial.HasCordials())
                       || (CordialType == CordialType.HiCordial || CordialType == CordialType.Auto) && Cordial.HasAnyCordials());
        }

        private async Task<bool> UseCordial(CordialType cordialType, int maxTimeoutSeconds = 5)
        {
            if (!(CordialSpellData.Cooldown.TotalSeconds < maxTimeoutSeconds))
            {
                return false;
            }

            BagSlot cordial = InventoryManager.FilledSlots.FirstOrDefault(slot => slot.RawItemId == (uint)cordialType);

            if (cordial != null)
            {
                StatusText = Localization.ExFish_UseCordialWhenAvailable;

                Logger.Info(
                    Localization.ExFish_UseCordial,
                    (int)CordialSpellData.Cooldown.TotalSeconds,
                    ExProfileBehavior.Me.CurrentGP);

                if (!await Coroutine.Wait(
                    TimeSpan.FromSeconds(maxTimeoutSeconds),
                    () =>
                    {
                        if (!ExProfileBehavior.Me.IsMounted || !(CordialSpellData.Cooldown.TotalSeconds < 2))
                        {
                            return cordial.CanUse(ExProfileBehavior.Me);
                        }

                        ActionManager.Dismount();
                        return false;
                    }))
                {
                    return false;
                }

                await Coroutine.Sleep(500);
                Logger.Info("Using " + cordialType);
                cordial.UseItem(ExProfileBehavior.Me);
                await Coroutine.Sleep(1500);
                return true;
            }
            Logger.Warn(Localization.ExFish_NoCordial + cordialType);

            return false;
        }

        private async Task<bool> WaitForGatherWindowToClose()
        {
            int ticks = 0;
            while (CanDoAbility(Ability.Gig) && ticks++ < 100 && Behaviors.ShouldContinue)
            {
                await Coroutine.Yield();
            }

            return true;
        }

        private async Task<bool> MoveFromGatherSpot() { return GatherSpot == null || await GatherSpot.MoveFromSpot(this); }

        #region Ability Checks and Actions

        internal bool CanDoAbility(Ability ability) { return ActionManager.CanCast(Abilities.Map[ClassJobType.Fisher][ability], ExProfileBehavior.Me); }

        internal bool DoAbility(Ability ability) { return ActionManager.DoAction(Abilities.Map[ClassJobType.Fisher][ability], ExProfileBehavior.Me); }

        #endregion Ability Checks and Actions
    }
}

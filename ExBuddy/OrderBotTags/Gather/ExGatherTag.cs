namespace ExBuddy.OrderBotTags.Gather
{
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ExBuddy.Attributes;
    using ExBuddy.Enumerations;
    using ExBuddy.Helpers;
    using ExBuddy.Interfaces;
    using ExBuddy.OrderBotTags.Behaviors;
    using ExBuddy.OrderBotTags.Gather.GatherSpots;
    using ExBuddy.OrderBotTags.Gather.Rotations;
    using ExBuddy.OrderBotTags.Objects;
    using ExBuddy.Windows;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;
    using ff14bot.NeoProfiles;
    using ff14bot.Objects;
    using ff14bot.RemoteWindows;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using Inventory;
    using Inventory.StockManagers;
    using Logging;
    using Strategies;
    using TreeSharp;

#if RB_CN
    using ActionManager = ff14bot.Managers.Actionmanager;
#endif

    [LoggerName("ExGather")]
	[XmlElement("ExGather")]
	[XmlElement("GatherCollectable")]
	public class ExGatherTag : ExProfileBehavior
	{
		private static readonly object Lock = new object();

		internal static volatile Dictionary<string, IGatheringRotation> Rotations;

	    internal CordialStockManager CordialStock = new CordialStockManager(defaultMountBehavior: true);

		internal Collectable CollectableItem;

		private Func<bool> freeRangeConditionFunc;

		internal GatheringItem GatherItem;

		internal bool GatherItemIsFallback;

		private IGatheringRotation gatherRotation;

		internal IGatherSpot GatherSpot;

		private IGatheringRotation initialGatherRotation;

	    private IGpRegenStrategy beforeGatherGpRegenStrategy;

	    private IGpRegenStrategy afterGatherGpRegenStrategy;

        private bool interactedWithNode;

		private int loopCount;

		internal GatheringPointObject Node;

        internal int GpPerTick;

		internal int NodesGatheredAtMaxGp;

		private Composite poiCoroutine;

		private DateTime startTime;

		internal Func<bool> WhileFunc;

		public ExGatherTag()
		{
		    this.beforeGatherGpRegenStrategy = new BeforeGatherGpRegenStrategy(
                this.CordialStock,
                new BeforeGatherGpRegenStrategyLogger(
                    this.Logger,
                    new GathererLogger(this.Logger),
                    new GpRegeneratorLogger(this.Logger),
                    new CordialConsumerLogger(this.Logger),
                    new ProfileBehaviorLogger(this)
                )
            );
            this.afterGatherGpRegenStrategy = new AfterGatherGpRegenStrategy(
                this.CordialStock,
                new CordialConsumerLogger(this.Logger)
            );

            if (Rotations == null)
			{
				lock (Lock)
				{
					if (Rotations == null)
					{
						Rotations = LoadRotationTypes();
					}
				}
			}
		}

		[DefaultValue(true)]
		[XmlAttribute("AlwaysGather")]
		public bool AlwaysGather { get; set; }

		[XmlElement("Collectables")]
		[Obsolete("Use Items instead.")]
		public List<Collectable> Collectables { get; set; }

		[DefaultValue(CordialTime.IfNeeded)]
		[XmlAttribute("CordialTime")]
		public CordialTime CordialTime { get; set; }

		[DefaultValue(CordialType.None)]
		[XmlAttribute("CordialType")]
		public CordialType CordialType { get; set; }

        // TODO: Look into making this use Type instead of Enum
        [DefaultValue(GatherSpotType.GatherSpot)]
		[XmlAttribute("DefaultGatherSpotType")]
		public GatherSpotType DefaultGatherSpotType { get; set; }

		[XmlAttribute("DisableRotationOverride")]
		public bool DisableRotationOverride { get; set; }

		[XmlAttribute("DiscoverUnknowns")]
		public bool DiscoverUnknowns { get; set; }

		[DefaultValue(2.5f)]
		[XmlAttribute("Distance")]
		public float Distance { get; set; }

		[XmlAttribute("FreeRange")]
		public bool FreeRange { get; set; }

		[DefaultValue("Condition.TrueFor(1, TimeSpan.FromHours(1))")]
		[XmlAttribute("FreeRangeCondition")]
		public string FreeRangeCondition { get; set; }

		[DefaultValue(GatherIncrease.Auto)]
		[XmlAttribute("GatherIncrease")]
		public GatherIncrease GatherIncrease { get; set; }

		[XmlElement("GatheringSkillOrder")]
		public GatheringSkillOrder GatheringSkillOrder { get; set; }

		// Backwards compatibility
		[XmlElement("GatherObject")]
		public string GatherObject { get; set; }

		[XmlElement("GatherObjects")]
		public List<string> GatherObjects { get; set; }

		// Maybe this should be an attribute?
		[DefaultValue("RegularNode")]
		[XmlElement("GatherRotation")]
		public string GatherRotation { get; set; }

		[XmlElement("GatherSpots")]
		public GatherSpotCollection GatherSpots { get; set; }

		[DefaultValue(GatherStrategy.GatherOrCollect)]
		[XmlAttribute("GatherStrategy")]
		public GatherStrategy GatherStrategy { get; set; }

		[XmlElement("HotSpots")]
		public IndexedList<HotSpot> HotSpots { get; set; }

		[XmlElement("ItemNames")]
		[Obsolete("Use Items instead.")]
		public List<string> ItemNames { get; set; }

		[XmlElement("Items")]
		public NamedItemCollection Items { get; set; }

		[DefaultValue(-1)]
		[XmlAttribute("Loops")]
		public int Loops { get; set; }

		[DefaultValue(3.0f)]
		[XmlAttribute("Radius")]
		public float Radius { get; set; }

		[DefaultValue(400)]
		[XmlAttribute("SkipWindowDelay")]
		public int SkipWindowDelay { get; set; }

		// I want this to be an attribute, but for backwards compatibilty, we will use element
		[DefaultValue(-1)]
		[XmlElement("Slot")]
		public int Slot { get; set; }

		[XmlAttribute("SpawnTimeout")]
		public int SpawnTimeout { get; set; }

		[XmlAttribute("SpellDelay")]
		public int SpellDelay { get; set; }

		[XmlAttribute("SwingsRemaining")]
		public byte SwingsRemaining { get; set; }

		[DefaultValue("True")]
		[XmlAttribute("While")]
		public string While { get; set; }

		[DefaultValue(1500)]
		[XmlAttribute("WindowDelay")]
		public int WindowDelay { get; set; }

		protected override Color Info
		{
			get { return Colors.Chartreuse; }
		}

		protected override void DoReset()
		{
			loopCount = 0;
			NodesGatheredAtMaxGp = 0;

			if (HotSpots != null)
			{
				HotSpots.Index = 0;
			}

			ResetInternal();
		}

		protected override async Task<bool> Main()
		{
			await CommonTasks.HandleLoading();

			return HandleDeath() || HandleCondition() || await CastTruth() || HandleReset() || await MoveToHotSpot()
				   || await FindNode() || await ResetOrDone();
		}

		protected override void OnDone()
		{
			TreeHooks.Instance.RemoveHook("PoiAction", poiCoroutine);
		}

		protected override void OnStart()
		{
			SpellDelay = SpellDelay < 0 ? 0 : SpellDelay;
			WindowDelay = WindowDelay < 500 ? 500 : WindowDelay;
			SkipWindowDelay = SkipWindowDelay < 200 ? 200 : SkipWindowDelay;
            GpPerTick = CharacterResource.GetGpPerTick();

			if (Distance > 3.5f)
			{
				TreeRoot.Stop(Localization.Localization.ExGather_Distance);
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

#pragma warning disable 618
				if (ItemNames != null)
				{
					foreach (var item in ItemNames)
					{
						Items.Add(new GatherItem { Name = item });
					}
				}

				if (Collectables != null)
				{
					foreach (var collectable in Collectables)
					{
						Items.Add(collectable);
					}
				}
#pragma warning restore 618
			}

			if (string.IsNullOrWhiteSpace(Name))
			{
				if (Items.Count > 0)
				{
					Name = Items.First().Name;
				}
				else
				{
					Name = string.Format(Localization.Localization.ExGather_Zone, WorldManager.ZoneId, ExProfileBehavior.Me.Location);
				}
			}

			StatusText = Name;

			poiCoroutine = new ActionRunCoroutine(ctx => ExecutePoiLogic());
			TreeHooks.Instance.AddHook("PoiAction", poiCoroutine);

			ResolveGatherRotation();
		}

		internal async Task<bool> Cast(uint id)
		{
			return await Actions.Cast(id, SpellDelay);
		}

		internal async Task<bool> Cast(Ability id)
		{
			return await Actions.Cast(id, SpellDelay);
		}

		internal async Task<bool> CastAura(uint spellId, int auraId = -1)
		{
			return await Actions.CastAura(spellId, SpellDelay, auraId);
		}

		internal async Task<bool> CastAura(Ability ability, AbilityAura auraId = AbilityAura.None)
		{
			return await Actions.CastAura(ability, SpellDelay, auraId);
		}

		internal async Task<bool> CloseGatheringWindow()
		{
			return
				await
					Gathering.CloseGently(
						(byte)(SkipWindowDelay < 33 ? 100 : Math.Max(1, 3000 / SkipWindowDelay)),
						(ushort)SkipWindowDelay);
		}

		/// <summary>
		/// called by moveto.
		/// </summary>
		/// <param name="distance">Distance to our target</param>
		/// <param name="radius">Radius we passed into the moveto</param>
		/// <returns></returns>
		internal bool MovementStopCallback(float distance, float radius)
		{
			return distance <= radius || !WhileFunc() || ExProfileBehavior.Me.IsDead;
		}

		internal void ResetInternal()
		{
			interactedWithNode = false;
			GatherSpot = null;
			Node = null;
			GatherItem = null;
			CollectableItem = null;
		}

		internal async Task<bool> ResolveGatherItem()
		{
			if (!GatheringManager.WindowOpen)
			{
				return false;
			}

			var previousGatherItem = GatherItem;
			GatherItemIsFallback = false;
			GatherItem = null;
			CollectableItem = null;

			var windowItems = GatheringManager.GatheringWindowItems.ToArray();

			// TODO: move method to common so we use it on fish too
			if (InventoryItemCount() >=
#if RB_CN
				100
#else
				140
#endif
				)
			{
				if (Items.Count > 0)
				{
					if (
						SetGatherItem(
							windowItems.OrderByDescending(i => i.SlotIndex).Where(i => i.IsFilled && !i.IsUnknown && i.ItemId < 20).ToArray()))
					{
						return true;
					}
				}

				GatherItem =
					windowItems.Where(i => i.IsFilled && !i.IsUnknown)
						.OrderByDescending(i => i.ItemId)
						.FirstOrDefault(i => i.ItemId < 20);

				if (GatherItem != null)
				{
					return true;
				}

				Logger.Warn(Localization.Localization.ExGather_FullInventory);
				return false;
			}

			if (DiscoverUnknowns)
			{
				var items = new[] { 0U, 1U, 2U, 3U, 4U, 5U, 6U, 7U }.Select(GatheringManager.GetGatheringItemByIndex);

				GatherItem = items.FirstOrDefault(i => i.IsUnknownChance() && i.Amount > 0);

				if (GatherItem != null)
				{
					return true;
				}
			}

			if (Items.Count > 0)
			{
				if (SetGatherItem(windowItems))
				{
					return true;
				}
			}

			if (Slot > -1 && Slot < 8)
			{
				GatherItem = GatheringManager.GetGatheringItemByIndex((uint)Slot);
			}

			if (GatherItem == null && (!AlwaysGather || GatherStrategy == GatherStrategy.TouchAndGo))
			{
				Poi.Clear(Localization.Localization.ExGather_SkipNode);

				await CloseGatheringWindow();

				return false;
			}

			if (GatherItem != null)
			{
				return true;
			}

			GatherItemIsFallback = true;

			GatherItem =
				windowItems.Where(i => i.IsFilled && !i.IsUnknown)
					.OrderByDescending(i => i.ItemId)
					.FirstOrDefault(i => i.ItemId < 20) // Try to gather cluster/crystal/shard
				?? windowItems.FirstOrDefault(
					i => i.IsFilled && !i.IsUnknown && !i.ItemData.Unique && !i.ItemData.Untradeable && i.ItemData.ItemCount() > 0)
				// Try to collect items you have that stack
				?? windowItems.Where(i => i.Amount > 0 && !i.ItemData.Unique && !i.ItemData.Untradeable)
					.OrderByDescending(i => i.SlotIndex)
					.FirstOrDefault(); // Take last item that is not unique or untradeable

			// Seems we only have unknowns.
			if (GatherItem == null)
			{
				var items = new[] { 0U, 1U, 2U, 3U, 4U, 5U, 6U, 7U }.Select(GatheringManager.GetGatheringItemByIndex).ToArray();

				GatherItem = items.FirstOrDefault(i => i.IsUnknownChance() && i.Amount > 0);

				if (GatherItem != null)
				{
					return true;
				}

				Logger.Warn(Localization.Localization.ExGather_NothingToGather);

				return false;
			}

			if (previousGatherItem == null || previousGatherItem.ItemId != GatherItem.ItemId)
			{
				Logger.Info(Localization.Localization.ExGather_NothingToGather2 + GatherItem.ItemData + Localization.Localization.ExGather_NothingToGather3);
			}

			return true;
		}

		private async Task<bool> AfterGather()
		{
			Logger.Verbose(
				Localization.Localization.ExGather_Finished,
				Node.EnglishName,
				ExProfileBehavior.Me.CurrentGP,
				WorldManager.EorzaTime.ToShortTimeString());

			// in case we failed our rotation or window stuck open because items are somehow left
			if (GatheringManager.SwingsRemaining > SwingsRemaining)
			{
				// TODO: Look into possibly smarter behavior.
				await CloseGatheringWindow();
			}

			if (ExProfileBehavior.Me.CurrentGP >= ExProfileBehavior.Me.MaxGP - 30)
			{
				NodesGatheredAtMaxGp++;
			}
			else
			{
				NodesGatheredAtMaxGp = 0;
			}

			if (!ReferenceEquals(gatherRotation, initialGatherRotation))
			{
				gatherRotation = initialGatherRotation;
				Logger.Info(Localization.Localization.ExGather_RotationReset + initialGatherRotation.Attributes.Name);
			}

		    var regenResult = await this.afterGatherGpRegenStrategy.RegenerateGp(
		        this.Node,
		        this.gatherRotation,
		        this.GatherStrategy,
		        this.CordialTime,
		        this.CordialType
		    );

		    return regenResult.StrategyState == GpRegenStrategyResult.GpRegenStrategyResultState.OK
		        && regenResult.UseState == InventoryItem.UseResult.OK;
		}

		private async Task<bool> BeforeGather()
		{
            CheckForEstimatedGatherRotation();

            // Execute before gather strategy to regen GP for gathering
		    var strategyResult = await this.beforeGatherGpRegenStrategy.RegenerateGp(this.Node, this.gatherRotation, this.GatherStrategy, this.CordialTime, this.CordialType);

		    // Blacklist the current node if the regen strategy returned a negative result
		    if (strategyResult.StrategyState != GpRegenStrategyResult.GpRegenStrategyResultState.OK)
		    {
		        BlacklistCurrentNode();
		    }

            return strategyResult.StrategyState == GpRegenStrategyResult.GpRegenStrategyResultState.OK;
        }

		private void BlacklistCurrentNode()
		{
			if (Poi.Current.Type != PoiType.Gather)
			{
				return;
			}

			if (!Blacklist.Contains(Poi.Current.Unit, BlacklistFlags.Interact))
			{
				var timeToBlacklist = GatherStrategy == GatherStrategy.TouchAndGo
					? TimeSpan.FromSeconds(12)
					: TimeSpan.FromSeconds(Math.Max(gatherRotation.Attributes.RequiredTimeInSeconds + 6, 30));
				Blacklist.Add(
					Poi.Current.Unit,
					BlacklistFlags.Interact,
					timeToBlacklist,
					Localization.Localization.ExGather_BlackListNode + Poi.Current.Unit);
			}
		}

		private async Task<bool> CastTruth()
		{
			if (ExProfileBehavior.Me.CurrentJob != ClassJobType.Miner && ExProfileBehavior.Me.CurrentJob != ClassJobType.Botanist)
			{
				return false;
			}

			if (ExProfileBehavior.Me.ClassLevel < 46
				|| ExProfileBehavior.Me.HasAura(
#if RB_CN
                    (int)
#else
					(uint)
#endif
						(ExProfileBehavior.Me.CurrentJob == ClassJobType.Miner
							? AbilityAura.TruthOfMountains
							: AbilityAura.TruthOfForests)))
			{
				return false;
			}

			return
				await
					CastAura(
						Ability.Truth,
						ExProfileBehavior.Me.CurrentJob == ClassJobType.Miner ? AbilityAura.TruthOfMountains : AbilityAura.TruthOfForests);
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
				// If finished current loop and set to not cyclic (we know this because if it was cyclic Next is always true)
				if (!HotSpots.Next())
				{
					Logger.Info(Localization.Localization.ExGather_FinishedLoop, ++loopCount, Loops);

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

		private void CheckForEstimatedGatherRotation()
		{
			if (!gatherRotation.CanBeOverriden || DisableRotationOverride)
			{
				return;
			}

			CollectableItem = Items.OfType<Collectable>().FirstOrDefault();

			if (CollectableItem != null)
			{
				Logger.Info(Localization.Localization.ExGather_Rotationbaseoff, CollectableItem);
			}
			else
			{
				Logger.Info(Localization.Localization.ExGather_RotationbaseoffGatherIncrease, GatherIncrease);
			}

			var rotation = GetOverrideRotation();

			if (rotation == null)
			{
				Logger.Info(Localization.Localization.ExGather_RotationNotChange);
				return;
			}

			Logger.Info(Localization.Localization.ExGather_RotationEstimate, gatherRotation.Attributes.Name, rotation.Attributes.Name);

			gatherRotation = rotation;
		}

		private void CheckForGatherRotationOverride()
		{
			if (!gatherRotation.CanBeOverriden || DisableRotationOverride)
			{
				if (!GatherItem.IsUnknown)
				{
					return;
				}

				Logger.Info(Localization.Localization.ExGather_RotationOverriding);
			}

			var rotation = GetOverrideRotation();

			if (rotation == null)
			{
				return;
			}

			Logger.Info(Localization.Localization.ExGather_RotationOverride, gatherRotation.Attributes.Name, rotation.Attributes.Name);

			gatherRotation = rotation;
		}

		private async Task<bool> ExecutePoiLogic()
		{
			if (Poi.Current.Type != PoiType.Gather)
			{
				return false;
			}

			var result = FindGatherSpot() || await GatherSequence();

			if (!result)
			{
				Poi.Clear(Localization.Localization.ExGather_ExecutePoiLogic);
			}

			if (Poi.Current.Type == PoiType.Gather && (!Poi.Current.Unit.IsValid || !Poi.Current.Unit.IsVisible))
			{
				Poi.Clear(Localization.Localization.ExGather_NodeIsGone);
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

			Logger.Info(Localization.Localization.ExGather_GatherSpotSet + GatherSpot);

			return true;
		}

		private async Task<bool> FindNode(bool retryCenterHotspot = true)
		{
			if (Node != null)
			{
				return false;
			}

			StatusText = Localization.Localization.ExGather_SearchForNodes;

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

					foreach (var node in
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
								Localization.Localization.ExGather_SkipFurthestNodes);
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
						var myLocation = ExProfileBehavior.Me.Location;

						var distanceToFurthestVisibleGameObject =
							GameObjectManager.GameObjects.Select(o => o.Location.Distance2D(myLocation))
								.OrderByDescending(o => o)
								.FirstOrDefault();

						var distanceToFurthestVectorInHotspot = myLocation.Distance2D(HotSpots.CurrentOrDefault)
																+ HotSpots.CurrentOrDefault.Radius;

						if (myLocation.Distance2D(HotSpots.CurrentOrDefault) > Radius && GatherStrategy == GatherStrategy.GatherOrCollect
							&& retryCenterHotspot && distanceToFurthestVisibleGameObject <= distanceToFurthestVectorInHotspot)
						{
							Logger.Verbose(Localization.Localization.ExGather_DistanceObject + distanceToFurthestVisibleGameObject);
							Logger.Verbose(Localization.Localization.ExGather_DistanceHotSpot + distanceToFurthestVectorInHotspot);

							Logger.Warn(
								Localization.Localization.ExGather_NoAvailableNode);
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

				var entry = Blacklist.GetEntry(Node.ObjectId);
				if (entry != null && entry.Flags.HasFlag(BlacklistFlags.Interact))
				{
					Logger.Warn(Localization.Localization.ExGather_NodeBlacklist);

					if (await
						Coroutine.Wait(entry.Length,
							() => entry.IsFinished || Node.Location.Distance2D(ExProfileBehavior.Me.Location) > Radius)
						|| Core.Player.IsDead)
					{
						if (!entry.IsFinished)
						{
							Node = null;
							Logger.Info(Localization.Localization.ExGather_NodeReset);
							return false;
						}
					}

					Logger.Info(Localization.Localization.ExGather_NodeBlacklistRemoved);
				}

				Logger.Info(Localization.Localization.ExGather_NodeSet + Node);

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

		private bool FreeRangeConditional()
		{
			if (freeRangeConditionFunc == null)
			{
				freeRangeConditionFunc = ScriptManager.GetCondition(FreeRangeCondition);
			}

			return freeRangeConditionFunc();
		}

		private async Task<bool> Gather()
		{
			return await InteractWithNode() && await gatherRotation.Prepare(this) && await gatherRotation.ExecuteRotation(this)
				   && await gatherRotation.Gather(this) && await HandleSwingsRemaining()
				   && await Coroutine.Wait(4000, () => !Node.CanGather) && await WaitForGatherWindowToClose();
		}

		private async Task<bool> GatherSequence()
		{
			return await MoveToGatherSpot() && await BeforeGather() && await Gather() && await AfterGather()
				   && await MoveFromGatherSpot();
		}

		private static Type[] GetKnownRotationTypes()
		{
			return new[]
			{
				typeof (RegularNodeGatheringRotation), typeof (UnspoiledGatheringRotation),
				typeof (DefaultCollectGatheringRotation), typeof (Collect115GatheringRotation), typeof (Collect345GatheringRotation),
				typeof (Collect450GatheringRotation), typeof (Collect470GatheringRotation), typeof (Collect550GatheringRotation),
				typeof (Collect570GatheringRotation), typeof (DiscoverUnknownsGatheringRotation),
				typeof (ElementalGatheringRotation),
				typeof (TopsoilGatheringRotation), typeof (MapGatheringRotation), typeof (SmartQualityGatheringRotation),
				typeof (SmartYieldGatheringRotation), typeof (YieldAndQualityGatheringRotation),
				typeof (NewbCollectGatheringRotation)
			};
		}

		private IGatheringRotation GetOverrideRotation()
		{
			var rotationAndTypes =
				Rotations.Select(r => new { Rotation = r.Value, OverrideValue = r.Value.ResolveOverridePriority(this) })
					.Where(r => r.OverrideValue > -1)
					.OrderByDescending(r => r.OverrideValue)
					.ToArray();

			var rotation = rotationAndTypes.FirstOrDefault();

			if (rotation == null || ReferenceEquals(rotation.Rotation, gatherRotation))
			{
				return null;
			}

			return rotation.Rotation;
		}

		private bool HandleCondition()
		{
			if (WhileFunc == null)
			{
				WhileFunc = ScriptManager.GetCondition(While);
			}

			// If statement is true, return false so we can continue the routine
			if (WhileFunc())
			{
				return false;
			}

			isDone = true;
			return true;
		}

		private bool HandleDeath()
		{
			if (ExProfileBehavior.Me.IsDead && Poi.Current.Type != PoiType.Death)
			{
				Poi.Current = new Poi(ExProfileBehavior.Me, PoiType.Death);
				return true;
			}

			return false;
		}

		private bool HandleReset()
		{
			if (Node == null ||
				(Node.IsValid && (!FreeRange || !(Node.Location.Distance3D(ExProfileBehavior.Me.Location) > Radius))))
			{
				return false;
			}

			OnResetCachedDone();
			return true;
		}

		private async Task<bool> HandleSwingsRemaining()
		{
			if (SwingsRemaining > 0 && Window<Gathering>.IsOpen)
			{
				await CloseGatheringWindow();
			}

			return true;
		}

		private async Task<bool> InteractWithNode()
		{
			StatusText = "Interacting with node";

			var attempts = 0;
			while (attempts++ < 5 && !GatheringManager.WindowOpen && Behaviors.ShouldContinue && Poi.Current.Unit.IsVisible
				   && Poi.Current.Unit.IsValid)
			{
				var ticks = 0;
				while (MovementManager.IsFlying
#if !RB_CN
				&& !MovementManager.IsDiving
#endif
				&& ticks++ < 5 && Behaviors.ShouldContinue && Poi.Current.Unit.IsVisible
					   && Poi.Current.Unit.IsValid)
				{
					var ground = ExProfileBehavior.Me.Location.GetFloor(5);
					if (Math.Abs(ground.Y - ExProfileBehavior.Me.Location.Y) > float.Epsilon)
					{
						var mover = Navigator.PlayerMover as IFlightEnabledPlayerMover;
						if (mover != null && !mover.IsLanding && !mover.IsTakingOff)
						{
							await CommonTasks.DescendTo(ground.Y);
						}
					}

					await Coroutine.Sleep(200);
				}

				Poi.Current.Unit.Interact();

				if (await Coroutine.Wait(WindowDelay, () => GatheringManager.WindowOpen))
				{
					break;
				}

				if (attempts == 1 && WindowDelay <= 3000 && await Coroutine.Wait(WindowDelay, () => GatheringManager.WindowOpen))
				{
					// wait double on first attempt if delay less than 2 seconds.
					break;
				}

				if (FreeRange)
				{
					Logger.Warn(Localization.Localization.ExGather_GatherWindow, attempts);
					continue;
				}

				Logger.Warn(Localization.Localization.ExGather_GatherWindow2, attempts);
				//SetFallbackGatherSpot(Node.Location, true);

				await MoveToGatherSpot();
			}

			if (!GatheringManager.WindowOpen)
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
				Localization.Localization.ExGather_GatherStart,
				Node.EnglishName,
				ExProfileBehavior.Me.CurrentGP,
				ExProfileBehavior.Me.MaxGP,
				WorldManager.EorzaTime.ToShortTimeString());

			if (!this.Node.IsUnspoiled() && !this.Node.IsConcealed())
			{
				BlacklistCurrentNode();
			}

			if (!await ResolveGatherItem())
			{
				await CloseGatheringWindow();
				ResetInternal();

				await Coroutine.Wait(2000, () => ExProfileBehavior.Me.InCombat || ActionManager.CanMount == 0);
				return false;
			}

			CheckForGatherRotationOverride();

			return true;
		}

		private int InventoryItemCount()
		{
			return InventoryManager.FilledSlots.Count(c => c.BagId != InventoryBagId.KeyItems);
		}

		private Dictionary<string, IGatheringRotation> LoadRotationTypes()
		{
			Type[] types = null;
			try
			{
				types =
					Assembly.GetExecutingAssembly()
						.GetTypes()
						.Where(
							t =>
								!t.IsAbstract && typeof(IGatheringRotation).IsAssignableFrom(t)
								&& t.GetCustomAttribute<GatheringRotationAttribute>() != null)
						.ToArray();
			}
			catch
			{
				Logger.Warn(Localization.Localization.ExGather_NoTypeSpecified);
			}

			if (types == null)
			{
				types = GetKnownRotationTypes();
			}

			ReflectionHelper.CustomAttributes<GatheringRotationAttribute>.RegisterTypes(types);

			var instances = types.Select(t => t.CreateInstance<IGatheringRotation>()).ToArray();

			foreach (var instance in instances)
			{
				Logger.Info(
					Localization.Localization.ExGather_RotationLoad,
					instance.Attributes.Name,
					instance.Attributes.RequiredGpBreakpoints[0],
					instance.Attributes.RequiredTimeInSeconds);
			}

			var dict = instances.ToDictionary(k => k.Attributes.Name, v => v, StringComparer.InvariantCultureIgnoreCase);

			return dict;
		}

		private async Task<bool> MoveFromGatherSpot()
		{
			return GatherSpot == null || await GatherSpot.MoveFromSpot(this);
		}

		private async Task<bool> MoveToGatherSpot()
		{
			var distance = Poi.Current.Location.Distance3D(ExProfileBehavior.Me.Location);
			if (FreeRange)
			{
				while (distance > Distance && distance <= Radius && Behaviors.ShouldContinue)
				{
					await Coroutine.Yield();
					distance = Poi.Current.Location.Distance3D(ExProfileBehavior.Me.Location);
				}
			}

			if (distance <= Distance)
				return true;
			await GatherSpot.MoveToSpot(this);

			return false;
		}

		private async Task<bool> MoveToHotSpot()
		{
			if (HotSpots != null && !HotSpots.CurrentOrDefault.WithinHotSpot2D(ExProfileBehavior.Me.Location))
			{
				var name = !string.IsNullOrWhiteSpace(HotSpots.CurrentOrDefault.Name)
					? "[" + HotSpots.CurrentOrDefault.Name + "] "
					: string.Empty;

				StatusText = string.Format(Localization.Localization.ExGather_MoveToHotSpot, name, HotSpots.CurrentOrDefault);

				await
					HotSpots.CurrentOrDefault.XYZ.MoveTo(
						radius: HotSpots.CurrentOrDefault.Radius * 0.75f,
						name: HotSpots.CurrentOrDefault.Name,
						stopCallback: MovementStopCallback);

				startTime = DateTime.Now;
				return true;
			}

			return false;
		}

		private async Task<bool> ResetOrDone()
		{
			while (ExProfileBehavior.Me.InCombat && Behaviors.ShouldContinue)
			{
				await Coroutine.Yield();
			}

			if (!FreeRange && (HotSpots == null || HotSpots.Count == 0 || (Node != null && this.Node.IsUnspoiled() && interactedWithNode)))
			{
				isDone = true;
			}
			else
			{
				ResetInternal();
			}

			return true;
		}

		private void ResolveGatherRotation()
		{
			if (gatherRotation != null)
			{
				return;
			}

			if (GatheringSkillOrder != null && GatheringSkillOrder.GatheringSkills.Count > 0)
			{
				initialGatherRotation = gatherRotation = new GatheringSkillOrderGatheringRotation();

				Logger.Info(Localization.Localization.ExGather_RotationUse + gatherRotation.Attributes.Name);
				return;
			}

			IGatheringRotation rotation;
			if (!Rotations.TryGetValue(GatherRotation, out rotation))
			{
				// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
				if (!Rotations.TryGetValue("RegularNode", out rotation))
				{
					rotation = new RegularNodeGatheringRotation();
				}
				else
				{
					rotation = Rotations["RegularNode"];
				}

				Logger.Warn(Localization.Localization.ExGather_RotationNotFound);
			}

			initialGatherRotation = gatherRotation = rotation;

			Logger.Info(string.Format("Using rotation -> {0} ({1} gp per tick)", rotation.Attributes.Name, this.GpPerTick));
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

		private bool SetGatherItem(ICollection<GatheringItem> windowItems)
		{
			foreach (var item in Items)
			{
				var items = windowItems.Where(i => i.IsFilled && !i.IsUnknown).ToArray();

				if (item.Id > 0)
				{
					GatherItem =
						items.FirstOrDefault(i => i.ItemData.Id == item.Id
						&& (!i.ItemData.Unique || i.ItemData.ItemCount() == 0));
				}

				GatherItem = GatherItem ??
					items.FirstOrDefault(
						i => string.Equals(item.LocalName, i.ItemData.CurrentLocaleName, StringComparison.InvariantCultureIgnoreCase)
						&& (!i.ItemData.Unique || i.ItemData.ItemCount() == 0)) ??
					items.FirstOrDefault(
						i => string.Equals(item.Name, i.ItemData.EngName, StringComparison.InvariantCultureIgnoreCase)
						&& (!i.ItemData.Unique || i.ItemData.ItemCount() == 0));

				if (GatherItem != null)
				{
					// We don't need to check null...since it will be null anyway.
					CollectableItem = item as Collectable;
					return true;
				}
			}

			return false;
		}

		private async Task<bool> WaitForGatherWindowToClose()
		{
			var ticks = 0;
			while (GatheringManager.WindowOpen && ticks++ < 100 && Behaviors.ShouldContinue)
			{
				await Coroutine.Yield();
			}

			return true;
		}
	}
}
 
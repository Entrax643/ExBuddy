namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Buddy.Coroutines;
	using ExBuddy.Helpers;
	using ff14bot;
	using ff14bot.Managers;
	using ff14bot.RemoteWindows;
	using System.Threading.Tasks;
	using ff14bot.Objects;

    public abstract class CollectableGatheringRotation : GatheringRotation
	{
		protected int CurrentRarity
		{
			get { return GatheringMasterpiece.Rarity; }
		}

		protected bool HasDiscerningEye
		{
			get { return Core.Player.HasAura((int)AbilityAura.DiscerningEye); }
		}

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			await DiscerningMethodical(tag);
			await DiscerningMethodical(tag);
			await DiscerningMethodical(tag);

			await IncreaseChance(tag);
			return true;
		}

		public override async Task<bool> Gather(ExGatherTag tag)
		{
			tag.StatusText = "Gathering collectable items";

			var rarity = CurrentRarity;

			while (tag.Node.CanGather && GatheringManager.SwingsRemaining > tag.SwingsRemaining && rarity > 0
				   && Behaviors.ShouldContinue)
			{
				while (!SelectYesNoItem.IsOpen && tag.Node.CanGather && GatheringManager.SwingsRemaining > tag.SwingsRemaining
					   && rarity > 0 && Behaviors.ShouldContinue)
				{
					if (!GatheringMasterpiece.IsOpen)
					{
						await Coroutine.Wait(3000, () => GatheringMasterpiece.IsOpen);
					}

					if (GatheringMasterpiece.IsOpen)
					{
						GatheringMasterpiece.Collect();
					}

					await Coroutine.Sleep(500);
				}

				await Coroutine.Yield();
				var swingsRemaining = GatheringManager.SwingsRemaining - 1;

				while (SelectYesNoItem.IsOpen && rarity > 0 && Behaviors.ShouldContinue)
				{
					tag.Logger.Info(
						"Collected item: {0}, value: {1} at {2} ET",
						tag.GatherItem.ItemData.EnglishName,
						SelectYesNoItem.CollectabilityValue,
						WorldManager.EorzaTime);

					SelectYesNoItem.Yes();
					await Coroutine.Wait(2000, () => !SelectYesNoItem.IsOpen);
				}

				var ticks = 0;
				while (swingsRemaining != GatheringManager.SwingsRemaining && ticks++ < 60 && Behaviors.ShouldContinue)
				{
					await Coroutine.Yield();
				}
			}

			tag.StatusText = "Gathering collectable items complete";

			return true;
		}

		public override async Task<bool> Prepare(ExGatherTag tag)
		{
			await tag.CastAura(Ability.CollectorsGlove, AbilityAura.CollectorsGlove);

			var ticks = 0;
			do
			{
				await Wait();

				if (!tag.GatherItem.TryGatherItem())
				{
					return false;
				}
			} while (ticks++ < 10 && !await Coroutine.Wait(3000, () => GatheringMasterpiece.IsOpen) && Behaviors.ShouldContinue);

			if (ticks > 10)
			{
				tag.Logger.Error("Timed out during collectable preparation");
			}

			return true;
		}

		public override bool ShouldForceGather(GatheringPointObject node)
		{
			return !node.IsEphemeral() && !node.IsUnspoiled();
		}

		protected async Task Discerning(ExGatherTag tag)
		{
			await tag.Cast(Ability.DiscerningEye);
		}

		protected async Task SingleMind(ExGatherTag tag)
		{
			await tag.Cast(Ability.SingleMind);
		}

		protected async Task UtmostCaution(ExGatherTag tag)
		{
			await tag.Cast(Ability.UtmostCaution);
		}

		protected async Task Methodical(ExGatherTag tag)
		{
			await tag.Cast(Ability.MethodicalAppraisal);
		}

		protected async Task DiscerningMethodical(ExGatherTag tag)
		{
			await Discerning(tag);
			await Methodical(tag);
		}

		protected async Task SingleMindMethodical(ExGatherTag tag)
		{
			await SingleMind(tag);
			await Methodical(tag);
		}

		protected async Task UtmostMethodical(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Methodical(tag);
		}

		protected async Task UtmostSingleMindMethodical(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await SingleMind(tag);
			await Methodical(tag);
		}

		protected async Task UtmostDiscerningMethodical(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Discerning(tag);
			await Methodical(tag);
		}

		protected async Task Impulsive(ExGatherTag tag)
		{
			if (Core.Player.ClassLevel > 60)
			{
				await tag.Cast(Ability.ImpulsiveAppraisalII);
			}
			else
			{
				await tag.Cast(Ability.ImpulsiveAppraisal);
			}
		}

		protected async Task DiscerningImpulsive(ExGatherTag tag)
		{
			await Discerning(tag);
			await Impulsive(tag);
		}

		protected async Task SingleMindImpulsive(ExGatherTag tag)
		{
			await SingleMind(tag);
			await Impulsive(tag);
		}

		protected async Task UtmostImpulsive(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Impulsive(tag);
		}

		protected async Task UtmostSingleMindImpulsive(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await SingleMind(tag);
			await Impulsive(tag);
		}

		protected async Task UtmostDiscerningImpulsive(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Discerning(tag);
			await Impulsive(tag);
		}

		protected async Task Instinctual(ExGatherTag tag)
		{
			await tag.Cast(Ability.InstinctualAppraisal);
		}

		protected async Task DiscerningInstinctual(ExGatherTag tag)
		{
			await Discerning(tag);
			await Instinctual(tag);
		}

		protected async Task SingleMindInstinctual(ExGatherTag tag)
		{
			await SingleMind(tag);
			await Instinctual(tag);
		}

		protected async Task UtmostInstinctual(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Instinctual(tag);
		}

		protected async Task UtmostSingleMindInstinctual(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await SingleMind(tag);
			await Instinctual(tag);
		}

		protected async Task UtmostDiscerningInstinctual(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Discerning(tag);
			await Instinctual(tag);
		}

		protected async Task AppraiseAndRebuff(ExGatherTag tag)
		{
			await Impulsive(tag);

			if (HasDiscerningEye)
			{
				await SingleMind(tag);
			}
			else
			{
				await Discerning(tag);
			}
		}

		protected async Task SingleMindAppraiseAndRebuff(ExGatherTag tag)
		{
			await SingleMindImpulsive(tag);

			if (HasDiscerningEye)
			{
				await SingleMind(tag);
			}
			else
			{
				await Discerning(tag);
			}
		}

		protected override async Task<bool> IncreaseChance(ExGatherTag tag)
		{
			var level = Core.Player.ClassLevel;
			if (Core.Player.CurrentGP >= 100 && tag.GatherItem.Chance < 95)
			{
				if (level >= 23 && GatheringManager.SwingsRemaining == 1)
				{
					await tag.Cast(Ability.IncreaseGatherChanceOnce15);
					return true;
				}

				await tag.Cast(Ability.IncreaseGatherChance15);
				return true;
			}

			if (Core.Player.CurrentGP >= 50 && tag.GatherItem.Chance < 100)
			{
				if (level >= 23 && GatheringManager.SwingsRemaining == 1)
				{
					await tag.Cast(Ability.IncreaseGatherChanceOnce15);
					return true;
				}

				await tag.Cast(Ability.IncreaseGatherChance5);
				return true;
			}

			return true;
		}

		protected async Task UtmostDiscerning(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await Discerning(tag);
		}

		protected async Task UtmostSingleMind(ExGatherTag tag)
		{
			await UtmostCaution(tag);
			await SingleMind(tag);
		}
	}
}
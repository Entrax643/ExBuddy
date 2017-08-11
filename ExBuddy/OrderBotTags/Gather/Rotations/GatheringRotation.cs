namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Buddy.Coroutines;
	using ExBuddy.Attributes;
	using ExBuddy.Helpers;
	using ExBuddy.Interfaces;
	using ff14bot;
	using ff14bot.Managers;
	using System;
	using System.Threading.Tasks;
	using ff14bot.Objects;

    public abstract class GatheringRotation : IGatheringRotation
	{
		protected internal static readonly uint[] WardSkills = { 236U, 293U, 234U, 292U, 217U, 219U };

		protected internal readonly IGetOverridePriority GetOverridePriorityCached;

		protected GatheringRotation()
		{
			GetOverridePriorityCached = this as IGetOverridePriority;
		}

		protected internal static async Task Wait()
		{
			if (GatheringManager.ShouldPause(DataManager.SpellCache[(uint)Ability.Preparation]))
			{
				var ticks = 0;
				while (ticks++ < 60 && Behaviors.ShouldContinue)
				{
					if (!GatheringManager.ShouldPause(DataManager.SpellCache[(uint)Ability.Preparation]))
					{
						break;
					}

					await Coroutine.Yield();
				}
			}
		}

		protected virtual async Task<bool> IncreaseChance(ExGatherTag tag)
		{
			var level = Core.Player.ClassLevel;
			if (Core.Player.CurrentGP >= 250 && tag.GatherItem.Chance < 51 && level > 10)
			{
				return await tag.Cast(Ability.IncreaseGatherChance50);
			}

			if (Core.Player.CurrentGP >= 100 && tag.GatherItem.Chance < 86 && level > 4)
			{
				if (level >= 23 && GatheringManager.SwingsRemaining == 1)
				{
					return await tag.Cast(Ability.IncreaseGatherChanceOnce15);
				}

				return await tag.Cast(Ability.IncreaseGatherChance15);
			}

			if (Core.Player.CurrentGP >= 50 && tag.GatherItem.Chance < 96 && level > 3)
			{
				if (level >= 23 && GatheringManager.SwingsRemaining == 1)
				{
					return await tag.Cast(Ability.IncreaseGatherChanceOnce15);
				}

				return await tag.Cast(Ability.IncreaseGatherChance5);
			}

			return true;
		}

		#region IGatheringRotation Members

		public virtual GatheringRotationAttribute Attributes
		{
			get { return ReflectionHelper.CustomAttributes<GatheringRotationAttribute>.NotInherited[GetType().GUID][0]; }
		}

		public virtual bool CanBeOverriden
		{
			get { return true; }
		}

		public virtual async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			await IncreaseChance(tag);
			return true;
		}

		public virtual async Task<bool> Gather(ExGatherTag tag)
		{
			tag.StatusText = "Gathering items";

			while (tag.Node.CanGather && GatheringManager.SwingsRemaining > tag.SwingsRemaining && Behaviors.ShouldContinue)
			{
				await Wait();

				if (GatheringManager.HqGatheringCombo == 2 && GatheringManager.SwingsRemaining > tag.SwingsRemaining)
				{
					await tag.Cast(Ability.Luck);

					await Wait();
				}

				if (GatheringManager.GatheringCombo == 4 && GatheringManager.SwingsRemaining > tag.SwingsRemaining)
				{
					await tag.Cast(Ability.IncreaseGatherChanceQuality100);

					await Wait();
				}

				if (!await tag.ResolveGatherItem())
				{
					return false;
				}

				var swingsRemaining = GatheringManager.SwingsRemaining - 1;

				if (!tag.GatherItem.TryGatherItem())
				{
					return false;
				}

				var ticks = 0;
				while (swingsRemaining != GatheringManager.SwingsRemaining && ticks++ < 60 && Behaviors.ShouldContinue)
				{
					await Coroutine.Yield();
				}
			}

			tag.StatusText = "Gathering items complete";

			return true;
		}

		public virtual async Task<bool> Prepare(ExGatherTag tag)
		{
			if (Core.Player.HasAura((int)AbilityAura.CollectorsGlove))
			{
				return await tag.Cast(Ability.CollectorsGlove);
			}

			return true;
		}

		public int ResolveOverridePriority(ExGatherTag tag)
		{
			if (GetOverridePriorityCached != null)
			{
				try
				{
					return GetOverridePriorityCached.GetOverridePriority(tag);
				}
				catch (Exception)
				{
					// TODO: do stuff with exceptions.
					return -1;
				}
			}

			return -1;
		}

		public virtual bool ShouldForceGather(GatheringPointObject node)
		{
			return false;
		}

		#endregion IGatheringRotation Members
	}
}
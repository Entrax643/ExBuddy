namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using ExBuddy.Attributes;
	using ExBuddy.Enumerations;
	using ExBuddy.Helpers;
	using ExBuddy.Interfaces;
	using ff14bot;
	using System.Threading.Tasks;

	//Name, RequiredTime, RequiredGpBreakpoints
	[GatheringRotation("Ephemeral")]
	public sealed class EphemeralGatheringRotation : GatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			if (tag.Node.IsEphemeral() && tag.CollectableItem == null)
			{
				return 9100;
			}

			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			var level = Core.Player.ClassLevel;
			var gp = Core.Player.CurrentGP;

			// Yield And Quality
			if (tag.GatherIncrease == GatherIncrease.YieldAndQuality
				|| (tag.GatherIncrease == GatherIncrease.Auto && level >= 40 && gp >= 650))
			{
				if (gp >= 500 && level >= 40)
				{
					await tag.Cast(Ability.IncreaseGatherYield2);

					if (Core.Player.CurrentGP >= 100)
					{
						await tag.Cast(Ability.IncreaseGatherQuality10);
					}

					return await base.ExecuteRotation(tag);
				}

				return true;
			}

			// Yield
			if (tag.GatherIncrease == GatherIncrease.Yield || (tag.GatherIncrease == GatherIncrease.Auto && level >= 40))
			{
				if (gp >= 500 && level >= 40)
				{
					await tag.Cast(Ability.IncreaseGatherYield2);
					return await base.ExecuteRotation(tag);
				}

				if (gp >= 400 && level >= 30 && (level < 40 || Core.Player.MaxGP < 500))
				{
					await tag.Cast(Ability.IncreaseGatherYield);
					return await base.ExecuteRotation(tag);
				}

				if (gp >= 300 && level >= 25 && (level < 30 || Core.Player.MaxGP < 400))
				{
					await Wait();

					if (!tag.GatherItem.TryGatherItem())
					{
						return false;
					}

					await tag.Cast(Ability.AdditionalAttempt);
					return await base.ExecuteRotation(tag);
				}

				return true;
			}

			// Quality
			if (tag.GatherIncrease == GatherIncrease.Quality
				|| (tag.GatherIncrease == GatherIncrease.Auto && level >= 15 && level < 40))
			{
				if (Core.Player.CurrentGP >= 300)
				{
					if (Core.Player.ClassLevel >= 63)
					{
						await tag.Cast(Ability.IncreaseGatherQuality30100);
					}
					else
					{
						await tag.Cast(Ability.IncreaseGatherQuality30);
					}
					return await base.ExecuteRotation(tag);
					;
				}

				return true;
			}

			return await base.ExecuteRotation(tag);
		}
	}
}
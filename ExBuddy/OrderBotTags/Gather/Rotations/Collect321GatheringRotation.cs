namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Attributes;
	using ff14bot;
	using ff14bot.Managers;
	using Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect321", 35, 600)]
	public sealed class Collect321GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 321: Priority 321
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 321)
			{
				return 321;
			}
			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await SingleMindImpulsive(tag);
				await SingleMindImpulsive(tag);
				await SingleMindMethodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
				if (Core.Player.CurrentGP >= 600 && GatheringManager.SwingsRemaining > 4)
				{
					if (Core.Player.ClassLevel >= 57)
					{
						await SingleMindImpulsive(tag);
						await SingleMindImpulsive(tag);
						await SingleMindMethodical(tag);
						await IncreaseChance(tag);
					}
					else
					{
						await Discerning(tag);
						await AppraiseAndRebuff(tag);
						await AppraiseAndRebuff(tag);
						await Methodical(tag);
						await IncreaseChance(tag);
					}
				}
				else
				{
					await Impulsive(tag);
					await Impulsive(tag);
					await Instinctual(tag);
				}
			}

			return true;
		}
	}
}
namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using ExBuddy.Attributes;
	using ExBuddy.Interfaces;
	using ff14bot;
	using ff14bot.Managers;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect480", 35, 600)]
	public sealed class Collect480GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 480: Priority 480
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 480)
			{
				return 480;
			}

			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await Discerning(tag);
				await AppraiseAndRebuff(tag);
				await AppraiseAndRebuff(tag);
				await Methodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
				if (Core.Player.CurrentGP >= 600)
				{
					if (GatheringManager.SwingsRemaining > 4)
					{
						await Discerning(tag);
						await AppraiseAndRebuff(tag);
						await AppraiseAndRebuff(tag);
						await Methodical(tag);
						await IncreaseChance(tag);
					}
					else
					{
						await Methodical(tag);
						await Methodical(tag);
						await Methodical(tag);
						await Methodical(tag);
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
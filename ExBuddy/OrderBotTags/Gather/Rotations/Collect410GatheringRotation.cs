namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Attributes;
	using ff14bot;
	using Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect410", 30, 600)]
	public sealed class Collect410GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 410: Priority 410
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 410)
			{
				return 410;
			}
			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await SingleMindAppraiseAndRebuff(tag);
				await AppraiseAndRebuff(tag);
				await Methodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
				if (Core.Player.CurrentGP >= 600)
				{
					await SingleMindAppraiseAndRebuff(tag);
					await AppraiseAndRebuff(tag);
					await Methodical(tag);
					await IncreaseChance(tag);
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
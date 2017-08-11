namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Attributes;
	using ff14bot;
	using Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect402", 35, 600)]
	public sealed class Collect402GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 402: Priority 402
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 402)
			{
				return 402;
			}
			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await SingleMindMethodical(tag);
				await SingleMindMethodical(tag);
				await DiscerningMethodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
#if RB_CN
				if (Core.Player.CurrentGP >= 600 && tag.GatherItem.Chance < 98)
				{
#else
				if (Core.Player.CurrentGP >= 600)
				{
#endif
					await SingleMindMethodical(tag);
					await SingleMindMethodical(tag);
					await DiscerningMethodical(tag);
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
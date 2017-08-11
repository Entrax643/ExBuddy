namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Attributes;
	using ff14bot;
	using Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect459", 35, 600)]
	public sealed class Collect459GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 459: Priority 459
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 459)
			{
				return 459;
			}
			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await SingleMindMethodical(tag);
				await DiscerningMethodical(tag);
				await DiscerningMethodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
				if (Core.Player.CurrentGP >= 600)
				{
					await SingleMindMethodical(tag);
					await DiscerningMethodical(tag);
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
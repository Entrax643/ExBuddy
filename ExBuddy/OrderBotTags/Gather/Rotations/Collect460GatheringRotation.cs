namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using Attributes;
	using ff14bot;
	using ff14bot.Managers;
	using Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    [GatheringRotation("Collect460", 33, 600)]
	public sealed class Collect460GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 460: Priority 460
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 460)
			{
				return 460;
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
			else if (tag.Node.IsEphemeral())
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
			else
			{
				if (Core.Player.CurrentGP >= 600)
				{
					if (GatheringManager.SwingsRemaining > 4)
					{
						await SingleMindMethodical(tag);
						await DiscerningMethodical(tag);
						await DiscerningMethodical(tag);
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
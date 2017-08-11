namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using ExBuddy.Attributes;
	using ExBuddy.Interfaces;
	using ff14bot;
	using System.Threading.Tasks;
	using Helpers;

    // Get One ++
	[GatheringRotation("Collect550", 33, 600)]
	public sealed class Collect550GatheringRotation : CollectableGatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			// if we have a collectable && the collectable value is greater than or equal to 550: Priority 550
			if (tag.CollectableItem != null && tag.CollectableItem.Value >= 550)
			{
				return 550;
			}

			return -1;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				await UtmostCaution(tag);
				await AppraiseAndRebuff(tag);
#if RB_CN
				await UtmostCaution(tag);
#endif
				await Methodical(tag);
				await AppraiseAndRebuff(tag);
				await Methodical(tag);
				await IncreaseChance(tag);
			}
			else
			{
				// if 58+
				if (tag.GatherItem.Chance < 98 && Core.Player.CurrentGP >= 600)
				{
					// if 60 or cordial is ready and is 58
					if (tag.GatherItem.Chance < 97)
					{
						var appraisalsRemaining = 4;
						await Impulsive(tag);
						appraisalsRemaining--;

						if (HasDiscerningEye)
						{
							await UtmostSingleMindMethodical(tag);
							appraisalsRemaining--;
						}

						await Impulsive(tag);
						appraisalsRemaining--;

						if (HasDiscerningEye)
						{
#if RB_CN
							await UtmostSingleMindMethodical(tag);
#else
							if (appraisalsRemaining == 1)
							{
								await SingleMindMethodical(tag);
							}
							else
							{
								await UtmostSingleMindMethodical(tag);
							}
#endif
							appraisalsRemaining--;
						}

						if (appraisalsRemaining == 2)
						{
							await Methodical(tag);
						}

						if (appraisalsRemaining == 1)
						{
#if RB_CN
							await UtmostSingleMindMethodical(tag);
#else
							await SingleMindMethodical(tag);
#endif
						}

						await IncreaseChance(tag);
						return true;
					}
				}

				await Impulsive(tag);
				await Impulsive(tag);
				await Instinctual(tag);

				return true;
			}

			return true;
		}
	}
}
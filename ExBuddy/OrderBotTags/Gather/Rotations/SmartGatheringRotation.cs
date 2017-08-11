namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using ExBuddy.Interfaces;
	using System.Threading.Tasks;
	using ff14bot.Objects;
	using Helpers;

    public abstract class SmartGatheringRotation : GatheringRotation
	{
		public override Task<bool> Prepare(ExGatherTag tag)
		{
			return ResolveInternalGatheringRotation(tag).Prepare(tag);
		}

		public override bool ShouldForceGather(GatheringPointObject node)
		{
			return !node.IsUnspoiled();
		}

		protected virtual IGatheringRotation ResolveInternalGatheringRotation(ExGatherTag tag)
		{
			if (tag.Node.IsUnspoiled())
			{
				return ExGatherTag.Rotations["Unspoiled"];
			}

			return ExGatherTag.Rotations["RegularNode"];
		}

		protected bool ShouldForceUseRotation(ExGatherTag tag, uint level)
		{
			if (!tag.GatherItemIsFallback && ((level < 50 && tag.NodesGatheredAtMaxGp > 4) || tag.NodesGatheredAtMaxGp > 6))
			{
				tag.Logger.Info("Using Gp since we have gathered {0} nodes at max Gp.", tag.NodesGatheredAtMaxGp);

				return true;
			}

			return false;
		}
	}
}
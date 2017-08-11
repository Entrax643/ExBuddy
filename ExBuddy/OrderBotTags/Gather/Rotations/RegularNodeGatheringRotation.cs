namespace ExBuddy.OrderBotTags.Gather.Rotations
{
	using ExBuddy.Attributes;
	using ExBuddy.Interfaces;
	using System.Threading.Tasks;
	using Helpers;

    //Name, RequiredTime, RequiredGpBreakpoints
	[GatheringRotation("RegularNode")]
	public sealed class RegularNodeGatheringRotation : GatheringRotation, IGetOverridePriority
	{
		#region IGetOverridePriority Members

		int IGetOverridePriority.GetOverridePriority(ExGatherTag tag)
		{
			if (tag.Node.IsEphemeral() || tag.Node.IsUnspoiled() || tag.CollectableItem != null)
			{
				return -1;
			}

			return 8000;
		}

		#endregion IGetOverridePriority Members

		public override async Task<bool> ExecuteRotation(ExGatherTag tag)
		{
			return true;
		}
	}
}
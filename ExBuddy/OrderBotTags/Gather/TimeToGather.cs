namespace ExBuddy.OrderBotTags.Gather
{
    using Helpers;

#if RB_CN
    using ActionManager = ff14bot.Managers.Actionmanager;
#endif

    public struct TimeToGather
	{
#pragma warning disable 414
		public int EorzeaMinutesTillDespawn;
#pragma warning restore 414
		public int RealSecondsTillStartGathering;

		public int TicksTillStartGathering
		{
			get { return EorzeaTimeHelper.ConvertSecondsToTicks(RealSecondsTillStartGathering); }
		}
	}
}
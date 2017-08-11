namespace ExBuddy.Logging
{
    using System;
    using OrderBotTags.Gather;
    using OrderBotTags.Gather.Strategies;

    internal class BeforeGatherGpRegenStrategyLogger : IBeforeGatherGpRegenStrategyLogger
    {
        private readonly Logger logger;
        private readonly IGathererLogger gathererLogger;
        private readonly IGpRegeneratorLogger gpRegeneratorLogger;
        private readonly ICordialConsumerLogger cordialConsumerLogger;
        private readonly IStatusLogger statusLogger;

        /// <summary>
        /// Instantiates a new instance of the <see cref="ExGatherTagLogger"/> class.
        /// </summary>
        public BeforeGatherGpRegenStrategyLogger(Logger logger, IGathererLogger gathererLogger, IGpRegeneratorLogger gpRegeneratorLogger, ICordialConsumerLogger cordialConsumerLogger, IStatusLogger statusLogger)
        {
            this.logger = logger ?? throw new ArgumentNullException("logger");
            this.gathererLogger = gathererLogger ?? throw new ArgumentNullException("gathererLogger");
            this.gpRegeneratorLogger = gpRegeneratorLogger ?? throw new ArgumentNullException("gpRegeneratorLogger");
            this.cordialConsumerLogger = cordialConsumerLogger ?? throw new ArgumentNullException("cordialConsumerLogger");
            this.statusLogger = statusLogger ?? throw new ArgumentNullException("statusLogger");
        }

        /// <summary>
        /// Logs the strategy status report
        /// </summary>
        public void LogReport(IGpRegenStrategy strategy)
        {
            this.logger.Info(strategy.ToString());
        }

        #region IGathererLogger Methods
        /// <summary>
        /// Logs when there is not enough time to begin a gather before the node disappears
        /// </summary>
        public void GatheringNotEnoughTime()
        {
            this.gathererLogger.GatheringNotEnoughTime();
        }

        /// <summary>
        /// Logs when the node is gone
        /// </summary>
        public void GatheringNodeIsGone()
        {
            this.gathererLogger.GatheringNodeIsGone();
        }

        /// <summary>
        /// Logs when the node is skipped because it is not ephemeral
        /// </summary>
        public void GatheringNodeSkippedNotEphemeral()
        {
            this.gathererLogger.GatheringNodeSkippedNotEphemeral();
        }

        #endregion

        #region IStatusTextLogger Methods
        /// <summary>
        /// Sets the status text
        /// </summary>
        public void SetStatusText(string text)
        {
            this.statusLogger.SetStatusText(text);
        }
        #endregion

        #region IGpRegeneratorLogger Methods
        /// <summary>
        /// Logs when cordial use is is disabled because there is no stock
        /// </summary>
        public void RegeneratingCordialUseDisabledNoStock()
        {
            this.gpRegeneratorLogger.RegeneratingCordialUseDisabledNoStock();
        }

        /// <summary>
        /// Logs when cordial use is skipped because there is no cordial that satisfies the missing GP requirements
        /// </summary>
        public void RegeneratingNoCordialWasAppropriate(int originalGP, int targetGP)
        {
            this.gpRegeneratorLogger.RegeneratingNoCordialWasAppropriate(originalGP, targetGP);
        }

        /// <summary>
        /// Logs when GP regeneration waiting begins
        /// </summary>
        public void RegeneratingGp(int waitInSeconds)
        {
            this.gpRegeneratorLogger.RegeneratingGp(waitInSeconds);
        }

        /// <summary>
        /// Logs when there is not enough GP to begin the gather
        /// </summary>
        public void RegeneratingNotEnoughGp()
        {
            this.gpRegeneratorLogger.RegeneratingNotEnoughGp();
        }

        /// <summary>
        /// Logs when wait time exceeds max ephemeral GP wait time
        /// </summary>
        public void RegeneratingSkippedExceedsEphemeralMaxWait(double expectedWait, double maxWait)
        {
            this.gpRegeneratorLogger.RegeneratingSkippedExceedsEphemeralMaxWait(expectedWait, maxWait);
        }

        #endregion

        #region ICordialConsumerLogger Methods
        public void CordialCannotBeUsedByDeadPlayer(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialCannotBeUsedByDeadPlayer(cordialName, hq);
        }

        public void CordialCannotBeUsedByMountedPlayer(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialCannotBeUsedByMountedPlayer(cordialName, hq);
        }

        public void CordialCannotBeUsedByPlayer(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialCannotBeUsedByPlayer(cordialName, hq);
        }

        public void CordialCannotBeUsedDismountFailed(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialCannotBeUsedDismountFailed(cordialName, hq);
        }

        public void CordialInventoryDepleted(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialInventoryDepleted(cordialName, hq);
        }

        public void CordialItemTypeInvalid(uint itemId)
        {
            this.cordialConsumerLogger.CordialItemTypeInvalid(itemId);
        }

        public void CordialOnCooldown(string cordialName, bool hq)
        {
            this.cordialConsumerLogger.CordialOnCooldown(cordialName, hq);
        }

        public void CordialUsed(string cordialName, bool hq, int cordialSize, int startingGp, int regeneratedGp, int targetGp)
        {
            this.cordialConsumerLogger.CordialUsed(cordialName, hq, cordialSize, startingGp, regeneratedGp, targetGp);
        }
        #endregion
    }
}
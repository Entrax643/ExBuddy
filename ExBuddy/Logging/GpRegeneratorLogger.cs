namespace ExBuddy.Logging
{
    internal class GpRegeneratorLogger : IGpRegeneratorLogger
    {
        private readonly Logger logger;

        /// <summary>
        /// Instantiates a new instance of the <see cref="GpRegeneratorLogger"/> class.
        /// </summary>
        public GpRegeneratorLogger(Logger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Logs when cordial use is is disabled because there is no stock
        /// </summary>
        public void RegeneratingCordialUseDisabledNoStock()
        {
            this.logger.Info(Localization.Localization.ExGather_CordialUseDisabledNoStock);
        }

        /// <summary>
        /// Logs when cordial use is skipped because there is no cordial that satisfies the missing GP requirements
        /// </summary>
        public void RegeneratingNoCordialWasAppropriate(int originalGP, int targetGP)
        {
            this.logger.Info(
                string.Format(Localization.Localization.ExGather_NoCordialWasAppropriate,
                    originalGP,
                    targetGP,
                    targetGP - originalGP
                )
            );
        }

        /// <summary>
        /// Logs when GP regeneration waiting begins
        /// </summary>
        public void RegeneratingGp(int waitInSeconds)
        {
            this.logger.Info(
                string.Format(Localization.Localization.ExGather_BeforeGatherWait,
                    waitInSeconds
                )
            );
        }

        /// <summary>
        /// Logs when there is not enough time to regenerate GP
        /// </summary>
        public void RegeneratingNotEnoughGp()
        {
            this.logger.Info(
                Localization.Localization.ExGather_NotEnoughGP
            );
        }

        /// <summary>
        /// Logs when wait time exceeds max ephemeral GP wait time
        /// </summary>
        public void RegeneratingSkippedExceedsEphemeralMaxWait(double expectedWait, double maxWait)
        {
            this.logger.Info(
                string.Format(Localization.Localization.ExGather_TnGSeconds,
                    expectedWait,
                    maxWait
                )
            );
        }
    }
}
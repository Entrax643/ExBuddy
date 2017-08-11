namespace ExBuddy.Logging
{
    using System;

    internal interface IGpRegeneratorLogger
    {
        /// <summary>
        /// Logs when cordial use is is disabled because there is no stock
        /// </summary>
        void RegeneratingCordialUseDisabledNoStock();

        /// <summary>
        /// Logs when cordial use is skipped because there is no cordial that satisfies the missing GP requirements
        /// </summary>
        void RegeneratingNoCordialWasAppropriate(int originalGP, int targetGP);

        /// <summary>
        /// Logs when GP regeneration waiting begins
        /// </summary>
        void RegeneratingGp(int waitInSeconds);

        /// <summary>
        /// Logs when there is not enough time to regenerate GP
        /// </summary>
        void RegeneratingNotEnoughGp();

        /// <summary>
        /// Logs when wait time exceeds max ephemeral GP wait time
        /// </summary>
        void RegeneratingSkippedExceedsEphemeralMaxWait(double expectedWait, double maxWait);
    }
}
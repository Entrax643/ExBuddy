namespace ExBuddy.Logging
{
    using System;

    internal class GathererLogger : IGathererLogger
    {
        private readonly Logger logger;

        /// <summary>
        /// Instantiates a new instance of the <see cref="GathererLogger"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public GathererLogger(Logger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Logs when there is not enough time to gather
        /// </summary>
        public void GatheringNotEnoughTime()
        {
            this.logger.Info(
                Localization.Localization.ExGather_NotEnoughTime
            );
        }

        /// <summary>
        /// Logs when the node is gone
        /// </summary>
        public void GatheringNodeIsGone()
        {
            this.logger.Info(
                Localization.Localization.ExGather_NodeIsGone
            );
        }

        /// <summary>
        /// Logs when the node is skipped because it is not ephemeral
        /// </summary>
        public void GatheringNodeSkippedNotEphemeral()
        {
            this.logger.Info(
                Localization.Localization.ExGather_SkipNode
            );
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExBuddy.Logging
{
    internal class CordialConsumerLogger : ICordialConsumerLogger
    {
        private readonly Logger logger;

        /// <summary>
        /// Instantiates a new instance of the <see cref="CordialConsumerLogger"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public CordialConsumerLogger(Logger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets a formatted name for a quality qualified cordial
        /// </summary>
        /// <param name="cordialName"></param>
        /// <param name="hq"></param>
        /// <returns></returns>
        private string GetQualifiedCordialName(string cordialName, bool hq)
        {
            return string.Format("{0}{1}", cordialName, hq ? "+" : string.Empty);
        }

        /// <summary>
        /// Logs the result of a successful cordial use.
        /// </summary>
        public void CordialUsed(string cordialName, bool hq, int cordialSize, int startingGp, int regeneratedGp, int targetGp)
        {
            this.logger.Info(
                string.Format(Localization.Localization.CordialConsumer_CordialUsed,
                    this.GetQualifiedCordialName(cordialName, hq),
                    startingGp,
                    cordialSize,
                    regeneratedGp,
                    targetGp
                )
            );
        }

        /// <summary>
        /// Logs the result when an inventory item refers to an item type that does not exist.
        /// </summary>
        public void CordialItemTypeInvalid(uint itemId)
        {
            this.logger.Error(
                string.Format(Localization.Localization.CordialConsumer_CordialItemTypeInvalid,
                    itemId
                )
            );
        }

        /// <summary>
        /// Logs the result when no cordial of the desired type could be found
        /// </summary>
        public void CordialInventoryDepleted(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialStockDepleted,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }

        /// <summary>
        /// Logs the result when cordial is on cooldown.
        /// </summary>
        public void CordialOnCooldown(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialOnCooldown,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }

        /// <summary>
        /// Logs the result when cordial cannot be used by the player.
        /// </summary>
        public void CordialCannotBeUsedByPlayer(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialCannotBeUsedByPlayer,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }

        /// <summary>
        /// Logs the result when cordial cannot be used because player is dead.
        /// </summary>
        public void CordialCannotBeUsedByDeadPlayer(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialCannotBeUsedByDeadPlayer,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }

        /// <summary>
        /// Logs the result when cordial cannot be used because player is mounted.
        /// </summary>
        public void CordialCannotBeUsedByMountedPlayer(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialCannotBeUsedByMountedPlayer,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }

        /// <summary>
        /// Logs the result when cordial cannot be used because dismount failed.
        /// </summary>
        public void CordialCannotBeUsedDismountFailed(string cordialName, bool hq)
        {
            this.logger.Warn(
                string.Format(Localization.Localization.CordialConsumer_CordialCannotBeUsedDismountFailed,
                    this.GetQualifiedCordialName(cordialName, hq)
                )
            );
        }
    }
}

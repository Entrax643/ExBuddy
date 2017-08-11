using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExBuddy.Logging
{
    internal interface IGathererLogger
    {
        /// <summary>
        /// Logs when there is not enough time to begin a gather
        /// </summary>
        void GatheringNotEnoughTime();

        /// <summary>
        /// Logs when the node is gone
        /// </summary>
        void GatheringNodeIsGone();

        /// <summary>
        /// Logs when the node is skipped because it is not ephemeral
        /// </summary>
        void GatheringNodeSkippedNotEphemeral();
    }
}

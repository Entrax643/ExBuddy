using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExBuddy.Logging
{
    using OrderBotTags.Gather;
    using OrderBotTags.Gather.Strategies;

    internal interface IBeforeGatherGpRegenStrategyLogger : ICordialConsumerLogger, IGpRegeneratorLogger, IStatusLogger, IGathererLogger
    {
        void LogReport(IGpRegenStrategy strategy);
    }
}

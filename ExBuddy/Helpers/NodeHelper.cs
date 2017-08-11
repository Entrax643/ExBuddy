using System;

namespace ExBuddy.Helpers
{
    using ff14bot.Managers;
    using ff14bot.Objects;
    using OrderBotTags.Gather;

    public static class NodeHelper
    {
        public class NodeLifespan
        {
            public NodeLifespan(int despawnSeconds)
            {
                this.DeSpawn = TimeSpan.FromSeconds(despawnSeconds);
                this.DeSpawnTicks = EorzeaTimeHelper.ConvertSecondsToTicks(despawnSeconds);
            }

            public readonly TimeSpan DeSpawn;
            public readonly int DeSpawnTicks;
        }

        /// <summary>
        /// Calculates and returns the expected real world seconds until the node despawns
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static NodeLifespan GetNodeLifespan(GatheringPointObject node)
        {
            var eorzeaMinutesTillDespawn = (int)byte.MaxValue;
            if (node.IsUnspoiled())
            {
                if (WorldManager.ZoneId > 350)
                {
                    eorzeaMinutesTillDespawn = 55 - WorldManager.EorzaTime.Minute;
                }
                else
                {
                    // We really don't know how much time is left on the node, but it does have at least the 5 more EM.
                    eorzeaMinutesTillDespawn = 60 - WorldManager.EorzaTime.Minute;
                }
            }

            if (node.IsEphemeral())
            {
                var hoursFromNow = WorldManager.EorzaTime.AddHours(4);
                var rounded = new DateTime(
                    hoursFromNow.Year,
                    hoursFromNow.Month,
                    hoursFromNow.Day,
                    hoursFromNow.Hour - (hoursFromNow.Hour % 4),
                    0,
                    0);

                eorzeaMinutesTillDespawn = (int)(rounded - WorldManager.EorzaTime).TotalMinutes;
            }

            return new NodeLifespan(eorzeaMinutesTillDespawn * 35 / 12);
        }

        /// <summary>
        /// Returns true if the node is an ephemeral node
        /// </summary>
        public static bool IsEphemeral(this GatheringPointObject node)
        {
            return node.EnglishName.IndexOf("ephemeral", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns true if the node is concealed
        /// </summary>
        public static bool IsConcealed(this GatheringPointObject node)
        {
            return node.EnglishName.IndexOf("concealed", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns true if the node is unspoiled or legendary
        /// </summary>
        public static bool IsUnspoiled(this GatheringPointObject node)
        {
            // Temporary until we decide if legendary have any diff properties or if we should treat them the same.
            return node.EnglishName.IndexOf("unspoiled", StringComparison.InvariantCultureIgnoreCase) >= 0
                   || node.EnglishName.IndexOf("legendary", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}

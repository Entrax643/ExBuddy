using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.Objects;

namespace ExBuddy.Helpers
{
    public static class CharacterResource
    {
        public static short GetGpPerTick()
        {
            return (CharacterResource.Me.CurrentJob == ClassJobType.Miner && ConditionParser.IsQuestCompleted(68094))
                || (CharacterResource.Me.CurrentJob == ClassJobType.Botanist && ConditionParser.IsQuestCompleted(68160))
                || (CharacterResource.Me.CurrentJob == ClassJobType.Fisher && ConditionParser.IsQuestCompleted(68435))
                ? (short) 6
                : (short) 5;
        }

        public static short GetEffectiveGp(int ticksTillGather)
        {
            return GetEffectiveGp(ticksTillGather, GetGpPerTick());
        }

        public static short GetEffectiveGp(int ticksTillGather, int gpPerTick)
        {
            return ticksTillGather <= 0 
                ? CharacterResource.Me.CurrentGP 
                : (short) Math.Min(CharacterResource.Me.CurrentGP + (ticksTillGather * gpPerTick), CharacterResource.Me.MaxGP);
        }

        public static TimeSpan EstimateExpectedRegenerationTime(short gpNeeded)
        {
            return EstimateExpectedRegenerationTime(gpNeeded, CharacterResource.GetGpPerTick());
        }

        public static TimeSpan EstimateExpectedRegenerationTime(short gpNeeded, short gpPerTick)
        {
            var gpNeededTicks = gpNeeded / gpPerTick;
            var gpNeededSeconds = gpNeededTicks * 3;

            return TimeSpan.FromSeconds(gpNeededSeconds);
        }

        private static LocalPlayer Me
        {
            get { return GameObjectManager.LocalPlayer; }
        }
    }
}

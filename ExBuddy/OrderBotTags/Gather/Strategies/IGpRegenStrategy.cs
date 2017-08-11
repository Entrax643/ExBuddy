namespace ExBuddy.OrderBotTags.Gather.Strategies
{
    using System.Threading.Tasks;
    using Enumerations;
    using ff14bot.Objects;
    using Interfaces;

    internal interface IGpRegenStrategy
    {
        Task<GpRegenStrategyResult> RegenerateGp(GatheringPointObject node, IGatheringRotation gatherRotation, GatherStrategy gatherStrategy, CordialTime cordialTime, CordialType cordialType);
    }
}
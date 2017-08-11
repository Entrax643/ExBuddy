namespace ExBuddy.OrderBotTags.Gather.Strategies
{
    using Enumerations;
    using Inventory;

    public class GpRegenStrategyResult
    {
        public enum GpRegenStrategyResultState
        {
            OK,
            NotEnoughGp,
            NotEnoughTime,
            NodeGone
        }

        public CordialType OriginalCordialType { get; set; }
        public CordialType EffectiveCordialType { get; set; }
        public GpRegenStrategyResultState StrategyState { get; set; }
        public InventoryItem.UseResult? UseState { get; set; }
    }
}

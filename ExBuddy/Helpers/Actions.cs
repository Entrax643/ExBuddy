namespace ExBuddy.Helpers
{
    using Buddy.Coroutines;
    using ExBuddy.Logging;
    using ff14bot;
    using ff14bot.Enums;
    using ff14bot.Managers;
    using ff14bot.Objects;
    using System.Threading.Tasks;

    internal static class Actions
    {
        internal static bool HasAction(Ability ability)
        {
            return HasAction(ability.GetAbilityId());
        }

        internal static bool HasAction(uint id)
        {
            return ActionManager.HasSpell(id);
        }

        internal static bool CanCast(Ability ability, uint cost, CostType costType)
        {
            return CanCast(ability.GetAbilityId(), cost, costType);
        }

        internal static bool CanCast(uint id, uint cost, CostType costType)
        {
            bool hasCost = false;

            switch (costType)
            {
                case CostType.None:
                    hasCost = true;
                    break;
                case CostType.CPCost:
                    hasCost = Core.Player.CurrentCP >= cost;
                    break;
                case CostType.GPCost:
                    hasCost = Core.Player.CurrentGP >= cost;
                    break;
                case CostType.Hp:
                    hasCost = Core.Player.CurrentHealth >= cost;
                    break;
                case CostType.Mp:
                    hasCost = Core.Player.CurrentMana >= cost;
                    break;
                case CostType.Tp:
                    hasCost = Core.Player.CurrentTP >= cost;
                    break;
                default:
                    Logger.Instance.Warn("{0}() does not support checking {1}.{2}", nameof(CanCast), nameof(CostType), cost);
                    hasCost = false;
                    break;
            }

            return hasCost && CanCast(id);
        }

        internal static bool CanCast(Ability ability)
        {
            return CanCast(ability.GetAbilityId());
        }

        internal static bool CanCast(uint id)
        {
            return ActionManager.CanCast(id, Core.Player);
        }

        internal static async Task<bool> Cast(uint id, int delay)
        {
            //TODO: check affinity, cost type, spell type, and add more informational logging and procedures to casting
            //Wait till we can cast the spell
            SpellData spellData;
            if (GatheringManager.ShouldPause(spellData = DataManager.SpellCache[id]))
            {
                await Coroutine.Wait(3500, () => !GatheringManager.ShouldPause(spellData));
            }

            bool result = ActionManager.DoAction(id, Core.Player);

            int ticks = 0;
            while (result == false && ticks++ < 10 && Behaviors.ShouldContinue)
            {
                result = ActionManager.DoAction(id, Core.Player);
                await Coroutine.Yield();
            }

            if (result)
            {
                Logger.Instance.Info("Casted Ability -> {0}", spellData.Name);
            }
            else
            {
                Logger.Instance.Error("Failed to cast Ability -> {0}", spellData.Name);
            }

            //Wait till we can cast again
            if (GatheringManager.ShouldPause(spellData))
            {
                await Coroutine.Wait(3500, () => !GatheringManager.ShouldPause(spellData));
            }
            if (delay > 0)
            {
                await Coroutine.Sleep(delay);
            }
            else
            {
                await Coroutine.Yield();
            }

            return result;
        }

        internal static async Task<bool> Cast(Ability ability, int delay)
        {
            return await Cast(ability.GetAbilityId(), delay);
        }

        internal static async Task<bool> CastAura(uint spellId, int delay, int auraId = -1)
        {
            bool result = false;
            if (auraId == -1 || !Core.Player.HasAura((uint)auraId))
            {
                SpellData spellData;
                if (GatheringManager.ShouldPause(spellData = DataManager.SpellCache[spellId]))
                {
                    await Coroutine.Wait(3500, () => !GatheringManager.ShouldPause(DataManager.SpellCache[spellId]));
                }

                result = ActionManager.DoAction(spellId, Core.Player);
                int ticks = 0;
                while (result == false && ticks++ < 5 && Behaviors.ShouldContinue)
                {
                    result = ActionManager.DoAction(spellId, Core.Player);
                    await Coroutine.Yield();
                }

                if (result)
                {
                    Logger.Instance.Info("Casted Aura -> {0}", spellData.Name);
                }
                else
                {
                    Logger.Instance.Error("Failed to cast Aura -> {0}", spellData.Name);
                }

                //Wait till we have the aura
                await Coroutine.Wait(3500, () => Core.Player.HasAura((uint)auraId));
                if (delay > 0)
                {
                    await Coroutine.Sleep(delay);
                }
                else
                {
                    await Coroutine.Yield();
                }
            }

            return result;
        }

        internal static async Task<bool> CastAura(Ability ability, int delay, AbilityAura aura = AbilityAura.None)
        {
            return await CastAura(ability.GetAbilityId(), delay, (int)aura);
        }
    }
}
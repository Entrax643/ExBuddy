using System.Threading.Tasks;

namespace ExBuddy.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Buddy.Coroutines;
    using ff14bot;
    using ff14bot.Managers;
    using ff14bot.Objects;
    using Helpers;
    using OrderBotTags.Behaviors;

    /// <summary>
    /// Utility wrapper and tracker around a specific in-game item.
    /// </summary>
    public class InventoryItem
    {
        public const int DEFAULT_TIMEOUT_SECONDS = 5;

        public enum UseResult
        {
            OK,
            UnknownItem,
            NoInventory,
            OnCooldown,
            NoUse,
            CantUse,
            PlayerDead,
            DismountFailed,
            Mounted
        }

        private Lazier<Item> itemData;
        private Lazier<SpellData> spellData;

        private readonly InventoryItemKey itemKey;
        private readonly TimeSpan defaultMaxTimeout;
        private readonly bool defaultMountBehavior;
        private readonly int defaultPreUseSleep;
        private readonly int defaultPostUseSleep;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryItem"/> class. 
        /// </summary>
        /// <param name="itemKey">Unique identifier of the item to wrap.</param>
        /// <param name="defaultPreUseSleep">Default number of milliseconds to wait before using the item.</param>
        /// <param name="defaultPostUseSleep">Default number of milliseconds to wait after using the item.</param>
        /// <param name="defaultMaxTimeout">Default timeout for item action to execute.</param>
        /// <param name="defaultMountBehavior">Default mount behavior. Set to true if player should dismount when
        /// using the item action.</param>
        public InventoryItem(InventoryItemKey itemKey, int defaultPreUseSleep = 500, int defaultPostUseSleep = 1500, TimeSpan? defaultMaxTimeout = null, bool defaultMountBehavior = false)
        {
            if (itemKey == null || itemKey.Id == 0) throw new ArgumentException("InventoryItem(itemId) must be defined with an Id greater than 0.");

            this.itemKey = itemKey;
            this.defaultMaxTimeout = defaultMaxTimeout ?? new TimeSpan(0, 0, 0, DEFAULT_TIMEOUT_SECONDS);
            this.defaultMountBehavior = defaultMountBehavior;
            this.defaultPreUseSleep = defaultPreUseSleep;
            this.defaultPostUseSleep = defaultPostUseSleep;

            this.itemData = new Lazier<Item>(GetItem, LazyThreadSafetyMode.ExecutionAndPublication, true);
            this.spellData = new Lazier<SpellData>(GetBackingAction, LazyThreadSafetyMode.ExecutionAndPublication, true);
        }

        /// <summary>
        /// Gets the item specified by this instance of <see cref="InventoryItem"/>.
        /// </summary>
        /// <returns></returns>
        protected Item GetItem()
        {
            return DataManager.GetItem(itemKey.Id, itemKey.Hq);
        }

        /// <summary>
        /// Gets the backing action for the item specified by this instance of <see cref="InventoryItem"/>.
        /// </summary>
        /// <returns></returns>
        protected SpellData GetBackingAction()
        {
            return this.ItemData == null ? null : this.ItemData.BackingAction;
        }

        /// <summary>
        /// Clones this item with a new quality indicator.
        /// </summary>
        /// <param name="hq"></param>
        /// <returns></returns>
        protected InventoryItem Clone(bool hq)
        {
            return new InventoryItem(
                new InventoryItemKey(this.ItemKey.Id, hq),
                this.defaultPreUseSleep,
                this.defaultPostUseSleep,
                this.defaultMaxTimeout,
                this.defaultMountBehavior
            );
        }

        /// <summary>
        /// Searches
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        protected virtual bool GetItemFilter(BagSlot slot)
        {
            return slot.RawItemId == this.itemKey.Id && (slot.IsHighQuality == this.itemKey.Hq);
        }

        /// <summary>
        /// Gets an item from the bags.
        /// </summary>
        /// <param name="index">Index location in the list of all matching items.</param>
        /// <returns></returns>
        public BagSlot GetItemFromBags(int index = 0)
        {
            var baggedItems = GetItemsFromBags();

            if (index == 0)
            {
                return baggedItems.FirstOrDefault();
            }

            var bagSlots = baggedItems.ToArray();
            return bagSlots.Length > index ? bagSlots[index] : null;
        }

        /// <summary>
        /// Gets a list of items from the bags.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BagSlot> GetItemsFromBags()
        {
            return InventoryManager.FilledSlots.Where(this.GetItemFilter);
        }

        /// <summary>
        /// Gets whether the target can use the item.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool CanUse(GameObject target)
        {
            var baggedItem = this.GetItemFromBags();

            return baggedItem != null && baggedItem.CanUse(target);
        }

        /// <summary>
        /// Uses the inventory item.
        /// </summary>
        /// <param name="target">Target of item use action.</param>
        /// <param name="preUseSleep">Milliseconds to wait before using the item.</param>
        /// <param name="postUseSleep">Milliseconds to wait after using the item.</param>
        /// <param name="maxTimeout">Maximum time to wait for coroutine to complete.</param>
        /// <param name="dismount">Player will dismount to use item if set to true.</param>
        /// <returns></returns>
        public virtual async Task<UseResult> Use(GameObject target, int? preUseSleep = null, int? postUseSleep = null, TimeSpan? maxTimeout = null, bool? dismount = null)
        {
            var shouldDismount = dismount ?? defaultMountBehavior;

            if (this.ItemData == null) return UseResult.UnknownItem;
            if (this.SpellData == null) return UseResult.NoUse;
            if (this.Quantity == 0) return UseResult.NoInventory;
            if (this.Cooldown > new TimeSpan(0)) return UseResult.OnCooldown;
            if (Core.Player.IsDead) return UseResult.PlayerDead;
            if (!shouldDismount && ExProfileBehavior.Me.IsMounted) return UseResult.Mounted;

            var baggedItem = this.GetItemFromBags();

            if (!baggedItem.CanUse(target)) return UseResult.CantUse;

            if (!shouldDismount || await Coroutine.Wait(
                    maxTimeout ?? defaultMaxTimeout,
                    () =>
                    {
                        if (!ExProfileBehavior.Me.IsMounted) return true;

                        ActionManager.Dismount();
                        return false;
                    }
                ))
            {
                await Coroutine.Sleep(preUseSleep ?? this.defaultPreUseSleep);
                baggedItem.UseItem(target);
                await Coroutine.Sleep(postUseSleep ?? this.defaultPostUseSleep);
                return UseResult.OK;
            }
            else
            {
                return UseResult.DismountFailed;
            }
        }

        /// <summary>
        /// Returns the normal quality version of this item.
        /// </summary>
        /// <returns></returns>
        public InventoryItem ToNormalQuality()
        {
            if (!this.ItemKey.Hq) return this;

            return this.Clone(false);
        }

        /// <summary>
        /// Returns the high quality version of this item.
        /// </summary>
        /// <returns></returns>
        public InventoryItem ToHighQuality()
        {
            if (this.ItemKey.Hq) return this;

            return this.Clone(true);
        }

        /// <summary>
        /// Gets the item key representing this item.
        /// </summary>
        public InventoryItemKey ItemKey
        {
            get { return this.itemKey; }
        }

        /// <summary>
        /// Attempts to get the ItemData object from the ff14bot DataManager.
        /// </summary>
        public Item ItemData
        {
            get
            {
                return this.itemData.Value;
            }
        }

        /// <summary>
        /// Gets the SpellData from the ff14bot DataManager.
        /// </summary>
        public SpellData SpellData
        {
            get
            {
                return this.spellData.Value;
            }
        }

        /// <summary>
        /// Gets the quantity of the item in the player inventory.
        /// </summary>
        public uint Quantity
        {
            get
            {
                var item = this.GetItemFromBags();
                return item == null ? 0 : item.Item.ItemCount();
            }
        }

        /// <summary>
        /// Gets the cooldown of the item in the player inventory.
        /// </summary>
        public TimeSpan Cooldown
        {
            get
            {
                return this.SpellData == null ? new TimeSpan(0) : this.SpellData.Cooldown;
            }
        }
    }
}

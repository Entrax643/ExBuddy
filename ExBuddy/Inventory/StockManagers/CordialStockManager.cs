using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExBuddy.Inventory.StockManagers
{
    using Enumerations;
    using OrderBotTags.Behaviors;

    /// <summary>
    /// Wraps common functionality to evaluate and use cordial items.
    /// </summary>
    public class CordialStockManager
    {
        /// <summary>
        /// Wrapper for cordial mapping data
        /// </summary>
        public class CordialItemData
        {
            public int Gp;
            public TimeSpan Cooldown;
        }

        /// <summary>
        /// Cached collection of cordial item key to item data maps
        /// </summary>
        public static Dictionary<InventoryItemKey, CordialItemData> CordialDataMap = new Dictionary<InventoryItemKey, CordialItemData>()
        {
            { new InventoryItemKey((uint) CordialType.WateredCordial, false), new CordialItemData() { Gp = 150, Cooldown = new TimeSpan(0, 0, 2, 20) } },
            { new InventoryItemKey((uint) CordialType.WateredCordial, true), new CordialItemData() { Gp = 200, Cooldown = new TimeSpan(0, 0, 2, 6) } },
            { new InventoryItemKey((uint) CordialType.Cordial, false), new CordialItemData() { Gp = 300, Cooldown = new TimeSpan(0, 0, 4, 0) } },
            { new InventoryItemKey((uint) CordialType.Cordial, true), new CordialItemData() { Gp = 350, Cooldown = new TimeSpan(0, 0, 3, 36) } },
            { new InventoryItemKey((uint) CordialType.HiCordial, false), new CordialItemData() { Gp = 400, Cooldown = new TimeSpan(0, 0, 3, 0) } },
        };

        protected Dictionary<InventoryItemKey, InventoryItem> cordialStock = new Dictionary<InventoryItemKey, InventoryItem>(CordialDataMap.Count);

        /// <summary>
        /// Instantiates a new instance of the <see cref="CordialStockManager"/> class.
        /// </summary>
        public CordialStockManager(int defaultPreUseSleep = 500, int defaultPostUseSleep = 1500, TimeSpan? defaultMaxTimeout = null, bool defaultMountBehavior = false)
        {
            foreach (var cordialItem in CordialDataMap.Keys)
            {
                this.cordialStock.Add(cordialItem, new InventoryItem(cordialItem, defaultPreUseSleep, defaultPostUseSleep, defaultMaxTimeout, defaultMountBehavior));
            }
        }

        /// <summary>
        /// Returns true if the player has any cordial in their inventory.
        /// </summary>
        /// <returns></returns>
        public bool HasStock()
        {
            return this.cordialStock.Any(k => k.Value.Quantity > 0);
        }

        /// <summary>
        /// Gets the most appropriate cordial available that gives the player enough GP.
        /// </summary>
        /// <param name="gpNeeded">Amount of GP needed to gather.</param>
        /// <returns></returns>
        public InventoryItem GetFulfillingCordial(int gpNeeded, CordialType cordialType)
        {
            var best = this.cordialStock
                .Where(k => 
                    cordialType != CordialType.None 
                    && (cordialType == CordialType.Auto || k.Key.Id == (uint)cordialType)
                    && k.Value.Quantity > 0 
                    && k.Value.CanUse(ExProfileBehavior.Me)
                    && CordialDataMap[k.Key].Gp >= gpNeeded)
                .OrderBy(k => CordialDataMap[k.Key].Gp)
                .Select(k => k.Value)
                .FirstOrDefault();

            return best;
        }

        /// <summary>
        /// Gets the most appropriate cordial available that gives the player GP without going over the missing GP.
        /// </summary>
        /// <param name="gpMissing"></param>
        /// <param name="cordialType"></param>
        /// <returns></returns>
        public InventoryItem GetBestCordial(int gpMissing, CordialType cordialType)
        {
            var best = this.cordialStock
                .Where(k =>
                    cordialType != CordialType.None
                    && (cordialType == CordialType.Auto || k.Key.Id == (uint) cordialType)
                    && k.Value.Quantity > 0
                    && k.Value.CanUse(ExProfileBehavior.Me)
                    && CordialDataMap[k.Key].Gp <= gpMissing)
                .OrderByDescending(k => CordialDataMap[k.Key].Gp)
                .Select(k => k.Value)
                .FirstOrDefault();

            return best;
        }

        /// <summary>
        /// Gets the largest cordial available to use.
        /// </summary>
        /// <param name="cordialType"></param>
        /// <returns></returns>
        public InventoryItem GetLargestCordial(CordialType cordialType)
        {
            return this.cordialStock
                .Where(k =>
                    cordialType != CordialType.None
                    && (cordialType == CordialType.Auto || k.Key.Id == (uint) cordialType)
                    && k.Value.Quantity > 0
                    && k.Value.CanUse(ExProfileBehavior.Me))
                .OrderByDescending(k => CordialDataMap[k.Key].Gp)
                .Select(k => k.Value)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the current cordial cooldown.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetCordialCooldown()
        {
            return this.cordialStock.First().Value.Cooldown;
        }
    }
}

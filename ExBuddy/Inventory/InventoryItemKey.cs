using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExBuddy.Inventory
{
    using Clio.Utilities;

    public class InventoryItemKey
    {
        public readonly uint Id;
        public readonly bool Hq;

        public InventoryItemKey(uint id, bool hq)
        {
            this.Id = id;
            this.Hq = hq;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Id * 397) ^ Hq.GetHashCode();
            }
        }

        protected bool Equals(InventoryItemKey other)
        {
            return Id == other.Id && Hq == other.Hq;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InventoryItemKey) obj);
        }
    }
}

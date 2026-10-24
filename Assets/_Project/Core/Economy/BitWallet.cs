using System;

namespace BitRunner.Core
{
    public interface IBitWallet
    {
        int Amount { get; }
        bool Add(int amount);
        bool TrySpend(int amount);
    }

    public sealed class MemoryBitWallet : IBitWallet
    {
        private int _amount;

        public MemoryBitWallet(int startingAmount = 0)
        {
            _amount = Math.Max(0, startingAmount);
        }

        public int Amount
        {
            get { return _amount; }
        }

        public bool Add(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            _amount += amount;
            return true;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (_amount < amount)
            {
                return false;
            }

            _amount -= amount;
            return true;
        }
    }

    public sealed class ItemBitWallet : IBitWallet
    {
        public const string BitItemId = "bit";

        private readonly BagInventory _stash;
        private readonly ItemCatalog _catalog;

        public ItemBitWallet(BagInventory stash, ItemCatalog catalog)
        {
            if (stash == null)
            {
                throw new ArgumentNullException(nameof(stash));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _stash = stash;
            _catalog = catalog;
        }

        public int Amount
        {
            get { return _stash.CountOf(BitItemId); }
        }

        public bool Add(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            ItemDefinition definition = _catalog.Get(BitItemId);
            int before = Amount;
            int added = _stash.AddAmount(BitItemId, definition.Shape, amount, definition.MaxStack);
            if (added == amount)
            {
                return true;
            }

            if (added > 0)
            {
                _stash.TryRemoveAmount(BitItemId, added);
            }

            return Amount > before;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return _stash.TryRemoveAmount(BitItemId, amount);
        }
    }
}

using System.Collections.Generic;

namespace BitRunner.Core
{
    public static class ItemDurability
    {
        public static int Current(PlacedItem item, ItemDefinition definition)
        {
            if (item == null || definition == null || definition.DurabilityMax <= 0)
            {
                return 0;
            }

            return item.Durability > 0 ? item.Durability : definition.DurabilityMax;
        }

        public static void EnsureInitialized(PlacedItem item, ItemDefinition definition)
        {
            if (item == null || definition == null || definition.DurabilityMax <= 0)
            {
                return;
            }

            if (item.Durability <= 0)
            {
                item.Durability = definition.DurabilityMax;
            }
        }

        public static int Consume(PlacedItem item, ItemDefinition definition, int amount)
        {
            int current = Current(item, definition);
            if (current <= 0 || amount <= 0)
            {
                return current;
            }

            int next = current - amount;
            if (next < 0)
            {
                next = 0;
            }

            item.Durability = next;
            return next;
        }

        public static void EnsureBag(BagInventory bag, ItemCatalog catalog)
        {
            if (bag == null || catalog == null)
            {
                return;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (catalog.TryGet(items[i].DefinitionId, out definition))
                {
                    EnsureInitialized(items[i], definition);
                }
            }
        }
    }
}

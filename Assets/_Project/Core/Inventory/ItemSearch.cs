using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 递归统计与消耗物品。容器物品（零件盒、武器箱等）里的内容物同样属于玩家资产，
    /// 因此材料统计必须穿透容器：否则「把废料收进零件盒」会导致设施升级判定为材料不足。
    /// 顶层物品优先消耗，容器只在顶层不够时才被动用，尽量避免打乱玩家整理好的容器。
    /// </summary>
    public static class ItemSearch
    {
        /// <summary>容器允许嵌套时的最大穿透层数，防止异常数据造成无限递归。</summary>
        public const int MaxDepth = 4;

        public static int CountOf(BagInventory bag, string definitionId)
        {
            return CountOf(bag, definitionId, 0);
        }

        /// <summary>顶层 + 容器内合计数量。</summary>
        private static int CountOf(BagInventory bag, string definitionId, int depth)
        {
            if (bag == null || string.IsNullOrEmpty(definitionId) || depth > MaxDepth)
            {
                return 0;
            }

            int total = bag.CountOf(definitionId);

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemContainer container = items[i].Container;
                if (container == null || container.Bag == null)
                {
                    continue;
                }

                total += CountOf(container.Bag, definitionId, depth + 1);
            }

            return total;
        }

        public static bool Has(BagInventory bag, string definitionId, int amount)
        {
            return amount <= 0 || CountOf(bag, definitionId) >= amount;
        }

        /// <summary>
        /// 消耗指定数量。必须先满足总量才会真正扣除（失败时背包不变）。
        /// </summary>
        public static bool TryConsume(BagInventory bag, string definitionId, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount == 0)
            {
                return true;
            }

            if (CountOf(bag, definitionId) < amount)
            {
                return false;
            }

            int remaining = amount;
            ConsumeFrom(bag, definitionId, ref remaining, 0);
            return remaining == 0;
        }

        /// <summary>按字典顺序批量消耗，先整体校验再扣，任一不足则完全不扣。</summary>
        public static bool TryConsumeAll(BagInventory bag, IReadOnlyDictionary<string, int> amounts)
        {
            if (amounts == null || amounts.Count == 0)
            {
                return true;
            }

            foreach (KeyValuePair<string, int> pair in amounts)
            {
                if (!Has(bag, pair.Key, pair.Value))
                {
                    return false;
                }
            }

            foreach (KeyValuePair<string, int> pair in amounts)
            {
                TryConsume(bag, pair.Key, pair.Value);
            }

            return true;
        }

        private static void ConsumeFrom(BagInventory bag, string definitionId, ref int remaining, int depth)
        {
            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                PlacedItem item = items[i];
                if (item.DefinitionId != definitionId)
                {
                    continue;
                }

                int take = Math.Min(item.Count, remaining);
                item.Count -= take;
                remaining -= take;
                if (item.Count <= 0)
                {
                    bag.Remove(item.InstanceId);
                }
            }

            if (remaining <= 0 || depth >= MaxDepth)
            {
                return;
            }

            // Items 是快照，上面的删除不会影响这里遍历到的容器列表。
            items = bag.Items;
            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                ItemContainer container = items[i].Container;
                if (container == null || container.Bag == null || container.Bag.Count == 0)
                {
                    continue;
                }

                ConsumeFrom(container.Bag, definitionId, ref remaining, depth + 1);
            }
        }
    }
}

using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 探索阵亡时的战利品清算。
    ///
    /// 规则：只丢弃「本次探索中新获得」的东西。
    /// 出发时就在背包里、阵亡时仍在背包里的物品会原地保留，连同已经消耗掉的耐久一起带回；
    /// 容器本身若是出发时就带着的，容器也保留，但里面新捡到的内容物照丢。
    ///
    /// 这条规则取代了早期「阵亡清空整个背包」的做法，也顺带解决了死亡螺旋：
    /// 玩家永远不会因为一次阵亡而变得无法再出门。
    ///
    /// 实现上用「绑定标记」而不是出发时的实例快照——物品在背包与容器之间移动时
    /// 会被重建成新的 <see cref="PlacedItem"/>，按对象引用比对会把玩家自己的东西误判成战利品。
    /// 标记由 <see cref="ItemTransfer.PreserveAll"/> 随物品一起搬运。
    /// </summary>
    public static class ExpeditionAttrition
    {
        public struct Report
        {
            /// <summary>阵亡后仍留在背包顶层的物品数量。</summary>
            public int Kept;

            /// <summary>被丢弃的物品数量（含容器内的内容物）。</summary>
            public int Lost;

            /// <summary>原本在自己容器里、因容器被丢弃而被迫搬到背包顶层保住的物品数量。</summary>
            public int Rescued;

            /// <summary>抢救失败、只能一起丢掉的物品数量。</summary>
            public int Orphaned;
        }

        /// <summary>出发时调用：把背包里现存的每件物品（含容器内容物）标记为「自带的」。</summary>
        public static int MarkOwned(BagInventory bag)
        {
            return Visit(bag, true, 0);
        }

        /// <summary>探索正常结束后调用，清掉标记，避免下一次探索继承上一趟的结果。</summary>
        public static int ClearMarks(BagInventory bag)
        {
            return Visit(bag, false, 0);
        }

        /// <summary>
        /// 阵亡时执行清算：丢掉所有未标记的物品。
        /// 容器被丢弃前会先把其中属于自带装备的物品抢救出来。
        /// </summary>
        public static Report Apply(BagInventory bag)
        {
            var report = new Report();
            if (bag == null)
            {
                return report;
            }

            var rescued = new List<PlacedItem>();

            // 第一步：只清理「容器内部」。顶层先不动，否则容器连同里面的自带装备
            // 会在抢救之前就被整个丢掉。
            IReadOnlyList<PlacedItem> roots = bag.Items;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i].Container != null)
                {
                    report.Lost += PruneUnbound(roots[i].Container.Bag, rescued);
                }
            }

            // 第二步：丢弃顶层不属于自己的东西。
            roots = bag.Items;
            for (int i = 0; i < roots.Count; i++)
            {
                PlacedItem root = roots[i];
                if (root.ExpeditionBound)
                {
                    continue;
                }

                if (root.Container != null)
                {
                    ExtractBound(root.Container.Bag, rescued);
                }

                bag.Remove(root.InstanceId);
                report.Lost++;
            }

            for (int i = 0; i < rescued.Count; i++)
            {
                if (TryRescue(bag, rescued[i]))
                {
                    report.Rescued++;
                }
                else
                {
                    report.Orphaned++;
                }
            }

            report.Kept = bag.Count;
            return report;
        }

        private static int Visit(BagInventory bag, bool bound, int touched)
        {
            if (bag == null)
            {
                return touched;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                item.ExpeditionBound = bound;
                touched++;
                if (item.Container != null)
                {
                    touched = Visit(item.Container.Bag, bound, touched);
                }
            }

            return touched;
        }

        /// <summary>
        /// 清理一个容器内部：先递归处理嵌套容器，再删掉本层未绑定的内容物。
        /// 只允许作用在容器的内部背包上，绝不能拿去处理顶层背包。
        /// </summary>
        private static int PruneUnbound(BagInventory bag, List<PlacedItem> rescued)
        {
            int removed = 0;
            if (bag == null)
            {
                return removed;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                if (item.Container != null)
                {
                    removed += PruneUnbound(item.Container.Bag, rescued);
                }

                if (item.ExpeditionBound)
                {
                    continue;
                }

                // 现有内容的容器都不允许嵌套，这条分支目前走不到；
                // 留着是为了以后开嵌套时不会静悄悄吞掉玩家自己塞进去的东西。
                if (item.Container != null)
                {
                    ExtractBound(item.Container.Bag, rescued);
                }

                bag.Remove(item.InstanceId);
                removed++;
            }

            return removed;
        }

        private static void ExtractBound(BagInventory bag, List<PlacedItem> rescued)
        {
            if (bag == null)
            {
                return;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                if (item.Container != null)
                {
                    ExtractBound(item.Container.Bag, rescued);
                }

                if (item.ExpeditionBound)
                {
                    bag.Remove(item.InstanceId);
                    rescued.Add(item);
                }
            }
        }

        private static bool TryRescue(BagInventory bag, PlacedItem item)
        {
            PlacedItem fresh;
            if (!bag.TryAdd(item.DefinitionId, item.BaseShape, item.Rotatable, item.Count, out fresh))
            {
                return false;
            }

            ItemTransfer.PreserveAll(item, fresh);
            return true;
        }
    }
}

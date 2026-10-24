namespace BitRunner.Core
{
    public static class ContainerOps
    {
        public static ItemContainer Ensure(PlacedItem item, ItemDefinition definition)
        {
            if (item == null || definition == null || definition.ContainerSpec == null)
            {
                return null;
            }

            if (item.Container == null)
            {
                item.Container = new ItemContainer(definition.ContainerSpec);
            }

            return item.Container;
        }

        public static bool HasContents(PlacedItem item)
        {
            return item != null && item.Container != null && item.Container.Bag.Count > 0;
        }

        public static bool IsEmpty(PlacedItem item)
        {
            return item == null || item.Container == null || item.Container.Bag.Count == 0;
        }

        public static bool TryPut(
            ItemContainer container,
            BagInventory source,
            int instanceId,
            ItemCatalog catalog,
            out string error)
        {
            error = null;
            if (container == null || source == null || catalog == null)
            {
                error = "容器无效。";
                return false;
            }

            if (source == container.Bag)
            {
                error = "物品已经在容器内。";
                return false;
            }

            PlacedItem item;
            if (!source.TryGet(instanceId, out item))
            {
                error = "物品不存在。";
                return false;
            }

            ItemDefinition definition;
            if (!catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "物品定义不存在。";
                return false;
            }

            if (!container.CanAccept(definition))
            {
                error = "该容器不能存放此类物品。";
                return false;
            }

            PlacedItem placed;
            if (!container.Bag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                error = string.IsNullOrEmpty(error) ? "容器空间不足。" : error;
                return false;
            }

            ItemTransfer.PreserveAll(item, placed);
            source.Remove(instanceId);
            error = null;
            return true;
        }

        public static bool TryTake(
            ItemContainer container,
            BagInventory target,
            int instanceId,
            ItemCatalog catalog,
            out string error)
        {
            error = null;
            if (container == null || target == null || catalog == null)
            {
                error = "目标背包无效。";
                return false;
            }

            if (target == container.Bag)
            {
                error = "物品已经在容器内。";
                return false;
            }

            PlacedItem item;
            if (!container.Bag.TryGet(instanceId, out item))
            {
                error = "物品不存在。";
                return false;
            }

            ItemDefinition definition;
            if (!catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "物品定义不存在。";
                return false;
            }

            PlacedItem placed;
            if (!target.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                error = string.IsNullOrEmpty(error) ? "背包空间不足。" : error;
                return false;
            }

            ItemTransfer.PreserveAll(item, placed);
            container.Bag.Remove(instanceId);
            error = null;
            return true;
        }
    }

    public static class ItemTransfer
    {
        private static void PreserveContainer(PlacedItem source, PlacedItem target)
        {
            if (source != null && target != null && source.Container != null)
            {
                target.Container = source.Container;
            }
        }

        /// <summary>
        /// 把源物品的全部运行时状态搬到目标物品上：容器内容物、已消耗的耐久、探索绑定标记。
        /// 只搬容器会让「挪一下背包」把耐久重置回满，等于免费修理；
        /// 漏搬绑定标记则会把自己带上船的装备判成战利品。
        /// </summary>
        public static void PreserveAll(PlacedItem source, PlacedItem target)
        {
            if (source == null || target == null)
            {
                return;
            }

            PreserveContainer(source, target);
            target.Durability = source.Durability;
            target.ExpeditionBound = source.ExpeditionBound;
        }
    }
}

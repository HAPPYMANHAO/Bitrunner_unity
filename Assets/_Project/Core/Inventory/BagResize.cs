using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 把背包换成一个新尺寸的背包，同时把每件物品的运行时状态（容器内容物、耐久、
    /// 探索绑定标记）搬过去。
    ///
    /// 设施升级（潜网舱 / 制作站 / 中央处理器 / 保险中枢 / 仓储）全部走这里。
    /// 之所以不能只按格子重建：<see cref="BagInventory.TryAddAt"/> 会造一个新的
    /// <see cref="PlacedItem"/>，容器里的东西和耐久都会凭空消失——那就是「升级一次
    /// 白送一次修理 + 清空容器」。
    /// </summary>
    public static class BagResize
    {
        public static BagInventory To(BagInventory source, int width, int height)
        {
            var result = new BagInventory(width, height);
            if (source == null)
            {
                return result;
            }

            var items = new List<PlacedItem>(source.Items);
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                PlacedItem placed;
                string error;

                // 先按原位置放；放不下（新格子更小或形状变化）就退化成自动找空位。
                if (result.TryAddAt(
                        item.DefinitionId,
                        item.BaseShape,
                        item.Origin,
                        item.Rotation,
                        item.Rotatable,
                        item.Count,
                        out placed,
                        out error))
                {
                    ItemTransfer.PreserveAll(item, placed);
                    continue;
                }

                if (result.TryAdd(item.DefinitionId, item.BaseShape, item.Rotatable, item.Count, out placed))
                {
                    ItemTransfer.PreserveAll(item, placed);
                }
            }

            return result;
        }
    }
}

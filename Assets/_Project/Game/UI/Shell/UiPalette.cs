using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;

namespace BitRunner.Game
{
    /// <summary>
    /// 物品按分类着色。原先 Base / Expedition / Combat 各有一份完全相同的实现，
    /// 且基地那份多覆盖了 Quest / Uplink 两类（另两份落到 default 灰）。
    /// 这里以基地那份为准——它是超集，另两份是漏更新的副本。
    /// </summary>
    public static class UiPalette
    {
        public static Dictionary<string, Color> BuildPalette(ItemCatalog catalog)
        {
            var palette = new Dictionary<string, Color>();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                palette[definition.Id] = ColorFor(definition.Category);
            }

            return palette;
        }

        public static Color ColorFor(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon:
                    return new Color32(232, 110, 64, 255);
                case ItemCategory.Armor:
                    return new Color32(80, 140, 220, 255);
                case ItemCategory.Tool:
                    return new Color32(79, 184, 200, 255);
                case ItemCategory.Consumable:
                    return new Color32(90, 200, 130, 255);
                case ItemCategory.Loot:
                    return new Color32(220, 200, 70, 255);
                case ItemCategory.Material:
                    return new Color32(150, 128, 100, 255);
                case ItemCategory.Key:
                    return new Color32(190, 120, 235, 255);
                case ItemCategory.Currency:
                    return new Color32(230, 196, 70, 255);
                case ItemCategory.Ammo:
                    return new Color32(200, 170, 120, 255);
                case ItemCategory.Container:
                    return new Color32(150, 118, 82, 255);

                // 契约用近白的羊皮纸色：它既不是战利品也不是材料，
                // 一眼要在柜台/背包里被认出来。
                case ItemCategory.Quest:
                    return new Color32(226, 214, 186, 255);

                // 上行链路刻意不用 Key 的紫色：它们是两种完全不同的东西，
                // 同色会让人以为「通讯链路能开潜网舱」。
                case ItemCategory.Uplink:
                    return new Color32(226, 118, 164, 255);
                default:
                    return new Color32(140, 150, 160, 255);
            }
        }
    }
}

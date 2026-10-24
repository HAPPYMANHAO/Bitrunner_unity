using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public static class FacilityUpgrade
    {
        public static bool CanAfford(
            CampaignState campaign,
            BagInventory stash,
            int bitCost,
            string[] materialIds,
            out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (stash == null)
            {
                throw new ArgumentNullException(nameof(stash));
            }

            if (campaign.Bit < bitCost)
            {
                error = "bit 不足，需要 " + bitCost + "。";
                return false;
            }

            if (!HasMaterials(stash, materialIds))
            {
                error = "材料不足。";
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryPay(
            CampaignState campaign,
            BagInventory stash,
            int bitCost,
            string[] materialIds,
            out string error)
        {
            if (!CanAfford(campaign, stash, bitCost, materialIds, out error))
            {
                return false;
            }

            if (!campaign.TrySpend(bitCost))
            {
                error = "bit 不足。";
                return false;
            }

            if (!RemoveMaterials(stash, materialIds))
            {
                campaign.AddBit(bitCost);
                error = "材料不足。";
                return false;
            }

            error = null;
            return true;
        }

        public static string RequirementText(ItemCatalog catalog, int bitCost, string[] materialIds)
        {
            return RequirementText(catalog, null, bitCost, materialIds);
        }

        /// <summary>
        /// 生成需求文案；传入 stash 时会额外标出「现有 / 所需」，方便玩家看出差多少。
        /// 现有数量包含仓库容器（零件盒等）里的内容物。
        /// </summary>
        public static string RequirementText(ItemCatalog catalog, BagInventory stash, int bitCost, string[] materialIds)
        {
            string text = bitCost + " bit";
            if (catalog == null || materialIds == null || materialIds.Length == 0)
            {
                return text;
            }

            var counts = Aggregate(materialIds);
            var ids = new List<string>(counts.Keys);
            ids.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                ItemDefinition definition;
                string name = catalog.TryGet(ids[i], out definition) ? definition.DisplayName : ids[i];
                text += "  " + name + "×" + counts[ids[i]];

                if (stash != null)
                {
                    int have = ItemSearch.CountOf(stash, ids[i]);
                    if (have < counts[ids[i]])
                    {
                        text += "(有 " + have + ")";
                    }
                }
            }

            return text;
        }

        private static Dictionary<string, int> Aggregate(string[] materialIds)
        {
            var counts = new Dictionary<string, int>();
            if (materialIds == null)
            {
                return counts;
            }

            for (int i = 0; i < materialIds.Length; i++)
            {
                string id = materialIds[i];
                int count;
                counts.TryGetValue(id, out count);
                counts[id] = count + 1;
            }

            return counts;
        }

        private static bool HasMaterials(BagInventory stash, string[] materialIds)
        {
            foreach (KeyValuePair<string, int> pair in Aggregate(materialIds))
            {
                if (ItemSearch.CountOf(stash, pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RemoveMaterials(BagInventory stash, string[] materialIds)
        {
            // 顶层优先，顶层不够才动容器；整体校验通过后才会扣除。
            return ItemSearch.TryConsumeAll(stash, Aggregate(materialIds));
        }
    }
}

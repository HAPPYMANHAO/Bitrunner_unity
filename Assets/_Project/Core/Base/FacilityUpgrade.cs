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
            string text = bitCost + " bit";
            if (catalog == null || materialIds == null || materialIds.Length == 0)
            {
                return text;
            }

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < materialIds.Length; i++)
            {
                string id = materialIds[i];
                int count;
                counts.TryGetValue(id, out count);
                counts[id] = count + 1;
            }

            var ids = new List<string>(counts.Keys);
            ids.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ids.Count; i++)
            {
                ItemDefinition definition;
                string name = catalog.TryGet(ids[i], out definition) ? definition.DisplayName : ids[i];
                text += "  " + name + "×" + counts[ids[i]];
            }

            return text;
        }

        private static bool HasMaterials(BagInventory stash, string[] materialIds)
        {
            var counts = new Dictionary<string, int>();
            if (materialIds != null)
            {
                for (int i = 0; i < materialIds.Length; i++)
                {
                    int count;
                    counts.TryGetValue(materialIds[i], out count);
                    counts[materialIds[i]] = count + 1;
                }
            }

            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (stash.CountOf(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RemoveMaterials(BagInventory stash, string[] materialIds)
        {
            if (materialIds == null)
            {
                return true;
            }

            for (int i = 0; i < materialIds.Length; i++)
            {
                if (!stash.TryRemoveAmount(materialIds[i], 1))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

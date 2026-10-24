using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class DivePlan
    {
        public string KeyItemId;
        public string Name;
        public int Width;
        public int Height;
        public int EnemyCount;
        public int LootCount;
        public int HazardCount;
        public int SeedOffset;
        public string[] LootPool;
        public string[] EnemyPool;
        public string[] ConsumeItemIds;
        public bool ConsumeKey;
    }

    public sealed class DiveKeyDefinition
    {
        public string ItemId;
        public string DungeonName;
        public bool Permanent = true;
        public int Width = 7;
        public int Height = 7;
        public int EnemyCount = 5;
        public int LootCount = 9;
        public int HazardCount = 6;
        public int SeedOffset;
        public string[] LootPool;
        public string[] EnemyPool;
    }

    public sealed class DiveRecipe
    {
        public string Id;
        public string Name;
        public string KeyItemId;
        public string[] RequiredItemIds;
        public string[] ConsumeItemIds;
        public int EnemyCount;
        public int LootCount;
        public int HazardCount;
        public int SeedOffset;
        public string[] LootPool;
        public string[] EnemyPool;
    }

    public static class DiveKeyLibrary
    {
        private static readonly string[] BaseLootPool =
        {
            "coin", "patch", "scrap_blade", "buckler", "jammer", "chip", "idol", "rod",
            "heat_suit", "filter", "respirator", "toxin_dart", "vacuum",
            "scrap", "wire", "circuit", "alloy", "ammo",
            "overclock_core", "coolant_loop", "targeting_array", "ram_piston",
            "corrosive_dart", "fence_module", "parts_case", "med_pouch"
        };

        private static readonly string[] RichLootPool =
        {
            "coin", "patch", "scrap_blade", "buckler", "jammer", "chip", "idol", "rod",
            "heat_suit", "filter", "respirator", "toxin_dart", "vacuum",
            "scrap", "wire", "circuit", "alloy", "ammo", "dive_military", "dive_signal",
            "overclock_core", "coolant_loop", "targeting_array", "ram_piston",
            "corrosive_dart", "fence_module", "parts_case", "med_pouch"
        };

        private static readonly string[] WildLootPool =
        {
            "stone", "branch", "junk", "scrap", "ammo"
        };

        private static readonly string[] WildEnemyPool =
        {
            "wild_dog", "squirrel"
        };

        private static readonly string[] StandardEnemyPool =
        {
            "scavenger", "enforcer", "drone", "bulwark", "shocker",
            "hacker", "poisoner", "suffocator", "raider"
        };

        private static readonly DiveKeyDefinition[] Definitions =
        {
            new DiveKeyDefinition
            {
                ItemId = "dive_wild",
                DungeonName = "旷野",
                Permanent = true,
                Width = 5,
                Height = 5,
                EnemyCount = 2,
                LootCount = 4,
                HazardCount = 1,
                SeedOffset = 19,
                LootPool = WildLootPool,
                EnemyPool = WildEnemyPool
            },
            new DiveKeyDefinition
            {
                ItemId = "dive_basic",
                DungeonName = "基础潜网",
                Permanent = true,
                Width = 7,
                Height = 7,
                EnemyCount = 5,
                LootCount = 9,
                HazardCount = 6,
                LootPool = BaseLootPool,
                EnemyPool = StandardEnemyPool
            },
            new DiveKeyDefinition
            {
                ItemId = "dive_military",
                DungeonName = "军用潜网",
                Permanent = true,
                Width = 7,
                Height = 7,
                EnemyCount = 7,
                LootCount = 10,
                HazardCount = 7,
                SeedOffset = 131,
                LootPool = RichLootPool,
                EnemyPool = StandardEnemyPool
            },
            new DiveKeyDefinition
            {
                ItemId = "dive_signal",
                DungeonName = "信号潜网",
                Permanent = true,
                Width = 7,
                Height = 7,
                EnemyCount = 6,
                LootCount = 12,
                HazardCount = 8,
                SeedOffset = 257,
                LootPool = RichLootPool,
                EnemyPool = StandardEnemyPool
            }
        };

        private static readonly DiveRecipe[] Recipes =
        {
            new DiveRecipe
            {
                Id = "overclock",
                Name = "超频潜网",
                KeyItemId = "dive_basic",
                RequiredItemIds = new[] { "chip" },
                ConsumeItemIds = new[] { "chip" },
                EnemyCount = 7,
                LootCount = 11,
                HazardCount = 6,
                SeedOffset = 37,
                LootPool = RichLootPool
            },
            new DiveRecipe
            {
                Id = "data_vault",
                Name = "数据金库",
                KeyItemId = "dive_basic",
                RequiredItemIds = new[] { "idol" },
                ConsumeItemIds = new[] { "idol" },
                EnemyCount = 3,
                LootCount = 14,
                HazardCount = 5,
                SeedOffset = 73,
                LootPool = RichLootPool
            },
            new DiveRecipe
            {
                Id = "toxic_net",
                Name = "毒雾潜网",
                KeyItemId = "dive_military",
                RequiredItemIds = new[] { "filter" },
                ConsumeItemIds = new[] { "filter" },
                EnemyCount = 6,
                LootCount = 10,
                HazardCount = 9,
                SeedOffset = 109,
                LootPool = RichLootPool
            },
            new DiveRecipe
            {
                Id = "deep_net",
                Name = "深网",
                KeyItemId = "dive_signal",
                RequiredItemIds = new[] { "circuit" },
                ConsumeItemIds = new[] { "circuit" },
                EnemyCount = 8,
                LootCount = 12,
                HazardCount = 8,
                SeedOffset = 149,
                LootPool = RichLootPool
            }
        };

        public static IReadOnlyList<string> BaseLoot
        {
            get { return BaseLootPool; }
        }

        public static bool TryGetKey(string itemId, out DiveKeyDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].ItemId == itemId)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static bool TryResolve(
            string keyItemId,
            IReadOnlyList<string> itemIds,
            out DivePlan plan,
            out string error)
        {
            plan = null;

            DiveKeyDefinition key;
            if (!TryGetKey(keyItemId, out key))
            {
                error = "未知潜网密钥。";
                return false;
            }

            plan = new DivePlan
            {
                KeyItemId = key.ItemId,
                Name = key.DungeonName,
                Width = key.Width,
                Height = key.Height,
                EnemyCount = key.EnemyCount,
                LootCount = key.LootCount,
                HazardCount = key.HazardCount,
                SeedOffset = key.SeedOffset,
                LootPool = key.LootPool,
                EnemyPool = key.EnemyPool,
                ConsumeItemIds = new string[0],
                ConsumeKey = !key.Permanent
            };

            DiveRecipe best = null;
            int bestCount = -1;
            for (int i = 0; i < Recipes.Length; i++)
            {
                DiveRecipe recipe = Recipes[i];
                if (recipe.KeyItemId != keyItemId)
                {
                    continue;
                }

                if (!ContainsAll(itemIds, recipe.RequiredItemIds))
                {
                    continue;
                }

                int count = recipe.RequiredItemIds == null ? 0 : recipe.RequiredItemIds.Length;
                if (count > bestCount)
                {
                    best = recipe;
                    bestCount = count;
                }
            }

            if (best != null)
            {
                plan.Name = best.Name;
                plan.EnemyCount = best.EnemyCount;
                plan.LootCount = best.LootCount;
                plan.HazardCount = best.HazardCount;
                plan.SeedOffset = best.SeedOffset;
                plan.LootPool = best.LootPool;
                if (best.EnemyPool != null && best.EnemyPool.Length > 0)
                {
                    plan.EnemyPool = best.EnemyPool;
                }

                plan.ConsumeItemIds = best.ConsumeItemIds ?? new string[0];
            }

            error = null;
            return true;
        }

        private static bool ContainsAll(IReadOnlyList<string> available, string[] required)
        {
            if (required == null || required.Length == 0)
            {
                return true;
            }

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < available.Count; i++)
            {
                string id = available[i];
                if (id == null)
                {
                    continue;
                }

                int count;
                counts.TryGetValue(id, out count);
                counts[id] = count + 1;
            }

            for (int i = 0; i < required.Length; i++)
            {
                string id = required[i];
                int count;
                if (!counts.TryGetValue(id, out count) || count <= 0)
                {
                    return false;
                }

                counts[id] = count - 1;
            }

            return true;
        }
    }
}

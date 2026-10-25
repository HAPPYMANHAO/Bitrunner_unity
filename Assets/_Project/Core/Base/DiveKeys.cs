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
        public int StageCount = 3;
        public int SeedOffset;
        public string[] LootPool;
        public string[] EnemyPool;
        public string[] ConsumeItemIds;
        public bool ConsumeKey;

        /// <summary>本地点掉落的最低档位，1 表示不限。</summary>
        public int TierMin = 1;

        /// <summary>档内比例，长度 = 档位跨度的档数，从 <see cref="TierMin"/> 往上排。</summary>
        public int[] TierWeights;
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
        public int StageCount = 3;
        public int SeedOffset;
        public string[] LootPool;
        public string[] EnemyPool;
        public int TierMin = 1;
        public int[] TierWeights;
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
        public int TierMin = 1;
        public int[] TierWeights;
    }

    public static class DiveKeyLibrary
    {
        private static readonly string[] BaseLootPool =
        {
            "coin", "patch", "scrap_blade", "buckler", "jammer", "chip", "idol", "rod",
            "heat_suit", "filter", "respirator", "toxin_dart", "vacuum",
            "scrap", "wire", "circuit", "alloy", "ammo",
            "overclock_core", "coolant_loop", "targeting_array", "ram_piston",
            "corrosive_dart", "fence_module", "parts_case", "med_pouch",
            "key_box", "servo", "capacitor", "coolant_tank"
        };

        /// <summary>
        /// 军用及以上地点共用的全量掉落池。**T4/T5 的内容全放在这里**：
        /// 具体某个地点能掉哪几档由 `DiveKeyDefinition.TierMin` + `TierWeights` 过滤，
        /// 不需要为每个地点再抄一份池子。
        /// </summary>
        private static readonly string[] RichLootPool =
        {
            "coin", "patch", "scrap_blade", "buckler", "jammer", "chip", "idol", "rod",
            "heat_suit", "filter", "respirator", "toxin_dart", "vacuum",
            "scrap", "wire", "circuit", "alloy", "ammo", "dive_military", "dive_signal",
            "overclock_core", "coolant_loop", "targeting_array", "ram_piston",
            "corrosive_dart", "fence_module", "parts_case", "med_pouch",
            "key_box", "servo", "capacitor", "coolant_tank",
            "superalloy", "optic_fiber", "plasma_cell", "dive_deep",
            "singularity_core", "monopole_wire", "corona_shard", "dive_core",
            "rail_lance", "plasma_cutter", "flechette_gun", "reactor_maul",
            "reactive_plating", "ablative_shell", "hazmat_rig",
            "nano_stitcher", "overdrive_relay", "targeting_lattice",
            "polymer_dagger", "singularity_hammer", "arc_reaver", "void_needler",
            "corona_aegis", "phase_weave", "void_suit",
            "full_repair_kit", "null_field_generator", "plasma_furnace"
        };

        private static readonly string[] WildLootPool =
        {
            "stone", "branch", "junk", "scrap", "ammo"
        };

        private static readonly string[] WildEnemyPool =
        {
            "wild_dog", "squirrel"
        };

        /// <summary>T1–T2 的敌人。基础潜网用它，保证开局第一层不会撞上重装守卫以外的硬骨头。</summary>
        private static readonly string[] BasicEnemyPool =
        {
            "scavenger", "enforcer", "drone", "bulwark", "shocker",
            "hacker", "poisoner", "suffocator", "raider"
        };

        /// <summary>T1–T3。军用与毒雾带 T2 新兵，前半段仍然打得动。</summary>
        private static readonly string[] MilitaryEnemyPool =
        {
            "scavenger", "enforcer", "drone", "bulwark", "shocker",
            "hacker", "poisoner", "suffocator", "raider",
            "ripper", "stalker_drone", "gas_hound", "bruiser",
            "arc_trooper", "warden", "corroder", "piston_brute"
        };

        /// <summary>T3–T4。信号与数据金库不该再刷野犬，改用中高档的专职单位。</summary>
        private static readonly string[] SignalEnemyPool =
        {
            "arc_trooper", "warden", "corroder", "piston_brute",
            "rail_sniper", "plasma_guard", "flechette_trooper", "hazmat_executor"
        };

        /// <summary>T4–T5。深空潜网的门槛单位。</summary>
        private static readonly string[] DeepEnemyPool =
        {
            "rail_sniper", "plasma_guard", "flechette_trooper", "hazmat_executor",
            "arc_reaver_unit", "void_stalker", "singularity_guard", "polymer_assassin"
        };

        /// <summary>T5 + Boss。核心潜网是终点，日冕战斗机甲在这里等待。</summary>
        private static readonly string[] CoreEnemyPool =
        {
            "arc_reaver_unit", "void_stalker", "singularity_guard", "polymer_assassin",
            "corona_mech"
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
                StageCount = 4,
                SeedOffset = 19,
                LootPool = WildLootPool,
                EnemyPool = WildEnemyPool,
                TierMin = 1,
                TierWeights = new[] { 100 }
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
                StageCount = 5,
                LootPool = BaseLootPool,
                EnemyPool = BasicEnemyPool,
                TierMin = 1,
                TierWeights = new[] { 40, 40, 20 }
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
                StageCount = 5,
                SeedOffset = 131,
                LootPool = RichLootPool,
                EnemyPool = MilitaryEnemyPool,
                TierMin = 2,
                TierWeights = new[] { 50, 35, 15 }
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
                StageCount = 5,
                SeedOffset = 257,
                LootPool = RichLootPool,
                EnemyPool = SignalEnemyPool,
                TierMin = 3,
                TierWeights = new[] { 70, 30 }
            },
            new DiveKeyDefinition
            {
                ItemId = "dive_deep",
                DungeonName = "深空潜网",
                Permanent = true,
                Width = 8,
                Height = 8,
                EnemyCount = 9,
                LootCount = 15,
                HazardCount = 10,
                StageCount = 6,
                SeedOffset = 331,
                LootPool = RichLootPool,
                EnemyPool = DeepEnemyPool,
                TierMin = 4,
                TierWeights = new[] { 60, 40 }
            },
            new DiveKeyDefinition
            {
                ItemId = "dive_core",
                DungeonName = "核心潜网",
                Permanent = true,
                Width = 8,
                Height = 8,
                EnemyCount = 11,
                LootCount = 18,
                HazardCount = 12,
                StageCount = 6,
                SeedOffset = 419,
                LootPool = RichLootPool,
                EnemyPool = CoreEnemyPool,
                TierMin = 5,
                TierWeights = new[] { 100 }
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
                LootPool = RichLootPool,
                TierMin = 2,
                TierWeights = new[] { 50, 50 }
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
                LootPool = RichLootPool,
                TierMin = 3,
                TierWeights = new[] { 70, 30 }
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
                LootPool = RichLootPool,
                TierMin = 2,
                TierWeights = new[] { 50, 35, 15 }
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
                LootPool = RichLootPool,
                TierMin = 3,
                TierWeights = new[] { 30, 50, 20 },
                EnemyPool = DeepEnemyPool
            },
            new DiveRecipe
            {
                Id = "core_breach",
                Name = "核心突入",
                KeyItemId = "dive_core",
                RequiredItemIds = new[] { "singularity_core" },
                ConsumeItemIds = new[] { "singularity_core" },
                EnemyCount = 13,
                LootCount = 20,
                HazardCount = 14,
                SeedOffset = 461,
                LootPool = RichLootPool,
                TierMin = 5,
                TierWeights = new[] { 100 },
                EnemyPool = CoreEnemyPool
            }
        };

        public static IReadOnlyList<string> BaseLoot
        {
            get { return BaseLootPool; }
        }

        /// <summary>全部地点（不含配方的变体），供 UI 与测试枚举。</summary>
        public static IReadOnlyList<DiveKeyDefinition> All
        {
            get { return Definitions; }
        }

        /// <summary>全部配方变体（超频潜网、数据金库……），同样供枚举。</summary>
        public static IReadOnlyList<DiveRecipe> AllRecipes
        {
            get { return Recipes; }
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
                StageCount = key.StageCount,
                SeedOffset = key.SeedOffset,
                LootPool = key.LootPool,
                EnemyPool = key.EnemyPool,
                ConsumeItemIds = new string[0],
                ConsumeKey = !key.Permanent,
                TierMin = key.TierMin,
                TierWeights = key.TierWeights
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
                if (best.TierWeights != null && best.TierWeights.Length > 0)
                {
                    plan.TierMin = best.TierMin;
                    plan.TierWeights = best.TierWeights;
                }
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

using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class CraftRecipe
    {
        public string Id;
        public string Name;
        public string[] RequiredItemIds;
        public string OutputItemId;

        /// <summary>
        /// 需要制作站达到的等级。制作站是第三道 tier 门（前两道是地点与商店）：
        /// 材料齐了但站不够高，配方不会匹配出来，UI 会提示差几级。
        /// </summary>
        public int MinStationLevel = 1;
    }

    public static class CraftRecipeLibrary
    {
        private static readonly CraftRecipe[] Recipes =
        {
            new CraftRecipe
            {
                Id = "scrap_blade",
                Name = "废铁刃",
                RequiredItemIds = new[] { "scrap", "scrap" },
                OutputItemId = "scrap_blade"
            },
            new CraftRecipe
            {
                Id = "patch",
                Name = "修复包",
                RequiredItemIds = new[] { "scrap", "wire" },
                OutputItemId = "patch"
            },
            new CraftRecipe
            {
                Id = "pulse",
                Name = "脉冲发射器",
                RequiredItemIds = new[] { "scrap", "scrap", "circuit" },
                OutputItemId = "pulse"
            },
            new CraftRecipe
            {
                Id = "buckler",
                Name = "偏导器",
                RequiredItemIds = new[] { "alloy", "wire" },
                OutputItemId = "buckler"
            },
            new CraftRecipe
            {
                Id = "plate",
                Name = "装甲板",
                RequiredItemIds = new[] { "alloy", "circuit", "scrap" },
                OutputItemId = "plate",
                MinStationLevel = 2
            },
            new CraftRecipe
            {
                Id = "dive_signal",
                Name = "信号潜网密钥",
                RequiredItemIds = new[] { "circuit", "idol" },
                OutputItemId = "dive_signal",
                MinStationLevel = 2
            },
            new CraftRecipe
            {
                Id = "key_box",
                Name = "密钥盒",
                RequiredItemIds = new[] { "alloy", "scrap", "scrap" },
                OutputItemId = "key_box",
                MinStationLevel = 2
            },

            // ---------- T4（制作站 Lv.4）----------
            new CraftRecipe
            {
                Id = "plasma_cutter",
                Name = "等离子切割器",
                RequiredItemIds = new[] { "superalloy", "plasma_cell" },
                OutputItemId = "plasma_cutter",
                MinStationLevel = 4
            },
            new CraftRecipe
            {
                Id = "rail_lance",
                Name = "磁轨长矛",
                RequiredItemIds = new[] { "superalloy", "superalloy", "plasma_cell" },
                OutputItemId = "rail_lance",
                MinStationLevel = 4
            },
            new CraftRecipe
            {
                Id = "reactive_plating",
                Name = "反应装甲",
                RequiredItemIds = new[] { "servo", "alloy", "alloy" },
                OutputItemId = "reactive_plating",
                MinStationLevel = 4
            },
            new CraftRecipe
            {
                Id = "nano_stitcher",
                Name = "纳米缝合器",
                RequiredItemIds = new[] { "optic_fiber", "optic_fiber", "patch" },
                OutputItemId = "nano_stitcher",
                MinStationLevel = 4
            },
            new CraftRecipe
            {
                Id = "dive_deep",
                Name = "深空潜网密钥",
                RequiredItemIds = new[] { "superalloy", "optic_fiber", "optic_fiber", "plasma_cell" },
                OutputItemId = "dive_deep",
                MinStationLevel = 4
            },

            // ---------- T5（制作站 Lv.5）----------
            new CraftRecipe
            {
                Id = "polymer_dagger",
                Name = "高分子匕首",
                RequiredItemIds = new[] { "singularity_core", "plasma_cell" },
                OutputItemId = "polymer_dagger",
                MinStationLevel = 5
            },
            new CraftRecipe
            {
                Id = "corona_aegis",
                Name = "日冕护盾甲",
                RequiredItemIds = new[] { "superalloy", "superalloy", "corona_shard" },
                OutputItemId = "corona_aegis",
                MinStationLevel = 5
            },
            new CraftRecipe
            {
                Id = "full_repair_kit",
                Name = "全修套装",
                RequiredItemIds = new[] { "optic_fiber", "optic_fiber", "corona_shard", "coolant_tank" },
                OutputItemId = "full_repair_kit",
                MinStationLevel = 5
            },
            new CraftRecipe
            {
                Id = "dive_core",
                Name = "核心潜网密钥",
                RequiredItemIds = new[] { "singularity_core", "monopole_wire", "corona_shard", "plasma_cell" },
                OutputItemId = "dive_core",
                MinStationLevel = 5
            }
        };

        public static IReadOnlyList<CraftRecipe> All
        {
            get { return Recipes; }
        }

        /// <summary>不限制作站等级的匹配。仅供测试与「差几级」提示使用。</summary>
        public static CraftRecipe Match(IReadOnlyList<PlacedItem> items)
        {
            return Match(items, int.MaxValue);
        }

        /// <summary>
        /// 匹配配方：先按制作站等级过滤，再取「需要的材料件数最多」的那条。
        /// 件数多者优先，是为了让「同一种材料多放一件 = 换一个产物」成立。
        /// 件数相同时按声明顺序，因此新增配方要避开「件数相同且材料集可同时满足」的组合。
        /// </summary>
        public static CraftRecipe Match(IReadOnlyList<PlacedItem> items, int stationLevel)
        {
            CraftRecipe best = null;
            int bestCount = -1;
            for (int i = 0; i < Recipes.Length; i++)
            {
                CraftRecipe recipe = Recipes[i];
                if (recipe.MinStationLevel > stationLevel)
                {
                    continue;
                }

                if (!ContainsAll(items, recipe.RequiredItemIds))
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

            return best;
        }

        /// <summary>忽略等级时能匹配到的配方。UI 用它把「材料够了但站不够」和「材料不够」区分开。</summary>
        public static CraftRecipe LockedMatch(IReadOnlyList<PlacedItem> items, int stationLevel)
        {
            CraftRecipe best = null;
            int bestCount = -1;
            for (int i = 0; i < Recipes.Length; i++)
            {
                CraftRecipe recipe = Recipes[i];
                if (recipe.MinStationLevel <= stationLevel)
                {
                    continue;
                }

                if (!ContainsAll(items, recipe.RequiredItemIds))
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

            return best;
        }

        private static bool ContainsAll(IReadOnlyList<PlacedItem> items, string[] required)
        {
            if (required == null || required.Length == 0)
            {
                return true;
            }

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < items.Count; i++)
            {
                string id = items[i].DefinitionId;
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

    public sealed class CraftingStation
    {
        public const int MaxLevel = 5;

        public int Level { get; private set; }
        public BagInventory Grid { get; private set; }

        public CraftingStation()
        {
            Level = 1;
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
        }

        internal void RestoreLevel(int level)
        {
            Level = level < 1 ? 1 : (level > MaxLevel ? MaxLevel : level);
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
        }

        internal void RestoreGrid(BagInventory grid)
        {
            if (grid != null)
            {
                Grid = grid;
            }
        }

        /// <summary>工作台宽度：2 / 3 / 3 / 4 / 5。宽度只增不减，方便一次摆更多材料。</summary>
        public static int WidthForLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 2;
                case 2:
                case 3:
                    return 3;
                case 4:
                    return 4;
                default:
                    return 5;
            }
        }

        /// <summary>高度封顶 3 行，理由同潜网舱：右侧面板只有 138px。</summary>
        public static int HeightForLevel(int level)
        {
            return level <= 2 ? 2 : 3;
        }

        public int Width
        {
            get { return WidthForLevel(Level); }
        }

        public int Height
        {
            get { return HeightForLevel(Level); }
        }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        public int UpgradeCost
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return 150;
                    case 2:
                        return 350;
                    case 3:
                        return 600;
                    case 4:
                        return 1200;
                    default:
                        return 0;
                }
            }
        }

        public CraftRecipe MatchingRecipe
        {
            get { return CraftRecipeLibrary.Match(Grid.Items, Level); }
        }

        /// <summary>材料够了但制作站等级不够的配方。UI 用它显示「需要制作站 Lv.N」。</summary>
        public CraftRecipe LockedRecipe
        {
            get { return CraftRecipeLibrary.LockedMatch(Grid.Items, Level); }
        }

        public bool TryCraft(ItemCatalog catalog, BagInventory stash, out PlacedItem placed, out string error)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (stash == null)
            {
                throw new ArgumentNullException(nameof(stash));
            }

            placed = null;
            CraftRecipe recipe = CraftRecipeLibrary.Match(Grid.Items, Level);
            if (recipe == null)
            {
                CraftRecipe locked = CraftRecipeLibrary.LockedMatch(Grid.Items, Level);
                error = locked != null
                    ? "需要制作站 Lv." + locked.MinStationLevel + " 才能制作" + locked.Name + "。"
                    : "没有匹配的配方。";
                return false;
            }

            ItemDefinition output = catalog.Get(recipe.OutputItemId);
            if (!stash.TryAdd(output.Id, output.Shape, out placed))
            {
                error = "仓库空间不足。";
                return false;
            }

            for (int i = 0; i < recipe.RequiredItemIds.Length; i++)
            {
                if (!RemoveOne(recipe.RequiredItemIds[i]))
                {
                    stash.Remove(placed.InstanceId);
                    placed = null;
                    error = "材料不足。";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public string[] UpgradeMaterialIds
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return new[] { "scrap", "wire" };
                    case 2:
                        return new[] { "alloy", "circuit", "circuit" };
                    case 3:
                        return new[] { "circuit", "servo", "capacitor" };
                    case 4:
                        return new[] { "superalloy", "plasma_cell", "optic_fiber" };
                    default:
                        return new string[0];
                }
            }
        }

        public bool TryUpgrade(CampaignState campaign, BagInventory stash, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (Level >= MaxLevel)
            {
                error = "制作站已满级。";
                return false;
            }

            if (!FacilityUpgrade.TryPay(campaign, stash, UpgradeCost, UpgradeMaterialIds, out error))
            {
                return false;
            }

            int nextLevel = Level + 1;
            Grid = FacilityGrid.Resize(Grid, WidthForLevel(nextLevel), HeightForLevel(nextLevel));
            Level = nextLevel;
            error = null;
            return true;
        }

        private bool RemoveOne(string definitionId)
        {
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    return Grid.Remove(items[i].InstanceId);
                }
            }

            return false;
        }
    }
}

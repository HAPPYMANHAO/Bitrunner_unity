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
                OutputItemId = "plate"
            },
            new CraftRecipe
            {
                Id = "dive_signal",
                Name = "信号潜网密钥",
                RequiredItemIds = new[] { "circuit", "idol" },
                OutputItemId = "dive_signal"
            }
        };

        public static IReadOnlyList<CraftRecipe> All
        {
            get { return Recipes; }
        }

        public static CraftRecipe Match(IReadOnlyList<PlacedItem> items)
        {
            CraftRecipe best = null;
            int bestCount = -1;
            for (int i = 0; i < Recipes.Length; i++)
            {
                CraftRecipe recipe = Recipes[i];
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
        public const int MaxLevel = 3;

        public int Level { get; private set; }
        public BagInventory Grid { get; private set; }

        public CraftingStation()
        {
            Level = 1;
            Grid = new BagInventory(2, 2);
        }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        public int UpgradeCost
        {
            get
            {
                if (Level >= MaxLevel)
                {
                    return 0;
                }

                return Level == 1 ? 150 : 350;
            }
        }

        public CraftRecipe MatchingRecipe
        {
            get { return CraftRecipeLibrary.Match(Grid.Items); }
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
            CraftRecipe recipe = CraftRecipeLibrary.Match(Grid.Items);
            if (recipe == null)
            {
                error = "没有匹配的配方。";
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
                if (Level >= MaxLevel)
                {
                    return new string[0];
                }

                return Level == 1
                    ? new[] { "scrap", "wire" }
                    : new[] { "alloy", "circuit", "circuit" };
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
            int nextWidth = nextLevel == 1 ? 2 : 3;
            int nextHeight = nextLevel == 3 ? 3 : 2;
            Grid = FacilityGrid.Resize(Grid, nextWidth, nextHeight);
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

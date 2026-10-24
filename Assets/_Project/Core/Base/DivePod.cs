using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class DivePod
    {
        public const int MaxLevel = 5;

        public int Level { get; private set; }
        public BagInventory Grid { get; private set; }
        public string SelectedKeyItemId { get; private set; }

        public DivePod()
        {
            Level = 1;
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
        }

        /// <summary>
        /// 舱体宽度 = 等级。密钥体积按档递增（1 / 2 / 4 / 6 / 9 格），
        /// 所以 L1 只塞得下 T1 密钥，最深的 3×3 密钥要 L3 才能放进去。
        /// </summary>
        public static int WidthForLevel(int level)
        {
            if (level < 1)
            {
                return 1;
            }

            return level > MaxLevel ? MaxLevel : level;
        }

        /// <summary>高度封顶 3 行：基地右侧面板只有 138px 高，再高就会压到说明文字和按钮。</summary>
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
                        return 200;
                    case 2:
                        return 450;
                    case 3:
                        return 900;
                    case 4:
                        return 1600;
                    default:
                        return 0;
                }
            }
        }

        public string[] UpgradeMaterialIds
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return new[] { "scrap", "scrap", "wire" };
                    case 2:
                        return new[] { "circuit", "alloy", "alloy" };
                    case 3:
                        return new[] { "alloy", "alloy", "circuit", "servo" };
                    case 4:
                        return new[] { "superalloy", "superalloy", "plasma_cell" };
                    default:
                        return new string[0];
                }
            }
        }

        /// <summary>
        /// 潜网舱等级决定能进多深的网：地点的**最低**档位不能超过舱体等级。
        /// 用最低档而不是最高档，是因为「基础潜网」本身覆盖 T1–T3——若按最高档限制，
        /// 开局的 L1 潜网舱连基础潜网都进不去。
        /// </summary>
        public int MaxDiveTier
        {
            get { return Level; }
        }

        /// <summary>上次解析失败只是因为舱体等级不够（UI 用它换按钮文案）。</summary>
        public bool LastResolveBlockedByTier { get; private set; }

        public void EnsureSelection(ItemCatalog catalog)
        {
            PlacedItem selected;
            if (TryFindSelectedKey(catalog, out selected))
            {
                return;
            }

            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (catalog.TryGet(items[i].DefinitionId, out definition)
                    && definition.Category == ItemCategory.Key)
                {
                    SelectedKeyItemId = definition.Id;
                    return;
                }
            }

            SelectedKeyItemId = null;
        }

        public bool SelectKey(ItemCatalog catalog, int instanceId)
        {
            PlacedItem item;
            if (!Grid.TryGet(instanceId, out item))
            {
                return false;
            }

            ItemDefinition definition;
            if (!catalog.TryGet(item.DefinitionId, out definition)
                || definition.Category != ItemCategory.Key)
            {
                return false;
            }

            SelectedKeyItemId = definition.Id;
            return true;
        }

        public bool SelectNextKey(ItemCatalog catalog)
        {
            var keys = new List<string>();
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (catalog.TryGet(items[i].DefinitionId, out definition)
                    && definition.Category == ItemCategory.Key
                    && !keys.Contains(definition.Id))
                {
                    keys.Add(definition.Id);
                }
            }

            if (keys.Count == 0)
            {
                SelectedKeyItemId = null;
                return false;
            }

            int current = keys.IndexOf(SelectedKeyItemId);
            SelectedKeyItemId = keys[(current + 1) % keys.Count];
            return true;
        }

        public bool TryResolveDive(ItemCatalog catalog, out DivePlan plan, out string error)
        {
            EnsureSelection(catalog);
            LastResolveBlockedByTier = false;

            PlacedItem key;
            if (!TryFindSelectedKey(catalog, out key))
            {
                plan = null;
                error = "潜网舱里没有密钥。";
                return false;
            }

            var ids = new List<string>();
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ids.Add(items[i].DefinitionId);
            }

            if (!DiveKeyLibrary.TryResolve(key.DefinitionId, ids, out plan, out error))
            {
                return false;
            }

            if (plan != null && plan.TierMin > MaxDiveTier)
            {
                LastResolveBlockedByTier = true;
                error = plan.Name + " 需要潜网舱 Lv." + plan.TierMin + "（当前 Lv." + Level + "）。";
                plan = null;
                return false;
            }

            return true;
        }

        public void Consume(DivePlan plan, ItemCatalog catalog)
        {
            if (plan == null)
            {
                return;
            }

            if (plan.ConsumeKey && !string.IsNullOrEmpty(SelectedKeyItemId))
            {
                RemoveOne(SelectedKeyItemId);
            }

            if (plan.ConsumeItemIds != null)
            {
                for (int i = 0; i < plan.ConsumeItemIds.Length; i++)
                {
                    RemoveOne(plan.ConsumeItemIds[i]);
                }
            }

            EnsureSelection(catalog);
        }

        public bool TryUpgrade(CampaignState campaign, BagInventory stash, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (Level >= MaxLevel)
            {
                error = "潜网舱已满级。";
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

        public string SelectedKeyName(ItemCatalog catalog)
        {
            PlacedItem key;
            if (!TryFindSelectedKey(catalog, out key))
            {
                return "无密钥";
            }

            return catalog.Get(key.DefinitionId).DisplayName;
        }

        private bool TryFindSelectedKey(ItemCatalog catalog, out PlacedItem selected)
        {
            if (!string.IsNullOrEmpty(SelectedKeyItemId))
            {
                IReadOnlyList<PlacedItem> items = Grid.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DefinitionId == SelectedKeyItemId)
                    {
                        selected = items[i];
                        return true;
                    }
                }
            }

            selected = null;
            return false;
        }

        private void RemoveOne(string definitionId)
        {
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    Grid.Remove(items[i].InstanceId);
                    return;
                }
            }
        }
    }
}

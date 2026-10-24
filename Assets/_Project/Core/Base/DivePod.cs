using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class DivePod
    {
        public const int MaxLevel = 3;

        public int Level { get; private set; }
        public BagInventory Grid { get; private set; }
        public string SelectedKeyItemId { get; private set; }

        public DivePod()
        {
            Level = 1;
            Grid = new BagInventory(1, 2);
        }

        public int Width
        {
            get { return Level == 1 ? 1 : Level == 2 ? 2 : 3; }
        }

        public int Height
        {
            get { return Level == 3 ? 3 : 2; }
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

                return Level == 1 ? 200 : 450;
            }
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
                    ? new[] { "scrap", "scrap", "wire" }
                    : new[] { "circuit", "alloy", "alloy" };
            }
        }

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

            return DiveKeyLibrary.TryResolve(key.DefinitionId, ids, out plan, out error);
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
            int nextWidth = nextLevel == 1 ? 1 : nextLevel == 2 ? 2 : 3;
            int nextHeight = nextLevel == 3 ? 3 : 2;
            Grid = FacilityGrid.Resize(Grid, nextWidth, nextHeight);
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

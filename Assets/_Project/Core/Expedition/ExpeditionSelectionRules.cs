using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class ExpeditionSelection
    {
        public bool IsValid;
        public bool IsAction;
        public bool IsItemUse;
        public ExpeditionActionOption Action;
        public PlacedItem Item;
        public string Reason;
    }

    public static class ExpeditionSelectionRules
    {
        public static ExpeditionSelection Evaluate(
            ExpeditionScenario scenario,
            IReadOnlyList<PlacedItem> items,
            ItemCatalog catalog,
            int currentHp,
            int maxHp)
        {
            var selection = new ExpeditionSelection();
            if (scenario == null)
            {
                selection.Reason = "没有进行中的探索。";
                return selection;
            }

            if (items == null || items.Count == 0)
            {
                selection.Reason = "先把一个物品拖到右侧格子。";
                return selection;
            }

            if (items.Count > 1)
            {
                selection.Reason = "一次只能放入一个物品。";
                return selection;
            }

            PlacedItem item = items[0];
            selection.Item = item;

            if (item.DefinitionId == null)
            {
                selection.Reason = "未知物品。";
                return selection;
            }

            if (scenario.Find(item.DefinitionId) != null)
            {
                selection.IsValid = true;
                selection.IsAction = true;
                selection.Action = scenario.Find(item.DefinitionId);
                return selection;
            }

            ItemDefinition definition;
            if (catalog == null || !catalog.TryGet(item.DefinitionId, out definition))
            {
                selection.Reason = "未知物品。";
                return selection;
            }

            if (ItemUse.CanUseOutsideCombat(definition))
            {
                if (currentHp >= maxHp)
                {
                    selection.Reason = "生命已满，不需要使用这件物品。";
                    return selection;
                }

                selection.IsValid = true;
                selection.IsItemUse = true;
                return selection;
            }

            selection.Reason = "这个物品无法在探索中使用。";
            return selection;
        }
    }
}

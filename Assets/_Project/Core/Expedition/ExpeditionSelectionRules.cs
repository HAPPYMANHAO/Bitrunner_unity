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

        /// <summary>本次选择携带的辅助标志（最多 3 个，同系列 1 个）。</summary>
        public List<PlacedItem> Assists = new List<PlacedItem>();
        public List<AssistSpec> AssistSpecs = new List<AssistSpec>();
        public int AssistAlertDelta;
        public int LootBonus;
        public int EnemyBonus;
        public int DamageBonus;

        /// <summary>使用治疗类物品/撤离时，辅助标志不生效。</summary>
        public bool AssistsIgnored;
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
            return Evaluate(scenario, items, null, catalog, currentHp, maxHp);
        }

        public static ExpeditionSelection Evaluate(
            ExpeditionScenario scenario,
            IReadOnlyList<PlacedItem> items,
            IReadOnlyList<PlacedItem> assists,
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
                if (!selection.Action.IsExit)
                {
                    if (!ValidateAssists(selection, assists, catalog))
                    {
                        return selection;
                    }
                }
                else
                {
                    selection.AssistsIgnored = true;
                }

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
                selection.AssistsIgnored = assists != null && assists.Count > 0;
                return selection;
            }

            selection.Reason = "这个物品无法在探索中使用。";
            return selection;
        }

        private static bool ValidateAssists(
            ExpeditionSelection selection,
            IReadOnlyList<PlacedItem> assists,
            ItemCatalog catalog)
        {
            if (assists == null || assists.Count == 0)
            {
                return true;
            }

            if (assists.Count > 3)
            {
                selection.IsValid = false;
                selection.Reason = "一次最多携带 3 个辅助标志。";
                return false;
            }

            var families = new List<string>();
            for (int i = 0; i < assists.Count; i++)
            {
                PlacedItem assist = assists[i];
                if (assist == null)
                {
                    continue;
                }

                ItemDefinition definition;
                if (catalog == null || !catalog.TryGet(assist.DefinitionId, out definition)
                    || definition.Assist == null)
                {
                    selection.IsValid = false;
                    selection.Reason = "辅助标志无效。";
                    return false;
                }

                if (!string.IsNullOrEmpty(definition.Assist.Family))
                {
                    if (families.Contains(definition.Assist.Family))
                    {
                        selection.IsValid = false;
                        selection.Reason = "同系列辅助只能携带一个。";
                        return false;
                    }

                    families.Add(definition.Assist.Family);
                }

                if (ItemDurability.Current(assist, definition) <= 0)
                {
                    selection.IsValid = false;
                    selection.Reason = definition.DisplayName + " 已耗尽，无法使用。";
                    return false;
                }

                selection.Assists.Add(assist);
                selection.AssistSpecs.Add(definition.Assist);
                selection.AssistAlertDelta += definition.Assist.AlertDelta;
                selection.LootBonus += definition.Assist.LootPercent;
                selection.EnemyBonus += definition.Assist.EnemyPercent;
                selection.DamageBonus += definition.Assist.DamagePercent;
            }

            return true;
        }
    }
}

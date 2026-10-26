using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 任务契约卡。
    ///
    /// 每个任务对应一件 1×1 的真实物品：它摆在 NPC 的柜台上，玩家拿走得越早、
    /// 背包就越挤；任务做完还得把它交回去。这样「能不能同时做几个任务」
    /// 由背包空间决定，而不是一个抽象数字。
    ///
    /// 单独的 <see cref="ItemCategory.Quest"/> 让它被所有容器、保险箱、
    /// 潜网舱/制作站/芯片槽与商人拒收——它只属于背包和仓库。
    /// </summary>
    public static class QuestItemLibrary
    {
        public const string DefinitionPrefix = "qcard_";

        public static string DefinitionIdOf(string questId)
        {
            return string.IsNullOrEmpty(questId) ? null : DefinitionPrefix + questId;
        }

        public static bool TryQuestId(string definitionId, out string questId)
        {
            questId = null;
            if (string.IsNullOrEmpty(definitionId) || !definitionId.StartsWith(DefinitionPrefix))
            {
                return false;
            }

            string candidate = definitionId.Substring(DefinitionPrefix.Length);
            QuestDefinition definition;
            if (!QuestLibrary.TryGet(candidate, out definition))
            {
                return false;
            }

            questId = candidate;
            return true;
        }

        public static bool IsQuestCard(string definitionId)
        {
            string questId;
            return TryQuestId(definitionId, out questId);
        }

        public static void AppendDefinitions(List<ItemDefinition> target)
        {
            if (target == null)
            {
                return;
            }

            IReadOnlyList<QuestDefinition> quests = QuestLibrary.All;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                target.Add(new ItemDefinition
                {
                    Id = DefinitionIdOf(quest.Id),
                    Level = 1,
                    DisplayName = "契约 · " + quest.Title,
                    Category = ItemCategory.Quest,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    MaxStack = 1,
                    Value = 0,
                    Description = quest.Description
                });
            }
        }
    }
}

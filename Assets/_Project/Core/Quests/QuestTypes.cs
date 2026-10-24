using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public enum QuestObjectiveKind
    {
        KillEnemies,
        KillTagged,
        ClearDive,
        CollectItem,
        CollectMine,
        DefeatCrisisBoss,
        ClearCrisis,
        PayDebt
    }

    public enum QuestState
    {
        /// <summary>可接，但没有占用任何上行链路。</summary>
        Offered,

        /// <summary>进行中，上行链路被锁定。</summary>
        Active,

        /// <summary>目标已达成，等玩家回基地领取。**仍然锁定链路**。</summary>
        Ready,

        Claimed,

        /// <summary>超时或主动放弃。链路已解锁。</summary>
        Failed
    }

    public sealed class QuestDefinition
    {
        public string Id;
        public string NpcId;
        public string Title;
        public string Description;

        public QuestObjectiveKind Objective;

        /// <summary>地点密钥 id / 物品 id / 敌人 id。不用的目标留空。</summary>
        public string TargetId;

        public int TargetCount = 1;

        /// <summary>0 = 不限时。非 0 时从接取当天起算。</summary>
        public int TimeLimitDays;

        public int RewardBit;
        public string[] RewardItemIds;

        /// <summary>完成后解锁下一位 NPC 的上行链路。</summary>
        public string RewardUplinkItemId;

        /// <summary>必须先把哪个任务领掉，这个任务才会出现在列表里。空 = 无前置。</summary>
        public string RequiresQuestId;

        /// <summary>目标是「持有数量」而不是「累计次数」——领奖时会扣掉。</summary>
        public bool IsHoldingObjective
        {
            get { return Objective == QuestObjectiveKind.CollectItem; }
        }

        /// <summary>
        /// 用「当前值」推进而不是「+1」。持有类目标（还要算缴款）用绝对量，
        /// 其余都是事件累加。
        /// </summary>
        public bool UsesAbsoluteProgress
        {
            get
            {
                return Objective == QuestObjectiveKind.CollectItem
                       || Objective == QuestObjectiveKind.PayDebt;
            }
        }

        public string ObjectiveText(ItemCatalog catalog)
        {
            string target = TargetName(catalog);
            switch (Objective)
            {
                case QuestObjectiveKind.KillEnemies:
                    return "击杀 " + TargetCount + " 个敌人";
                case QuestObjectiveKind.KillTagged:
                    return "击杀 " + TargetCount + " 个" + target;
                case QuestObjectiveKind.ClearDive:
                    return "通关" + target + " " + TargetCount + " 次";
                case QuestObjectiveKind.CollectItem:
                    return "交付 " + TargetCount + " 个" + target;
                case QuestObjectiveKind.CollectMine:
                    return "收取比特矿场 " + TargetCount + " 次";
                case QuestObjectiveKind.DefeatCrisisBoss:
                    return "击败危机首领 " + TargetCount + " 次";
                case QuestObjectiveKind.ClearCrisis:
                    return "通关危机模式 " + TargetCount + " 次";
                case QuestObjectiveKind.PayDebt:
                    return "累计缴款 " + TargetCount + " bit";
                default:
                    return "完成目标";
            }
        }

        private string TargetName(ItemCatalog catalog)
        {
            if (string.IsNullOrEmpty(TargetId))
            {
                return "目标";
            }

            DiveKeyDefinition dive;
            if (DiveKeyLibrary.TryGetKey(TargetId, out dive))
            {
                return dive.DungeonName;
            }

            if (catalog != null)
            {
                ItemDefinition item;
                if (catalog.TryGet(TargetId, out item))
                {
                    return item.DisplayName;
                }
            }

            EnemyArchetype enemy = EnemyLibrary.Get(TargetId);
            if (enemy != null)
            {
                return enemy.DisplayName;
            }

            return TargetId;
        }
    }

    public sealed class QuestProgress
    {
        public string QuestId;
        public QuestState State = QuestState.Active;

        /// <summary>当前进度。持有类目标是绝对数量，其余是累计次数。</summary>
        public int Count;

        /// <summary>接取当天（绝对天序号，周×7+日），用于算超时。</summary>
        public int AcceptedDay;

        /// <summary>截止的绝对天序号，0 = 不限时。</summary>
        public int DeadlineDay;

        public bool IsExpired(int today)
        {
            return DeadlineDay > 0 && today > DeadlineDay;
        }

        public bool Reached(QuestDefinition definition)
        {
            return definition != null && Count >= definition.TargetCount;
        }
    }
}

using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 10 个任务，4 条链。链与链之间靠**上行链路物品**串起来，
    /// 而不是靠任务 id 串——这样「社交进度」才是唯一的主线：
    /// 老陈（初始赠送）→ 完成 q_junk_2 拿到 K-7 的链路 → q_arms_3 拿到白鸦的链路 → 白鸦给 q_intel_2 的出口。
    /// </summary>
    public static class QuestLibrary
    {
        private static readonly QuestDefinition[] Definitions =
        {
            // ---------- 老陈：教学链 ----------
            new QuestDefinition
            {
                Id = "q_junk_1",
                NpcId = NpcLibrary.Junk,
                Title = "清一清场",
                Description = "老陈想知道外面还有没有能走的路线。带一次结果回来就行。",
                Objective = QuestObjectiveKind.ClearDive,
                TargetId = "dive_wild",
                TargetCount = 1,
                RewardBit = 80
            },
            new QuestDefinition
            {
                Id = "q_junk_2",
                NpcId = NpcLibrary.Junk,
                Title = "收废料",
                Description = "五个废料换一段军用信道。他说这条频道闲着也是闲着。",
                Objective = QuestObjectiveKind.CollectItem,
                TargetId = "scrap",
                TargetCount = 5,
                RewardBit = 120,
                RewardUplinkItemId = "uplink_arms",
                RequiresQuestId = "q_junk_1"
            },
            new QuestDefinition
            {
                Id = "q_junk_3",
                NpcId = NpcLibrary.Junk,
                Title = "矿场学徒",
                Description = "他年轻时也守过矿场。去收三次，他给你讲点老规矩。",
                Objective = QuestObjectiveKind.CollectMine,
                TargetCount = 3,
                RewardBit = 100,
                RewardItemIds = new[] { "patch", "patch" },
                RequiresQuestId = "q_junk_1"
            },

            // ---------- K-7：击杀链 ----------
            new QuestDefinition
            {
                Id = "q_arms_1",
                NpcId = NpcLibrary.Arms,
                Title = "打得动",
                Description = "他不看简历，只看你杀过多少。十个。",
                Objective = QuestObjectiveKind.KillEnemies,
                TargetCount = 10,
                RewardBit = 200
            },
            new QuestDefinition
            {
                Id = "q_arms_2",
                NpcId = NpcLibrary.Arms,
                Title = "军用规格",
                Description = "两次军用潜网。他说那地方才叫训练。限期五天。",
                Objective = QuestObjectiveKind.ClearDive,
                TargetId = "dive_military",
                TargetCount = 2,
                TimeLimitDays = 5,
                RewardBit = 260,
                RewardItemIds = new[] { "plasma_cutter" },
                RequiresQuestId = "q_arms_1"
            },
            new QuestDefinition
            {
                Id = "q_arms_3",
                NpcId = NpcLibrary.Arms,
                Title = "深一点",
                Description = "去一趟深空潜网。活着回来，他把白鸦的频道给你。",
                Objective = QuestObjectiveKind.ClearDive,
                TargetId = "dive_deep",
                TargetCount = 1,
                RewardBit = 400,
                RewardUplinkItemId = "uplink_intel",
                RequiresQuestId = "q_arms_2"
            },

            // ---------- 白鸦：探索链，通往危机模式 ----------
            new QuestDefinition
            {
                Id = "q_intel_1",
                NpcId = NpcLibrary.Intel,
                Title = "带样本回来",
                Description = "三块超合金板。她没说用途，你也没问。",
                Objective = QuestObjectiveKind.CollectItem,
                TargetId = "superalloy",
                TargetCount = 3,
                RewardBit = 350,
                RewardItemIds = new[] { "cipher_sequencer" }
            },
            new QuestDefinition
            {
                Id = "q_intel_2",
                NpcId = NpcLibrary.Intel,
                Title = "第一次危机",
                Description = "带着测序器进一次危机模式并活着出来。她想看看你的数据。",
                Objective = QuestObjectiveKind.ClearCrisis,
                TargetCount = 1,
                RewardBit = 800,
                RewardUplinkItemId = "uplink_seed",
                RequiresQuestId = "q_intel_1"
            },

            // ---------- SEED：终局链 ----------
            new QuestDefinition
            {
                Id = "q_seed_1",
                NpcId = NpcLibrary.Seed,
                Title = "核心环",
                Description = "击败一次危机首领。它想确认你不是只来听声音的。",
                Objective = QuestObjectiveKind.DefeatCrisisBoss,
                TargetCount = 1,
                RewardBit = 1200,
                RewardItemIds = new[] { "corona_aegis" }
            },
            new QuestDefinition
            {
                Id = "q_seed_2",
                NpcId = NpcLibrary.Seed,
                Title = "缴清",
                Description = "累计缴出一万比特。它说这条记录比任何武器都难伪造。",
                Objective = QuestObjectiveKind.PayDebt,
                TargetCount = 10000,
                RewardBit = 2000
            }
        };

        public static IReadOnlyList<QuestDefinition> All
        {
            get { return Definitions; }
        }

        public static bool TryGet(string id, out QuestDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Id == id)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static QuestDefinition Get(string id)
        {
            QuestDefinition definition;
            if (!TryGet(id, out definition))
            {
                throw new KeyNotFoundException("Unknown quest id '" + id + "'.");
            }

            return definition;
        }

        /// <summary>某位 NPC 能发布的任务，按定义顺序。</summary>
        public static List<QuestDefinition> ForNpc(string npcId)
        {
            var result = new List<QuestDefinition>();
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].NpcId == npcId)
                {
                    result.Add(Definitions[i]);
                }
            }

            return result;
        }
    }
}

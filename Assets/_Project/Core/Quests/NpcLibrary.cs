using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 特殊 NPC。玩家用一条 <see cref="ItemCategory.Uplink"/> 联系他，
    /// 之后他能做两件事：卖东西、发任务。
    ///
    /// NPC 的「更好的购买选项」不是更便宜，而是**不吃周次限制**：
    /// 主商人受 <see cref="Merchant.MaxTierForWeek"/> 约束（W1 最高 T2），
    /// K-7 只要你联系上就能卖 T4。代价是价格 ×4 且他不收购任何东西——
    /// 否则玩家会绕开主商人的 50% 折价，把经济设计打穿。
    /// </summary>
    public sealed class NpcDefinition
    {
        public string Id;
        public string DisplayName;

        /// <summary>能联系到他的那件上行链路物品 id。</summary>
        public string UplinkItemId;

        /// <summary>1–4，与上行链路体积绑定（1 / 2 / 4 / 6 格）。</summary>
        public int Tier;

        public string Faction;
        public string Greeting;

        /// <summary>专属货架候选。货架每次只摆 <see cref="NpcMarket.StockSize"/> 件。</summary>
        public string[] StockPool;

        /// <summary>货架件数。3 件是「专而少」，配合 ×4 的价格让他不是商人的替代品。</summary>
        public int StockSize = NpcMarket.StockSize;

        public int RefreshDays = NpcMarket.RefreshDays;

        public string[] QuestPool;
    }

    public static class NpcLibrary
    {
        public const string Junk = "npc_junk";
        public const string Arms = "npc_arms";
        public const string Intel = "npc_intel";
        public const string Seed = "npc_seed";

        private static readonly NpcDefinition[] Definitions =
        {
            new NpcDefinition
            {
                Id = Junk,
                DisplayName = "回收商 · 老陈",
                UplinkItemId = "uplink_junk",
                Tier = 1,
                Faction = "什么都要，什么都卖",
                Greeting = "哟，还有人用这条旧频道。废料拿来，我给你换成能用的东西。",
                StockPool = new[] { "scrap", "wire", "alloy", "circuit", "patch", "medkit" },
                QuestPool = new[] { "q_junk_1", "q_junk_2", "q_junk_3" }
            },
            new NpcDefinition
            {
                Id = Arms,
                DisplayName = "军械贩 · K-7",
                UplinkItemId = "uplink_arms",
                Tier = 2,
                Faction = "不问来路，只问预算",
                Greeting = "加密握手通过。我这里的货不看周次，只看你付不付得起。",
                StockPool = new[]
                {
                    "blade", "plate", "pulse", "buckler", "nano_stitcher",
                    "plasma_cutter", "rail_lance", "flechette_gun",
                    "reactive_plating", "ablative_shell", "hazmat_rig"
                },
                QuestPool = new[] { "q_arms_1", "q_arms_2", "q_arms_3" }
            },
            new NpcDefinition
            {
                Id = Intel,
                DisplayName = "掮客 · 白鸦",
                UplinkItemId = "uplink_intel",
                Tier = 3,
                Faction = "卖消息，也卖能撬开消息的东西",
                Greeting = "你付的不是钱，是让我觉得你值得被记住。",
                // 只上架 Value > 0 的东西。密码测序器（Value 0）刻意不卖：
                // 它是 q_intel_1 的奖励，作为「危机模式的唯一入口」应该是一次事件，
                // 而不是货架上的一件商品。深空密钥同理，走制作站。
                StockPool = new[] { "data_core", "idol", "trinket", "chip", "overclock_core" },
                QuestPool = new[] { "q_intel_1", "q_intel_2" }
            },
            new NpcDefinition
            {
                Id = Seed,
                DisplayName = "核心之声 · SEED",
                UplinkItemId = "uplink_seed",
                Tier = 4,
                Faction = "核心环里唯一还在应答的东西",
                Greeting = "……信号确认。你已经走到这里了。那么，你想要什么？",
                StockPool = new[]
                {
                    "corona_aegis", "phase_weave", "void_suit",
                    "singularity_hammer", "arc_reaver", "void_needler",
                    "full_repair_kit", "null_field_generator", "plasma_furnace"
                },
                QuestPool = new[] { "q_seed_1", "q_seed_2" }
            }
        };

        public static IReadOnlyList<NpcDefinition> All
        {
            get { return Definitions; }
        }

        public static bool TryGet(string id, out NpcDefinition definition)
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

        /// <summary>按上行链路物品 id 反查 NPC。通讯中枢扫描格子时用。</summary>
        public static bool TryFindByUplink(string uplinkItemId, out NpcDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].UplinkItemId == uplinkItemId)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class TraitCatalog
    {
        public const string KeenEdge = "keen_edge";
        public const string HeavySmash = "heavy_smash";
        public const string HeavyArmor = "heavy_armor";
        public const string LightFrame = "light_frame";

        public const string EnergyCell = "energy_cell";
        public const string OverheatStrike = "overheat_strike";
        public const string SignalJam = "signal_jam";
        public const string CorrosionDose = "corrosion_dose";
        public const string Nanites = "nanites";
        public const string EvasionGuard = "evasion_guard";
        public const string ShieldPiston = "shield_piston";
        public const string OverloadPurge = "overload_purge";
        public const string CoolantFlow = "coolant_flow";
        public const string OverloadLoad = "overload_load";
        public const string OverclockFlow = "overclock_flow";
        public const string ShortCircuitCharge = "short_circuit_charge";
        public const string CacheField = "cache_field";

        // ---- CPU 芯片词条：只在背包之外生效（GlobalModifiers）----
        public const string HpChip = "hp_chip";
        public const string BaseEnergyChip = "base_energy_chip";
        public const string ArmorChip = "armor_chip";

        /// <summary>能量芯片的代价。值为负的生命上限，见 <see cref="TraitEffect.AddMaxHp"/>。</summary>
        public const string EnergyDrawback = "energy_drawback";

        private static readonly TraitCatalog DefaultInstance = new TraitCatalog(CreateDefaults());

        private readonly Dictionary<string, TraitDefinition> _definitions =
            new Dictionary<string, TraitDefinition>();

        public TraitCatalog(IEnumerable<TraitDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            foreach (TraitDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    throw new ArgumentException("Every trait needs an Id.", nameof(definitions));
                }

                if (definition.RankValues == null || definition.RankValues.Length != 5)
                {
                    throw new ArgumentException("Trait '" + definition.Id + "' needs five rank values.", nameof(definitions));
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException("Duplicate trait id '" + definition.Id + "'.", nameof(definitions));
                }

                _definitions.Add(definition.Id, definition);
            }
        }

        public static TraitCatalog Default
        {
            get { return DefaultInstance; }
        }

        public int Count
        {
            get { return _definitions.Count; }
        }

        public IEnumerable<TraitDefinition> Definitions
        {
            get { return _definitions.Values; }
        }

        public bool TryGet(string id, out TraitDefinition definition)
        {
            if (string.IsNullOrEmpty(id))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(id, out definition);
        }

        public TraitDefinition Get(string id)
        {
            TraitDefinition definition;
            if (!TryGet(id, out definition))
            {
                throw new KeyNotFoundException("Unknown trait id '" + id + "'.");
            }

            return definition;
        }

        private static IEnumerable<TraitDefinition> CreateDefaults()
        {
            return new[]
            {
                new TraitDefinition
                {
                    Id = KeenEdge,
                    DisplayName = "锋锐",
                    DescriptionFormat = "使用后，周围 8 格内每件武器本回合 +{0} 伤害",
                    Trigger = TraitTrigger.OnUse,
                    Condition = TraitCondition.None,
                    Target = TraitTarget.Surround8Weapons,
                    Effect = TraitEffect.AddDamage,
                    Duration = TraitDuration.UntilEndOfTurn,
                    StackRule = TraitStackRule.Max,
                    RankValues = new[] { 2, 3, 4, 5, 6 }
                },
                new TraitDefinition
                {
                    Id = HeavySmash,
                    DisplayName = "重砸",
                    DescriptionFormat = "周围每 1 格空格 +{0} 伤害（最多 8 格）",
                    Trigger = TraitTrigger.Passive,
                    Condition = TraitCondition.Surround8EmptyCells,
                    Target = TraitTarget.Self,
                    Effect = TraitEffect.AddDamage,
                    Duration = TraitDuration.Instant,
                    RankValues = new[] { 1, 2, 4, 5, 7 }
                },
                new TraitDefinition
                {
                    Id = HeavyArmor,
                    DisplayName = "重甲",
                    DescriptionFormat = "回合开始：周围每件护甲获得 {0} 护盾",
                    Trigger = TraitTrigger.TurnStart,
                    Condition = TraitCondition.Surround8Armor,
                    Target = TraitTarget.Self,
                    Effect = TraitEffect.AddShield,
                    Duration = TraitDuration.Instant,
                    RankValues = new[] { 2, 4, 5, 6, 8 }
                },
                new TraitDefinition
                {
                    Id = LightFrame,
                    DisplayName = "轻灵",
                    DescriptionFormat = "回合开始：背包护甲 ≤{1} 件时获得 {0} 层闪避",
                    Trigger = TraitTrigger.TurnStart,
                    Condition = TraitCondition.BagArmorAtMost,
                    Target = TraitTarget.Self,
                    Effect = TraitEffect.GrantStatus,
                    Duration = TraitDuration.Instant,
                    Status = StatusType.Evasion,
                    StatusTurns = 2,
                    RankValues = new[] { 1, 1, 2, 2, 3 },
                    ConditionValues = new[] { 1, 2, 3, 4, 5 }
                },
                new TraitDefinition
                {
                    Id = EnergyCell,
                    DisplayName = "能量单元",
                    DescriptionFormat = "能量上限 +{0}",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddEnergy,
                    RankValues = new[] { 1, 2, 3, 4, 5 }
                },
                new TraitDefinition
                {
                    Id = OverheatStrike,
                    DisplayName = "击穿",
                    DescriptionFormat = "命中时施加 {0} 层过热（2 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Overheat,
                    StatusTurns = 2,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = SignalJam,
                    DisplayName = "信号干扰",
                    DescriptionFormat = "命中时施加 {0} 层过载（1 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Overload,
                    StatusTurns = 1,
                    RankValues = new[] { 1, 1, 1, 2, 2 }
                },
                new TraitDefinition
                {
                    Id = CorrosionDose,
                    DisplayName = "腐蚀涂层",
                    DescriptionFormat = "命中时施加 {0} 层腐蚀（3 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Corrosion,
                    StatusTurns = 3,
                    RankValues = new[] { 2, 2, 3, 3, 4 }
                },
                new TraitDefinition
                {
                    Id = Nanites,
                    DisplayName = "纳米修复",
                    DescriptionFormat = "使用后获得 {0} 层纳米修复（3 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Nanorepair,
                    StatusTurns = 3,
                    RankValues = new[] { 2, 2, 3, 3, 4 }
                },
                new TraitDefinition
                {
                    Id = EvasionGuard,
                    DisplayName = "偏导",
                    DescriptionFormat = "使用后获得 {0} 层闪避（2 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Evasion,
                    StatusTurns = 2,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = ShieldPiston,
                    DisplayName = "护盾活塞",
                    DescriptionFormat = "伤害 +当前护盾/2（每 2 点护盾 +1 伤害）",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddDamageFromShield,
                    ShieldDivisor = 2,
                    RankValues = new[] { 1, 1, 1, 1, 1 }
                },
                new TraitDefinition
                {
                    Id = OverloadPurge,
                    DisplayName = "过载清除",
                    DescriptionFormat = "回合开始：清除 {0} 层过载",
                    Trigger = TraitTrigger.TurnStart,
                    Effect = TraitEffect.ConsumeStatus,
                    Status = StatusType.Overload,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = CoolantFlow,
                    DisplayName = "冷却流体",
                    DescriptionFormat = "回合开始：获得 {0} 层冷却（3 回合）",
                    Trigger = TraitTrigger.TurnStart,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Coolant,
                    StatusTurns = 3,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = OverloadLoad,
                    DisplayName = "过载负载",
                    DescriptionFormat = "回合开始：获得 {0} 层过载",
                    Trigger = TraitTrigger.TurnStart,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Overload,
                    StatusTurns = 1,
                    RankValues = new[] { 1, 1, 1, 2, 2 }
                },
                new TraitDefinition
                {
                    Id = OverclockFlow,
                    DisplayName = "超频流",
                    DescriptionFormat = "回合开始：获得 {0} 层超频（3 回合）",
                    Trigger = TraitTrigger.TurnStart,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Overclock,
                    StatusTurns = 3,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = ShortCircuitCharge,
                    DisplayName = "短路脉冲",
                    DescriptionFormat = "命中时施加 {0} 层短路（2 回合）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.ShortCircuit,
                    StatusTurns = 2,
                    RankValues = new[] { 1, 1, 2, 2, 3 }
                },
                new TraitDefinition
                {
                    Id = CacheField,
                    DisplayName = "缓存场",
                    DescriptionFormat = "使用后获得 {0} 层缓存（保留护盾）",
                    Trigger = TraitTrigger.OnUse,
                    Effect = TraitEffect.GrantStatus,
                    Status = StatusType.Cache,
                    StatusTurns = 1,
                    RankValues = new[] { 1, 1, 1, 1, 1 }
                },

                // ---- CPU 芯片 ----
                // 这三条只在 CPU 网格上生效，不进战斗背包，由 TraitEngine.EvaluateChips 汇总。
                new TraitDefinition
                {
                    Id = HpChip,
                    DisplayName = "生命芯片",
                    DescriptionFormat = "生命上限 +{0}",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddMaxHp,
                    RankValues = new[] { 15, 30, 50, 75, 110 }
                },
                new TraitDefinition
                {
                    Id = BaseEnergyChip,
                    DisplayName = "能量芯片",
                    DescriptionFormat = "基础能量 +{0}（全游戏只生效一张）",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddBaseEnergy,
                    RankValues = new[] { 1, 1, 1, 1, 1 }
                },
                new TraitDefinition
                {
                    Id = ArmorChip,
                    DisplayName = "底甲芯片",
                    DescriptionFormat = "五系护甲基底 +{0}",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddArmorBase,
                    RankValues = new[] { 1, 2, 3, 4, 6 }
                },
                new TraitDefinition
                {
                    Id = EnergyDrawback,
                    DisplayName = "能量代价",
                    DescriptionFormat = "生命上限 {0}",
                    Trigger = TraitTrigger.Passive,
                    Effect = TraitEffect.AddMaxHp,
                    RankValues = new[] { -18, -15, -12, -9, -6 }
                }
            };
        }
    }
}

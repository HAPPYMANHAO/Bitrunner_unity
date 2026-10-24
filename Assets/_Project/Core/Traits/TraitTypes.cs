namespace BitRunner.Core
{
    public enum TraitTrigger
    {
        Passive,
        OnUse,
        TurnStart,
        TurnEnd
    }

    public enum TraitCondition
    {
        None,
        Surround8EmptyCells,
        Surround8Armor,
        BagArmorAtMost
    }

    public enum TraitTarget
    {
        Self,
        Surround8Weapons
    }

    public enum TraitEffect
    {
        AddDamage,
        AddDamageFromShield,
        AddShield,
        AddEnergy,
        GrantStatus,
        ConsumeStatus,

        // ---- 以下三种只在背包之外生效（CPU 芯片），由 TraitEngine.EvaluateChips 汇总。----
        // 它们故意和 AddEnergy 分开：AddEnergy 是「背包内物品提供的每回合能量」，
        // 会随装备丢失而消失；AddBaseEnergy 是永久的基础能量。

        /// <summary>生命上限加成，可为负（能量芯片的副作用）。</summary>
        AddMaxHp,

        /// <summary>基础能量加成，全游戏只生效最高一张。</summary>
        AddBaseEnergy,

        /// <summary>五系护甲基底加成。</summary>
        AddArmorBase
    }

    public enum TraitDuration
    {
        Instant,
        UntilEndOfTurn
    }

    public enum TraitStackRule
    {
        Stack,
        Max
    }

    public struct TraitRef
    {
        public string TraitId;
        public int Rank;

        public TraitRef(string traitId, int rank)
        {
            TraitId = traitId;
            Rank = rank;
        }
    }

    public sealed class TraitDefinition
    {
        public string Id;
        public string DisplayName;
        public string DescriptionFormat = "{0}";

        public TraitTrigger Trigger = TraitTrigger.Passive;
        public TraitCondition Condition = TraitCondition.None;
        public TraitTarget Target = TraitTarget.Self;
        public TraitEffect Effect = TraitEffect.AddDamage;
        public TraitDuration Duration = TraitDuration.Instant;
        public TraitStackRule StackRule = TraitStackRule.Max;

        public StatusType Status = StatusType.None;
        public int StatusTurns = 1;

        /// <summary>AddDamageFromShield 使用：每 N 点护盾 +1 伤害。</summary>
        public int ShieldDivisor = 1;

        public int[] RankValues;
        public int[] ConditionValues;

        public int MaxTriggersPerTurn = 1;

        public int AmountAtRank(int rank)
        {
            return ValueAt(RankValues, rank);
        }

        public int ConditionAtRank(int rank)
        {
            return ValueAt(ConditionValues, rank);
        }

        public bool HasConditionValue
        {
            get { return ConditionValues != null && ConditionValues.Length == 5; }
        }

        private static int ValueAt(int[] values, int rank)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            int index = rank - 1;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= values.Length)
            {
                index = values.Length - 1;
            }

            return values[index];
        }
    }
}

using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public enum SynergyTarget
    {
        Self,
        Other
    }

    public sealed class SynergyRule
    {
        public ItemTag OwnerTag;
        public ItemTag ConditionTag;
        public SynergyEffect Effect;
        public int[] RankValues;
        public SynergyCondition Condition;
        public SynergyTarget Target;
        public int MaxPerItem;

        public SynergyRule(
            ItemTag ownerTag,
            ItemTag conditionTag,
            SynergyEffect effect,
            int[] rankValues)
            : this(ownerTag, conditionTag, effect, rankValues, SynergyCondition.Adjacent, SynergyTarget.Self, 0)
        {
        }

        public SynergyRule(
            ItemTag ownerTag,
            ItemTag conditionTag,
            SynergyEffect effect,
            int[] rankValues,
            SynergyCondition condition)
            : this(ownerTag, conditionTag, effect, rankValues, condition, SynergyTarget.Self, 0)
        {
        }

        public SynergyRule(
            ItemTag ownerTag,
            ItemTag conditionTag,
            SynergyEffect effect,
            int[] rankValues,
            SynergyCondition condition,
            SynergyTarget target,
            int maxPerItem)
        {
            if (rankValues == null || rankValues.Length != 5)
            {
                throw new ArgumentException("A tag effect needs five rank values.", nameof(rankValues));
            }

            OwnerTag = ownerTag;
            ConditionTag = conditionTag;
            Effect = effect;
            RankValues = rankValues;
            Condition = condition;
            Target = target;
            MaxPerItem = maxPerItem;
        }

        public int AmountAtRank(int rank)
        {
            if (rank < 1)
            {
                rank = 1;
            }
            else if (rank > RankValues.Length)
            {
                rank = RankValues.Length;
            }

            return RankValues[rank - 1];
        }
    }

    public struct SynergyBonus
    {
        public int Damage;
        public int Shield;
        public int Energy;
        public int CooldownReduction;

        public void Add(SynergyEffect effect, int amount)
        {
            switch (effect)
            {
                case SynergyEffect.Damage:
                    Damage += amount;
                    break;
                case SynergyEffect.Shield:
                    Shield += amount;
                    break;
                case SynergyEffect.Energy:
                    Energy += amount;
                    break;
                case SynergyEffect.CooldownReduction:
                    CooldownReduction += amount;
                    break;
            }
        }
    }

    public static class SynergyResolver
    {
        public static readonly IReadOnlyList<SynergyRule> DefaultRules = new[]
        {
            new SynergyRule(
                ItemTag.Blade,
                ItemTag.Blade,
                SynergyEffect.Damage,
                Ranks(2, 3, 4, 5, 6)),
            new SynergyRule(
                ItemTag.Shield,
                ItemTag.Armor,
                SynergyEffect.Shield,
                Ranks(3, 4, 5, 6, 7)),
            new SynergyRule(
                ItemTag.Chip,
                ItemTag.Blade,
                SynergyEffect.CooldownReduction,
                Ranks(1, 1, 2, 2, 3),
                SynergyCondition.Adjacent,
                SynergyTarget.Other,
                1),
            new SynergyRule(
                ItemTag.Projectile,
                ItemTag.Blade,
                SynergyEffect.Damage,
                Ranks(1, 1, 2, 2, 3),
                SynergyCondition.Adjacent,
                SynergyTarget.Self,
                1),
            new SynergyRule(
                ItemTag.Targeting,
                ItemTag.Blade,
                SynergyEffect.Damage,
                Ranks(1, 1, 2, 2, 3),
                SynergyCondition.SameRow,
                SynergyTarget.Other,
                1),
            new SynergyRule(
                ItemTag.Salvage,
                ItemTag.Data | ItemTag.Mechanical,
                SynergyEffect.Shield,
                Ranks(1, 1, 2, 2, 3),
                SynergyCondition.Adjacent,
                SynergyTarget.Self,
                4),
            new SynergyRule(
                ItemTag.Cooling,
                ItemTag.Energy,
                SynergyEffect.Shield,
                Ranks(1, 1, 2, 2, 3),
                SynergyCondition.Adjacent,
                SynergyTarget.Self,
                1)
        };

        public static bool HasUniqueOwnerTags(IReadOnlyList<SynergyRule> rules)
        {
            if (rules == null)
            {
                return false;
            }

            ItemTag seen = ItemTag.None;
            for (int i = 0; i < rules.Count; i++)
            {
                if ((seen & rules[i].OwnerTag) != 0)
                {
                    return false;
                }

                seen |= rules[i].OwnerTag;
            }

            return true;
        }

        private static int[] Ranks(int rank1, int rank2, int rank3, int rank4, int rank5)
        {
            return new[] { rank1, rank2, rank3, rank4, rank5 };
        }

        public static Dictionary<int, SynergyBonus> Resolve(
            BagInventory bag,
            ItemCatalog catalog,
            IReadOnlyList<SynergyRule> rules)
        {
            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (rules == null)
            {
                rules = DefaultRules;
            }

            var bonuses = new Dictionary<int, SynergyBonus>();
            IReadOnlyList<PlacedItem> items = bag.Items;
            var applied = new Dictionary<long, int>();

            for (int ownerIndex = 0; ownerIndex < items.Count; ownerIndex++)
            {
                PlacedItem owner = items[ownerIndex];
                ItemDefinition ownerDef;
                if (!catalog.TryGet(owner.DefinitionId, out ownerDef))
                {
                    continue;
                }

                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    SynergyRule rule = rules[ruleIndex];
                    if ((ownerDef.Tags & rule.OwnerTag) == 0)
                    {
                        continue;
                    }

                    int rank = ownerDef.GetTagRank(rule.OwnerTag);
                    int amount = rule.AmountAtRank(rank);
                    if (amount <= 0)
                    {
                        continue;
                    }

                    for (int otherIndex = 0; otherIndex < items.Count; otherIndex++)
                    {
                        if (otherIndex == ownerIndex)
                        {
                            continue;
                        }

                        PlacedItem other = items[otherIndex];
                        ItemDefinition otherDef;
                        if (!catalog.TryGet(other.DefinitionId, out otherDef))
                        {
                            continue;
                        }

                        if (rule.ConditionTag != ItemTag.None
                            && (otherDef.Tags & rule.ConditionTag) == 0)
                        {
                            continue;
                        }

                        if (!MatchesCondition(bag, owner, other, rule.Condition))
                        {
                            continue;
                        }

                        int targetId = rule.Target == SynergyTarget.Self
                            ? owner.InstanceId
                            : other.InstanceId;
                        AddBonus(
                            bonuses,
                            applied,
                            targetId,
                            ruleIndex,
                            rule,
                            amount);
                    }
                }
            }

            return bonuses;
        }

        public static SynergyBonus Get(Dictionary<int, SynergyBonus> bonuses, int instanceId)
        {
            SynergyBonus bonus;
            return bonuses != null && bonuses.TryGetValue(instanceId, out bonus)
                ? bonus
                : default(SynergyBonus);
        }

        public static IReadOnlyList<int> FindLinked(
            BagInventory bag,
            ItemCatalog catalog,
            IReadOnlyList<SynergyRule> rules,
            int instanceId)
        {
            var linked = new List<int>();
            if (bag == null || catalog == null || rules == null)
            {
                return linked;
            }

            PlacedItem source;
            ItemDefinition sourceDef;
            if (!bag.TryGet(instanceId, out source)
                || !catalog.TryGet(source.DefinitionId, out sourceDef))
            {
                return linked;
            }

            var seen = new HashSet<int>();
            IReadOnlyList<PlacedItem> items = bag.Items;

            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                SynergyRule rule = rules[ruleIndex];
                bool sourceOwner = IsActiveOwner(sourceDef, rule);
                bool sourceCondition = rule.ConditionTag != ItemTag.None
                    && (sourceDef.Tags & rule.ConditionTag) != 0;
                if (!sourceOwner && !sourceCondition)
                {
                    continue;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    PlacedItem other = items[i];
                    if (other.InstanceId == instanceId)
                    {
                        continue;
                    }

                    ItemDefinition otherDef;
                    if (!catalog.TryGet(other.DefinitionId, out otherDef))
                    {
                        continue;
                    }

                    bool otherOwner = IsActiveOwner(otherDef, rule);
                    bool otherCondition = rule.ConditionTag == ItemTag.None
                        || (otherDef.Tags & rule.ConditionTag) != 0;

                    bool linkedByOwner = sourceOwner
                        && otherCondition
                        && MatchesCondition(bag, source, other, rule.Condition);
                    bool linkedByCondition = sourceCondition
                        && otherOwner
                        && MatchesCondition(bag, other, source, rule.Condition);

                    if ((linkedByOwner || linkedByCondition) && seen.Add(other.InstanceId))
                    {
                        linked.Add(other.InstanceId);
                    }
                }
            }

            return linked;
        }

        private static bool IsActiveOwner(ItemDefinition definition, SynergyRule rule)
        {
            if (definition == null || (definition.Tags & rule.OwnerTag) == 0)
            {
                return false;
            }

            int rank = definition.GetTagRank(rule.OwnerTag);
            return rule.AmountAtRank(rank) > 0;
        }

        private static void AddBonus(
            Dictionary<int, SynergyBonus> bonuses,
            Dictionary<long, int> applied,
            int instanceId,
            int ruleIndex,
            SynergyRule rule,
            int amount)
        {
            long key = ((long)instanceId << 32) | (uint)ruleIndex;
            int count;
            applied.TryGetValue(key, out count);
            if (rule.MaxPerItem > 0 && count >= rule.MaxPerItem)
            {
                return;
            }

            applied[key] = count + 1;

            SynergyBonus bonus;
            bonuses.TryGetValue(instanceId, out bonus);
            bonus.Add(rule.Effect, amount);
            bonuses[instanceId] = bonus;
        }

        private static bool MatchesCondition(
            BagInventory bag,
            PlacedItem first,
            PlacedItem second,
            SynergyCondition condition)
        {
            switch (condition)
            {
                case SynergyCondition.SameRow:
                    return SharesRow(first, second);
                case SynergyCondition.SameColumn:
                    return SharesColumn(first, second);
                default:
                    return bag.AreAdjacent(first.InstanceId, second.InstanceId);
            }
        }

        private static bool SharesRow(PlacedItem first, PlacedItem second)
        {
            IReadOnlyList<Cell> firstCells = first.GetOccupiedCells();
            IReadOnlyList<Cell> secondCells = second.GetOccupiedCells();

            for (int i = 0; i < firstCells.Count; i++)
            {
                for (int j = 0; j < secondCells.Count; j++)
                {
                    if (firstCells[i].Y == secondCells[j].Y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SharesColumn(PlacedItem first, PlacedItem second)
        {
            IReadOnlyList<Cell> firstCells = first.GetOccupiedCells();
            IReadOnlyList<Cell> secondCells = second.GetOccupiedCells();

            for (int i = 0; i < firstCells.Count; i++)
            {
                for (int j = 0; j < secondCells.Count; j++)
                {
                    if (firstCells[i].X == secondCells[j].X)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class ExpeditionOddsEntry
    {
        public ExpeditionOutcomeEntry Entry;
        public double BasePercent;
        public double FinalPercent;
    }

    /// <summary>
    /// 探索结果概率：UI 预览与实际抽取共用这一份计算，避免显示与结果不一致。
    /// 基础权重先按血量/背包修正，再把警戒和辅助标志按百分点平移。
    /// </summary>
    public static class ExpeditionOdds
    {
        public const double AlertPercentPerPoint = 5.0;

        public static List<ExpeditionOddsEntry> Compute(
            ExpeditionActionOption action,
            ExpeditionContext context,
            IReadOnlyList<AssistSpec> assists)
        {
            var result = new List<ExpeditionOddsEntry>();
            if (action == null || action.Outcomes == null || action.Outcomes.Count == 0)
            {
                return result;
            }

            var kinds = new List<ExpeditionOutcomeKind>();
            var basePct = new Dictionary<ExpeditionOutcomeKind, double>();
            var kindWeight = new Dictionary<ExpeditionOutcomeKind, double>();
            var entryWeight = new Dictionary<ExpeditionOutcomeEntry, double>();
            double total = 0;

            for (int i = 0; i < action.Outcomes.Count; i++)
            {
                ExpeditionOutcomeEntry entry = action.Outcomes[i];
                double weight = BaseWeight(entry, context);
                entryWeight[entry] = weight;
                total += weight;

                double current;
                kindWeight.TryGetValue(entry.Kind, out current);
                kindWeight[entry.Kind] = current + weight;
                if (!basePct.ContainsKey(entry.Kind))
                {
                    basePct[entry.Kind] = 0;
                    kinds.Add(entry.Kind);
                }
            }

            if (total <= 0)
            {
                return result;
            }

            var finalPct = new Dictionary<ExpeditionOutcomeKind, double>();
            for (int i = 0; i < kinds.Count; i++)
            {
                ExpeditionOutcomeKind kind = kinds[i];
                finalPct[kind] = kindWeight[kind] / total * 100.0;
                basePct[kind] = finalPct[kind];
            }

            var deltas = new Dictionary<ExpeditionOutcomeKind, double>();
            AddDelta(deltas, ExpeditionOutcomeKind.Combat, context.Alert * AlertPercentPerPoint);

            int loot = 0;
            int enemy = 0;
            int damage = 0;
            if (assists != null)
            {
                for (int i = 0; i < assists.Count; i++)
                {
                    AssistSpec assist = assists[i];
                    if (assist == null)
                    {
                        continue;
                    }

                    loot += assist.LootPercent;
                    enemy += assist.EnemyPercent;
                    damage += assist.DamagePercent;
                }
            }

            AddDelta(deltas, ExpeditionOutcomeKind.Loot, loot);
            AddDelta(deltas, ExpeditionOutcomeKind.Combat, enemy);
            AddDelta(deltas, ExpeditionOutcomeKind.Damage, damage);
            ApplyDeltas(finalPct, basePct, kinds, deltas);

            for (int i = 0; i < kinds.Count; i++)
            {
                ExpeditionOutcomeKind kind = kinds[i];
                double kindShare = kindWeight[kind] > 0 ? finalPct[kind] / kindWeight[kind] : 0;
                for (int j = 0; j < action.Outcomes.Count; j++)
                {
                    ExpeditionOutcomeEntry entry = action.Outcomes[j];
                    if (entry.Kind != kind)
                    {
                        continue;
                    }

                    result.Add(new ExpeditionOddsEntry
                    {
                        Entry = entry,
                        BasePercent = basePct[kind] * (entryWeight[entry] / kindWeight[kind]),
                        FinalPercent = kindShare * entryWeight[entry]
                    });
                }
            }

            return result;
        }

        public static ExpeditionOutcomeEntry Pick(
            ExpeditionActionOption action,
            ExpeditionContext context,
            IReadOnlyList<AssistSpec> assists,
            Random random)
        {
            if (action == null || action.Outcomes == null || action.Outcomes.Count == 0)
            {
                return null;
            }

            List<ExpeditionOddsEntry> odds = Compute(action, context, assists);
            if (odds.Count == 0)
            {
                return action.Outcomes[0];
            }

            double roll = random.NextDouble() * 100.0;
            double cumulative = 0;
            for (int i = 0; i < odds.Count; i++)
            {
                cumulative += odds[i].FinalPercent;
                if (roll < cumulative)
                {
                    return odds[i].Entry;
                }
            }

            return odds[odds.Count - 1].Entry;
        }

        /// <summary>基础权重只保留血量与背包的既有修正；警戒改为线性百分点。</summary>
        private static double BaseWeight(ExpeditionOutcomeEntry entry, ExpeditionContext context)
        {
            double weight = Math.Max(1, entry.Weight);
            if (entry.Kind == ExpeditionOutcomeKind.Damage && context.Hp <= 15)
            {
                weight += 5;
            }

            if (entry.Kind == ExpeditionOutcomeKind.Loot && context.BagFree <= 2)
            {
                weight -= 4;
            }

            return Math.Max(1, weight);
        }

        private static void AddDelta(
            Dictionary<ExpeditionOutcomeKind, double> deltas,
            ExpeditionOutcomeKind kind,
            double delta)
        {
            if (Math.Abs(delta) < 0.0001)
            {
                return;
            }

            double current;
            deltas.TryGetValue(kind, out current);
            deltas[kind] = current + delta;
        }

        /// <summary>
        /// 所有百分点一次性相对基础概率结算：被点名的类别先得到完整的 ±pp，
        /// 剩余概率再由未被点名的类别按基础占比吸收。这样 +12pp 在 UI 上就是 +12pp。
        /// </summary>
        private static void ApplyDeltas(
            Dictionary<ExpeditionOutcomeKind, double> percentages,
            Dictionary<ExpeditionOutcomeKind, double> basePct,
            List<ExpeditionOutcomeKind> kinds,
            Dictionary<ExpeditionOutcomeKind, double> deltas)
        {
            var targets = new List<ExpeditionOutcomeKind>();
            double untargetedBase = 0;
            for (int i = 0; i < kinds.Count; i++)
            {
                ExpeditionOutcomeKind kind = kinds[i];
                double delta;
                if (deltas.TryGetValue(kind, out delta) && Math.Abs(delta) > 0.0001)
                {
                    targets.Add(kind);
                }
                else
                {
                    untargetedBase += basePct[kind];
                }
            }

            if (targets.Count == 0)
            {
                return;
            }

            double targetSum = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                ExpeditionOutcomeKind kind = targets[i];
                double value = basePct[kind] + deltas[kind];
                if (value < 0)
                {
                    value = 0;
                }
                else if (value > 100)
                {
                    value = 100;
                }

                percentages[kind] = value;
                targetSum += value;
            }

            if (untargetedBase <= 0.0001 || targetSum >= 100)
            {
                if (targetSum >= 100 && targetSum > 0)
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        percentages[targets[i]] = percentages[targets[i]] / targetSum * 100.0;
                    }

                    for (int i = 0; i < kinds.Count; i++)
                    {
                        if (!deltas.ContainsKey(kinds[i]))
                        {
                            percentages[kinds[i]] = 0;
                        }
                    }
                }

                Normalize(percentages, kinds);
                return;
            }

            double remaining = 100.0 - targetSum;
            for (int i = 0; i < kinds.Count; i++)
            {
                ExpeditionOutcomeKind kind = kinds[i];
                if (targets.Contains(kind))
                {
                    continue;
                }

                percentages[kind] = remaining * (basePct[kind] / untargetedBase);
            }

            Normalize(percentages, kinds);
        }

        private static void Normalize(
            Dictionary<ExpeditionOutcomeKind, double> percentages,
            List<ExpeditionOutcomeKind> kinds)
        {
            double sum = 0;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (percentages[kinds[i]] < 0)
                {
                    percentages[kinds[i]] = 0;
                }

                sum += percentages[kinds[i]];
            }

            if (sum <= 0.0001)
            {
                return;
            }

            for (int i = 0; i < kinds.Count; i++)
            {
                percentages[kinds[i]] = percentages[kinds[i]] / sum * 100.0;
            }
        }
    }
}

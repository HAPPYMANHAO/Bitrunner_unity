using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ExpeditionOddsTests
    {
        [Test]
        public void BasePercentagesSumToOneHundred()
        {
            var action = Action();
            List<ExpeditionOddsEntry> odds = ExpeditionOdds.Compute(action, Context(0), null);

            double sum = 0;
            for (int i = 0; i < odds.Count; i++)
            {
                sum += odds[i].FinalPercent;
                Assert.AreEqual(odds[i].BasePercent, odds[i].FinalPercent, 0.001);
            }

            Assert.AreEqual(100.0, sum, 0.01);
        }

        [Test]
        public void AlertRaisesCombatChance()
        {
            var action = Action();
            double baseCombat = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Combat);
            double alertCombat = KindPercent(action, Context(2), null, ExpeditionOutcomeKind.Combat);

            Assert.AreEqual(baseCombat + 10.0, alertCombat, 0.01);
        }

        [Test]
        public void LightAddsLootAndAlertRaisesCombat()
        {
            var assists = new List<AssistSpec>
            {
                new AssistSpec { Family = "light", LootPercent = 12, AlertDelta = 1 }
            };
            var action = Action();
            double baseLoot = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Loot);
            double baseCombat = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Combat);

            // 光源先加 1 点警戒，再给战利品 +12pp。
            double loot = KindPercent(action, Context(1), assists, ExpeditionOutcomeKind.Loot);
            double combat = KindPercent(action, Context(1), assists, ExpeditionOutcomeKind.Combat);

            Assert.AreEqual(baseLoot + 12.0, loot, 0.01);
            Assert.AreEqual(baseCombat + 5.0, combat, 0.01);
        }

        [Test]
        public void MuffleLowersCombatAndLoot()
        {
            var assists = new List<AssistSpec>
            {
                new AssistSpec { Family = "muffle", LootPercent = -6, EnemyPercent = -12 }
            };
            var action = Action();
            double baseLoot = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Loot);
            double baseCombat = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Combat);
            double loot = KindPercent(action, Context(0), assists, ExpeditionOutcomeKind.Loot);
            double combat = KindPercent(action, Context(0), assists, ExpeditionOutcomeKind.Combat);

            Assert.AreEqual(baseLoot - 6.0, loot, 0.01);
            Assert.AreEqual(baseCombat - 12.0, combat, 0.01);
        }

        [Test]
        public void WardLowersDamage()
        {
            var assists = new List<AssistSpec>
            {
                new AssistSpec { Family = "ward", DamagePercent = -12 }
            };
            var action = Action();
            double baseDamage = KindPercent(action, Context(0), null, ExpeditionOutcomeKind.Damage);
            double damage = KindPercent(action, Context(0), assists, ExpeditionOutcomeKind.Damage);

            Assert.AreEqual(baseDamage - 12.0, damage, 0.01);
        }

        [Test]
        public void ShiftCannotPushPercentagesBelowZero()
        {
            var assists = new List<AssistSpec>
            {
                new AssistSpec { Family = "ward", DamagePercent = -500 }
            };
            List<ExpeditionOddsEntry> odds = ExpeditionOdds.Compute(Action(), Context(0), assists);

            double sum = 0;
            for (int i = 0; i < odds.Count; i++)
            {
                Assert.GreaterOrEqual(odds[i].FinalPercent, 0);
                sum += odds[i].FinalPercent;
            }

            Assert.AreEqual(100.0, sum, 0.01);
        }

        [Test]
        public void PickReturnsAnEntryFromTheAction()
        {
            var action = Action();
            ExpeditionOutcomeEntry picked = ExpeditionOdds.Pick(action, Context(0), null, new System.Random(7));

            Assert.IsNotNull(picked);
            CollectionAssert.Contains(action.Outcomes, picked);
        }

        private static double KindPercent(
            ExpeditionActionOption action,
            ExpeditionContext context,
            IReadOnlyList<AssistSpec> assists,
            ExpeditionOutcomeKind kind)
        {
            List<ExpeditionOddsEntry> odds = ExpeditionOdds.Compute(action, context, assists);
            double sum = 0;
            for (int i = 0; i < odds.Count; i++)
            {
                if (odds[i].Entry.Kind == kind)
                {
                    sum += odds[i].FinalPercent;
                }
            }

            return sum;
        }

        private static ExpeditionContext Context(int alert)
        {
            return new ExpeditionContext
            {
                Hp = 40,
                MaxHp = 40,
                BagFree = 20,
                Alert = alert
            };
        }

        private static ExpeditionActionOption Action()
        {
            return new ExpeditionActionOption
            {
                Id = "test",
                Label = "测试",
                Outcomes = new List<ExpeditionOutcomeEntry>
                {
                    new ExpeditionOutcomeEntry { Kind = ExpeditionOutcomeKind.Loot, Weight = 50 },
                    new ExpeditionOutcomeEntry { Kind = ExpeditionOutcomeKind.Combat, Weight = 20 },
                    new ExpeditionOutcomeEntry { Kind = ExpeditionOutcomeKind.Damage, Weight = 20 },
                    new ExpeditionOutcomeEntry { Kind = ExpeditionOutcomeKind.Clue, Weight = 10 }
                }
            };
        }
    }
}

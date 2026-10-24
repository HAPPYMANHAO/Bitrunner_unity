using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ExpeditionScenarioTests
    {
        private static DivePlan MakePlan(string key = "dive_basic")
        {
            return new DivePlan
            {
                KeyItemId = key,
                Name = "测试潜网",
                EnemyCount = 5,
                LootCount = 9,
                HazardCount = 6,
                SeedOffset = 17,
                LootPool = new[] { "coin", "patch", "blade", "ammo" },
                EnemyPool = new[] { "scavenger", "drone", "hacker" }
            };
        }

        private static ExpeditionContext Context()
        {
            return new ExpeditionContext
            {
                Hp = 40,
                MaxHp = 40,
                BagFree = 20,
                Alert = 0
            };
        }

        [Test]
        public void Generate_IsDeterministic()
        {
            ExpeditionScenario first = ExpeditionScenario.Generate(123, MakePlan());
            ExpeditionScenario second = ExpeditionScenario.Generate(123, MakePlan());

            Assert.AreEqual(first.Title, second.Title);
            Assert.AreEqual(first.IntroText, second.IntroText);
            Assert.AreEqual(first.CurrentOptions.Count, second.CurrentOptions.Count);
            for (int i = 0; i < first.CurrentOptions.Count; i++)
            {
                Assert.AreEqual(first.CurrentOptions[i].Id, second.CurrentOptions[i].Id);
            }
        }

        [Test]
        public void InitialOptionsHaveFourChoicesAndRetreat()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());

            Assert.GreaterOrEqual(scenario.CurrentOptions.Count, 4);
            bool hasRetreat = false;
            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                hasRetreat |= scenario.CurrentOptions[i].IsExit;
            }

            Assert.IsTrue(hasRetreat);
        }

        [Test]
        public void ActionAdvancesStageAndRebuildsOptions()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());
            ExpeditionActionOption action = FirstNonExit(scenario);

            ExpeditionResolution result = scenario.Resolve(action.Id, Context());

            Assert.IsNotNull(result);
            Assert.AreEqual(1, scenario.Stage);
            Assert.IsTrue(result.AdvancedStage);
            Assert.IsFalse(scenario.IsComplete);
            Assert.GreaterOrEqual(scenario.CurrentOptions.Count, 1);
        }

        [Test]
        public void RetreatCompletesTheScenario()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());
            ExpeditionActionOption retreat = FindExit(scenario);

            ExpeditionResolution result = scenario.Resolve(retreat.Id, Context());

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsExit);
            Assert.IsTrue(scenario.IsComplete);
            Assert.AreEqual(0, scenario.CurrentOptions.Count);
        }

        [Test]
        public void UnknownActionReturnsNull()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());

            Assert.IsNull(scenario.Resolve("missing_action", Context()));
            Assert.AreEqual(0, scenario.Stage);
        }

        [Test]
        public void LootOutcomesCarryLootIds()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());

            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                ExpeditionActionOption action = scenario.CurrentOptions[i];
                for (int j = 0; j < action.Outcomes.Count; j++)
                {
                    ExpeditionOutcomeEntry outcome = action.Outcomes[j];
                    if (outcome.Kind == ExpeditionOutcomeKind.Loot)
                    {
                        Assert.IsNotNull(outcome.LootIds);
                        Assert.Greater(outcome.LootIds.Length, 0);
                    }
                }
            }
        }

        [Test]
        public void CombatOutcomesUseEnemyPool()
        {
            var pool = new[] { "scavenger", "drone", "hacker" };
            DivePlan plan = MakePlan();
            plan.EnemyPool = pool;
            ExpeditionScenario scenario = ExpeditionScenario.Generate(99, plan);

            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                ExpeditionActionOption action = scenario.CurrentOptions[i];
                for (int j = 0; j < action.Outcomes.Count; j++)
                {
                    ExpeditionOutcomeEntry outcome = action.Outcomes[j];
                    if (outcome.Kind == ExpeditionOutcomeKind.Combat)
                    {
                        CollectionAssert.Contains(pool, outcome.EnemyId);
                    }
                }
            }
        }

        [Test]
        public void AfterThreeStagesOnlyRetreatRemains()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(123, MakePlan());

            for (int i = 0; i < ExpeditionScenario.MaxStages; i++)
            {
                ExpeditionActionOption action = FirstNonExit(scenario);
                Assert.IsNotNull(action);
                scenario.Resolve(action.Id, Context());
            }

            Assert.AreEqual(ExpeditionScenario.MaxStages, scenario.Stage);
            Assert.AreEqual(1, scenario.CurrentOptions.Count);
            Assert.IsTrue(scenario.CurrentOptions[0].IsExit);
        }

        [Test]
        public void ThemeFollowsDiveKey()
        {
            Assert.AreEqual(ExpeditionSiteTheme.Wild,
                ExpeditionScenario.Generate(1, MakePlan("dive_wild")).Theme);
            Assert.AreEqual(ExpeditionSiteTheme.Military,
                ExpeditionScenario.Generate(1, MakePlan("dive_military")).Theme);
            Assert.AreEqual(ExpeditionSiteTheme.Signal,
                ExpeditionScenario.Generate(1, MakePlan("dive_signal")).Theme);
            Assert.AreEqual(ExpeditionSiteTheme.Facility,
                ExpeditionScenario.Generate(1, MakePlan("dive_basic")).Theme);
        }

        private static ExpeditionActionOption FirstNonExit(ExpeditionScenario scenario)
        {
            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                if (!scenario.CurrentOptions[i].IsExit)
                {
                    return scenario.CurrentOptions[i];
                }
            }

            return null;
        }

        private static ExpeditionActionOption FindExit(ExpeditionScenario scenario)
        {
            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                if (scenario.CurrentOptions[i].IsExit)
                {
                    return scenario.CurrentOptions[i];
                }
            }

            return null;
        }
    }
}

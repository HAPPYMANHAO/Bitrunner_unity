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

            for (int i = 0; i < scenario.StageCount; i++)
            {
                ExpeditionActionOption action = FirstNonExit(scenario);
                Assert.IsNotNull(action);
                scenario.Resolve(action.Id, Context());
            }

            Assert.AreEqual(scenario.StageCount, scenario.Stage);
            Assert.AreEqual(1, scenario.CurrentOptions.Count);
            Assert.IsTrue(scenario.CurrentOptions[0].IsExit);
        }

        [Test]
        public void BasicDiveKeyHasFourteenStages()
        {
            DivePlan plan;
            string error;
            Assert.IsTrue(DiveKeyLibrary.TryResolve("dive_basic", new string[0], out plan, out error), error);
            Assert.AreEqual(14, plan.StageCount);
        }

        [Test]
        public void EveryDiveKeyExploresAtLeastTwelveStages()
        {
            string[] keys = { "dive_wild", "dive_basic", "dive_military", "dive_signal", "dive_deep", "dive_core" };
            for (int i = 0; i < keys.Length; i++)
            {
                DivePlan plan;
                string error;
                Assert.IsTrue(DiveKeyLibrary.TryResolve(keys[i], new string[0], out plan, out error), error);
                Assert.GreaterOrEqual(plan.StageCount, 12, keys[i]);
            }
        }

        [Test]
        public void ZonesChangeAcrossStages()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(2024, MakePlan());
            var seen = new HashSet<string> { scenario.ZoneId };
            for (int step = 0; step < 8 && scenario.Stage < scenario.StageCount; step++)
            {
                ExpeditionActionOption action = FirstNonExit(scenario);
                if (action == null)
                {
                    break;
                }

                scenario.Resolve(action.Id, Context());
                Assert.IsFalse(string.IsNullOrEmpty(scenario.ZoneId));
                seen.Add(scenario.ZoneId);
            }

            Assert.Greater(seen.Count, 1, "走过的区域应该多于一个");
        }

        [Test]
        public void BranchActionLeadsToItsZone()
        {
            ExpeditionScenario scenario = ExpeditionScenario.Generate(77, MakePlan());
            for (int step = 0; step < scenario.StageCount - 1; step++)
            {
                ExpeditionActionOption branch = null;
                for (int i = 0; i < scenario.CurrentOptions.Count; i++)
                {
                    ExpeditionActionOption option = scenario.CurrentOptions[i];
                    if (!option.IsExit && !string.IsNullOrEmpty(option.NextZoneId))
                    {
                        branch = option;
                        break;
                    }
                }

                if (branch != null)
                {
                    string target = branch.NextZoneId;
                    scenario.Resolve(branch.Id, Context());
                    Assert.AreEqual(target, scenario.ZoneId);
                    return;
                }

                ExpeditionActionOption action = FirstNonExit(scenario);
                Assert.IsNotNull(action);
                scenario.Resolve(action.Id, Context());
            }

            Assert.Fail("整条路线里没有出现带分支的行动");
        }

        [Test]
        public void StageCountComesFromPlan()
        {
            DivePlan plan = MakePlan();
            plan.StageCount = 4;
            ExpeditionScenario scenario = ExpeditionScenario.Generate(321, plan);

            Assert.AreEqual(4, scenario.StageCount);
            for (int i = 0; i < 4; i++)
            {
                ExpeditionActionOption action = FirstNonExit(scenario);
                Assert.IsNotNull(action, "第 " + (i + 1) + " 阶段仍有可选行动");
                scenario.Resolve(action.Id, Context());
            }

            Assert.AreEqual(4, scenario.Stage);
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

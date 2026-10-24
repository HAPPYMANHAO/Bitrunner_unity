using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ExpeditionActionItemLibraryTests
    {
        [Test]
        public void ActionItemsAreTwoByTwoSpecialItems()
        {
            ItemCatalog catalog = ExpeditionActionItemLibrary.CreateCatalog();

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                Assert.AreEqual(ItemCategory.Action, definition.Category);
                Assert.AreEqual(2, definition.Shape.Width);
                Assert.AreEqual(2, definition.Shape.Height);
                Assert.AreEqual(1, definition.MaxStack);
                Assert.IsFalse(SecureContainerRules.IsEligible(definition));
            }
        }

        [Test]
        public void EveryScenarioOptionHasAnActionItem()
        {
            ItemCatalog catalog = ExpeditionActionItemLibrary.CreateCatalog();
            var plan = new DivePlan
            {
                KeyItemId = "dive_basic",
                Name = "测试潜网",
                EnemyCount = 5,
                LootCount = 9,
                HazardCount = 6,
                LootPool = new[] { "coin" },
                EnemyPool = new[] { "scavenger" }
            };

            ExpeditionScenario scenario = ExpeditionScenario.Generate(44, plan);
            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                Assert.IsTrue(catalog.TryGet(scenario.CurrentOptions[i].Id, out ItemDefinition definition), scenario.CurrentOptions[i].Id);
                Assert.IsNotNull(definition);
            }
        }
    }
}

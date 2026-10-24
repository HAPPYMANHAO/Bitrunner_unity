using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ExpeditionSelectionRulesTests
    {
        [Test]
        public void ActionItemIsValid()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog combined = Combine(gameCatalog, actionCatalog);
            var bag = new BagInventory(4, 4);
            ExpeditionActionOption action = FirstAction(scenario);
            Place(bag, actionCatalog, action.Id);

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, bag.Items, combined, 40, 40);

            Assert.IsTrue(selection.IsValid);
            Assert.IsTrue(selection.IsAction);
            Assert.AreEqual(action.Id, selection.Action.Id);
        }

        [Test]
        public void HealingItemFromBackpackIsValidWhenHurt()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            var bag = new BagInventory(4, 4);
            Place(bag, gameCatalog, "patch");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, bag.Items, Combine(gameCatalog, actionCatalog), 30, 40);

            Assert.IsTrue(selection.IsValid);
            Assert.IsTrue(selection.IsItemUse);
            Assert.AreEqual("patch", selection.Item.DefinitionId);
        }

        [Test]
        public void WeaponIsNotValidForAnExpeditionAction()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            var bag = new BagInventory(4, 4);
            Place(bag, gameCatalog, "blade");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, bag.Items, Combine(gameCatalog, actionCatalog), 40, 40);

            Assert.IsFalse(selection.IsValid);
            Assert.IsFalse(selection.IsAction);
            Assert.IsFalse(selection.IsItemUse);
            Assert.IsNotEmpty(selection.Reason);
        }

        [Test]
        public void MultipleItemsBlockTheAction()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            var bag = new BagInventory(4, 4);
            Place(bag, actionCatalog, FirstAction(scenario).Id);
            Place(bag, gameCatalog, "patch");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, bag.Items, Combine(gameCatalog, actionCatalog), 30, 40);

            Assert.IsFalse(selection.IsValid);
            Assert.IsNotEmpty(selection.Reason);
        }

        private static ExpeditionScenario Scenario()
        {
            return ExpeditionScenario.Generate(123, new DivePlan
            {
                KeyItemId = "dive_basic",
                Name = "test dive",
                EnemyCount = 5,
                LootCount = 9,
                HazardCount = 6,
                SeedOffset = 17,
                LootPool = new[] { "coin", "patch", "blade", "ammo" },
                EnemyPool = new[] { "scavenger", "drone", "hacker" }
            });
        }

        private static ExpeditionActionOption FirstAction(ExpeditionScenario scenario)
        {
            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                if (!scenario.CurrentOptions[i].IsExit)
                {
                    return scenario.CurrentOptions[i];
                }
            }

            Assert.Fail("Scenario has no non-exit action.");
            return null;
        }

        private static void Place(BagInventory bag, ItemCatalog catalog, string id)
        {
            PlacedItem placed;
            Assert.IsTrue(bag.TryAdd(id, catalog.Get(id).Shape, out placed), id);
        }

        private static ItemCatalog Combine(ItemCatalog first, ItemCatalog second)
        {
            var definitions = new List<ItemDefinition>();
            definitions.AddRange(first.Definitions);
            definitions.AddRange(second.Definitions);
            return new ItemCatalog(definitions);
        }
    }
}

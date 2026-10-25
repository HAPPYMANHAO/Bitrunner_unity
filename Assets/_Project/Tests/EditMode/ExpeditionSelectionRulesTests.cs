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

        [Test]
        public void ActionWithAssistIsValidAndCarriesBonuses()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog combined = Combine(gameCatalog, actionCatalog);
            var actionBag = new BagInventory(4, 4);
            var assistBag = new BagInventory(4, 4);
            Place(actionBag, actionCatalog, FirstAction(scenario).Id);
            Place(assistBag, gameCatalog, "flag_light_1");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, actionBag.Items, assistBag.Items, combined, 40, 40);

            Assert.IsTrue(selection.IsValid);
            Assert.IsTrue(selection.IsAction);
            Assert.AreEqual(1, selection.Assists.Count);
            Assert.AreEqual(1, selection.AssistSpecs.Count);
            Assert.AreEqual(12, selection.LootBonus);
            Assert.AreEqual(1, selection.AssistAlertDelta);
        }

        [Test]
        public void DuplicateAssistFamilyIsRejected()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog combined = Combine(gameCatalog, actionCatalog);
            var actionBag = new BagInventory(4, 4);
            var assistBag = new BagInventory(6, 4);
            Place(actionBag, actionCatalog, FirstAction(scenario).Id);
            Place(assistBag, gameCatalog, "flag_light_1");
            Place(assistBag, gameCatalog, "flag_light_2");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, actionBag.Items, assistBag.Items, combined, 40, 40);

            Assert.IsFalse(selection.IsValid);
            StringAssert.Contains("同系列", selection.Reason);
        }

        [Test]
        public void MoreThanThreeAssistsAreRejected()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog combined = Combine(gameCatalog, actionCatalog);
            var actionBag = new BagInventory(4, 4);
            var assistBag = new BagInventory(8, 4);
            Place(actionBag, actionCatalog, FirstAction(scenario).Id);
            Place(assistBag, gameCatalog, "flag_light_1");
            Place(assistBag, gameCatalog, "flag_light_2");
            Place(assistBag, gameCatalog, "flag_muffle_1");
            Place(assistBag, gameCatalog, "flag_ward_1");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, actionBag.Items, assistBag.Items, combined, 40, 40);

            Assert.IsFalse(selection.IsValid);
            StringAssert.Contains("3", selection.Reason);
        }

        [Test]
        public void HealingItemIgnoresAssists()
        {
            ExpeditionScenario scenario = Scenario();
            ItemCatalog gameCatalog = DefaultItemLibrary.CreateCatalog();
            ItemCatalog actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            ItemCatalog combined = Combine(gameCatalog, actionCatalog);
            var actionBag = new BagInventory(4, 4);
            var assistBag = new BagInventory(4, 4);
            Place(actionBag, gameCatalog, "patch");
            Place(assistBag, gameCatalog, "flag_ward_1");

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                scenario, actionBag.Items, assistBag.Items, combined, 30, 40);

            Assert.IsTrue(selection.IsValid);
            Assert.IsTrue(selection.IsItemUse);
            Assert.IsTrue(selection.AssistsIgnored);
            Assert.AreEqual(0, selection.AssistSpecs.Count);
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

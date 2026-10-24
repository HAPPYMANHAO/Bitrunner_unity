using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class BuffTests
    {
        [Test]
        public void OverclockAddsDamageAndCapsAtFive()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            player.ApplyStatus(StatusType.Overclock, 3, 5);
            player.ApplyStatus(StatusType.Overclock, 3, 5);

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            Assert.AreEqual(5, player.GetStatusStacks(StatusType.Overclock));
            Assert.AreEqual(10, engine.GetDamage(player, blade));
        }

        [Test]
        public void CoolantAddsShieldAndCapsAtFive()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("plate", 3, 0, ItemTag.Shield | ItemTag.Armor, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "plate");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            player.ApplyStatus(StatusType.Coolant, 3, 5);
            player.ApplyStatus(StatusType.Coolant, 3, 5);

            PlacedItem plate = CombatTestFactory.FirstItem(player);
            Assert.AreEqual(5, player.GetStatusStacks(StatusType.Coolant));
            Assert.AreEqual(8, engine.GetShield(player, plate));
        }

        [Test]
        public void CorrosionDealsTrueDamageAtEndOfTurnAndIgnoresShield()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            player.SetShield(10);
            player.ApplyStatus(StatusType.Cache, 1, 1);
            player.ApplyStatus(StatusType.Corrosion, 3, 2);
            engine.EndTurn();

            Assert.AreEqual(28, player.Hp);
            Assert.AreEqual(10, player.Shield);
        }

        [Test]
        public void OverheatDamagesAtStartOfTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            player.ApplyStatus(StatusType.Overheat, 2, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(28, player.Hp);
        }

        [Test]
        public void NanorepairHealsAtStartOfTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            player.SetHp(20);
            player.ApplyStatus(StatusType.Nanorepair, 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(22, player.Hp);
        }

        [Test]
        public void EvasionConsumesChargeAndNegatesDamage()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            enemy.ApplyStatus(StatusType.Evasion, 3, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.AreEqual(20, enemy.Hp);
            Assert.AreEqual(0, enemy.GetStatusStacks(StatusType.Evasion));
        }

        [Test]
        public void OverclockCoreAddsEnergyAndOverload()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                new ItemDefinition
                {
                    Id = "core",
                    DisplayName = "core",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    BonusEnergy = 2,
                    TurnStartSelfStatus = StatusType.Overload,
                    TurnStartSelfStatusTurns = 1,
                    TurnStartSelfStatusStacks = 1
                });

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "core");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(5, player.Energy);
            Assert.AreEqual(1, player.GetStatusStacks(StatusType.Overload));

            engine.EndTurn();

            Assert.AreEqual(4, player.Energy);
        }

        [Test]
        public void CoolantLoopCleansesOverloadBeforeEnergy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                new ItemDefinition
                {
                    Id = "loop",
                    DisplayName = "loop",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Cooling,
                    TurnStartCleanseStatus = StatusType.Overload,
                    TurnStartCleanseStacks = 1
                });

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "loop");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            player.ApplyStatus(StatusType.Overload, 1, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(2, player.Energy);
            Assert.AreEqual(0, player.GetStatusStacks(StatusType.Overload));
        }

        [Test]
        public void TargetingArrayBoostsWeaponInSameRow()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                new ItemDefinition
                {
                    Id = "array",
                    DisplayName = "array",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Targeting
                });

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "array", 4, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            PlacedItem blade = CombatTestFactory.FindItem(player, "blade");

            Assert.AreEqual(6, engine.GetDamage(player, blade));
        }

        [Test]
        public void DefaultPhaseOneItemsAreWired()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            CombatActor player = CombatTestFactory.Actor(
                "player",
                30,
                3,
                6,
                5,
                catalog,
                "ram_piston",
                "corrosive_dart");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            player.SetShield(7);

            PlacedItem piston = CombatTestFactory.FindItem(player, "ram_piston");
            PlacedItem dart = CombatTestFactory.FindItem(player, "corrosive_dart");
            Assert.AreEqual(3, engine.GetDamage(player, piston));

            string error;
            Assert.IsTrue(engine.TryUseItem(player, dart.InstanceId, enemy, out error), error);
            Assert.AreEqual(2, enemy.GetStatusStacks(StatusType.Corrosion));
        }

        [Test]
        public void FenceModuleCountsAdjacentLoot()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                new ItemDefinition
                {
                    Id = "fence",
                    DisplayName = "fence",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Tool | ItemTag.Salvage,
                    Shield = 1
                },
                CombatTestFactory.Loot("coin", 10, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            PlacedItem fence = CombatTestFactory.PlaceAt(player, catalog, "fence", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "coin", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(2, engine.GetShield(player, fence));
        }
    }
}

using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class TraitTests
    {
        private static ItemDefinition TraitWeapon(string id, int damage, string pattern, string traitId, int rank)
        {
            ItemDefinition definition = CombatTestFactory.Weapon(id, damage, 1, 0, ItemTag.None, pattern);
            definition.Level = TierForRank(rank);
            definition.Traits = new[] { new TraitRef(traitId, rank) };
            return definition;
        }

        private static ItemDefinition TraitArmor(string id, string pattern, string traitId, int rank)
        {
            ItemDefinition definition = CombatTestFactory.Armor(id, 5, 4, 0, 0, 0, pattern);
            definition.Level = TierForRank(rank);
            definition.Traits = new[] { new TraitRef(traitId, rank) };
            return definition;
        }

        /// <summary>词条 rank 不能超过物品等级，测试按最高 rank 抬档（上限 T5）。</summary>
        private static int TierForRank(int rank)
        {
            if (rank < 1)
            {
                return 1;
            }

            return rank > ItemCatalog.MaxLevel ? ItemCatalog.MaxLevel : rank;
        }

        private static ItemDefinition PlainArmor(string id, string pattern)
        {
            return CombatTestFactory.Armor(id, 5, 4, 0, 0, 0, pattern);
        }

        [Test]
        public void HeavySmashScalesWithSurroundingEmptyCells()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("club", 4, "#", TraitCatalog.HeavySmash, 1));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 3, 3, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            PlacedItem club = CombatTestFactory.PlaceAt(player, catalog, "club", 1, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(12, engine.GetDamage(player, club));
        }

        [Test]
        public void HeavySmashAtCornerCountsOnlyValidCells()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("club", 4, "#", TraitCatalog.HeavySmash, 1));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 3, 3, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            PlacedItem club = CombatTestFactory.PlaceAt(player, catalog, "club", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(7, engine.GetDamage(player, club));
        }

        [Test]
        public void HeavySmashCapsAtEightCells()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("maul", 4, "##", TraitCatalog.HeavySmash, 1));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 5, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            PlacedItem maul = CombatTestFactory.PlaceAt(player, catalog, "maul", 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(8, engine.Traits.CountSurround8Empty(player.Bag, maul));
            Assert.AreEqual(12, engine.GetDamage(player, maul));
        }

        [Test]
        public void HeavySmashRankScalesPerEmptyCell()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("club", 4, "#", TraitCatalog.HeavySmash, 2));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 3, 3, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            PlacedItem club = CombatTestFactory.PlaceAt(player, catalog, "club", 1, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(4 + 8 * 2, engine.GetDamage(player, club));
        }

        [Test]
        public void KeenEdgeBuffsDiagonallyAdjacentWeaponOnUse()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("blade", 5, "#", TraitCatalog.KeenEdge, 1),
                CombatTestFactory.Weapon("knife", 5, 1, 0, ItemTag.None, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog);
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 1, 1);
            PlacedItem knife = CombatTestFactory.PlaceAt(player, catalog, "knife", 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.AreEqual(7, engine.GetDamage(player, knife));
        }

        [Test]
        public void KeenEdgeDoesNotBuffItself()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("blade", 5, "#", TraitCatalog.KeenEdge, 1),
                CombatTestFactory.Weapon("knife", 5, 1, 0, ItemTag.None, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog);
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 1, 1);
            CombatTestFactory.PlaceAt(player, catalog, "knife", 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.AreEqual(5, engine.GetDamage(player, blade));
        }

        [Test]
        public void KeenEdgeExpiresAtEndOfTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("blade", 5, "#", TraitCatalog.KeenEdge, 1),
                CombatTestFactory.Weapon("knife", 5, 1, 0, ItemTag.None, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog);
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 1, 1);
            PlacedItem knife = CombatTestFactory.PlaceAt(player, catalog, "knife", 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.AreEqual(7, engine.GetDamage(player, knife));

            engine.EndTurn();

            Assert.AreEqual(5, engine.GetDamage(player, knife));
        }

        [Test]
        public void KeenEdgeKeepsHighestRankInsteadOfStacking()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("blade_a", 5, "#", TraitCatalog.KeenEdge, 1),
                TraitWeapon("blade_b", 5, "#", TraitCatalog.KeenEdge, 2),
                CombatTestFactory.Weapon("knife", 5, 1, 0, ItemTag.None, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 40, 3, 4, 3, catalog);
            PlacedItem bladeA = CombatTestFactory.PlaceAt(player, catalog, "blade_a", 0, 0);
            PlacedItem bladeB = CombatTestFactory.PlaceAt(player, catalog, "blade_b", 2, 0);
            PlacedItem knife = CombatTestFactory.PlaceAt(player, catalog, "knife", 1, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, bladeA.InstanceId, enemy, out error), error);
            Assert.AreEqual(7, engine.GetDamage(player, knife));
            Assert.IsTrue(engine.TryUseItem(player, bladeB.InstanceId, enemy, out error), error);
            Assert.AreEqual(8, engine.GetDamage(player, knife));
        }

        [Test]
        public void HeavyArmorGrantsShieldAtTurnStart()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("loadout", "##", TraitCatalog.HeavyArmor, 1),
                PlainArmor("plate", "##"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "loadout", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "plate", 0, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(2, player.Shield);
        }

        [Test]
        public void HeavyArmorExcludesItself()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("loadout", "##", TraitCatalog.HeavyArmor, 1));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "loadout", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(0, player.Shield);
        }

        [Test]
        public void HeavyArmorCountsEachNeighbouringArmorOnce()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("loadout", "##", TraitCatalog.HeavyArmor, 2),
                PlainArmor("plate_a", "##"),
                PlainArmor("plate_b", "##"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "loadout", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "plate_a", 0, 1);
            CombatTestFactory.PlaceAt(player, catalog, "plate_b", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(2 * 4, player.Shield);
        }

        [Test]
        public void LightFrameGrantsEvasionWhenArmorCountIsLow()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("vest", "##", TraitCatalog.LightFrame, 1));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "vest", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(1, player.GetStatusStacks(StatusType.Evasion));
        }

        [Test]
        public void LightFrameSkipsWhenArmorCountExceedsCondition()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("vest", "##", TraitCatalog.LightFrame, 1),
                PlainArmor("plate", "##"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "vest", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "plate", 0, 1);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(0, player.GetStatusStacks(StatusType.Evasion));
        }

        [Test]
        public void TraitsResolveForEnemyTurnsAsWell()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitArmor("loadout", "##", TraitCatalog.HeavyArmor, 1),
                PlainArmor("plate", "##"),
                CombatTestFactory.Weapon("prop", 1, 99, 0, ItemTag.Blade, "##"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            CombatTestFactory.PlaceAt(enemy, catalog, "loadout", 0, 0);
            CombatTestFactory.PlaceAt(enemy, catalog, "plate", 0, 1);
            CombatTestFactory.PlaceAt(enemy, catalog, "prop", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(2, enemy.Shield);
        }

        [Test]
        public void TraitRankIsClampedToFive()
        {
            ItemDefinition definition = TraitWeapon("club", 4, "#", TraitCatalog.HeavySmash, 9);

            Assert.AreEqual(5, definition.GetTraitRank(TraitCatalog.HeavySmash));
            Assert.IsTrue(definition.HasTrait(TraitCatalog.HeavySmash));
            Assert.AreEqual(0, definition.GetTraitRank(TraitCatalog.KeenEdge));
        }

        [Test]
        public void TraitTooltipListsRankAndEffect()
        {
            ItemDefinition definition = TraitWeapon("club", 4, "##", TraitCatalog.HeavySmash, 1);

            IReadOnlyList<string> lines = ItemText.TraitLines(definition);

            Assert.AreEqual(1, lines.Count);
            Assert.IsTrue(lines[0].StartsWith("重砸 I"));
            StringAssert.Contains("词条：", ItemText.Describe(definition));
        }

        [Test]
        public void DefaultLibraryWiresFourTraits()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.AreEqual(1, catalog.Get("blade").GetTraitRank(TraitCatalog.KeenEdge));
            Assert.AreEqual(1, catalog.Get("club").GetTraitRank(TraitCatalog.HeavySmash));
            Assert.AreEqual(1, catalog.Get("heavy_plate").GetTraitRank(TraitCatalog.HeavyArmor));
            Assert.AreEqual(1, catalog.Get("scrap_vest").GetTraitRank(TraitCatalog.LightFrame));
        }

        [Test]
        public void ItemLevelDefaultsToOne()
        {
            ItemDefinition definition = CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##");

            Assert.AreEqual(1, definition.Level);
        }

        [Test]
        public void UsingTraitItemEmitsTraitEvent()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                TraitWeapon("blade", 5, "#", TraitCatalog.KeenEdge, 1),
                CombatTestFactory.Weapon("knife", 5, 1, 0, ItemTag.None, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog);
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 1, 1);
            CombatTestFactory.PlaceAt(player, catalog, "knife", 2, 2);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            bool found = false;
            for (int i = 0; i < engine.Events.Count; i++)
            {
                if (engine.Events[i].Type == CombatEventType.TraitTriggered)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found);
        }
    }
}

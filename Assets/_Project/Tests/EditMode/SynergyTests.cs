using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class SynergyTests
    {
        [Test]
        public void DefaultRulesHaveOneOwnerPerTag()
        {
            Assert.IsTrue(SynergyResolver.HasUniqueOwnerTags(SynergyResolver.DefaultRules));
        }

        [Test]
        public void BladeRankTwoAddsThreeDamage()
        {
            ItemDefinition bladeDefinition = CombatTestFactory.Weapon(
                "blade",
                5,
                1,
                0,
                ItemTag.Blade,
                "##");
            bladeDefinition.TagRanks = new[] { new TagRank(ItemTag.Blade, 2) };
            ItemCatalog catalog = CombatTestFactory.Catalog(bladeDefinition);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem first = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "blade", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(8, engine.GetDamage(player, first));
        }

        [Test]
        public void ProjectileTagBoostsItsOwnDamage()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.Weapon("gun", 5, 1, 0, ItemTag.Projectile, "##", 10, DamageType.Projectile));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            PlacedItem gun = CombatTestFactory.PlaceAt(player, catalog, "gun", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(6, engine.GetDamage(player, gun));
            Assert.AreEqual(5, engine.GetDamage(player, blade));
        }

        [Test]
        public void HoveredBladeLinksAdjacentBlade()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            PlacedItem first = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            PlacedItem second = CombatTestFactory.PlaceAt(player, catalog, "blade", 2, 0);

            IReadOnlyList<int> linked = SynergyResolver.FindLinked(
                player.Bag,
                catalog,
                SynergyResolver.DefaultRules,
                first.InstanceId);

            Assert.AreEqual(1, linked.Count);
            Assert.AreEqual(second.InstanceId, linked[0]);
        }

        [Test]
        public void HoveredChipLinksAdjacentBladeBothWays()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.Passive("chip", 0, 0, ItemTag.Chip, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            PlacedItem chip = CombatTestFactory.PlaceAt(player, catalog, "chip", 2, 0);

            IReadOnlyList<int> bladeLinks = SynergyResolver.FindLinked(
                player.Bag,
                catalog,
                SynergyResolver.DefaultRules,
                blade.InstanceId);
            IReadOnlyList<int> chipLinks = SynergyResolver.FindLinked(
                player.Bag,
                catalog,
                SynergyResolver.DefaultRules,
                chip.InstanceId);

            Assert.AreEqual(1, bladeLinks.Count);
            Assert.AreEqual(chip.InstanceId, bladeLinks[0]);
            Assert.AreEqual(1, chipLinks.Count);
            Assert.AreEqual(blade.InstanceId, chipLinks[0]);
        }

        [Test]
        public void HoveredTargetingArrayLinksSameRowBlade()
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
            PlacedItem blade = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            PlacedItem array = CombatTestFactory.PlaceAt(player, catalog, "array", 4, 0);

            IReadOnlyList<int> links = SynergyResolver.FindLinked(
                player.Bag,
                catalog,
                SynergyResolver.DefaultRules,
                blade.InstanceId);

            Assert.AreEqual(1, links.Count);
            Assert.AreEqual(array.InstanceId, links[0]);
        }

        [Test]
        public void BladeDescriptionShowsOnlyItsOwnedTagEffect()
        {
            ItemDefinition blade = CombatTestFactory.Weapon(
                "blade",
                5,
                1,
                0,
                ItemTag.Blade,
                "##");

            IReadOnlyList<string> effects = ItemText.TagEffectLines(blade);

            Assert.AreEqual(1, effects.Count);
            Assert.IsTrue(effects[0].StartsWith("利刃 I"));
        }

        [Test]
        public void AdjacentBladesGainDamageForPlayer()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem first = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            PlacedItem second = CombatTestFactory.PlaceAt(player, catalog, "blade", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(7, engine.GetDamage(player, first));
            Assert.AreEqual(7, engine.GetDamage(player, second));
        }

        [Test]
        public void AdjacentBladesGainDamageForEnemy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem first = CombatTestFactory.PlaceAt(enemy, catalog, "blade", 0, 0);
            PlacedItem second = CombatTestFactory.PlaceAt(enemy, catalog, "blade", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(7, engine.GetDamage(enemy, first));
            Assert.AreEqual(7, engine.GetDamage(enemy, second));
        }

        [Test]
        public void NonAdjacentBladesGetNoBonus()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem first = CombatTestFactory.PlaceAt(player, catalog, "blade", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "blade", 3, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(5, engine.GetDamage(player, first));
        }

        [Test]
        public void ChipRemovesRodCooldown()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#"),
                CombatTestFactory.Passive("chip", 0, 0, ItemTag.Chip, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem rod = CombatTestFactory.PlaceAt(player, catalog, "rod", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "chip", 1, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(0, engine.GetCooldownAfterUse(player, rod));
        }

        [Test]
        public void RodWithoutChipKeepsCooldown()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem rod = CombatTestFactory.PlaceAt(player, catalog, "rod", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(2, engine.GetCooldownAfterUse(player, rod));
        }

        [Test]
        public void AdjacentPlatesBoostActiveShieldUse()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("plate", 4, 0, ItemTag.Shield | ItemTag.Armor, "##\n##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem first = CombatTestFactory.PlaceAt(player, catalog, "plate", 0, 0);
            PlacedItem second = CombatTestFactory.PlaceAt(player, catalog, "plate", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(7, engine.GetShield(player, first));
            Assert.AreEqual(7, engine.GetShield(player, second));
        }

        [Test]
        public void SinglePlateKeepsBaseShield()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("plate", 4, 0, ItemTag.Shield | ItemTag.Armor, "##\n##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            PlacedItem plate = CombatTestFactory.PlaceAt(player, catalog, "plate", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(4, engine.GetShield(player, plate));
        }
    }
}

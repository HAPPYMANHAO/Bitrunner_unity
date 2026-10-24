using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ArmorTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        [Test]
        public void ReductionCurveMatchesDesignAnchors()
        {
            Assert.AreEqual(25, ArmorMath.ReductionPercent(12));
            Assert.AreEqual(86, ArmorMath.ReductionPercent(100));
            Assert.AreEqual(0, ArmorMath.ReductionPercent(0));
        }

        [Test]
        public void ArmorHasDiminishingReturns()
        {
            float first = ArmorMath.Reduction(12) - ArmorMath.Reduction(0);
            float second = ArmorMath.Reduction(24) - ArmorMath.Reduction(12);

            Assert.Greater(first, second);
            Assert.Greater(ArmorMath.Reduction(100), ArmorMath.Reduction(50));
        }

        [Test]
        public void DamageAfterArmorRoundsDownAndKeepsOnePoint()
        {
            Assert.AreEqual(7, ArmorMath.DamageAfterArmor(10, 12));
            Assert.AreEqual(8, ArmorMath.DamageAfterArmor(10, 8));
            Assert.AreEqual(1, ArmorMath.DamageAfterArmor(1, 100));
        }

        [Test]
        public void ArmorStacksPerDamageType()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("plate", 12, 8, 2, 1, 0, "##\n##"),
                CombatTestFactory.Armor("buckler", 3, 10, 0, 0, 0, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "plate", "buckler");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(15, engine.GetArmor(player, DamageType.Melee));
            Assert.AreEqual(18, engine.GetArmor(player, DamageType.Projectile));
            Assert.AreEqual(2, engine.GetArmor(player, DamageType.Burn));
            Assert.AreEqual(1, engine.GetArmor(player, DamageType.Toxin));
            Assert.AreEqual(0, engine.GetArmor(player, DamageType.Asphyxiation));
        }

        [Test]
        public void MeleeDamageUsesMeleeArmor()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("plate", 12, 0, 0, 0, 0, "##\n##"),
                CombatTestFactory.Weapon("blade", 10, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "plate");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "blade");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(23, player.Hp);
        }

        [Test]
        public void ProjectileDamageUsesProjectileArmor()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("plate", 12, 8, 0, 0, 0, "##\n##"),
                CombatTestFactory.Weapon("pulse", 10, 1, 0, ItemTag.Blade, "##", 10, DamageType.Projectile));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "plate");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "pulse");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(22, player.Hp);
        }
    }
}

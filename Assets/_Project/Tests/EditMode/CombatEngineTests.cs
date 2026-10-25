using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class CombatEngineTests
    {
        [Test]
        public void WeaponDealsDamageAndSpendsAp()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.AreEqual(15, enemy.Hp);
            Assert.AreEqual(2, player.Energy);
        }

        [Test]
        public void BlockAbsorbsDamageBeforeHp()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            enemy.SetShield(4);

            Assert.AreEqual(4, enemy.Shield);

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.AreEqual(0, enemy.Shield);
            Assert.AreEqual(19, enemy.Hp);
        }

        [Test]
        public void BlockResetsAtStartOfOwnersTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.ActiveSelf("shield", 1, 4, 0, false, StatusType.None, 2, ItemTag.Tool, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "shield");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem shield = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, shield.InstanceId, null, out error), error);
            Assert.AreEqual(4, player.Shield);

            engine.EndTurn();

            Assert.AreEqual(0, player.Shield);
        }


        [Test]
        public void WeaponDurabilityBreaksAfterTwoUses()
        {
            ItemDefinition bladeDefinition = CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##");
            bladeDefinition.MaxDurability = 2;
            ItemCatalog catalog = CombatTestFactory.Catalog(bladeDefinition);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.AreEqual(1, blade.Durability);

            engine.EndTurn();
            blade = CombatTestFactory.FirstItem(player);
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.IsNull(CombatTestFactory.FindItem(player, "blade"));
            Assert.IsTrue(HasEvent(engine, CombatEventType.ItemBroken, "blade"));
        }

        [Test]
        public void PlayerWithoutWeaponCanRetreat()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("vest", 1, 1, 1, 1, 1, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 4, 3, catalog, "vest");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.IsFalse(engine.CanPlayerAttack);

            string error;
            Assert.IsTrue(engine.TryRetreat(out error), error);
            Assert.AreEqual(CombatPhase.PlayerRetreat, engine.Phase);
            Assert.IsTrue(engine.IsFinished);
            Assert.IsTrue(HasEvent(engine, CombatEventType.CombatEnded, null));
        }

        [Test]
        public void ArmedPlayerCannotRetreat()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.IsTrue(engine.CanPlayerAttack);

            string error;
            Assert.IsFalse(engine.TryRetreat(out error));
            Assert.IsNotNull(error);
            Assert.AreEqual(CombatPhase.PlayerTurn, engine.Phase);
        }

        [Test]
        public void WeaponWithoutAmmoIsNotAnAttackOption()
        {
            ItemDefinition gunDefinition = CombatTestFactory.Weapon("gun", 5, 1, 0, ItemTag.Projectile, "##");
            gunDefinition.AmmoItemId = "ammo";
            ItemCatalog catalog = CombatTestFactory.Catalog(gunDefinition);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "gun");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.IsFalse(engine.CanPlayerAttack);

            string error;
            Assert.IsTrue(engine.TryRetreat(out error), error);
            Assert.AreEqual(CombatPhase.PlayerRetreat, engine.Phase);
        }

        [Test]
        public void BothSidesWithoutWeapons_EnemyFleesInsteadOfDeadlock()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("vest", 1, 1, 1, 1, 1, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "vest");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "vest");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            Assert.IsFalse(engine.CanPlayerAttack);
            Assert.IsFalse(engine.CanActorAttack(enemy));

            engine.EndTurn();
            Assert.IsFalse(enemy.HasFled, "僵持还不到 3 回合，敌人先不走");
            Assert.AreEqual(CombatPhase.PlayerTurn, engine.Phase);

            engine.EndTurn();
            Assert.IsFalse(enemy.HasFled);

            engine.EndTurn();
            Assert.IsTrue(enemy.HasFled);
            Assert.AreEqual(CombatPhase.EnemyFled, engine.Phase);
            Assert.IsTrue(HasEvent(engine, CombatEventType.ActorFled, null));
            Assert.IsTrue(engine.IsFinished);
        }

        [Test]
        public void WeaponlessEnemyStaysWhilePlayerCanAttack()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.Armor("vest", 1, 1, 1, 1, 1, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "vest");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            Assert.IsTrue(engine.CanPlayerAttack);
            Assert.IsFalse(engine.CanActorAttack(enemy));

            engine.EndTurn();

            Assert.IsFalse(enemy.HasFled);
            Assert.AreEqual(CombatPhase.PlayerTurn, engine.Phase);
        }

        [Test]
        public void EnemyWithWeaponDoesNotFlee()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 3, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "blade");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            Assert.IsTrue(engine.CanActorAttack(enemy));

            engine.EndTurn();

            Assert.IsFalse(enemy.HasFled);
            Assert.AreEqual(CombatPhase.PlayerTurn, engine.Phase);
        }

        [Test]
        public void DurabilitySurvivesAcrossCombats()
        {
            ItemDefinition bladeDefinition = CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##");
            bladeDefinition.MaxDurability = 2;
            ItemCatalog catalog = CombatTestFactory.Catalog(bladeDefinition);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor firstEnemy = CombatTestFactory.Actor("enemy-a", 20, 0, 4, 3, catalog);
            var firstCombat = new CombatEngine(player, new[] { firstEnemy }, catalog);
            firstCombat.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(firstCombat.TryUseItem(player, blade.InstanceId, firstEnemy, out error), error);
            Assert.AreEqual(1, blade.Durability);

            CombatActor secondEnemy = CombatTestFactory.Actor("enemy-b", 20, 0, 4, 3, catalog);
            var secondCombat = new CombatEngine(player, new[] { secondEnemy }, catalog);
            secondCombat.Start();

            blade = CombatTestFactory.FirstItem(player);
            Assert.AreEqual(1, secondCombat.GetDurability(blade));
            Assert.IsTrue(secondCombat.TryUseItem(player, blade.InstanceId, secondEnemy, out error), error);
            Assert.IsNull(CombatTestFactory.FindItem(player, "blade"));
        }

        [Test]
        public void ArmorDurabilityBreaksAfterTwoHits()
        {
            ItemDefinition vestDefinition = CombatTestFactory.Passive("vest", 0, 0, ItemTag.Armor, "##");
            vestDefinition.MaxDurability = 2;
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 1, 1, 0, ItemTag.Blade, "##"),
                vestDefinition);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog, "vest");

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.AreEqual(1, CombatTestFactory.FindItem(enemy, "vest").Durability);

            engine.EndTurn();
            blade = CombatTestFactory.FirstItem(player);
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.IsNull(CombatTestFactory.FindItem(enemy, "vest"));
            Assert.IsTrue(HasEvent(engine, CombatEventType.ItemBroken, "vest"));
        }

        [Test]
        public void ActiveShieldSpendsEnergy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.ActiveSelf("barrier", 2, 4, 0, false, StatusType.None, 2, ItemTag.Tool, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "barrier");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem barrier = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, barrier.InstanceId, null, out error), error);
            Assert.AreEqual(4, player.Shield);
            Assert.AreEqual(1, player.Energy);

            engine.EndTurn();
            Assert.AreEqual(0, player.Shield);
        }

        private static bool HasEvent(CombatEngine engine, CombatEventType type, string itemId)
        {
            for (int i = 0; i < engine.Events.Count; i++)
            {
                if (engine.Events[i].Type == type && engine.Events[i].ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void PassiveChipGrantsExtraAp()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("chip", 0, 1, ItemTag.Chip, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "chip");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(4, player.Energy);
        }

        [Test]
        public void OverloadReducesNextTurnAp()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.ActiveEnemy("jammer", 1, 0, StatusType.Overload, 1, ItemTag.Tool | ItemTag.Chip, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "jammer");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(0, player.GetStatusStacks(StatusType.Overload));
            Assert.AreEqual(2, player.Energy);
        }

        [Test]
        public void ShortCircuitAddsDamage()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.ActiveEnemy("shock", 1, 0, StatusType.ShortCircuit, 2, ItemTag.Tool, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade", "shock");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem shock = CombatTestFactory.FindItem(player, "shock");
            PlacedItem blade = CombatTestFactory.FindItem(player, "blade");

            string error;
            Assert.IsTrue(engine.TryUseItem(player, shock.InstanceId, enemy, out error), error);
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            Assert.AreEqual(13, enemy.Hp);
        }

        [Test]
        public void CacheRetainsBlockAtStartOfTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.ActiveSelf("shield", 1, 4, 0, false, StatusType.None, 2, ItemTag.Tool, "#"),
                CombatTestFactory.ActiveSelf("cache", 1, 0, 0, false, StatusType.Cache, 1, ItemTag.Tool, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "shield", "cache");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, CombatTestFactory.FindItem(player, "shield").InstanceId, null, out error), error);
            Assert.IsTrue(engine.TryUseItem(player, CombatTestFactory.FindItem(player, "cache").InstanceId, null, out error), error);

            engine.EndTurn();

            Assert.AreEqual(4, player.Shield);
            Assert.AreEqual(0, player.GetStatusStacks(StatusType.Cache));
        }

        [Test]
        public void CooldownSkipsOneTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "rod");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 40, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem rod = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, rod.InstanceId, enemy, out error), error);
            Assert.AreEqual(2, player.GetCooldown(rod.InstanceId));

            engine.EndTurn();
            Assert.IsFalse(engine.CanUseItem(player, rod, out error));

            engine.EndTurn();
            Assert.IsTrue(engine.CanUseItem(player, rod, out error));
        }

        [Test]
        public void ItemCanOnlyBeUsedOncePerTurn()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);
            Assert.IsFalse(engine.TryUseItem(player, blade.InstanceId, enemy, out error));
            StringAssert.Contains("已经使用", error);

            engine.EndTurn();
            Assert.IsTrue(engine.CanUseItem(player, blade, out error));
        }

        [Test]
        public void ConsumableHealsAndIsRemoved()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("heavy", 12, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.ActiveSelf("patch", 1, 0, 8, true, StatusType.None, 2, ItemTag.Consumable, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "patch");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 40, 3, 4, 3, catalog, "heavy");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(18, player.Hp);

            PlacedItem patch = CombatTestFactory.FindItem(player, "patch");
            string error;
            Assert.IsTrue(engine.TryUseItem(player, patch.InstanceId, player, out error), error);

            Assert.AreEqual(26, player.Hp);
            Assert.IsNull(CombatTestFactory.FindItem(player, "patch"));
        }

        [Test]
        public void KillingAllEnemiesEndsCombat()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 4, 3, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, CombatTestFactory.FirstItem(player).InstanceId, enemy, out error), error);

            Assert.AreEqual(0, enemy.Hp);
            Assert.AreEqual(CombatPhase.Victory, engine.Phase);
        }

        [Test]
        public void ShieldAbsorbsBeforeArmor()
        {
            ItemDefinition vest = CombatTestFactory.Armor(
                "vest", 12, 0, 0, 0, 0, "##\n##");
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 10, 1, 0, ItemTag.Blade, "##"),
                vest);

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "vest");

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            enemy.SetShield(5);

            PlacedItem blade = CombatTestFactory.FirstItem(player);
            string error;
            Assert.IsTrue(engine.TryUseItem(player, blade.InstanceId, enemy, out error), error);

            // 10 damage - 5 shield = 5, then 12 melee armor (25%) = 3 HP damage.
            Assert.AreEqual(0, enemy.Shield);
            Assert.AreEqual(17, enemy.Hp);
        }

        [Test]
        public void ProjectileWeaponConsumesAmmoFromTheSameBag()
        {
            ItemDefinition pulse = CombatTestFactory.Weapon(
                "pulse", 4, 1, 0, ItemTag.Blade, "##", 10, DamageType.Projectile);
            pulse.AmmoItemId = "ammo";
            pulse.AmmoPerUse = 1;

            ItemDefinition ammo = new ItemDefinition
            {
                Id = "ammo",
                DisplayName = "ammo",
                Category = ItemCategory.Ammo,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#"),
                MaxStack = 30,
                Value = 1
            };

            ItemCatalog catalog = CombatTestFactory.Catalog(pulse, ammo);
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 4, 3, catalog, "pulse");
            PlacedItem ammoItem;
            player.Bag.TryAddAt("ammo", ammo.Shape, new Cell(2, 0), 0, true, 2, out ammoItem, out _);

            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog);
            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, CombatTestFactory.FindItem(player, "pulse").InstanceId, enemy, out error), error);
            Assert.AreEqual(1, player.Bag.CountOf("ammo"));
            Assert.AreEqual(16, enemy.Hp);

            engine.EndTurn();
            Assert.IsTrue(engine.TryUseItem(player, CombatTestFactory.FindItem(player, "pulse").InstanceId, enemy, out error), error);
            Assert.AreEqual(0, player.Bag.CountOf("ammo"));
            Assert.AreEqual(12, enemy.Hp);

            engine.EndTurn();
            Assert.IsFalse(engine.TryUseItem(player, CombatTestFactory.FindItem(player, "pulse").InstanceId, enemy, out error));
            StringAssert.Contains("弹药", error);
        }

        [Test]
        public void EnemyUsesTheSameDamageRules()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "blade");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(25, player.Hp);
        }
    }
}

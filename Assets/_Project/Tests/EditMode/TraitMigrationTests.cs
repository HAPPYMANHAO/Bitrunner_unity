using System;
using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class TraitMigrationTests
    {
        private static ItemDefinition WithTraits(ItemDefinition definition, params TraitRef[] traits)
        {
            definition.Traits = traits;

            // 词条 rank 不能超过物品自身等级（ItemCatalog 强校验）。测试关心的是词条行为，
            // 不是档位是否合理，所以按最高 rank 自动抬档，免得每个用例都手写 Level。
            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i].Rank > definition.Level)
                {
                    definition.Level = traits[i].Rank;
                }
            }

            return definition;
        }

        [Test]
        public void EnergyCellTraitAddsPassiveEnergy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("crystal", 0, 2, ItemTag.Chip | ItemTag.Energy, "#"));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "crystal");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(5, player.Energy);
        }

        [Test]
        public void ShieldPistonTurnsCurrentShieldIntoDamage()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                WithTraits(
                    CombatTestFactory.Weapon("piston", 0, 1, 0, ItemTag.Mechanical, "##"),
                    new TraitRef(TraitCatalog.ShieldPiston, 1)));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 40, 0, 4, 3, catalog);
            PlacedItem piston = CombatTestFactory.PlaceAt(player, catalog, "piston", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            player.SetShield(7);

            Assert.AreEqual(3, engine.GetDamage(player, piston));
        }

        [Test]
        public void NanitesTraitGrantsNanorepairOnUse()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                WithTraits(
                    CombatTestFactory.ActiveSelf("patch", 1, 0, 8, true, StatusType.None, 1, ItemTag.Consumable, "#"),
                    new TraitRef(TraitCatalog.Nanites, 1)));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            player.SetHp(10);
            PlacedItem patch = CombatTestFactory.PlaceAt(player, catalog, "patch", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, patch.InstanceId, null, out error), error);
            Assert.AreEqual(18, player.Hp);
            Assert.AreEqual(2, player.GetStatusStacks(StatusType.Nanorepair));
        }

        [Test]
        public void SignalJamTraitAppliesOverloadToEnemy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                WithTraits(
                    CombatTestFactory.ActiveEnemy("jammer", 1, 0, StatusType.None, 1, ItemTag.Tool | ItemTag.Chip, "#"),
                    new TraitRef(TraitCatalog.SignalJam, 1)));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            PlacedItem jammer = CombatTestFactory.PlaceAt(player, catalog, "jammer", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, jammer.InstanceId, enemy, out error), error);
            Assert.AreEqual(1, enemy.GetStatusStacks(StatusType.Overload));
        }

        [Test]
        public void OverheatStacksFromTraitsRespectBuffCap()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                WithTraits(
                    CombatTestFactory.ActiveEnemy("rod_a", 1, 0, StatusType.None, 2, ItemTag.Blade, "#"),
                    new TraitRef(TraitCatalog.OverheatStrike, 5)),
                WithTraits(
                    CombatTestFactory.ActiveEnemy("rod_b", 1, 0, StatusType.None, 2, ItemTag.Blade, "#"),
                    new TraitRef(TraitCatalog.OverheatStrike, 5)),
                WithTraits(
                    CombatTestFactory.ActiveEnemy("rod_c", 1, 0, StatusType.None, 2, ItemTag.Blade, "#"),
                    new TraitRef(TraitCatalog.OverheatStrike, 5)));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 100, 0, 4, 3, catalog);
            PlacedItem rodA = CombatTestFactory.PlaceAt(player, catalog, "rod_a", 0, 0);
            PlacedItem rodB = CombatTestFactory.PlaceAt(player, catalog, "rod_b", 1, 0);
            PlacedItem rodC = CombatTestFactory.PlaceAt(player, catalog, "rod_c", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            string error;
            Assert.IsTrue(engine.TryUseItem(player, rodA.InstanceId, enemy, out error), error);
            Assert.IsTrue(engine.TryUseItem(player, rodB.InstanceId, enemy, out error), error);
            Assert.IsTrue(engine.TryUseItem(player, rodC.InstanceId, enemy, out error), error);

            Assert.AreEqual(BuffLibrary.MaxStacks(StatusType.Overheat), enemy.GetStatusStacks(StatusType.Overheat));
        }

        [Test]
        public void PurgeRunsBeforeEnergyAndLoadReappliesAfter()
        {
            ItemDefinition core = WithTraits(
                new ItemDefinition
                {
                    Id = "core",
                    DisplayName = "core",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy
                },
                new TraitRef(TraitCatalog.EnergyCell, 2),
                new TraitRef(TraitCatalog.OverloadLoad, 1));

            ItemDefinition loop = WithTraits(
                new ItemDefinition
                {
                    Id = "loop",
                    DisplayName = "loop",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Cooling
                },
                new TraitRef(TraitCatalog.OverloadPurge, 1));

            ItemCatalog catalog = CombatTestFactory.Catalog(core, loop);
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "core", 0, 0);
            CombatTestFactory.PlaceAt(player, catalog, "loop", 2, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(5, player.Energy);

            engine.EndTurn();

            Assert.AreEqual(5, player.Energy);
            Assert.AreEqual(1, player.GetStatusStacks(StatusType.Overload));
        }

        [Test]
        public void CoolantFlowGrantsCoolantAtTurnStart()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                WithTraits(
                    new ItemDefinition
                    {
                        Id = "loop",
                        DisplayName = "loop",
                        Category = ItemCategory.Tool,
                        Activation = ItemActivation.Passive,
                        Shape = ItemShape.Parse("##"),
                        Tags = ItemTag.Cooling
                    },
                    new TraitRef(TraitCatalog.CoolantFlow, 1)));
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            CombatTestFactory.PlaceAt(player, catalog, "loop", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(1, player.GetStatusStacks(StatusType.Coolant));
        }

        [Test]
        public void DefaultLibraryUsesMigratedTraits()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsTrue(catalog.Get("rod").HasTrait(TraitCatalog.OverheatStrike));
            Assert.IsTrue(catalog.Get("jammer").HasTrait(TraitCatalog.SignalJam));
            Assert.IsTrue(catalog.Get("patch").HasTrait(TraitCatalog.Nanites));
            Assert.IsTrue(catalog.Get("buckler").HasTrait(TraitCatalog.EvasionGuard));
            Assert.IsTrue(catalog.Get("corrosive_dart").HasTrait(TraitCatalog.CorrosionDose));
            Assert.IsTrue(catalog.Get("targeting_array").HasTrait(TraitCatalog.OverclockFlow));
            Assert.IsTrue(catalog.Get("ram_piston").HasTrait(TraitCatalog.ShieldPiston));
            Assert.IsTrue(catalog.Get("coolant_loop").HasTrait(TraitCatalog.OverloadPurge));
            Assert.IsTrue(catalog.Get("coolant_loop").HasTrait(TraitCatalog.CoolantFlow));
            Assert.AreEqual(1, catalog.Get("chip").GetTraitRank(TraitCatalog.EnergyCell));
            Assert.AreEqual(2, catalog.Get("overclock_core").GetTraitRank(TraitCatalog.EnergyCell));
            Assert.IsTrue(catalog.Get("overclock_core").HasTrait(TraitCatalog.OverloadLoad));
        }

        [Test]
        public void DuplicateTraitsOnOneItemAreRejected()
        {
            ItemDefinition broken = WithTraits(
                CombatTestFactory.Weapon("broken", 5, 1, 0, ItemTag.Blade, "##"),
                new TraitRef(TraitCatalog.KeenEdge, 1),
                new TraitRef(TraitCatalog.KeenEdge, 2));

            Assert.Throws<ArgumentException>(() => CombatTestFactory.Catalog(broken));
        }

        [Test]
        public void UnknownTraitIdsAreIgnoredWithoutThrowing()
        {
            ItemDefinition unknown = WithTraits(
                CombatTestFactory.Weapon("weird", 5, 1, 0, ItemTag.None, "#"),
                new TraitRef("no_such_trait", 3));
            ItemCatalog catalog = CombatTestFactory.Catalog(unknown);
            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);
            PlacedItem weird = CombatTestFactory.PlaceAt(player, catalog, "weird", 0, 0);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();

            Assert.AreEqual(5, engine.GetDamage(player, weird));
            IReadOnlyList<string> lines = ItemText.TraitLines(unknown);
            Assert.AreEqual(0, lines.Count);
        }
    }
}

using System;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// C-3：CPU 芯片的全局加成。
    ///
    /// 芯片插在 CPU 网格里、不在战斗背包里，所以它们的效果不经过背包词条扫描，
    /// 而是由 <see cref="TraitEngine.EvaluateChips"/> 汇总成 <see cref="GlobalModifiers"/>，
    /// 在创建 <see cref="CombatActor"/> 时注入。
    /// </summary>
    public class CpuModifierTests
    {
        private static ItemDefinition Chip(string id, int level, params TraitRef[] traits)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Level = level,
                Category = ItemCategory.Tool,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#"),
                Tags = ItemTag.Chip,
                EnergyCost = 0,
                Traits = traits,
                Value = 10
            };
        }

        private static GlobalModifiers Evaluate(ItemCatalog catalog, BagInventory grid)
        {
            return new TraitEngine(catalog).EvaluateChips(grid);
        }

        [Test]
        public void EmptyGrid_PublishesNoModifiers()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("hp1", 1, new TraitRef(TraitCatalog.HpChip, 1)));
            GlobalModifiers modifiers = Evaluate(catalog, new BagInventory(2, 2));

            Assert.IsTrue(modifiers.IsEmpty);
            Assert.AreEqual(0, modifiers.MaxHp);
            Assert.AreEqual(0, modifiers.BaseEnergy);
            Assert.AreEqual(0, modifiers.ArmorBase);
        }

        [Test]
        public void NullGrid_IsTolerated()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("hp1", 1, new TraitRef(TraitCatalog.HpChip, 1)));

            Assert.IsTrue(Evaluate(catalog, null).IsEmpty);
        }

        [Test]
        public void HpChips_StackAdditively()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("hp1", 1, new TraitRef(TraitCatalog.HpChip, 1)),
                Chip("hp2", 2, new TraitRef(TraitCatalog.HpChip, 2)),
                Chip("hp3", 3, new TraitRef(TraitCatalog.HpChip, 3)));
            var grid = new BagInventory(3, 3);
            PlacedItem a, b, c;
            grid.TryAdd("hp1", catalog.Get("hp1").Shape, out a);
            grid.TryAdd("hp2", catalog.Get("hp2").Shape, out b);
            grid.TryAdd("hp3", catalog.Get("hp3").Shape, out c);

            // 15 + 30 + 50
            Assert.AreEqual(95, Evaluate(catalog, grid).MaxHp);
        }

        [Test]
        public void ArmorChips_StackAdditively()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("ar1", 1, new TraitRef(TraitCatalog.ArmorChip, 1)),
                Chip("ar2", 3, new TraitRef(TraitCatalog.ArmorChip, 3)));
            var grid = new BagInventory(2, 2);
            PlacedItem a, b;
            grid.TryAdd("ar1", catalog.Get("ar1").Shape, out a);
            grid.TryAdd("ar2", catalog.Get("ar2").Shape, out b);

            // 1 + 3
            Assert.AreEqual(4, Evaluate(catalog, grid).ArmorBase);
        }

        [Test]
        public void EnergyChips_TakeHighestInsteadOfSumming()
        {
            // 能量芯片最危险：如果能叠加，塞满 CPU 就能无限行动。
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("en1", 1,
                    new TraitRef(TraitCatalog.BaseEnergyChip, 1),
                    new TraitRef(TraitCatalog.EnergyDrawback, 1)),
                Chip("en2", 2,
                    new TraitRef(TraitCatalog.BaseEnergyChip, 2),
                    new TraitRef(TraitCatalog.EnergyDrawback, 2)));
            var grid = new BagInventory(2, 2);
            PlacedItem a, b;
            grid.TryAdd("en1", catalog.Get("en1").Shape, out a);
            grid.TryAdd("en2", catalog.Get("en2").Shape, out b);

            GlobalModifiers modifiers = Evaluate(catalog, grid);
            Assert.AreEqual(1, modifiers.BaseEnergy);

            // 代价照旧叠加：-18 + -15
            Assert.AreEqual(-33, modifiers.MaxHp);
        }

        [Test]
        public void EnergyChip_WithoutDrawback_IsRejectedByCatalog()
        {
            ItemDefinition free = Chip("free", 1, new TraitRef(TraitCatalog.BaseEnergyChip, 1));

            var error = Assert.Throws<ArgumentException>(() => CombatTestFactory.Catalog(free));
            StringAssert.Contains("副作用", error.Message);
        }

        [Test]
        public void EnergyChip_WithDrawback_IsAcceptedAndCostsMaxHp()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("en1", 1,
                    new TraitRef(TraitCatalog.BaseEnergyChip, 1),
                    new TraitRef(TraitCatalog.EnergyDrawback, 1)));
            var grid = new BagInventory(1, 1);
            PlacedItem placed;
            grid.TryAdd("en1", catalog.Get("en1").Shape, out placed);

            GlobalModifiers modifiers = Evaluate(catalog, grid);
            Assert.AreEqual(1, modifiers.BaseEnergy);
            Assert.AreEqual(-18, modifiers.MaxHp);
            Assert.AreEqual(22, modifiers.ApplyMaxHp(40));
        }

        [Test]
        public void ApplyMaxHp_ClampsToOne()
        {
            var modifiers = new GlobalModifiers { MaxHp = -500 };

            Assert.AreEqual(1, modifiers.ApplyMaxHp(40));
        }

        [Test]
        public void ApplyArmorBase_NeverNegative()
        {
            var modifiers = new GlobalModifiers { ArmorBase = -4 };

            Assert.AreEqual(0, modifiers.ApplyArmorBase());
        }

        [Test]
        public void NegativeMaxHp_ChipReducesHealthWithoutTouchingArmor()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                Chip("cost", 1, new TraitRef(TraitCatalog.EnergyDrawback, 1)));
            var grid = new BagInventory(1, 1);
            PlacedItem placed;
            grid.TryAdd("cost", catalog.Get("cost").Shape, out placed);

            GlobalModifiers modifiers = Evaluate(catalog, grid);
            Assert.AreEqual(-18, modifiers.MaxHp);
            Assert.AreEqual(0, modifiers.ArmorBase);
            Assert.IsFalse(modifiers.IsEmpty);
        }

        [Test]
        public void ArmorBase_AddsToEngineArmor()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Loot("coin", 10, "#"));
            var player = new CombatActor("player", "player", 30, 3, new BagInventory(4, 4), true, 3);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            // 没有护甲装备，护甲全部来自基底。
            Assert.AreEqual(3, engine.GetArmor(player, DamageType.Melee));
            Assert.AreEqual(3, engine.GetArmor(player, DamageType.Asphyxiation));
            Assert.AreEqual(0, engine.GetArmor(enemy, DamageType.Melee));
        }

        [Test]
        public void ArmorBase_StacksOnTopOfBagArmor()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Armor("plate", 4, 2, 0, 0, 0, "##\n##"));
            var player = new CombatActor("player", "player", 30, 3, new BagInventory(4, 4), true, 3);
            CombatTestFactory.PlaceAt(player, catalog, "plate", 0, 0);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 0, 4, 3, catalog);

            var engine = new CombatEngine(player, new[] { enemy }, catalog);

            Assert.AreEqual(7, engine.GetArmor(player, DamageType.Melee));
            Assert.AreEqual(5, engine.GetArmor(player, DamageType.Projectile));
            Assert.AreEqual(3, engine.GetArmor(player, DamageType.Toxin));
        }

        [Test]
        public void Combine_KeepsEnergyAtMaxButSumsTheRest()
        {
            var left = new GlobalModifiers { MaxHp = 10, BaseEnergy = 1, ArmorBase = 2 };
            var right = new GlobalModifiers { MaxHp = 5, BaseEnergy = 1, ArmorBase = 1 };

            GlobalModifiers combined = left.Combine(right);

            Assert.AreEqual(15, combined.MaxHp);
            Assert.AreEqual(1, combined.BaseEnergy);
            Assert.AreEqual(3, combined.ArmorBase);
        }

        [Test]
        public void DefaultCatalogCarriesChipTraits()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            // 内容还没加芯片（C-4），但词条表必须先就位，否则内容一写就是未知词条。
            Assert.IsTrue(TraitCatalog.Default.TryGet(TraitCatalog.HpChip, out _));
            Assert.IsTrue(TraitCatalog.Default.TryGet(TraitCatalog.BaseEnergyChip, out _));
            Assert.IsTrue(TraitCatalog.Default.TryGet(TraitCatalog.ArmorChip, out _));
            Assert.IsTrue(TraitCatalog.Default.TryGet(TraitCatalog.EnergyDrawback, out _));
            Assert.IsTrue(catalog.Count > 0);
        }
    }
}

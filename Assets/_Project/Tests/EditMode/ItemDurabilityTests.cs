using System;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>装备耐久必须写在数据里，不能靠隐藏默认值。</summary>
    public class ItemDurabilityTests
    {
        [Test]
        public void EveryWeaponAndArmorDeclaresDurability()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            int checkedCount = 0;
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (!definition.HasDurability)
                {
                    continue;
                }

                checkedCount++;
                Assert.GreaterOrEqual(definition.MaxDurability, 0, definition.Id + " 必须显式声明 MaxDurability");
                Assert.AreEqual(definition.MaxDurability, definition.DurabilityMax, definition.Id);
            }

            Assert.GreaterOrEqual(checkedCount, 30, "武器/护甲数量异常，校验可能没跑起来");
        }

        [Test]
        public void CatalogRejectsWeaponThatOmitsDurability()
        {
            var definition = new ItemDefinition
            {
                Id = "test_undurable_blade",
                DisplayName = "测试刀",
                Category = ItemCategory.Weapon,
                Activation = ItemActivation.Active,
                Target = ItemTarget.Enemy,
                Shape = ItemShape.Parse("#")
            };

            var error = Assert.Throws<ArgumentException>(() => new ItemCatalog(new[] { definition }));
            StringAssert.Contains("MaxDurability", error.Message);
        }

        [Test]
        public void CatalogRejectsArmorThatOmitsDurability()
        {
            var definition = new ItemDefinition
            {
                Id = "test_undurable_armor",
                DisplayName = "测试甲",
                Category = ItemCategory.Armor,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#")
            };

            Assert.Throws<ArgumentException>(() => new ItemCatalog(new[] { definition }));
        }

        [Test]
        public void UnsetDurabilityIsZeroNotEight()
        {
            var definition = new ItemDefinition
            {
                Id = "test_unset",
                Category = ItemCategory.Armor,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#")
            };

            Assert.AreEqual(-1, definition.MaxDurability, "未填写时应保持 -1，便于校验发现");
            Assert.AreEqual(0, definition.DurabilityMax, "不能再退回隐藏的 8");
        }

        [Test]
        public void ExplicitZeroDurabilityMeansIndestructible()
        {
            var definition = new ItemDefinition
            {
                Id = "test_bite",
                DisplayName = "撕咬",
                Category = ItemCategory.Weapon,
                Activation = ItemActivation.Active,
                Target = ItemTarget.Enemy,
                Shape = ItemShape.Parse("#"),
                Damage = 2,
                MaxDurability = 0
            };

            var catalog = new ItemCatalog(new[] { definition });
            Assert.AreEqual(0, catalog.Get("test_bite").DurabilityMax);
        }
    }
}

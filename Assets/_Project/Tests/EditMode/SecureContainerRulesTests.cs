using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class SecureContainerRulesTests
    {
        [Test]
        public void OneByOneLootIsEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsTrue(SecureContainerRules.IsEligible(catalog.Get("coin")));
        }

        [Test]
        public void OneByOneConsumableIsEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsTrue(SecureContainerRules.IsEligible(catalog.Get("patch")));
        }

        [Test]
        public void WeaponsAreNotEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsFalse(SecureContainerRules.IsEligible(catalog.Get("blade")));
            Assert.IsFalse(SecureContainerRules.IsEligible(catalog.Get("scrap_blade")));
        }

        [Test]
        public void ArmorIsNotEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsFalse(SecureContainerRules.IsEligible(catalog.Get("plate")));
            Assert.IsFalse(SecureContainerRules.IsEligible(catalog.Get("buckler")));
        }

        [Test]
        public void MultiCellLootIsEligibleWhenItFits()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            // 2×2 的雕像正好占满一个初始保险箱：这是"保底带走"该有的样子。
            Assert.IsTrue(SecureContainerRules.IsEligible(catalog.Get("idol")));
        }

        [Test]
        public void ActionItemsAreNotEligible()
        {
            ItemCatalog catalog = ExpeditionActionItemLibrary.CreateCatalog();

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                Assert.IsFalse(SecureContainerRules.IsEligible(definition), definition.Id);
            }
        }

        [Test]
        public void OneByOneToolsAreEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsTrue(SecureContainerRules.IsEligible(catalog.Get("chip")));
            Assert.IsTrue(SecureContainerRules.IsEligible(catalog.Get("jammer")));
        }
    }
}

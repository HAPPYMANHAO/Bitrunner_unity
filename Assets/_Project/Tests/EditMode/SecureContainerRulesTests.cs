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
        public void MultiCellLootIsNotEligible()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            Assert.IsFalse(SecureContainerRules.IsEligible(catalog.Get("idol")));
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

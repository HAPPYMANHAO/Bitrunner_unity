using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 词条可见性守卫：武器/护甲必须挂词条，否则提示里只剩「标签」，玩家会以为联动消失了。
    /// </summary>
    public class TraitCoverageTests
    {
        [Test]
        public void EveryWeaponAndArmorCarriesATrait()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            int checkedCount = 0;

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (!definition.HasDurability)
                {
                    continue;
                }

                if (definition.Id == "wild_bite")
                {
                    // 野兽的天然武器，不掉落也不进背包，不需要词条。
                    continue;
                }

                checkedCount++;
                Assert.IsNotNull(definition.Traits, definition.Id + " 没有词条，提示里只会剩标签");
                Assert.Greater(definition.Traits.Length, 0, definition.Id + " 没有词条，提示里只会剩标签");
            }

            Assert.GreaterOrEqual(checkedCount, 30, "武器/护甲数量异常，校验可能没跑起来");
        }

        [Test]
        public void PassiveArmorWithoutShieldHasNoOnUseTraits()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (definition.Category != ItemCategory.Armor)
                {
                    continue;
                }

                // 有护盾的被动甲可以被「使用」（CombatEngine.CanUseItem），OnUse 词条会触发；
                // 没有护盾又不能用的被动甲挂 OnUse 词条就是永远不会亮的死条目。
                if (definition.IsActive || definition.Shield > 0)
                {
                    continue;
                }

                if (definition.Traits == null)
                {
                    continue;
                }

                for (int i = 0; i < definition.Traits.Length; i++)
                {
                    TraitDefinition trait;
                    Assert.IsTrue(
                        TraitCatalog.Default.TryGet(definition.Traits[i].TraitId, out trait),
                        definition.Id + " 引用了未知词条 " + definition.Traits[i].TraitId);
                    Assert.AreNotEqual(
                        TraitTrigger.OnUse,
                        trait.Trigger,
                        definition.Id + " 的「" + trait.DisplayName + "」是 OnUse，但这件甲既不能用也没有护盾，永远不会触发");
                }
            }
        }

        [Test]
        public void TraitBlockRendersInTooltip()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            string text = ItemText.Describe(catalog.Get("shiv"));

            StringAssert.Contains("词条", text);
            StringAssert.Contains("锋锐", text);
            StringAssert.Contains("标签", text);
        }
    }
}

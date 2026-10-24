using System;

namespace BitRunner.Core
{
    public static class ItemUse
    {
        public static bool CanUseOutsideCombat(ItemDefinition definition)
        {
            return definition != null
                   && definition.Activation == ItemActivation.Active
                   && definition.Target == ItemTarget.Self
                   && definition.Heal > 0;
        }

        public static bool TryUseOutsideCombat(
            ItemDefinition definition,
            BagInventory bag,
            int instanceId,
            int currentHp,
            int maxHp,
            out int newHp,
            out string message)
        {
            newHp = currentHp;

            if (definition == null)
            {
                message = "未知物品。";
                return false;
            }

            PlacedItem item;
            if (!bag.TryGet(instanceId, out item))
            {
                message = "物品不存在。";
                return false;
            }

            if (!CanUseOutsideCombat(definition))
            {
                message = definition.Activation == ItemActivation.Passive
                    ? "被动物品无需使用。"
                    : "该物品只能在战斗中使用。";
                return false;
            }

            if (currentHp >= maxHp)
            {
                message = "生命已满，不需要使用。";
                return false;
            }

            int healed = Math.Min(definition.Heal, maxHp - currentHp);
            newHp = currentHp + healed;

            if (definition.ConsumeOnUse)
            {
                bag.Remove(instanceId);
            }

            message = "使用 " + definition.DisplayName + "，恢复 " + healed + " 生命。";
            return true;
        }
    }
}

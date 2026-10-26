namespace BitRunner.Core
{
    /// <summary>
    /// 保险箱的准入规则。
    ///
    /// 保险箱是「阵亡也不会丢」的那几格，2×2（可扩容）。准入只看两件事：
    ///   1. 形状放得下——原来还额外要求"只能 1×1"，让 2×2 的箱子实际只有 4 个单据位，
    ///      连一件 1×2 的工具或 2×2 的雕像都装不进去，等于把"保底带走"锁死在最碎的东西上。
    ///      现在改成按形状判断：宽高都不超过 2 就行。
    ///   2. 不是战斗装备 / 货币 / 契约 / 行动道具——这些东西要么该在战斗中承担风险，
    ///      要么本来就不是"战利品"。
    /// </summary>
    public static class SecureContainerRules
    {
        public const int Width = 2;
        public const int Height = 2;

        public static bool IsEligible(ItemDefinition definition)
        {
            if (definition == null || definition.Shape == null)
            {
                return false;
            }

            if (definition.Shape.Width > Width || definition.Shape.Height > Height)
            {
                return false;
            }

            if (definition.Category == ItemCategory.Weapon
                || definition.Category == ItemCategory.Armor
                || definition.Category == ItemCategory.Currency
                || definition.Category == ItemCategory.Quest
                || definition.Category == ItemCategory.Action)
            {
                return false;
            }

            return true;
        }
    }
}

namespace BitRunner.Core
{
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

            if (definition.Shape.CellCount != 1)
            {
                return false;
            }

            if (definition.Category == ItemCategory.Weapon
                || definition.Category == ItemCategory.Armor
                || definition.Category == ItemCategory.Currency)
            {
                return false;
            }

            return true;
        }
    }
}

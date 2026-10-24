using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class ContainerSpec
    {
        public int Width = 1;
        public int Height = 1;
        public ItemCategory[] AllowedCategories = new ItemCategory[0];
        public ItemTag RequiredAnyTags = ItemTag.None;
        public bool AllowNestedContainers;

        public bool Accepts(ItemDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (!AllowNestedContainers && definition.Category == ItemCategory.Container)
            {
                return false;
            }

            if (AllowedCategories != null && AllowedCategories.Length > 0)
            {
                bool categoryAllowed = false;
                for (int i = 0; i < AllowedCategories.Length; i++)
                {
                    if (AllowedCategories[i] == definition.Category)
                    {
                        categoryAllowed = true;
                        break;
                    }
                }

                if (!categoryAllowed)
                {
                    return false;
                }
            }

            if (RequiredAnyTags != ItemTag.None && (definition.Tags & RequiredAnyTags) == 0)
            {
                return false;
            }

            return true;
        }

        public string FilterText
        {
            get
            {
                if (AllowedCategories == null || AllowedCategories.Length == 0)
                {
                    return "任意物品";
                }

                var names = new List<string>();
                for (int i = 0; i < AllowedCategories.Length; i++)
                {
                    names.Add(ItemText.CategoryName(AllowedCategories[i]));
                }

                return string.Join(" / ", names);
            }
        }
    }
}

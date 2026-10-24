using System.Collections.Generic;

namespace BitRunner.Core
{
    internal static class FacilityGrid
    {
        public static BagInventory Resize(BagInventory source, int width, int height)
        {
            var result = new BagInventory(width, height);
            var items = new List<PlacedItem>(source.Items);
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                PlacedItem placed;
                string error;
                if (result.TryAddAt(item.DefinitionId, item.BaseShape, item.Origin, item.Rotation, item.Rotatable, item.Count, out placed, out error))
                {
                    continue;
                }

                result.TryAdd(item.DefinitionId, item.BaseShape, item.Rotatable, item.Count, out placed);
            }

            return result;
        }
    }
}

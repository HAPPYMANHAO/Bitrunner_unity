using System.Collections.Generic;

namespace BitRunner.Core
{
    internal static class FacilityGrid
    {
        /// <summary>设施升级换格子。真正的实现在 <see cref="BagResize"/>，这里只是历史入口。</summary>
        public static BagInventory Resize(BagInventory source, int width, int height)
        {
            return BagResize.To(source, width, height);
        }
    }
}

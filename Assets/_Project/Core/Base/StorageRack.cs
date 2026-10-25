using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 仓储设备：扩容基地仓库。
    ///
    /// 仓库固定 10 列、与背包同尺寸的 16px 格子，横向明显比 6 列背包宽；
    /// 扩容只加行数（6 → 8 → 10），面板用纵向滚动条承载。
    /// 仓库越大，能同时养几套装备就越多，这件事直接决定「阵亡之后还有没有备用品」。
    /// </summary>
    public sealed class StorageRack
    {
        public const int MaxLevel = 3;
        public const int Width = 10;

        public int Level { get; private set; }

        public StorageRack()
        {
            Level = 1;
        }

        public static int HeightForLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 6;
                case 2:
                    return 8;
                default:
                    return 10;
            }
        }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        public int UpgradeCost
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return 350;
                    case 2:
                        return 800;
                    default:
                        return 0;
                }
            }
        }

        public string[] UpgradeMaterialIds
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return new[] { "scrap", "scrap", "scrap", "wire" };
                    case 2:
                        return new[] { "alloy", "alloy", "circuit", "circuit" };
                    default:
                        return new string[0];
                }
            }
        }

        public bool TryUpgrade(CampaignState campaign, BagInventory stash, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (Level >= MaxLevel)
            {
                error = "仓储设备已满级。";
                return false;
            }

            if (!FacilityUpgrade.TryPay(campaign, stash, UpgradeCost, UpgradeMaterialIds, out error))
            {
                return false;
            }

            Level++;
            error = null;
            return true;
        }
    }
}

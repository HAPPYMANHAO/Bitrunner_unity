using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 仓储设备：扩容基地仓库。
    ///
    /// 只加高不加宽——仓库面板已经有纵向滚动条，横向加宽要重做滚动与视口布局，
    /// 收益远不如直接把行数堆上去。仓库越大，能同时养几套装备就越多，
    /// 这件事直接决定「阵亡之后还有没有备用品」。
    /// </summary>
    public sealed class StorageRack
    {
        public const int MaxLevel = 3;
        public const int Width = 6;

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
                    return 10;
                case 2:
                    return 13;
                default:
                    return 16;
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

using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 子空间保险网络中枢：扩容保险箱。
    ///
    /// 保险箱是唯一「阵亡也不会丢」的格子，所以它的容量直接等于「一次探索能保底带走多少」。
    /// 只加宽/加高，不改变 <see cref="SecureContainerRules"/> 的准入规则（收能放进 2×2 的东西，
    /// 不收武器/护甲/比特）。
    /// </summary>
    public sealed class InsuranceHub
    {
        public const int MaxLevel = 3;

        public int Level { get; private set; }

        public InsuranceHub()
        {
            Level = 1;
        }

        internal void RestoreLevel(int level)
        {
            Level = level < 1 ? 1 : (level > MaxLevel ? MaxLevel : level);
        }

        public static int WidthForLevel(int level)
        {
            return level <= 1 ? 2 : 3;
        }

        public static int HeightForLevel(int level)
        {
            return level >= 3 ? 3 : 2;
        }

        /// <summary>扩容后的保险箱格子数，UI 拿它显示「保险箱 n/格」。</summary>
        public int CapacityFor(int level)
        {
            return WidthForLevel(level) * HeightForLevel(level);
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
                        return 300;
                    case 2:
                        return 700;
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
                        return new[] { "wire", "wire", "circuit" };
                    case 2:
                        return new[] { "circuit", "circuit", "alloy", "alloy" };
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
                error = "保险网络中枢已满级。";
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

using System;

namespace BitRunner.Core
{
    public sealed class BitMine
    {
        public const int MaxLevel = 3;
        public const int YieldPerCell = 5;
        public const string OutputItemId = ItemBitWallet.BitItemId;

        private bool[] _filled;
        private int _filledWeek;
        private int _filledDay;

        public BitMine()
        {
            Level = 1;
            Width = 1;
            Height = 1;
            _filled = new bool[1];
        }

        public int Level { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        public int UpgradeCost
        {
            get
            {
                if (Level >= MaxLevel)
                {
                    return 0;
                }

                return Level == 1 ? 100 : 250;
            }
        }

        public int FilledCells
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _filled.Length; i++)
                {
                    if (_filled[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int StoredBit
        {
            get { return FilledCells * YieldPerCell; }
        }

        public bool IsFilled(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return false;
            }

            return _filled[y * Width + x];
        }

        public void EnsureFilledForDay(int week, int day)
        {
            if (_filledWeek == week && _filledDay == day)
            {
                return;
            }

            _filledWeek = week;
            _filledDay = day;
            for (int i = 0; i < _filled.Length; i++)
            {
                _filled[i] = true;
            }
        }

        public int Collect()
        {
            int amount = StoredBit;
            for (int i = 0; i < _filled.Length; i++)
            {
                _filled[i] = false;
            }

            return amount;
        }

        public int TryCollect(CampaignState campaign, out bool collected)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            int amount = StoredBit;
            if (amount <= 0 || !campaign.AddBit(amount))
            {
                collected = false;
                return amount;
            }

            for (int i = 0; i < _filled.Length; i++)
            {
                _filled[i] = false;
            }

            collected = true;
            return amount;
        }

        public string[] UpgradeMaterialIds
        {
            get
            {
                if (Level >= MaxLevel)
                {
                    return new string[0];
                }

                return Level == 1
                    ? new[] { "scrap", "wire" }
                    : new[] { "alloy", "alloy", "circuit" };
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
                error = "比特矿场已满级。";
                return false;
            }

            if (!FacilityUpgrade.TryPay(campaign, stash, UpgradeCost, UpgradeMaterialIds, out error))
            {
                return false;
            }

            int nextLevel = Level + 1;
            int nextWidth = nextLevel == 1 ? 1 : 2;
            int nextHeight = nextLevel == 2 ? 1 : 2;
            var next = new bool[nextWidth * nextHeight];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_filled[y * Width + x] && x < nextWidth && y < nextHeight)
                    {
                        next[y * nextWidth + x] = true;
                    }
                }
            }

            Level = nextLevel;
            Width = nextWidth;
            Height = nextHeight;
            _filled = next;
            error = null;
            return true;
        }
    }
}

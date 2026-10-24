using System;

namespace BitRunner.Core
{
    public enum CampaignStatus
    {
        Active,
        Victory,
        Defeat
    }

    /// <summary>
    /// 战役的终结方式。债务是无限的，所以「还清」不再是结局，
    /// 必须由外部条件收束：击杀终局 Boss、主动逃亡，或者缴款失败破产。
    /// </summary>
    public enum CampaignEnding
    {
        None,
        BossSlain,
        Escaped,
        Bankrupt
    }

    public sealed class ExpeditionResult
    {
        public bool PaymentDue;
        public bool PaymentSucceeded;
        public int Paid;
        public int BitRemaining;
        public int DueThisWeek;
        public int TotalPaid;
        public int Week;
        public int Day;
        public CampaignStatus Status;
        public CampaignEnding Ending;
        public string Message;
    }

    /// <summary>
    /// 战役时钟与债务。债务没有终点：按曲线爬升到 <see cref="DebtCap"/> 之后每周固定上交，
    /// 直到玩家击杀终局 Boss、主动逃亡，或者某周缴不出来而破产。
    /// </summary>
    public sealed class CampaignState
    {
        public const int DaysPerWeek = 7;

        /// <summary>债务封顶额度。爬到这一档之后每周固定缴纳，不再增长。</summary>
        public const int DebtCap = 2000;

        private static readonly int[] DefaultCurve = { 400, 700, 1000, 1300, 1600, DebtCap };

        private readonly int[] _installments;
        private readonly IBitWallet _wallet;
        private int _weekIndex;
        private int _day;
        private int _totalPaid;
        private bool _expeditionOpen;

        /// <summary>每周应缴额度表。爬到最后一档后一直维持该额度。</summary>
        public static int[] CreateDefaultInstallments()
        {
            var copy = new int[DefaultCurve.Length];
            Array.Copy(DefaultCurve, copy, DefaultCurve.Length);
            return copy;
        }

        public CampaignState(int[] installments)
            : this(installments, new MemoryBitWallet(0))
        {
        }

        public CampaignState(int[] installments, int startingBit)
            : this(installments, new MemoryBitWallet(startingBit))
        {
        }

        public CampaignState(int[] installments, IBitWallet wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

            if (installments == null || installments.Length == 0)
            {
                throw new ArgumentException("At least one installment is required.", nameof(installments));
            }

            _installments = new int[installments.Length];
            for (int i = 0; i < installments.Length; i++)
            {
                if (installments[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be positive.");
                }

                _installments[i] = installments[i];
            }

            _day = 1;
            _weekIndex = 0;
            _totalPaid = 0;
            Status = CampaignStatus.Active;
            Ending = CampaignEnding.None;
        }

        public int Week
        {
            get { return _weekIndex + 1; }
        }

        public int Day
        {
            get { return _day; }
        }

        public int DaysRemaining
        {
            get { return DaysPerWeek - _day + 1; }
        }

        public int Bit
        {
            get { return _wallet.Amount; }
        }

        /// <summary>累计已上缴的 bit。无限债务下这是唯一的进度感指标。</summary>
        public int TotalPaid
        {
            get { return _totalPaid; }
        }

        /// <summary>本周应缴额度。战役结束后返回 0。</summary>
        public int CurrentDue
        {
            get
            {
                if (Status != CampaignStatus.Active)
                {
                    return 0;
                }

                int index = _weekIndex < _installments.Length ? _weekIndex : _installments.Length - 1;
                return _installments[index];
            }
        }

        /// <summary>本周额度是否已经爬到封顶档。</summary>
        public bool AtDebtCap
        {
            get
            {
                int last = _installments[_installments.Length - 1];
                return CurrentDue >= last;
            }
        }

        /// <summary>距离爬满债务曲线还有几周。</summary>
        public int WeeksUntilDebtCap
        {
            get
            {
                int remaining = _installments.Length - 1 - _weekIndex;
                return remaining > 0 ? remaining : 0;
            }
        }

        public bool IsActive
        {
            get { return Status == CampaignStatus.Active; }
        }

        public CampaignStatus Status { get; private set; }

        public CampaignEnding Ending { get; private set; }

        public bool ExpeditionOpen
        {
            get { return _expeditionOpen; }
        }

        public bool BeginExpedition()
        {
            if (!IsActive || _expeditionOpen)
            {
                return false;
            }

            _expeditionOpen = true;
            return true;
        }

        /// <summary>
        /// 结算一次探索。第 7 次探索结束时收缴本周额度：
        /// 缴得出就进入下一周（额度按曲线爬升），缴不出来就地破产。
        /// </summary>
        public ExpeditionResult SettleExpedition()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Campaign is already over.");
            }

            if (!_expeditionOpen)
            {
                throw new InvalidOperationException("No expedition is open.");
            }

            _expeditionOpen = false;
            _day++;

            if (_day <= DaysPerWeek)
            {
                return BuildResult(false, false, 0, 0, "Day " + _day);
            }

            int due = CurrentDue;
            if (!_wallet.TrySpend(due))
            {
                EndCampaign(CampaignEnding.Bankrupt);
                return BuildResult(true, false, 0, due, "Payment failed");
            }

            _totalPaid += due;
            _weekIndex++;
            _day = 1;
            return BuildResult(true, true, due, due, "Week " + Week);
        }

        /// <summary>
        /// 主动终结战役。破产、击杀终局 Boss、逃亡都走这里；
        /// <see cref="CampaignEnding.Bankrupt"/> 记为失败，其余记为胜利。
        /// 战役结束后不再接受新的探索。
        /// </summary>
        public bool EndCampaign(CampaignEnding ending)
        {
            if (!IsActive || ending == CampaignEnding.None)
            {
                return false;
            }

            Ending = ending;
            Status = ending == CampaignEnding.Bankrupt ? CampaignStatus.Defeat : CampaignStatus.Victory;
            _expeditionOpen = false;
            return true;
        }

        public bool AddBit(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return _wallet.Add(amount);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return _wallet.TrySpend(amount);
        }

        private ExpeditionResult BuildResult(
            bool paymentDue,
            bool paymentSucceeded,
            int paid,
            int dueThisWeek,
            string message)
        {
            return new ExpeditionResult
            {
                PaymentDue = paymentDue,
                PaymentSucceeded = paymentSucceeded,
                Paid = paid,
                BitRemaining = Bit,
                DueThisWeek = dueThisWeek,
                TotalPaid = _totalPaid,
                Week = Week,
                Day = _day,
                Status = Status,
                Ending = Ending,
                Message = message
            };
        }
    }
}

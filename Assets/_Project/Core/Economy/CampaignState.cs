using System;

namespace BitRunner.Core
{
    public enum CampaignStatus
    {
        Active,
        Victory,
        Defeat
    }

    public sealed class ExpeditionResult
    {
        public bool PaymentDue;
        public bool PaymentSucceeded;
        public int Paid;
        public int BitRemaining;
        public int DebtRemaining;
        public int Week;
        public int Day;
        public CampaignStatus Status;
        public string Message;
    }

    public sealed class CampaignState
    {
        public const int DaysPerWeek = 7;

        private readonly int[] _installments;
        private readonly int _totalDebt;
        private readonly IBitWallet _wallet;
        private int _weekIndex;
        private int _day;
        private int _debtRemaining;
        private bool _expeditionOpen;

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
                _totalDebt += installments[i];
            }

            _debtRemaining = _totalDebt;
            _day = 1;
            _weekIndex = 0;
            Status = CampaignStatus.Active;
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

        public int TotalDebt
        {
            get { return _totalDebt; }
        }

        public int DebtRemaining
        {
            get { return _debtRemaining > 0 ? _debtRemaining : 0; }
        }

        public int CurrentDue
        {
            get { return Status == CampaignStatus.Active && _weekIndex < _installments.Length ? _installments[_weekIndex] : 0; }
        }

        public bool IsActive
        {
            get { return Status == CampaignStatus.Active; }
        }

        public CampaignStatus Status { get; private set; }

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
                return BuildResult(false, false, 0, "Day " + _day);
            }

            int due = _installments[_weekIndex];
            if (!_wallet.TrySpend(due))
            {
                Status = CampaignStatus.Defeat;
                return BuildResult(true, false, 0, "Payment failed");
            }

            _debtRemaining -= due;

            if (_debtRemaining <= 0)
            {
                _debtRemaining = 0;
                Status = CampaignStatus.Victory;
                return BuildResult(true, true, due, "Debt cleared");
            }

            _weekIndex++;
            _day = 1;
            return BuildResult(true, true, due, "Week " + Week);
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

        private ExpeditionResult BuildResult(bool paymentDue, bool paymentSucceeded, int paid, string message)
        {
            return new ExpeditionResult
            {
                PaymentDue = paymentDue,
                PaymentSucceeded = paymentSucceeded,
                Paid = paid,
                BitRemaining = Bit,
                DebtRemaining = DebtRemaining,
                Week = Week,
                Day = _day,
                Status = Status,
                Message = message
            };
        }
    }
}

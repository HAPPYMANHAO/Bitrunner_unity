using System;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 债务是无限的：曲线爬到 2000 之后一直收 2000，直到玩家击杀终局 Boss、
    /// 主动逃亡，或者某周缴不出来破产。这里锁定这条时间线的每一个节点。
    /// </summary>
    public class CampaignStateTests
    {
        [Test]
        public void NewCampaign_StartsAtWeekOneDayOne()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments());

            Assert.AreEqual(1, campaign.Week);
            Assert.AreEqual(1, campaign.Day);
            Assert.AreEqual(7, campaign.DaysRemaining);
            Assert.AreEqual(400, campaign.CurrentDue);
            Assert.AreEqual(0, campaign.TotalPaid);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(5, campaign.WeeksUntilDebtCap);
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
            Assert.AreEqual(CampaignEnding.None, campaign.Ending);
        }

        [Test]
        public void DefaultCurve_RampsToCapAndStaysThere()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100000);

            Assert.AreEqual(400, campaign.CurrentDue);
            SettleWeek(campaign);
            Assert.AreEqual(2, campaign.Week);
            Assert.AreEqual(700, campaign.CurrentDue);

            SettleWeek(campaign);
            SettleWeek(campaign);
            SettleWeek(campaign);
            Assert.AreEqual(5, campaign.Week);
            Assert.AreEqual(1600, campaign.CurrentDue);
            Assert.AreEqual(1, campaign.WeeksUntilDebtCap);
            Assert.IsFalse(campaign.AtDebtCap);

            SettleWeek(campaign);
            Assert.AreEqual(6, campaign.Week);
            Assert.AreEqual(CampaignState.DebtCap, campaign.CurrentDue);
            Assert.IsTrue(campaign.AtDebtCap);
            Assert.AreEqual(0, campaign.WeeksUntilDebtCap);

            SettleWeek(campaign);
            SettleWeek(campaign);
            SettleWeek(campaign);
            Assert.AreEqual(9, campaign.Week);
            Assert.AreEqual(CampaignState.DebtCap, campaign.CurrentDue);
            Assert.AreEqual(400 + 700 + 1000 + 1300 + 1600 + 2000 * 3, campaign.TotalPaid);
        }

        [Test]
        public void SixSettlementsDoNotTriggerPayment()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments());

            for (int i = 0; i < 6; i++)
            {
                Settle(campaign);
            }

            Assert.AreEqual(7, campaign.Day);
            Assert.AreEqual(1, campaign.Week);
            Assert.AreEqual(400, campaign.CurrentDue);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(0, campaign.TotalPaid);
        }

        [Test]
        public void SeventhSettlementPaysAndStartsNextWeek()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 1000);

            ExpeditionResult result = SettleWeek(campaign);

            Assert.IsTrue(result.PaymentDue);
            Assert.IsTrue(result.PaymentSucceeded);
            Assert.AreEqual(400, result.Paid);
            Assert.AreEqual(400, result.DueThisWeek);
            Assert.AreEqual(600, campaign.Bit);
            Assert.AreEqual(400, campaign.TotalPaid);
            Assert.AreEqual(2, campaign.Week);
            Assert.AreEqual(1, campaign.Day);
            Assert.AreEqual(700, campaign.CurrentDue);
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
        }

        [Test]
        public void PayingOffTheCurveDoesNotEndCampaign()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100000);

            for (int i = 0; i < 10; i++)
            {
                SettleWeek(campaign);
            }

            Assert.AreEqual(11, campaign.Week);
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
            Assert.AreEqual(CampaignEnding.None, campaign.Ending);
            Assert.AreEqual(CampaignState.DebtCap, campaign.CurrentDue);
        }

        [Test]
        public void PaymentFailureEndsCampaignAsBankrupt()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 399);

            ExpeditionResult result = SettleWeek(campaign);

            Assert.IsTrue(result.PaymentDue);
            Assert.IsFalse(result.PaymentSucceeded);
            Assert.AreEqual(CampaignStatus.Defeat, campaign.Status);
            Assert.AreEqual(CampaignEnding.Bankrupt, campaign.Ending);
            Assert.AreEqual(399, campaign.Bit);
            Assert.AreEqual(400, result.DueThisWeek);
        }

        [Test]
        public void EndCampaign_BossSlainMarksVictory()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100);

            Assert.IsTrue(campaign.EndCampaign(CampaignEnding.BossSlain));
            Assert.AreEqual(CampaignStatus.Victory, campaign.Status);
            Assert.AreEqual(CampaignEnding.BossSlain, campaign.Ending);
            Assert.AreEqual(0, campaign.CurrentDue);
        }

        [Test]
        public void EndCampaign_EscapedMarksVictory()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100);

            Assert.IsTrue(campaign.EndCampaign(CampaignEnding.Escaped));
            Assert.AreEqual(CampaignStatus.Victory, campaign.Status);
        }

        [Test]
        public void EndCampaign_CanOnlyHappenOnce()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100);

            Assert.IsTrue(campaign.EndCampaign(CampaignEnding.Escaped));
            Assert.IsFalse(campaign.EndCampaign(CampaignEnding.BossSlain));
            Assert.AreEqual(CampaignEnding.Escaped, campaign.Ending);
        }

        [Test]
        public void EndCampaign_RejectsNone()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100);

            Assert.IsFalse(campaign.EndCampaign(CampaignEnding.None));
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
        }

        [Test]
        public void EndedCampaignRejectsNewExpedition()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100);
            campaign.EndCampaign(CampaignEnding.BossSlain);

            Assert.IsFalse(campaign.BeginExpedition());
            Assert.Throws<InvalidOperationException>(() => campaign.SettleExpedition());
        }

        [Test]
        public void SettleWithoutOpenExpeditionThrows()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments());

            Assert.Throws<InvalidOperationException>(() => campaign.SettleExpedition());
        }

        [Test]
        public void BeginExpedition_CannotOpenTwice()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments());

            Assert.IsTrue(campaign.BeginExpedition());
            Assert.IsFalse(campaign.BeginExpedition());

            campaign.SettleExpedition();
            Assert.IsTrue(campaign.BeginExpedition());
        }

        [Test]
        public void BitCanBeAddedAndSpent()
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments());

            campaign.AddBit(100);
            Assert.AreEqual(100, campaign.Bit);

            Assert.IsTrue(campaign.TrySpend(40));
            Assert.AreEqual(60, campaign.Bit);

            Assert.IsFalse(campaign.TrySpend(100));
            Assert.AreEqual(60, campaign.Bit);
        }

        [Test]
        public void CreateDefaultInstallments_ReturnsIndependentCopy()
        {
            int[] first = CampaignState.CreateDefaultInstallments();
            first[0] = 99999;

            Assert.AreEqual(400, CampaignState.CreateDefaultInstallments()[0]);
        }

        [Test]
        public void InvalidInstallmentsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new CampaignState(new int[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignState(new[] { 0 }));
        }

        [Test]
        public void SingleInstallmentRepeatsForever()
        {
            var campaign = new CampaignState(new[] { 600 }, 10000);

            SettleWeek(campaign);
            SettleWeek(campaign);
            SettleWeek(campaign);

            Assert.AreEqual(4, campaign.Week);
            Assert.AreEqual(600, campaign.CurrentDue);
            Assert.AreEqual(1800, campaign.TotalPaid);
        }

        private static ExpeditionResult Settle(CampaignState campaign)
        {
            Assert.IsTrue(campaign.BeginExpedition());
            return campaign.SettleExpedition();
        }

        private static ExpeditionResult SettleWeek(CampaignState campaign)
        {
            ExpeditionResult result = null;
            for (int i = 0; i < CampaignState.DaysPerWeek; i++)
            {
                result = Settle(campaign);
            }

            return result;
        }
    }
}

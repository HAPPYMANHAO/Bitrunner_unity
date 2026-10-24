using System;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class CampaignStateTests
    {
        [Test]
        public void NewCampaign_StartsAtWeekOneDayOne()
        {
            var campaign = new CampaignState(new[] { 600, 900, 1200 });

            Assert.AreEqual(1, campaign.Week);
            Assert.AreEqual(1, campaign.Day);
            Assert.AreEqual(7, campaign.DaysRemaining);
            Assert.AreEqual(600, campaign.CurrentDue);
            Assert.AreEqual(2700, campaign.TotalDebt);
            Assert.AreEqual(2700, campaign.DebtRemaining);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
        }

        [Test]
        public void SixSettlementsDoNotTriggerPayment()
        {
            var campaign = new CampaignState(new[] { 600, 900, 1200 });

            for (int i = 0; i < 6; i++)
            {
                Settle(campaign);
            }

            Assert.AreEqual(7, campaign.Day);
            Assert.AreEqual(1, campaign.Week);
            Assert.AreEqual(600, campaign.CurrentDue);
            Assert.AreEqual(0, campaign.Bit);
        }

        [Test]
        public void SeventhSettlementPaysAndStartsNextWeek()
        {
            var campaign = new CampaignState(new[] { 600, 900, 1200 }, 1000);

            ExpeditionResult result = null;
            for (int i = 0; i < 7; i++)
            {
                result = Settle(campaign);
            }

            Assert.IsTrue(result.PaymentDue);
            Assert.IsTrue(result.PaymentSucceeded);
            Assert.AreEqual(600, result.Paid);
            Assert.AreEqual(400, campaign.Bit);
            Assert.AreEqual(2100, campaign.DebtRemaining);
            Assert.AreEqual(2, campaign.Week);
            Assert.AreEqual(1, campaign.Day);
            Assert.AreEqual(900, campaign.CurrentDue);
            Assert.AreEqual(CampaignStatus.Active, campaign.Status);
        }

        [Test]
        public void PaymentFailureEndsCampaign()
        {
            var campaign = new CampaignState(new[] { 600, 900, 1200 }, 599);

            ExpeditionResult result = null;
            for (int i = 0; i < 7; i++)
            {
                result = Settle(campaign);
            }

            Assert.IsTrue(result.PaymentDue);
            Assert.IsFalse(result.PaymentSucceeded);
            Assert.AreEqual(CampaignStatus.Defeat, campaign.Status);
            Assert.AreEqual(599, campaign.Bit);
            Assert.AreEqual(2700, campaign.DebtRemaining);
        }

        [Test]
        public void PayingFinalInstallmentWins()
        {
            var campaign = new CampaignState(new[] { 600 }, 600);

            ExpeditionResult result = null;
            for (int i = 0; i < 7; i++)
            {
                result = Settle(campaign);
            }

            Assert.IsTrue(result.PaymentSucceeded);
            Assert.AreEqual(0, campaign.DebtRemaining);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(CampaignStatus.Victory, campaign.Status);
        }

        [Test]
        public void SettleWithoutOpenExpeditionThrows()
        {
            var campaign = new CampaignState(new[] { 600 });

            Assert.Throws<InvalidOperationException>(() => campaign.SettleExpedition());
        }

        [Test]
        public void BeginExpedition_CannotOpenTwice()
        {
            var campaign = new CampaignState(new[] { 600 });

            Assert.IsTrue(campaign.BeginExpedition());
            Assert.IsFalse(campaign.BeginExpedition());

            campaign.SettleExpedition();
            Assert.IsTrue(campaign.BeginExpedition());
        }

        [Test]
        public void BitCanBeAddedAndSpent()
        {
            var campaign = new CampaignState(new[] { 600 });

            campaign.AddBit(100);
            Assert.AreEqual(100, campaign.Bit);

            Assert.IsTrue(campaign.TrySpend(40));
            Assert.AreEqual(60, campaign.Bit);

            Assert.IsFalse(campaign.TrySpend(100));
            Assert.AreEqual(60, campaign.Bit);
        }

        [Test]
        public void InvalidInstallmentsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new CampaignState(new int[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignState(new[] { 0 }));
        }

        private static ExpeditionResult Settle(CampaignState campaign)
        {
            Assert.IsTrue(campaign.BeginExpedition());
            return campaign.SettleExpedition();
        }
    }
}

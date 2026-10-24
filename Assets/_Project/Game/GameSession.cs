using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;

namespace BitRunner.Game
{
    public static class GameSession
    {
        public static CampaignState Campaign { get; private set; }
        public static ItemCatalog Catalog { get; private set; }
        public static Merchant Merchant { get; private set; }
        public static DivePod Pod { get; private set; }
        public static BitMine Mine { get; private set; }
        public static CraftingStation Craft { get; private set; }
        public static BagInventory Stash { get; private set; }
        public static BagInventory PlayerBag { get; private set; }
        public static BagInventory Secure { get; private set; }
        public static string CurrentDiveName { get; private set; }
        public static string LastMessage { get; set; }
        public static ExpeditionScenario Scenario { get; private set; }
        public static int ExpeditionHp { get; set; }
        public static int EncountersCleared { get; private set; }
        public static int LootTaken { get; private set; }
        public static bool HasPendingEncounter { get; private set; }
        public static string PendingEnemyId { get; private set; }

        public const int ExpeditionMaxHp = 40;
        private const int PostCombatHeal = 4;

        public static bool ExpeditionActive
        {
            get { return Campaign != null && Campaign.ExpeditionOpen; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Campaign = null;
            Catalog = null;
            Merchant = null;
            Pod = null;
            Mine = null;
            Craft = null;
            Stash = null;
            PlayerBag = null;
            Secure = null;
            LastMessage = null;
            Scenario = null;
            ExpeditionHp = 0;
            EncountersCleared = 0;
            LootTaken = 0;
            HasPendingEncounter = false;
            PendingEnemyId = null;
            CurrentDiveName = null;
        }

        public static void EnsureInitialized()
        {
            if (Campaign == null)
            {
                StartNewGame();
            }
        }

        public static void EnsureMerchantStockForDay()
        {
            EnsureInitialized();

            if (Campaign == null || Merchant == null)
            {
                return;
            }

            if (Merchant.HasStockFor(Campaign.Week, Campaign.Day))
            {
                return;
            }

            Merchant.RestockForDay(
                Campaign.Week,
                Campaign.Day,
                DaySeed(Campaign.Week, Campaign.Day),
                Merchant.DefaultStockSize);
        }

        public static void EnsureFacilitiesForDay()
        {
            EnsureInitialized();

            if (Campaign == null)
            {
                return;
            }

            if (Pod != null)
            {
                Pod.EnsureSelection(Catalog);
            }

            if (Mine != null)
            {
                Mine.EnsureFilledForDay(Campaign.Week, Campaign.Day);
            }
        }

        public static void StartNewGame()
        {
            Catalog = DefaultItemLibrary.CreateCatalog();
            Stash = new BagInventory(6, 10);
            PlayerBag = new BagInventory(6, 5);
            Secure = new BagInventory(SecureContainerRules.Width, SecureContainerRules.Height);
            var wallet = new ItemBitWallet(Stash, Catalog);
            Campaign = new CampaignState(new[] { 600, 900, 1200 }, wallet);
            Merchant = new Merchant(Catalog);
            Pod = new DivePod();
            Mine = new BitMine();
            Craft = new CraftingStation();
            Place(Pod.Grid, "dive_basic", 0, 0);
            EnsureMerchantStockForDay();
            EnsureFacilitiesForDay();
            LastMessage = "7 次探索内缴清 600 bit。";

            Place(PlayerBag, "plate", 0, 0);
            Place(PlayerBag, "blade", 2, 0);
            Place(PlayerBag, "pulse", 2, 1);
            Place(PlayerBag, "chip", 4, 0);
            Place(PlayerBag, "patch", 4, 1);
            Place(PlayerBag, "parts_case", 0, 2);
            PlaceStack(PlayerBag, "ammo", 5, 0, 8);

            Place(Secure, "coin", 0, 0);

            Place(Stash, "buckler", 0, 0);
            Place(Stash, "jammer", 2, 0);
            Place(Stash, "coin", 3, 0);
            Place(Stash, "idol", 0, 2);
            Place(Stash, "scrap_blade", 2, 2);
            Place(Stash, "scrap", 4, 0);
            Place(Stash, "wire", 4, 1);
            Place(Stash, "dive_wild", 1, 1);
            Place(Stash, "scrap", 3, 1);
            Place(Stash, "weapon_crate", 0, 4);
            Place(Stash, "circuit", 2, 3);
            Place(Stash, "alloy", 4, 2);
            ItemDurability.EnsureBag(PlayerBag, Catalog);
            ItemDurability.EnsureBag(Secure, Catalog);
            ItemDurability.EnsureBag(Stash, Catalog);
            Campaign.AddBit(300);
        }

        public static bool BeginExpedition()
        {
            EnsureInitialized();
            EnsureFacilitiesForDay();

            DivePlan plan;
            string error;
            if (!Pod.TryResolveDive(Catalog, out plan, out error))
            {
                LastMessage = error;
                return false;
            }

            if (!Campaign.BeginExpedition())
            {
                LastMessage = Campaign.Status == CampaignStatus.Active
                    ? "已有探索进行中。"
                    : "战役已结束。";
                return false;
            }

            Scenario = ExpeditionScenario.Generate(
                DaySeed(Campaign.Week, Campaign.Day) + plan.SeedOffset,
                plan);
            Pod.Consume(plan, Catalog);
            CurrentDiveName = plan.Name;
            ExpeditionHp = ExpeditionMaxHp;
            EncountersCleared = 0;
            LootTaken = 0;
            HasPendingEncounter = false;
            LastMessage = plan.Name + " 开始。第 " + Campaign.Day + "/7 天。";
            return true;
        }

        public static void AddLootTaken()
        {
            LootTaken++;
        }

        public static int CollectMine()
        {
            EnsureFacilitiesForDay();

            bool collected;
            int amount = Mine.TryCollect(Campaign, out collected);
            if (!collected)
            {
                LastMessage = amount > 0
                    ? "仓库没有空间存放 " + amount + " bit，矿场尚未收取。"
                    : "矿场今天已经收取过了。";
                return 0;
            }

            LastMessage = "收取比特矿场 " + amount + " bit，已存入仓库。";
            return amount;
        }

        public static bool UpgradePod(out string error)
        {
            EnsureInitialized();
            return Pod.TryUpgrade(Campaign, Stash, out error);
        }

        public static bool UpgradeMine(out string error)
        {
            EnsureInitialized();
            return Mine.TryUpgrade(Campaign, Stash, out error);
        }

        public static bool UpgradeCraft(out string error)
        {
            EnsureInitialized();
            return Craft.TryUpgrade(Campaign, Stash, out error);
        }

        public static bool CraftItem(out PlacedItem placed, out string error)
        {
            EnsureInitialized();
            return Craft.TryCraft(Catalog, Stash, out placed, out error);
        }

        public static ExpeditionResolution ResolveExpeditionAction(string actionId)
        {
            if (Scenario == null || !Campaign.ExpeditionOpen)
            {
                return null;
            }

            var context = new ExpeditionContext
            {
                Hp = ExpeditionHp,
                MaxHp = ExpeditionMaxHp,
                BagFree = PlayerBag != null ? PlayerBag.FreeCellCount : 0,
                Alert = Scenario.Alert
            };

            ExpeditionResolution resolution = Scenario.Resolve(actionId, context);
            if (resolution == null)
            {
                return null;
            }

            LastMessage = resolution.Text;
            if (resolution.Kind == ExpeditionOutcomeKind.Damage)
            {
                ExpeditionHp = Mathf.Max(0, ExpeditionHp - resolution.Damage);
            }

            if (resolution.Kind == ExpeditionOutcomeKind.Combat)
            {
                PendingEnemyId = resolution.EnemyId;
                HasPendingEncounter = true;
            }

            return resolution;
        }

        public static bool UseExpeditionItem(BagInventory source, int instanceId, out string message)
        {
            message = null;
            if (Scenario == null || source == null)
            {
                message = "没有进行中的探索。";
                return false;
            }

            PlacedItem item;
            if (!source.TryGet(instanceId, out item))
            {
                message = "物品不存在。";
                return false;
            }

            ItemDefinition definition;
            if (!Catalog.TryGet(item.DefinitionId, out definition))
            {
                message = "未知物品。";
                return false;
            }

            int newHp;
            if (!ItemUse.TryUseOutsideCombat(
                    definition,
                    source,
                    instanceId,
                    ExpeditionHp,
                    ExpeditionMaxHp,
                    out newHp,
                    out message))
            {
                return false;
            }

            string advanceError;
            if (!Scenario.AdvanceAfterItemUse(out advanceError))
            {
                message = advanceError;
                return false;
            }

            ExpeditionHp = newHp;
            LastMessage = message;
            return true;
        }

        public static void CompleteEncounter(int hp)
        {
            HasPendingEncounter = false;
            PendingEnemyId = null;

            int recovered = 0;
            if (hp > 0)
            {
                int clamped = Mathf.Clamp(hp, 1, ExpeditionMaxHp);
                int healed = Mathf.Min(ExpeditionMaxHp, clamped + PostCombatHeal);
                recovered = healed - clamped;
                ExpeditionHp = healed;
            }

            EncountersCleared++;
            if (Scenario == null)
            {
                LastMessage = "战斗结束。";
                return;
            }

            string healText = recovered > 0 ? "，恢复 " + recovered + " 生命" : string.Empty;
            LastMessage = "敌人已清除" + healText + "。继续寻找撤离路线。";
        }

        public static void CompleteExpedition(bool survived)
        {
            if (Campaign == null || !Campaign.ExpeditionOpen)
            {
                return;
            }

            Scenario = null;
            HasPendingEncounter = false;
            PendingEnemyId = null;
            CurrentDiveName = null;
            ExpeditionHp = 0;
            EncountersCleared = 0;
            LootTaken = 0;

            if (!survived)
            {
                ClearBag();
            }

            ExpeditionResult result = Campaign.SettleExpedition();

            if (Campaign.Status == CampaignStatus.Active)
            {
                EnsureMerchantStockForDay();
                EnsureFacilitiesForDay();
            }

            if (result.PaymentDue)
            {
                if (!result.PaymentSucceeded)
                {
                    LastMessage = "缴款失败。游戏结束。";
                    return;
                }

                if (Campaign.Status == CampaignStatus.Victory)
                {
                    LastMessage = "债务已清，你自由了。";
                    return;
                }

                LastMessage = "已缴 " + result.Paid + " bit。第 " + Campaign.Week + " 周开始。";
                return;
            }

            LastMessage = survived
                ? "成功返回。第 " + Campaign.Day + "/7 天。"
                : "背包丢失。第 " + Campaign.Day + "/7 天。";
        }

        public static void ClearBag()
        {
            var items = new List<PlacedItem>(PlayerBag.Items);
            for (int i = 0; i < items.Count; i++)
            {
                PlayerBag.Remove(items[i].InstanceId);
            }
        }

        private static void Place(BagInventory bag, string itemId, int x, int y)
        {
            PlaceStack(bag, itemId, x, y, 1);
        }

        private static void PlaceStack(BagInventory bag, string itemId, int x, int y, int count)
        {
            PlacedItem placed;
            string error;
            ItemDefinition definition = Catalog.Get(itemId);
            if (!bag.TryAddAt(itemId, definition.Shape, new Cell(x, y), 0, true, count, out placed, out error))
            {
                LastMessage = "初始物品放置失败：" + error;
                return;
            }

            ContainerOps.Ensure(placed, definition);
        }

        private static int WeekSeed(int week)
        {
            return 1000 + week * 7919;
        }

        private static int DaySeed(int week, int day)
        {
            return WeekSeed(week) + day * 7919;
        }
    }
}

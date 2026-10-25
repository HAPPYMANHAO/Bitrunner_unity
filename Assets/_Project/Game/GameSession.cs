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
        public static CpuCore Cpu { get; private set; }
        public static InsuranceHub Insurance { get; private set; }
        public static StorageRack Rack { get; private set; }
        public static CommsHub Comms { get; private set; }
        public static QuestLog Quests { get; private set; }
        public static NpcMarket NpcStocks { get; private set; }
        public static BagInventory Stash { get; private set; }
        public static BagInventory PlayerBag { get; private set; }
        public static BagInventory Secure { get; private set; }

        /// <summary>
        /// 中央处理器的芯片网格。**不占战斗背包格子**——这是本作能同时做 RPG 成长
        /// 与背包解谜的结构性原因（见 docs/ITEM_TIER_PLAN.md §3）。
        /// </summary>
        public static BagInventory CpuGrid
        {
            get { return Cpu != null ? Cpu.Grid : null; }
        }

        public static string CurrentDiveName { get; private set; }
        public static string LastMessage { get; set; }
        public static ExpeditionScenario Scenario { get; private set; }

        /// <summary>已选行动里的辅助标志实例 ID；跨战斗场景重载保留。</summary>
        public static readonly List<int> ExpeditionAssistIds = new List<int>();
        public static int ExpeditionHp { get; set; }
        public static int EncountersCleared { get; private set; }
        public static int LootTaken { get; private set; }
        public static bool HasPendingEncounter { get; private set; }
        public static string PendingEnemyId { get; private set; }

        /// <summary>上一次阵亡清算的结果，供结算界面复述。</summary>
        public static ExpeditionAttrition.Report LastAttrition { get; private set; }

        /// <summary>没有任何芯片时的玩家生命上限。所有额外生命都必须来自 CPU（决策：MaxHp 属于 CPU）。</summary>
        public const int BaseMaxHp = 40;

        /// <summary>没有任何芯片时的每回合基础能量。芯片最多再 +1，且不可叠加。</summary>
        public const int BaseEnergyPerTurn = 3;

        /// <summary>CPU L1 的空间（2×2）。升级扩容见 <see cref="CpuCore"/>。</summary>
        public const int CpuGridSize = 2;

        private const int PostCombatHeal = 4;

        private static TraitEngine _traitEngine;

        /// <summary>
        /// 玩家生命上限 = 底板 + CPU 芯片加成。护甲不提供 HP，只管减伤——
        /// 两处给同一个数值会堆叠失控，也会削弱 CPU 的成长感。
        /// </summary>
        public static int ExpeditionMaxHp
        {
            get { return CpuModifiers.ApplyMaxHp(BaseMaxHp); }
        }

        /// <summary>当前 CPU 芯片提供的全局加成。芯片随时可换，换完立刻反映到下一场战斗。</summary>
        public static GlobalModifiers CpuModifiers
        {
            get
            {
                if (CpuGrid == null || Catalog == null)
                {
                    return GlobalModifiers.None;
                }

                if (_traitEngine == null)
                {
                    _traitEngine = new TraitEngine(Catalog);
                }

                return _traitEngine.EvaluateChips(CpuGrid);
            }
        }

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
            Cpu = null;
            Insurance = null;
            Rack = null;
            Comms = null;
            Quests = null;
            NpcStocks = null;
            _traitEngine = null;
            LastMessage = null;
            Scenario = null;
            ExpeditionHp = 0;
            EncountersCleared = 0;
            LootTaken = 0;
            HasPendingEncounter = false;
            PendingEnemyId = null;
            CurrentDiveName = null;
            LastAttrition = default(ExpeditionAttrition.Report);
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

            int today = QuestLog.Today(Campaign);

            if (NpcStocks != null)
            {
                NpcStocks.EnsureAll(today);
            }

            if (Quests != null)
            {
                // 先判超时，再重算持有类目标——顺序反了的话，超时任务会先被刷成 Ready。
                Quests.Tick(Comms, today);
                Quests.RefreshStatic(Catalog, Stash, PlayerBag, Campaign);
            }
        }

        public static void StartNewGame()
        {
            Catalog = DefaultItemLibrary.CreateCatalog();
            Cpu = new CpuCore();
            Insurance = new InsuranceHub();
            Rack = new StorageRack();
            Stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(Rack.Level));
            PlayerBag = new BagInventory(6, 5);
            Secure = new BagInventory(
                InsuranceHub.WidthForLevel(Insurance.Level),
                InsuranceHub.HeightForLevel(Insurance.Level));
            _traitEngine = new TraitEngine(Catalog);
            var wallet = new ItemBitWallet(Stash, Catalog);
            Campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), wallet);
            Merchant = new Merchant(Catalog);
            Pod = new DivePod();
            Mine = new BitMine();
            Craft = new CraftingStation();
            Comms = new CommsHub();
            Quests = new QuestLog();
            NpcStocks = new NpcMarket(Catalog);
            Place(Pod.Grid, "dive_basic", 0, 0);
            EnsureMerchantStockForDay();
            EnsureFacilitiesForDay();
            LastMessage = "本周应缴 " + Campaign.CurrentDue + " bit，7 次探索内筹齐。";

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
            Place(Stash, "weapon_crate", 7, 0);
            Place(Stash, "circuit", 2, 3);
            Place(Stash, "alloy", 4, 2);
            // 密钥盒：密钥是永久物，越深网体积越大（最深 3×3），散在仓库里会吃掉整块空间。
            Place(Stash, "key_box", 4, 4);

            // 开局就送老陈的上行链路并直接插进通讯中枢：和开局送芯片同理，
            // 玩家一进基地就能看到「通讯」页里有个人在等他，不用先学会拖拽才知道这套系统存在。
            Place(Comms.Grid, "uplink_junk", 0, 0);

            // 开局就送两张芯片并直接插在 CPU 上：玩家一进基地就能看到「生命上限 40 → 55」，
            // 不需要先学会拖拽才知道这套系统存在。
            Place(Cpu.Grid, "chip_hp_1", 0, 0);
            Place(Cpu.Grid, "chip_armor_1", 1, 0);
            ItemDurability.EnsureBag(PlayerBag, Catalog);
            ItemDurability.EnsureBag(Secure, Catalog);
            ItemDurability.EnsureBag(Stash, Catalog);
            Campaign.AddBit(300);
            NpcStocks.EnsureAll(QuestLog.Today(Campaign), true);
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

            // 按地点档位重排掉落池：低档物品不会在高级地点刷出，高档物品也不会在开局就掉。
            plan.LootPool = LootTierTable.Expand(plan.LootPool, Catalog, plan.TierMin, plan.TierWeights);

            Scenario = ExpeditionScenario.Generate(
                DaySeed(Campaign.Week, Campaign.Day) + plan.SeedOffset,
                plan);
            Pod.Consume(plan, Catalog);
            CurrentDiveName = plan.Name;
            ExpeditionHp = ExpeditionMaxHp;
            EncountersCleared = 0;
            LootTaken = 0;
            HasPendingEncounter = false;
            LastAttrition = default(ExpeditionAttrition.Report);
            ExpeditionAttrition.MarkOwned(PlayerBag);
            ExpeditionAssistIds.Clear();
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
            Quests.ReportMineCollected();
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

        public static bool UpgradeCpu(out string error)
        {
            EnsureInitialized();
            return Cpu.TryUpgrade(Campaign, Stash, out error);
        }

        public static bool UpgradeInsurance(out string error)
        {
            EnsureInitialized();
            if (!Insurance.TryUpgrade(Campaign, Stash, out error))
            {
                return false;
            }

            Secure.ResizeTo(
                InsuranceHub.WidthForLevel(Insurance.Level),
                InsuranceHub.HeightForLevel(Insurance.Level));
            return true;
        }

        public static bool UpgradeStorage(out string error)
        {
            EnsureInitialized();
            if (!Rack.TryUpgrade(Campaign, Stash, out error))
            {
                return false;
            }

            // 支付是从仓库里扣的，所以必须在扣完之后再换尺寸。
            Stash.ResizeTo(StorageRack.Width, StorageRack.HeightForLevel(Rack.Level));
            return true;
        }

        public static bool UpgradeComms(out string error)
        {
            EnsureInitialized();
            return Comms.TryUpgrade(Campaign, Stash, out error);
        }

        public static bool AcceptQuest(string questId, out string error)
        {
            EnsureInitialized();

            QuestDefinition definition;
            if (!QuestLibrary.TryGet(questId, out definition))
            {
                error = "任务不存在。";
                return false;
            }

            if (!Quests.TryAccept(definition, Comms, QuestLog.Today(Campaign), out error))
            {
                return false;
            }

            // 持有类目标（交付物品 / 累计缴款）接取时可能已经达成，立刻算一次。
            Quests.RefreshStatic(Catalog, Stash, PlayerBag, Campaign);
            error = null;
            return true;
        }

        public static bool ClaimQuest(string questId, out string error)
        {
            EnsureInitialized();

            QuestDefinition definition;
            if (!QuestLibrary.TryGet(questId, out definition))
            {
                error = "任务不存在。";
                return false;
            }

            return Quests.TryClaim(definition, Comms, Campaign, Stash, PlayerBag, Catalog, out error);
        }

        public static bool AbandonQuest(string questId, out string error)
        {
            EnsureInitialized();

            QuestDefinition definition;
            if (!QuestLibrary.TryGet(questId, out definition))
            {
                error = "任务不存在。";
                return false;
            }

            return Quests.Abandon(definition, Comms, QuestLog.Today(Campaign), out error);
        }

        public static bool BuyFromNpc(string npcId, int instanceId, out string error)
        {
            EnsureInitialized();

            NpcDefinition npc;
            if (!NpcLibrary.TryGet(npcId, out npc))
            {
                error = "联系人不存在。";
                return false;
            }

            return NpcStocks.TryBuy(npc, instanceId, Campaign, Stash, out error);
        }

        public static bool CraftItem(out PlacedItem placed, out string error)
        {
            EnsureInitialized();
            return Craft.TryCraft(Catalog, Stash, out placed, out error);
        }

        /// <summary>
        /// 组装进入战斗的玩家角色。生命上限、基础能量、护甲基底全部从 CPU 快照注入，
        /// 战斗过程中不再变化——换芯片从下一场战斗开始生效。
        /// </summary>
        public static CombatActor CreatePlayerActor()
        {
            EnsureInitialized();
            GlobalModifiers modifiers = CpuModifiers;
            return new CombatActor(
                "player",
                "跑者",
                modifiers.ApplyMaxHp(BaseMaxHp),
                BaseEnergyPerTurn + modifiers.BaseEnergy,
                PlayerBag,
                true,
                modifiers.ApplyArmorBase());
        }

        public static ExpeditionResolution ResolveExpeditionAction(
            string actionId,
            IReadOnlyList<PlacedItem> assistItems = null)
        {
            if (Scenario == null || !Campaign.ExpeditionOpen)
            {
                return null;
            }

            var assists = new List<AssistSpec>();
            var usedAssists = new List<PlacedItem>();
            if (assistItems != null && PlayerBag != null && Catalog != null)
            {
                var families = new List<string>();
                for (int i = 0; i < assistItems.Count && families.Count < 3; i++)
                {
                    PlacedItem requested = assistItems[i];
                    if (requested == null)
                    {
                        continue;
                    }

                    PlacedItem owned;
                    if (!PlayerBag.TryGet(requested.InstanceId, out owned))
                    {
                        continue;
                    }

                    ItemDefinition definition = Catalog.Get(owned.DefinitionId);
                    if (definition == null || definition.Assist == null)
                    {
                        continue;
                    }

                    string family = definition.Assist.Family;
                    if (!string.IsNullOrEmpty(family) && families.Contains(family))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(family))
                    {
                        families.Add(family);
                    }

                    assists.Add(definition.Assist);
                    usedAssists.Add(owned);
                }
            }

            var context = new ExpeditionContext
            {
                Hp = ExpeditionHp,
                MaxHp = ExpeditionMaxHp,
                BagFree = PlayerBag != null ? PlayerBag.FreeCellCount : 0,
                Alert = Scenario.Alert
            };

            ExpeditionResolution resolution = Scenario.Resolve(actionId, context, assists);
            if (resolution == null)
            {
                return null;
            }

            LastMessage = resolution.Text;
            if (!resolution.IsExit && usedAssists.Count > 0)
            {
                var broken = new List<string>();
                for (int i = 0; i < usedAssists.Count; i++)
                {
                    PlacedItem item = usedAssists[i];
                    ItemDefinition definition = Catalog.Get(item.DefinitionId);
                    int next = ItemDurability.Consume(item, definition, 1);
                    if (next <= 0)
                    {
                        PlayerBag.Remove(item.InstanceId);
                        broken.Add(definition.DisplayName);
                    }
                }

                if (broken.Count > 0)
                {
                    LastMessage += " " + string.Join("、", broken.ToArray()) + " 耐久耗尽，已损坏。";
                }
            }

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
            // 先把敌人 id 取出来：下面立刻要清空 PendingEnemyId，而任务需要它。
            string enemyId = PendingEnemyId;
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
            if (Quests != null)
            {
                // C-10 会把第二个参数换成 CrisisRules.IsCrisisBoss(enemyId)，
                // 那时击杀危机首领才能推进 q_seed_1。
                Quests.ReportKill(enemyId, false);
            }

            if (Scenario == null)
            {
                LastMessage = "战斗结束。";
                return;
            }

            string healText = recovered > 0 ? "，恢复 " + recovered + " 生命" : string.Empty;
            LastMessage = "敌人已清除" + healText + "。继续寻找撤离路线。";
        }

        /// <summary>
        /// 战斗没打完就离开：玩家没武器主动撤离，或敌人无武器逃跑。
        /// 背包与当前生命保留，但不加战后回复、不算清场、不记击杀。
        /// </summary>
        public static void RetreatEncounter(int hp, bool enemyFled)
        {
            HasPendingEncounter = false;
            PendingEnemyId = null;

            if (hp > 0)
            {
                ExpeditionHp = Mathf.Clamp(hp, 1, ExpeditionMaxHp);
            }

            LastMessage = enemyFled
                ? "敌人逃离了战斗。继续寻找撤离路线。"
                : "你脱离了战斗。继续寻找撤离路线。";
        }

        public static void CompleteExpedition(bool survived)
        {
            ExpeditionAssistIds.Clear();
            if (Campaign == null || !Campaign.ExpeditionOpen)
            {
                return;
            }

            // 必须先快照：下面会把 Scenario 置 null，而任务需要知道通的是哪个地点。
            string siteId = Scenario != null ? Scenario.SiteId : null;

            Scenario = null;
            HasPendingEncounter = false;
            PendingEnemyId = null;
            CurrentDiveName = null;
            ExpeditionHp = 0;
            EncountersCleared = 0;
            LootTaken = 0;

            ExpeditionAttrition.Report attrition = default(ExpeditionAttrition.Report);
            if (!survived)
            {
                attrition = ExpeditionAttrition.Apply(PlayerBag);
                LastAttrition = attrition;
            }

            ExpeditionAttrition.ClearMarks(PlayerBag);

            // 只有活着回来才算通关。阵亡不算「打过了」——这一点和掉落实体一致：
            // 没带回来的东西都不算你的。
            if (survived && !string.IsNullOrEmpty(siteId) && Quests != null)
            {
                // C-10 会把第二个参数换成危机标记。
                Quests.ReportDiveCleared(siteId, false);
            }

            ExpeditionResult result = Campaign.SettleExpedition();

            if (Campaign.Status == CampaignStatus.Active)
            {
                EnsureMerchantStockForDay();
                EnsureFacilitiesForDay();
            }

            LastMessage = ComposeSettlementText(survived, attrition, result);
        }

        /// <summary>把一次结算拼成底部消息栏能放下的一句话。</summary>
        private static string ComposeSettlementText(
            bool survived,
            ExpeditionAttrition.Report attrition,
            ExpeditionResult result)
        {
            if (result.PaymentDue)
            {
                if (!result.PaymentSucceeded)
                {
                    return "筹不齐 " + result.DueThisWeek + " bit，债务违约。游戏结束。";
                }

                if (Campaign.Status != CampaignStatus.Active)
                {
                    return EndingText(Campaign.Ending);
                }

                string paid = "已缴 " + result.Paid + " bit，第 " + Campaign.Week + " 周应缴 "
                              + Campaign.CurrentDue + "。";
                return survived ? paid : LostText(attrition) + " " + paid;
            }

            if (survived)
            {
                return "成功返回。第 " + Campaign.Day + "/7 天。";
            }

            return LostText(attrition) + " 第 " + Campaign.Day + "/7 天。";
        }

        private static string LostText(ExpeditionAttrition.Report attrition)
        {
            string text = "你被抬了回来：丢弃 " + attrition.Lost + " 件战利品，保住 " + attrition.Kept + " 件自带装备";
            if (attrition.Rescued > 0)
            {
                text += "（抢救 " + attrition.Rescued + " 件）";
            }

            return text + "。";
        }

        private static string EndingText(CampaignEnding ending)
        {
            switch (ending)
            {
                case CampaignEnding.BossSlain:
                    return "日冕战斗机甲倒下。你切断了它的债务，也切断了追兵。";
                case CampaignEnding.Escaped:
                    return "你带着能带走的一切离开了这片空域。";
                default:
                    return "债务违约。游戏结束。";
            }
        }

        /// <summary>
        /// 主动终结战役。击杀终局 Boss 与主动逃亡都走这里，破产由结算内部触发。
        /// 逃亡入口的内容（买断价、解锁周次）留给 C-8，这里只提供状态机接口。
        /// </summary>
        public static bool TryEndCampaign(CampaignEnding ending)
        {
            EnsureInitialized();
            if (!Campaign.EndCampaign(ending))
            {
                return false;
            }

            LastMessage = EndingText(ending);
            return true;
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

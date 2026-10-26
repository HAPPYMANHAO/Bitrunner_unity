using System;
using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BitRunner.Game.UiFactory;
using static BitRunner.Game.UiPalette;
using static BitRunner.Game.UiTheme;

namespace BitRunner.Game
{
    /// <summary>
    /// 战斗屏。
    ///
    /// 两种进法：
    ///   1. **压栈**（正常路径）：探索里触发战斗时由 <see cref="ExpeditionPrototypeBootstrap"/> 调
    ///      <see cref="Launch"/>，本组件挂到同一个 GameObject 上。A/E/F1/F3/G 五个区原样复用外壳实例，
    ///      所以战斗前后背包里物品的格子位置、日志、账本全是连续的；C/D/F2/B 换成本组件的四块新容器。
    ///   2. **独立场景**（旧路径，Step 4 拆除）：CombatPrototype.unity 里的组件自己起外壳。
    ///
    /// 三种模式的七个区对应关系见 docs/PLAN_UI_SHELL.md §2。
    /// </summary>
    public sealed class CombatPrototypeBootstrap : MonoBehaviour
    {
        /// <summary>战斗怎么结束的。探索据此决定刷新什么。</summary>
        public enum CombatExit
        {
            /// <summary>打赢并搜刮完毕，次数 +1，物品已带走。</summary>
            Looted,

            /// <summary>撤离 / 敌人逃离，生命写回，可直接继续探索。</summary>
            Retreated,

            /// <summary>倒下。探索已结束，得回基地。</summary>
            Defeated
        }

        private sealed class BagBinding
        {
            public CombatActor Owner;
            public BagInventory Bag;
            public RectTransform Rect;
            public BagView View;

            /// <summary>
            /// 这个背包里的物品会不会被"使用"。保险箱在战斗里只当投放目标（物品全灰），
            /// 一旦参与使用动画的查找，就会因为<b>每个背包各自从 1 编号</b>而抢走编号相同的格子。
            /// </summary>
            public bool IsUseSource = true;
        }

        /// <summary>一张敌人卡的标准宽度。两张并排（300×2 + 8）刚好 608，居中留 16 边距。</summary>
        private const float EnemyCardWidth = 300f;

        /// <summary>卡间距。</summary>
        private const float EnemyCardGap = 8f;

        private ItemCatalog _catalog;
        private CombatEngine _engine;
        private CombatActor _player;
        private List<CombatActor> _enemies;

        private readonly List<BagBinding> _bags = new List<BagBinding>();

        /// <summary>战场带容器。直接挂 ScreenLayer——它跨 A/C/D 三个区，走 Region() 要绕一圈坐标换算。</summary>
        private RectTransform _band;

        // ---- 战场带：每敌一张卡 ----
        // 一张卡 = 一个敌人 = 名字/血量/血条/背包/能量/护盾/护甲/减伤/意图/收益/状态/底标。
        // 分成这么多个 Text 只有一个原因：每行要在同一帧里单独改字，其它一概不拆。
        private readonly List<RectTransform> _enemyCards = new List<RectTransform>();
        private readonly List<Image> _enemyCardImages = new List<Image>();
        private readonly List<Text> _enemyNameTexts = new List<Text>();
        private readonly List<Text> _enemyHpTexts = new List<Text>();
        private readonly List<Image> _enemyHpFills = new List<Image>();
        private readonly List<Text> _enemyEnergyTexts = new List<Text>();
        private readonly List<Text> _enemyShieldTexts = new List<Text>();
        private readonly List<Text> _enemyArmorTexts = new List<Text>();
        private readonly List<Text> _enemyReductionTexts = new List<Text>();
        private readonly List<Text> _enemyIntentTexts = new List<Text>();
        private readonly List<Text> _enemyLootTexts = new List<Text>();
        private readonly List<Text> _enemyStatusTexts = new List<Text>();
        private readonly List<Text> _enemyTagTexts = new List<Text>();
        private readonly List<BagView> _enemyViews = new List<BagView>();

        // ---- F2 战利品板：这一场的账 + 搜刮收尾 ----
        private Text _lootHeadline;
        private Text _lootCapacity;
        private Text _lootHint;
        private RectTransform _lootAllRect;
        private Text _lootAllText;

        private Dictionary<string, Color> _palette;
        private BagView _playerView;
        private BagView _secureView;
        private RectTransform _playerBagRect;
        private RectTransform _secureRect;
        private RectTransform _discardRect;

        private Text _messageText;
        private Text _endTurnText;
        private Image _endTurnImage;
        private RectTransform _endTurnRect;

        /// <summary>B 区次要按钮。撤离 / 搜刮全部共用它——两者互斥，没必要占两个位置。</summary>
        private RectTransform _secondaryRect;
        private Image _secondaryImage;
        private Text _secondaryText;

        // ---- 外壳与屏幕栈 ----
        private GameShell _shell;
        private ShellScreen _screen;

        /// <summary>true = 由探索压栈进来；false = 旧 CombatPrototype 场景自己起的。</summary>
        private bool _pushed;

        /// <summary>退出回调。压栈模式下由探索设置；独立场景模式下为 null。</summary>
        private Action<CombatExit> _onExit;

        /// <summary>已经推到 G 区的战斗事件下标，避免重复追加。</summary>
        private int _loggedEventCount;

        private GameObject _tooltipPanel;
        private RectTransform _tooltipRect;
        private Text _tooltipText;
        private RectTransform _canvasRect;

        private GameObject _endOverlay;
        private Text _endTitle;
        private Text _endSubtitle;

        private bool _lootPhase;
        private int _takenValue;
        private readonly List<RectTransform> _enemyBagRects = new List<RectTransform>();
        private LootInteraction _lootInteraction;
        private FloatingWindowManager _containerWindows;

        private int _selectedEnemy;
        private bool _ended;

        /// <summary>这一场是怎么结束的，弹栈时交给探索。</summary>
        private CombatExit _exit;

        private sealed class UseAnimationStep
        {
            public BagBinding Source;
            public int InstanceId;
            public CombatActor Target;
            public string ActorId;
            public string DefinitionId;
        }

        private const float AnimPulseSeconds = 0.18f;
        private const float AnimSparkStart = 0.06f;
        private const float AnimSparkSeconds = 0.13f;
        private const float AnimFlashStart = 0.16f;
        private const float AnimFlashSeconds = 0.16f;
        private const float AnimStepSeconds = 0.34f;

        /// <summary>
        /// 命中闪光的强度上限。旧战斗界面的面板只有 170×130，0.9 是"闪一下"；
        /// 现在敌人卡是 300×168，同样的 0.9 会把整张卡冲成米色、字全看不清，所以压到 0.4。
        /// </summary>
        private const float AnimFlashStrength = 0.4f;

        private readonly List<UseAnimationStep> _animSteps = new List<UseAnimationStep>();
        private bool _animating;
        private int _animStepIndex;
        private float _animTime;
        private RectTransform _animTargetRect;
        private Image _animTargetImage;
        private Color _animTargetBaseColor;
        private Image _fxPulse;
        private Image _fxSpark;

        // 光点起点用「画布坐标 + 尺寸」表达，而不是只抱着一个 RectTransform：
        // 格子被 Refresh 销毁后 RectTransform 会变空，但起点位置依然是对的。
        private bool _animHasItem;
        private Vector2 _animItemPoint;
        private Vector2 _animItemSize;

        // 点击瞬间的格子快照：消耗品用掉后背包里已经没有这个 ItemView 了。
        // **必须连 actor 一起记**：BagInventory 的 InstanceId 是从 1 开始按背包各自编号的，
        // 玩家背包里的 #5 和敌人背包里的 #5 是两个东西，只比 id 会把起点画到别人身上。
        private int _usedItemInstance = -1;
        private string _usedItemActorId;
        private RectTransform _usedItemRect;
        private Sprite _usedItemSprite;
        private bool _usedItemPointValid;
        private Vector2 _usedItemPoint;
        private Vector2 _usedItemSize;
        private RectTransform _speedRect;
        private Text _speedText;
        private int _animationSpeed = 1;
        private BagInteraction _playerInteraction;

        private bool _initialized;

        /// <summary>压栈那一帧的点击不算数——理由同探索：发起压栈的那次按下会漏到新一屏。</summary>
        private int _ignoreInputUntilFrame = -1;

        /// <summary>
        /// 由探索压栈启动战斗。挂到独立 GameObject 上，不加载场景。
        ///
        /// 这里直接调 <see cref="Initialize"/> 而不等 Unity 的 Start：
        /// 压栈是在别的组件的调用栈里发起的，等 Start 会在某些时机被推迟到场景切换之后，
        /// 结果就是战斗组件挂上了但什么都没建。显式初始化把时序握在自己手里。
        /// </summary>
        public static CombatPrototypeBootstrap Launch(GameShell shell, Action<CombatExit> onExit)
        {
            var host = new GameObject("CombatScreen");
            var combat = host.AddComponent<CombatPrototypeBootstrap>();
            combat._shell = shell;
            combat._pushed = true;
            combat._onExit = onExit;
            combat.Initialize();
            return combat;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _ignoreInputUntilFrame = Time.frameCount;

            GameSession.EnsureInitialized();
            _catalog = GameSession.Catalog;
            _palette = BuildPalette(_catalog);
            _animationSpeed = GameSettings.AnimationSpeed;
            GameSettings.Changed += OnGameSettingsChanged;
            ItemDurability.EnsureBag(GameSession.PlayerBag, _catalog);
            _player = GameSession.CreatePlayerActor();
            if (GameSession.ExpeditionHp > 0)
            {
                _player.SetHp(GameSession.ExpeditionHp);
            }

            _enemies = CreateEnemies();

            _engine = new CombatEngine(_player, _enemies, _catalog);
            _engine.Start();

            if (_shell == null)
            {
                _shell = GameShell.Ensure(GameSession.PlayerBag, GameSession.Secure, _palette);
            }

            // 压栈：探索那一屏整组隐藏（不销毁），战斗这一屏新开四个容器。
            // 外壳持有的 A / E / F1 / F3 / G 不参与栈，因此战斗前后是同一批控件。
            _screen = _shell.PushScreen("Combat");
            _canvasRect = _shell.Root;

            // A 账本让位给敌人卡：战斗里不看债务，看的是面前这几个人的背包。
            _shell.SetLedgerVisible(false);

            BuildLayout();
            SetMessage("你的回合 · 点击背包里的物品使用。");
            RefreshAll();
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput || Time.frameCount <= _ignoreInputUntilFrame)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (_animating)
            {
                if (mouse.leftButton.wasPressedThisFrame && SpeedButtonContains(screen))
                {
                    CycleAnimationSpeed();
                }

                UpdateAnimation();
                return;
            }

            if (_ended)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Leave();
                }

                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && SpeedButtonContains(screen))
            {
                CycleAnimationSpeed();
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                // 撤离：只在玩家回合出现。搜刮阶段它是隐藏的，所以这条判断天然不会误触。
                if (_secondaryRect != null
                    && _secondaryRect.gameObject.activeSelf
                    && Contains(_secondaryRect, screen))
                {
                    int retreatEventStart = _engine.Events.Count;
                    string retreatError;
                    if (_engine.TryRetreat(out retreatError))
                    {
                        SetMessage("开始撤离，下一回合自动脱离战斗。");
                        if (!StartAnimations(CollectUseSteps(retreatEventStart)))
                        {
                            RefreshAll();
                        }
                    }
                    else
                    {
                        SetMessage(retreatError);
                    }

                    return;
                }

                // 「搜刮全部」在 F2 的战利品板上，不再是 B 区的次要按钮。
                if (_lootPhase
                    && _lootAllRect != null
                    && _lootAllRect.gameObject.activeSelf
                    && Contains(_lootAllRect, screen))
                {
                    LootAll();
                    return;
                }

                if (_endTurnRect != null && Contains(_endTurnRect, screen))
                {
                    if (_lootPhase)
                    {
                        FinishLoot();
                        return;
                    }

                    if (_engine.Phase == CombatPhase.PlayerTurn)
                    {
                        int turnEventStart = _engine.Events.Count;
                        _engine.EndTurn();
                        SetMessage("敌方回合结束");
                        if (!StartAnimations(CollectUseSteps(turnEventStart)))
                        {
                            RefreshAll();
                        }

                        return;
                    }
                }

                if (SelectCardAt(screen))
                {
                    return;
                }
            }

            UpdateTooltip(screen);
        }

        private List<CombatActor> CreateEnemies()
        {
            int bonusHp = Mathf.Min(15, GameSession.EncountersCleared * 3);
            var enemies = new List<CombatActor>();
            int baseSeed = GameSession.Scenario != null ? GameSession.Scenario.Seed : 0;
            int encounterSeed = baseSeed + GameSession.EncountersCleared * 7919;

            string primaryId = GameSession.PendingEnemyId;
            if (string.IsNullOrEmpty(primaryId))
            {
                enemies.Add(EnemyLibrary.Create("scavenger", _catalog, bonusHp, encounterSeed + 1));
                enemies.Add(EnemyLibrary.Create("enforcer", _catalog, bonusHp, encounterSeed + 2));
                return enemies;
            }

            enemies.Add(EnemyLibrary.Create(primaryId, _catalog, bonusHp, encounterSeed + 11));
            if (GameSession.EncountersCleared >= 1)
            {
                string minionId = EnemyLibrary.PickMinion(baseSeed, primaryId, GameSession.EncountersCleared);
                enemies.Add(EnemyLibrary.Create(minionId, _catalog, bonusHp / 2, encounterSeed + 23));
            }

            return enemies;
        }

        // ---------- 布局 ----------

        /// <summary>
        /// 战斗只建三块内容：B 主行动条、战场带（敌人卡）、F2 战利品板，外加 A 让出来的播报行。
        /// A 账本在战斗中隐藏（<see cref="GameShell.SetLedgerVisible"/>），E 左栏 / F1 背包 /
        /// F3 保险箱 / G 日志全部由外壳持有，本方法一行都不碰——
        /// 这正是"战斗前后背包里的东西还在原处、日志接着往上写"的实现方式。
        /// </summary>
        private void BuildLayout()
        {
            _playerView = _shell.PackView;
            _playerBagRect = _shell.PackBagRect;
            _secureView = _shell.SecureView;
            _secureRect = _shell.SecureBagRect;

            // 保险箱在战斗中只作为投放目标：物品不可点击使用，但搜刮时能拖进去。
            _secureView.ItemEnabledPredicate = item => false;
            _bags.Add(new BagBinding
            {
                Owner = _player,
                Bag = GameSession.Secure,
                Rect = _secureRect,
                View = _secureView,
                IsUseSource = false
            });

            _bags.Add(new BagBinding { Owner = _player, Bag = _player.Bag, Rect = _playerBagRect, View = _playerView });

            // 复用外壳的 F1，就得自己接管它的可用性判断：战斗里只有本回合能用的道具才亮。
            _playerView.ItemEnabledPredicate = IsPlayerItemAvailable;

            BuildPrimary(_screen.Region(ShellRegionId.Primary));
            BuildBattlefield();
            BuildLootBoard(_screen.Region(ShellRegionId.Dock));
            BuildMessageLine();

            BuildTooltip(_shell.TooltipLayer);
            BuildFxLayer(_shell.DragLayer);
            BuildEndOverlay(_shell.EndLayer);

            BuildPlayerInteraction();
            BuildLootInteraction();
            BuildContainerWindows();
        }

        /// <summary>
        /// B 主行动条：整个区域就是一颗按钮，"结束回合 / 返回地点 / 离开"共用同一个矩形。
        /// 与探索的「执行」、基地的「出征」逐像素相同（ShellRegions.PrimaryAction）。
        /// 两侧的次要按钮落在 ShellRegions.SecondaryAction1/2，即主按钮左边那条空白带。
        /// </summary>
        private void BuildPrimary(RectTransform region)
        {
            _endTurnRect = CreateCenteredPanel(
                region, "EndTurn", Vector2.zero, ShellRegions.SizeOf(ShellRegions.PrimaryAction), AccentColor);
            AddBorder(_endTurnRect, new Color32(96, 142, 186, 255));
            _endTurnImage = _endTurnRect.GetComponent<Image>();
            _endTurnText = CreateCenteredText(
                _endTurnRect, "Label", "结束回合", FontTitle, TextColor, Vector2.zero, new Vector2(156f, 26f));

            // 次要按钮：撤离（搜刮阶段的「搜刮全部」在 F2 战利品板上，不占这里）。
            Vector2 retreatCenter = ToRegion(ShellRegions.CenterOf(ShellRegions.SecondaryAction1), ShellRegions.PrimaryAction);
            _secondaryRect = CreateCenteredPanel(
                region, "Secondary", retreatCenter, ShellRegions.SizeOf(ShellRegions.SecondaryAction1), RetreatColor);
            _secondaryImage = _secondaryRect.GetComponent<Image>();
            _secondaryText = CreateCenteredText(
                _secondaryRect, "Label", "撤离", FontBody, TextColor, Vector2.zero, new Vector2(56f, 16f));

            Vector2 speedCenter = ToRegion(ShellRegions.CenterOf(ShellRegions.SecondaryAction2), ShellRegions.PrimaryAction);
            _speedRect = CreateCenteredPanel(
                region, "AnimationSpeed", speedCenter, ShellRegions.SizeOf(ShellRegions.SecondaryAction2),
                new Color32(40, 46, 56, 255));
            _speedText = CreateCenteredText(
                _speedRect, "Label", "动画 " + _animationSpeed + "×", FontBody, MutedColor, Vector2.zero, new Vector2(56f, 16f));
        }

        /// <summary>把"相对画布中心"的坐标换成"相对某个区域容器中心"的坐标。</summary>
        private static Vector2 ToRegion(Vector2 canvasCenter, Rect container)
        {
            Vector2 containerCenter = ShellRegions.CenterOf(container);
            return new Vector2(canvasCenter.x - containerCenter.x, canvasCenter.y - containerCenter.y);
        }

        /// <summary>
        /// 战场带：所有敌人卡排成一行，横跨 A 的下半 + C + D（(4,46,632,168)）。
        ///
        /// 卡宽取 min(标准宽, 带宽均分)，整行居中。当前引擎最多 2 个敌人（主敌 + 1 随从），
        /// 所以实际永远是 300 宽两张；3 个以上的窄版式在 <see cref="BuildEnemyCard"/> 里用宽窄分支兜住。
        /// </summary>
        private void BuildBattlefield()
        {
            int count = _enemies.Count;
            if (count <= 0)
            {
                return;
            }

            Rect band = ShellRegions.Battlefield;
            float width = Mathf.Min(EnemyCardWidth, (band.width - EnemyCardGap * (count - 1)) / count);
            float total = width * count + EnemyCardGap * (count - 1);
            float x0 = (band.width - total) * 0.5f;

            _band = CreateCenteredPanel(
                _shell.ScreenLayer, "Band_Battlefield", ShellRegions.CenterOf(band), ShellRegions.SizeOf(band),
                new Color(0f, 0f, 0f, 0f));
            _band.GetComponent<Image>().raycastTarget = false;

            for (int i = 0; i < count; i++)
            {
                BuildEnemyCard(_enemies[i], x0 + i * (width + EnemyCardGap), width);
            }
        }

        /// <summary>
        /// 一张敌人卡：左边是背包（图案），右边是数字（硬不硬），底部是意图、收益、状态。
        ///
        /// 这些字段必须同屏，因为 DESIGN.md §4.3 让"打谁"变成比较：
        /// 背着金雕像的清道夫好打但肥，塞满武器的执行者硬但穷——两件事得一眼同时看到。
        /// 之前的 C 焦点 / D 三行 / F2 目标背包三分法要求先点一遍再回来看，正是本次要修掉的问题。
        /// </summary>
        private void BuildEnemyCard(CombatActor enemy, float localX, float width)
        {
            float height = ShellRegions.Battlefield.height;

            RectTransform card = CreateCenteredPanel(
                _band,
                "EnemyCard_" + enemy.Id,
                ShellRegions.LocalIn(ShellRegions.Battlefield, localX, 0f, width, height),
                new Vector2(width, height),
                PanelColor);
            AddBorder(card, new Color32(46, 54, 66, 255));
            card.gameObject.AddComponent<RectMask2D>();

            _enemyCards.Add(card);
            _enemyCardImages.Add(card.GetComponent<Image>());

            _enemyNameTexts.Add(CreateText(
                card, "Name", enemy.DisplayName, FontTitle, TextColor,
                new Vector2(12f, -5f), new Vector2(width - 132f, 16f), TextAnchor.UpperLeft));

            _enemyHpTexts.Add(CreateText(
                card, "Hp", string.Empty, FontBody, MutedColor,
                new Vector2(width - 104f, -7f), new Vector2(92f, 14f), TextAnchor.UpperRight));

            _enemyHpFills.Add(CreateBar(
                card, "HpBar", new Vector2(12f, -23f), new Vector2(width - 24f, 8f), EnemyHpColor));

            Vector2 bagSize = new Vector2(4f * BagView.CellSize, 3f * BagView.CellSize);
            RectTransform bagRect = CreateBagRect(
                card,
                ShellRegions.LocalIn(new Rect(0f, 0f, width, height), 12f, 40f, bagSize.x, bagSize.y),
                bagSize);
            BagView view = bagRect.gameObject.AddComponent<BagView>();
            view.Initialize(enemy.Bag, bagRect, _palette);
            view.ItemEnabledPredicate = item => IsEnemyItemAvailable(enemy, item);
            _bags.Add(new BagBinding { Owner = enemy, Bag = enemy.Bag, Rect = bagRect, View = view });
            _enemyViews.Add(view);
            _enemyBagRects.Add(bagRect);

            // 头部分隔线：让"名字/血量"和下面的"背包/数字"读成两块，而不是一堆散字。
            RectTransform divider = CreateAnchoredPanel(
                card, "Divider", new Vector2(12f, -37f), new Vector2(width - 24f, 1f), new Color32(38, 44, 54, 255));
            divider.GetComponent<Image>().raycastTarget = false;

            // 数字列：能量 / 护盾 / 护甲 / 减伤，四行贴着背包右侧排。
            // 不用两列——护甲和减伤要写满五种伤害类型，窄列会换行撞进下一行，所以一律不换行。
            float statWidth = width - 100f;

            _enemyEnergyTexts.Add(CreateNoWrapText(
                card, "Energy", string.Empty, FontBody, TextColor, new Vector2(88f, -40f), new Vector2(statWidth, 12f)));

            _enemyShieldTexts.Add(CreateNoWrapText(
                card, "Shield", string.Empty, FontBody, TextColor, new Vector2(88f, -54f), new Vector2(statWidth, 12f)));

            _enemyArmorTexts.Add(CreateNoWrapText(
                card, "Armor", string.Empty, FontBody, MutedColor, new Vector2(88f, -68f), new Vector2(statWidth, 12f)));

            _enemyReductionTexts.Add(CreateNoWrapText(
                card, "Reduction", string.Empty, FontBody, MutedColor, new Vector2(88f, -82f), new Vector2(statWidth, 12f)));

            _enemyLootTexts.Add(CreateNoWrapText(
                card, "Loot", string.Empty, FontBody, MutedColor, new Vector2(12f, -96f), new Vector2(width - 24f, 12f)));

            _enemyIntentTexts.Add(CreateNoWrapText(
                card, "Intent", string.Empty, FontPanelTitle, AlertColor, new Vector2(12f, -112f), new Vector2(width - 24f, 14f)));

            _enemyStatusTexts.Add(CreateNoWrapText(
                card, "Status", string.Empty, FontBody, MutedColor, new Vector2(12f, -128f), new Vector2(width - 24f, 12f)));

            _enemyTagTexts.Add(CreateNoWrapText(
                card, "Tag", string.Empty, FontBody, MutedColor, new Vector2(12f, -148f), new Vector2(width - 24f, 12f)));
        }

        /// <summary>卡片里的单行文本一律不换行：宁可溢出到内边距，也不许挤到下一行的位置上。</summary>
        private static Text CreateNoWrapText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
        {
            Text text = CreateText(parent, name, content, size, color, position, sizeDelta, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// F2：这一场的账。与仓库（基地）、结果预估（探索）同一个角色。
        ///
        /// 战斗中是"打完能拿多少、装不装得下"——这是要不要继续推的依据；
        /// 搜刮时是进度 + 唯一的收尾动作（搜刮全部）。按钮不再借用 B 区的次要位置，
        /// 那一格留给撤离，两件事各归其位。
        /// </summary>
        private void BuildLootBoard(RectTransform region)
        {
            Rect board = ShellRegions.BenchMid;
            RectTransform panel = CreateCenteredPanel(region, "LootBoard", Vector2.zero, ShellRegions.SizeOf(board), PanelColor);
            panel.gameObject.AddComponent<RectMask2D>();

            CreateText(panel, "Title", "战利品", FontPanelTitle, TextColor,
                new Vector2(8f, -6f), new Vector2(120f, 12f), TextAnchor.UpperLeft);

            _lootHeadline = CreateText(panel, "Headline", string.Empty, FontBody, TextColor,
                new Vector2(8f, -22f), new Vector2(168f, 14f), TextAnchor.UpperLeft);

            _lootCapacity = CreateText(panel, "Capacity", string.Empty, FontCaption, MutedColor,
                new Vector2(8f, -38f), new Vector2(168f, 12f), TextAnchor.UpperLeft);

            _lootHint = CreateText(panel, "Hint", string.Empty, FontCaption, MutedColor,
                new Vector2(8f, -52f), new Vector2(168f, 12f), TextAnchor.UpperLeft);

            _lootAllRect = CreateCenteredPanel(
                panel, "LootAll",
                ShellRegions.LocalIn(board, 8f, 70f, 168f, 22f),
                new Vector2(168f, 22f), AccentColor);
            AddBorder(_lootAllRect, new Color32(96, 142, 186, 255));
            _lootAllText = CreateCenteredText(
                _lootAllRect, "Label", "搜刮全部", FontBody, TextColor, Vector2.zero, new Vector2(164f, 20f));
            _lootAllRect.gameObject.SetActive(false);

            // 丢弃区位置不动：把物品拖出去是最远的动作，藏在右下角正合适。
            _discardRect = CreateCenteredPanel(
                panel, "Discard",
                ShellRegions.LocalIn(board, 108f, 100f, 68f, 18f),
                new Vector2(68f, 18f), SellColor);
            CreateCenteredText(_discardRect, "Label", "丢弃", FontCaption, MutedColor, Vector2.zero, new Vector2(64f, 16f));
        }

        /// <summary>
        /// 战斗播报：A 账本在战斗中隐藏，让出的那一条正好放这句话。
        /// 原来它写在 C 舞台最底行；C 不再存在，它必须有个新家。
        /// </summary>
        private void BuildMessageLine()
        {
            _messageText = CreateText(
                _shell.ScreenLayer, "CombatMessage", string.Empty, FontBody, TextColor,
                new Vector2(ShellRegions.MessageLine.x, -ShellRegions.MessageLine.y),
                ShellRegions.SizeOf(ShellRegions.MessageLine),
                TextAnchor.MiddleLeft);
        }

        private void BuildPlayerInteraction()
        {
            // F1 的 BagInteraction 只在战斗期间存在（探索/基地有自己的拖放逻辑），
            // 所以它挂在格子容器上、随战斗屏一起销毁。
            var interaction = _playerBagRect.gameObject.AddComponent<BagInteraction>();
            interaction.Initialize(_player.Bag, _playerView, _playerBagRect, null, _discardRect);
            interaction.ItemClicked = OnPlayerItemClicked;
            interaction.ItemDiscarded += OnPlayerItemDiscarded;
            interaction.LockPosition = true;
            _playerInteraction = interaction;
        }

        private void BuildTooltip(Transform layer)
        {
            _tooltipPanel = new GameObject("CombatTooltip", typeof(RectTransform), typeof(Image));
            _tooltipPanel.transform.SetParent(layer, false);

            _tooltipRect = (RectTransform)_tooltipPanel.transform;
            _tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltipRect.pivot = new Vector2(0f, 1f);
            _tooltipRect.sizeDelta = new Vector2(240f, 180f);
            _tooltipPanel.GetComponent<Image>().color = new Color32(12, 14, 18, 245);
            _tooltipPanel.GetComponent<Image>().raycastTarget = false;

            _tooltipText = CreateText(_tooltipPanel.transform, "Text", string.Empty, FontBody, TextColor, new Vector2(8f, -6f), new Vector2(224f, 168f), TextAnchor.UpperLeft);
            _tooltipPanel.SetActive(false);
        }

        /// <summary>结局遮罩挂在外壳的 EndLayer 上——它必须压住日志和背包，说明"这一场结束了"。</summary>
        private void BuildEndOverlay(Transform layer)
        {
            _endOverlay = new GameObject("CombatEndOverlay", typeof(RectTransform), typeof(Image));
            _endOverlay.transform.SetParent(layer, false);

            var rect = (RectTransform)_endOverlay.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = ShellRegions.ReferenceResolution;
            _endOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            RectTransform box = CreateCenteredPanel(_endOverlay.transform, "Box", Vector2.zero, new Vector2(320f, 150f), PanelColor);
            _endTitle = CreateCenteredText(box, "Title", string.Empty, 26, TextColor, new Vector2(0f, 28f), new Vector2(300f, 36f));
            _endSubtitle = CreateCenteredText(box, "Subtitle", string.Empty, FontPanelTitle, MutedColor, new Vector2(0f, -18f), new Vector2(290f, 44f));

            _endOverlay.SetActive(false);
        }

        private bool IsPlayerItemAvailable(PlacedItem item)
        {
            if (_engine == null || _player == null || item == null)
            {
                return true;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            if (!definition.IsActive && definition.Shield <= 0)
            {
                return false;
            }

            if (_engine.Phase != CombatPhase.PlayerTurn)
            {
                return false;
            }

            string error;
            return _engine.CanUseItem(_player, item, out error);
        }

        private bool IsEnemyItemAvailable(CombatActor enemy, PlacedItem item)
        {
            if (_engine == null || enemy == null || item == null || !enemy.IsAlive || enemy.HasFled)
            {
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            if (!definition.IsActive && definition.Shield <= 0)
            {
                return false;
            }

            string error;
            return _engine.CanUseItemPreview(enemy, item, out error);
        }

        private void OnPlayerItemClicked(int instanceId)
        {
            if (_engine.Phase != CombatPhase.PlayerTurn)
            {
                return;
            }

            PlacedItem item;
            if (!_player.Bag.TryGet(instanceId, out item))
            {
                return;
            }

            ItemDefinition definition = _catalog.Get(item.DefinitionId);
            if (!definition.IsActive && definition.Shield <= 0)
            {
                SetMessage(definition.DisplayName + " 是被动物品。");
                return;
            }

            CombatActor target = definition.Target == ItemTarget.Enemy ? CurrentTarget() : _player;
            int eventStart = _engine.Events.Count;

            // 先用后算：消耗品一用就从背包消失，那时再去查 ItemView 已经查不到了，
            // 脉冲和火花都不会出来。所以在这里把"格子图标 + 位置"扣下来。
            SnapshotUsedItem(instanceId);

            string error;
            if (_engine.TryUseItem(_player, instanceId, target, out error))
            {
                SetMessage("已使用 " + definition.DisplayName + "。");
                EnsureSelection();
                if (!StartAnimations(CollectUseSteps(eventStart)))
                {
                    RefreshAll();
                }
            }
            else
            {
                SetMessage(error);
            }
        }

        private void RefreshAll()
        {
            EnsureSelection();

            // 外壳的 RefreshContainers 顺手把保险箱标题的件数也更新了，所以这里不直接调 BagView.Refresh。
            _shell.RefreshContainers();

            for (int i = 0; i < _enemyViews.Count; i++)
            {
                _enemyViews[i].Refresh();
            }

            UpdateShell();
            UpdateEnemyCards();
            UpdateLootBoard();
            FlushLog();
            UpdateEndTurnButton();
            UpdateSecondaryButton();
            CheckEnd();
        }

        /// <summary>
        /// A 账本 + E 左栏。这两个区归外壳所有，战斗只负责填数——
        /// 与探索用的是同一套行布局，所以"生命 / 能量"两行在两处像素一致。
        /// （A 区本屏是隐藏的，照样写：数据保持新鲜，弹栈回探索时不用补一次。）
        /// </summary>
        private void UpdateShell()
        {
            _shell.SetLedger(GameSession.Campaign);

            float hpRatio = _player.MaxHp > 0 ? Mathf.Clamp01((float)_player.Hp / _player.MaxHp) : 0f;
            int energyMax = Mathf.Max(1, MaxEnergy(_player));

            // 能量不画条：它是"每回合可用量"，回合结束就重置，没有累积进度可言。
            // 基地和探索也是这么显示的，三处一致。
            _shell.SetStatus(
                null,
                GameShell.StatusRow.Bar("生命", _player.Hp + "/" + _player.MaxHp, hpRatio, HpBar(hpRatio)),
                GameShell.StatusRow.Text("能量", _player.Energy + "/" + energyMax),
                GameShell.StatusRow.Text("护盾", _player.Shield.ToString()),
                GameShell.StatusRow.Text("回合", _lootPhase ? "搜刮" : PhaseName()),
                GameShell.StatusRow.Text("弹药", _player.Bag.CountOf("ammo").ToString()));
        }

        private string PhaseName()
        {
            switch (_engine.Phase)
            {
                case CombatPhase.PlayerTurn: return "我的回合";
                case CombatPhase.EnemyTurn: return "敌方回合";
                case CombatPhase.PlayerRetreat: return "撤离中";
                case CombatPhase.EnemyFled: return "敌人逃离";
                case CombatPhase.Victory: return "胜利";
                case CombatPhase.Defeat: return "倒下";
                default: return "-";
            }
        }

        /// <summary>
        /// 战场带：一张卡 = 一个敌人的全部可见信息。
        /// 名字 / 血量 / 血条 / 背包 / 能量 / 护盾 / 护甲 / 减伤 / 意图 / 战利品 / 状态 / 底标。
        /// </summary>
        private void UpdateEnemyCards()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                CombatActor enemy = _enemies[i];
                bool down = !enemy.IsAlive || enemy.HasFled;
                bool focused = i == _selectedEnemy && (_lootPhase || !down);

                float ratio = enemy.MaxHp > 0 ? Mathf.Clamp01((float)enemy.Hp / enemy.MaxHp) : 0f;
                _enemyHpTexts[i].text = enemy.Hp + "/" + enemy.MaxHp;
                _enemyHpFills[i].fillAmount = ratio;

                _enemyEnergyTexts[i].text = "能量 " + enemy.Energy + "/" + MaxEnergy(enemy);
                _enemyShieldTexts[i].text = "护盾 " + enemy.Shield;
                _enemyArmorTexts[i].text = ArmorLine(enemy);
                _enemyReductionTexts[i].text = ReductionLine(enemy);

                _enemyIntentTexts[i].text = down
                    ? (enemy.HasFled ? "已逃离" : "已倒下")
                    : "意图 " + IntentWord(enemy);

                int lootCount;
                int lootValue;
                CountLoot(enemy.Bag, out lootCount, out lootValue);
                _enemyLootTexts[i].text = "物 " + enemy.Bag.Count + " 件 · 战利 " + lootCount + " · ¥" + lootValue;

                string status = StatusSuffix(enemy).Trim();
                _enemyStatusTexts[i].text = status.Length == 0 ? "状态 无" : "状态 " + status;

                _enemyNameTexts[i].color = down ? MutedColor : TextColor;
                _enemyCardImages[i].color = focused ? PanelSelectedColor : (down ? DisabledColor : PanelColor);
                _enemyHpTexts[i].color = focused ? TextColor : MutedColor;

                // 搜刮阶段选中尸体也要看得出来——"正在搜刮"和"当前目标"不是一件事。
                if (focused)
                {
                    _enemyTagTexts[i].text = _lootPhase ? "▍正在搜刮" : "▍当前目标";
                }
                else
                {
                    _enemyTagTexts[i].text = down ? "已倒下" : "点击切换目标";
                }

                _enemyTagTexts[i].color = focused ? LootColor : MutedColor;
            }
        }

        /// <summary>
        /// F2 战利品板。战斗中是"这一场能拿多少 / 还剩几格装"，
        /// 搜刮时切成进度 + 「搜刮全部」——同一块板，两种语义对应两个阶段。
        /// </summary>
        private void UpdateLootBoard()
        {
            int totalCount = 0;
            int totalValue = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                int count;
                int value;
                CountLoot(_enemies[i].Bag, out count, out value);
                totalCount += count;
                totalValue += value;
            }

            _lootHeadline.text = _lootPhase
                ? "已取 ¥" + _takenValue + " · 剩余 " + totalCount + " 件"
                : "场上 " + _enemies.Count + " 处 · " + totalCount + " 件 · 估 ¥" + totalValue;

            _lootCapacity.text = "背包空 " + _player.Bag.FreeCellCount + " 格 · 保险箱空 "
                                 + GameSession.Secure.FreeCellCount + " 格";

            _lootHint.text = _lootPhase
                ? "拖到背包或保险箱 · 双击容器展开"
                : "点卡片切目标 · 悬停看物品";

            if (_lootAllRect != null)
            {
                if (_lootAllRect.gameObject.activeSelf != _lootPhase)
                {
                    _lootAllRect.gameObject.SetActive(_lootPhase);
                }

                if (_lootPhase)
                {
                    _lootAllText.text = "搜刮全部（" + totalCount + "）";
                }
            }
        }

        /// <summary>统计一个背包里的战利品：件数 + 估值。只认 Loot 类，武器护甲是战力不是钱。</summary>
        private void CountLoot(BagInventory bag, out int count, out int value)
        {
            count = 0;
            value = 0;

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = _catalog.Get(items[i].DefinitionId);
                if (definition == null)
                {
                    continue;
                }

                if (definition.Category == ItemCategory.Loot)
                {
                    count += 1;
                    value += definition.Value;
                }
            }
        }

        private static bool Contains(RectTransform rect, Vector2 screen)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
        }

        /// <summary>
        /// 点卡片切目标。搜刮阶段点的是"还有东西的尸体"，战斗回合点的是活着的——
        /// 两条规则不同，所以判断放在这里，而不是各调用点抄一遍。
        /// </summary>
        private bool SelectCardAt(Vector2 screen)
        {
            for (int i = 0; i < _enemyCards.Count; i++)
            {
                bool selectable = _lootPhase
                    ? _enemies[i].Bag.Items.Count > 0
                    : (_enemies[i].IsAlive && !_enemies[i].HasFled);

                if (selectable && Contains(_enemyCards[i], screen))
                {
                    _selectedEnemy = i;
                    RefreshAll();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 战斗事件推到 G 区。外壳的日志是跨模式连续的，所以这里只追加增量，
        /// 不重写整块——探索里最后发生的事还留在最上面。
        /// </summary>
        private void FlushLog()
        {
            if (_loggedEventCount > _engine.Events.Count)
            {
                _loggedEventCount = 0;
            }

            for (int i = _loggedEventCount; i < _engine.Events.Count; i++)
            {
                _shell.AppendLog(_engine.Events[i].Message);
            }

            _loggedEventCount = _engine.Events.Count;
        }

        /// <summary>
        /// B 区主按钮。一颗按钮承担全部"往下推进"的语义：
        /// 结束回合 → 返回地点 → 离开。横幅上永远只有一件事可做，这是这个屏幕的规则。
        /// </summary>
        private void UpdateEndTurnButton()
        {
            if (_lootPhase)
            {
                _endTurnImage.color = AccentColor;
                _endTurnText.color = TextColor;
                _endTurnText.text = GameSession.ExpeditionActive && GameSession.Scenario != null
                    ? "返回地点"
                    : "离开";
                return;
            }

            if (_engine.Phase == CombatPhase.PlayerRetreat)
            {
                _endTurnImage.color = DisabledColor;
                _endTurnText.text = "已撤离";
                return;
            }

            if (_engine.Phase == CombatPhase.EnemyFled)
            {
                _endTurnImage.color = DisabledColor;
                _endTurnText.text = "敌人已逃离";
                return;
            }

            if (_engine.Phase == CombatPhase.Victory)
            {
                _endTurnImage.color = DisabledColor;
                _endTurnText.text = "胜利";
                return;
            }

            if (_engine.Phase == CombatPhase.Defeat)
            {
                _endTurnImage.color = DisabledColor;
                _endTurnText.text = "失败";
                return;
            }

            bool playerTurn = _engine.Phase == CombatPhase.PlayerTurn;
            _endTurnImage.color = playerTurn ? AccentColor : DisabledColor;
            _endTurnText.color = playerTurn ? TextColor : MutedColor;
            _endTurnText.text = playerTurn ? "结束回合" : "敌方回合";
        }

        /// <summary>
        /// B 区次要按钮：只有「撤离」，玩家回合才出现。
        /// 「搜刮全部」搬去了 F2 的战利品板——它是收尾动作，属于那一场的账，不属于命令行。
        /// </summary>
        private void UpdateSecondaryButton()
        {
            if (_secondaryRect == null)
            {
                return;
            }

            bool show = !_lootPhase && !_ended && _engine.Phase == CombatPhase.PlayerTurn;
            if (_secondaryRect.gameObject.activeSelf != show)
            {
                _secondaryRect.gameObject.SetActive(show);
            }

            _secondaryImage.color = RetreatColor;
            _secondaryText.text = "撤离";
            _secondaryText.color = MutedColor;
        }

        private void CheckEnd()
        {
            if (!_engine.IsFinished || _ended)
            {
                return;
            }

            _tooltipPanel.SetActive(false);

            if (_engine.Phase == CombatPhase.Victory)
            {
                BeginLoot();
                return;
            }

            if (_engine.Phase == CombatPhase.PlayerRetreat || _engine.Phase == CombatPhase.EnemyFled)
            {
                EndRetreat(_engine.Phase == CombatPhase.EnemyFled);
                return;
            }

            if (_secondaryRect != null)
            {
                _secondaryRect.gameObject.SetActive(false);
            }

            GameSession.CompleteExpedition(false);
            _ended = true;
            _exit = CombatExit.Defeated;
            UpdateShell();
            _endOverlay.SetActive(true);
            _endTitle.text = "失败";
            _endSubtitle.text = "背包已丢失。\n点击返回基地。";
        }

        private void EndRetreat(bool enemyFled)
        {
            _ended = true;
            _exit = CombatExit.Retreated;
            if (_secondaryRect != null)
            {
                _secondaryRect.gameObject.SetActive(false);
            }

            if (GameSession.ExpeditionActive && GameSession.Scenario != null)
            {
                // 关键差异：不再 LoadScene 回探索。弹栈即可——背包、日志、账本一点没动过。
                GameSession.RetreatEncounter(_player.Hp, enemyFled);
                UpdateShell();
                _endTitle.text = enemyFled ? "敌人已逃离" : "已撤离";
                _endSubtitle.text = enemyFled
                    ? "敌人逃走了，没有战利品。\n点击继续探索。"
                    : "你脱离了战斗，背包保留。\n点击继续探索。";
                _endOverlay.SetActive(true);
                return;
            }

            GameSession.CompleteExpedition(true);
            UpdateShell();
            _endOverlay.SetActive(true);
            _endTitle.text = enemyFled ? "敌人已逃离" : "已撤离";
            _endSubtitle.text = enemyFled
                ? "敌人逃走了，没有战利品。\n点击返回基地。"
                : "你脱离了战斗，背包保留。\n点击返回基地。";
        }

        private void BeginLoot()
        {
            _lootPhase = true;
            _takenValue = 0;
            _tooltipPanel.SetActive(false);
            if (_playerInteraction != null)
            {
                _playerInteraction.LockPosition = false;
            }
            if (_lootInteraction != null)
            {
                _lootInteraction.enabled = true;
            }

            // 打完仗默认焦点会落在还活着的那只身上，但搜刮要看的是尸体。
            // 这里跳到第一个还有东西的敌人，省得玩家自己找。
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Bag.Items.Count > 0)
                {
                    _selectedEnemy = i;
                    break;
                }
            }

            SetMessage("打赢了。把战利品拖进背包或保险箱，或者点「搜刮全部」。");
            UpdateShell();
            UpdateSecondaryButton();
            UpdateEndTurnButton();
            UpdateEnemyCards();
            UpdateLootBoard();
        }

        private void LootAll()
        {
            if (_lootInteraction == null)
            {
                return;
            }

            _lootInteraction.TakeAll();
            _shell.RefreshContainers();
            UpdateEnemyCards();
            UpdateLootBoard();
            UpdateEndTurnButton();
        }

        private void BuildLootInteraction()
        {
            var sources = new List<LootSource>();
            for (int i = 0; i < _enemies.Count; i++)
            {
                sources.Add(new LootSource
                {
                    Bag = _enemies[i].Bag,
                    Rect = _enemyBagRects[i],
                    View = _enemyViews[i]
                });
            }

            _lootInteraction = gameObject.AddComponent<LootInteraction>();
            _lootInteraction.Initialize(_player.Bag, _playerBagRect, _playerView, GameSession.Secure, _secureRect, _secureView, sources, _catalog, _palette, _canvasRect);
            _lootInteraction.ItemTaken += OnLootTaken;
            _lootInteraction.Message += SetMessage;
            _lootInteraction.enabled = false;
        }

        private void OnPlayerItemDiscarded(string definitionId)
        {
            ItemDefinition definition = _catalog.Get(definitionId);
            SetMessage("已丢弃 " + (definition != null ? definition.DisplayName : definitionId) + "。");
            RefreshAll();
        }

        /// <summary>
        /// 战斗里的容器统一走和基地/探索同一套窗口：搜刮阶段双击容器即可打开，
        /// 打开的窗口同时接成搜刮拖拽源，物品可以拖进拖出。
        /// </summary>
        private void BuildContainerWindows()
        {
            _containerWindows = gameObject.AddComponent<FloatingWindowManager>();
            _containerWindows.Initialize(_canvasRect, _catalog, _palette);
            _containerWindows.WindowOpened += OnContainerWindowOpened;
            _containerWindows.AttachBag(_player.Bag);
            _containerWindows.AttachBag(GameSession.Secure);
            for (int i = 0; i < _enemies.Count; i++)
            {
                _containerWindows.AttachBag(_enemies[i].Bag);
            }

            _containerWindows.WindowClosed += OnContainerWindowClosed;

            if (_lootInteraction != null)
            {
                _lootInteraction.TryActivateItem = (source, instanceId) =>
                    _containerWindows != null && _containerWindows.TryOpen(source.Bag, instanceId);
                _lootInteraction.FindDropContainer = screen =>
                    _containerWindows != null ? _containerWindows.FindContainerAt(screen) : null;
                _lootInteraction.ViewsRefreshed += RefreshContainerWindows;
            }
        }

        private void OnContainerWindowOpened(ContainerWindow window)
        {
            if (_lootInteraction == null || window == null || window.Source == null)
            {
                return;
            }

            _lootInteraction.AddSource(new LootSource
            {
                Bag = window.Source.Bag,
                Rect = window.Source.Rect,
                View = window.View
            });
        }

        private void OnContainerWindowClosed(ContainerWindow window)
        {
            if (_lootInteraction != null && window != null && window.Source != null)
            {
                _lootInteraction.RemoveSource(window.Source.Bag);
            }
        }

        private void RefreshContainerWindows()
        {
            if (_containerWindows != null)
            {
                _containerWindows.RefreshAll();
            }
        }

        private void OnLootTaken(string displayName, int value)
        {
            _takenValue += value;
            SetMessage("获得 " + displayName + "（+" + value + "）");
            _shell.RefreshContainers();
            UpdateLootBoard();
        }

        private void FinishLoot()
        {
            _lootPhase = false;
            if (_lootInteraction != null)
            {
                _lootInteraction.enabled = false;
            }

            if (GameSession.ExpeditionActive && GameSession.Scenario != null)
            {
                GameSession.CompleteEncounter(_player.Hp);
                _ended = true;
                _exit = CombatExit.Looted;
                _endOverlay.SetActive(true);
                _endTitle.text = "胜利";
                _endSubtitle.text = "带出战利品：" + _takenValue + "\n点击继续探索。";
                return;
            }

            GameSession.CompleteExpedition(true);
            _ended = true;
            _exit = CombatExit.Looted;
            _endOverlay.SetActive(true);
            _endTitle.text = "已撤离";
            _endSubtitle.text = "带出战利品：" + _takenValue + "\n点击返回基地。";
        }

        /// <summary>
        /// 点击结局遮罩后离场。压栈模式只弹栈不换场景——这是本步最大的行为变化：
        /// 原来赢一场战斗会 LoadScene 把探索界面的 2122 行 UI 全部重建，现在只销毁四个容器。
        /// </summary>
        private void Leave()
        {
            if (!_pushed)
            {
                SceneManager.LoadScene("BasePrototype");
                return;
            }

            Action<CombatExit> callback = _onExit;
            _onExit = null;
            CombatExit exit = _exit;

            TearDown();

            if (_shell != null && _screen != null)
            {
                _shell.PopScreen(_screen);
                _screen = null;
            }

            Destroy(gameObject);

            if (callback != null)
            {
                callback(exit);
            }
        }

        /// <summary>
        /// 把战斗期间挂到外壳公共层上的东西收干净。战斗屏自己的四个容器由 PopScreen 销毁，
        /// 但 F1 的交互组件、tooltip、特效、搜刮 ghost 和浮动窗口挂在外壳上，必须手动清。
        /// </summary>
        private void TearDown()
        {
            // 账本是外壳的，战斗只是临时借走它那块位置——离场必须还回去。
            if (_shell != null)
            {
                _shell.SetLedgerVisible(true);
            }

            if (_playerInteraction != null)
            {
                Destroy(_playerInteraction);
                _playerInteraction = null;
            }

            if (_playerView != null)
            {
                _playerView.ItemEnabledPredicate = null;
                _playerView.Refresh();
            }

            if (_secureView != null)
            {
                _secureView.ItemEnabledPredicate = null;
                _secureView.Refresh();
            }

            if (_containerWindows != null)
            {
                _containerWindows.CloseAll();
            }

            // 战场带和播报行直接挂在 ScreenLayer 上（它们跨 A/C/D 三个区，不属于任何一个洞），
            // 所以 PopScreen 不会替我们销毁，必须自己收。
            if (_band != null)
            {
                Destroy(_band.gameObject);
                _band = null;
            }

            if (_messageText != null)
            {
                Destroy(_messageText.gameObject);
                _messageText = null;
            }

            if (_tooltipPanel != null)
            {
                Destroy(_tooltipPanel);
                _tooltipPanel = null;
            }

            if (_endOverlay != null)
            {
                Destroy(_endOverlay);
                _endOverlay = null;
            }

            if (_fxPulse != null)
            {
                Destroy(_fxPulse.gameObject);
                _fxPulse = null;
            }

            if (_fxSpark != null)
            {
                Destroy(_fxSpark.gameObject);
                _fxSpark = null;
            }

            if (_lootInteraction != null)
            {
                Destroy(_lootInteraction);
                _lootInteraction = null;
            }

            Transform ghost = _shell != null ? _shell.Root.Find("LootDragGhost") : null;
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
            }
        }

        // ---------- 道具使用动画 ----------

        private void BuildFxLayer(Transform canvas)
        {
            _fxPulse = CreateFxImage(canvas, "UsePulse");
            _fxSpark = CreateFxImage(canvas, "UseSpark");
            _fxSpark.rectTransform.sizeDelta = new Vector2(9f, 9f);
        }

        private static Image CreateFxImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.White;
            image.raycastTarget = false;
            go.SetActive(false);
            return image;
        }

        private bool SpeedButtonContains(Vector2 screen)
        {
            return _speedRect != null
                   && _speedRect.gameObject.activeSelf
                   && RectTransformUtility.RectangleContainsScreenPoint(_speedRect, screen, null);
        }

        private void CycleAnimationSpeed()
        {
            _animationSpeed = _animationSpeed == 1 ? 2 : (_animationSpeed == 2 ? 4 : 1);
            GameSettings.AnimationSpeed = _animationSpeed;
            if (_speedText != null)
            {
                _speedText.text = "动画 " + _animationSpeed + "×";
            }

            SetMessage("战斗动画速度：×" + _animationSpeed);
        }

        private void OnGameSettingsChanged()
        {
            _animationSpeed = GameSettings.AnimationSpeed;
            if (_speedText != null)
            {
                _speedText.text = "动画 " + _animationSpeed + "×";
            }
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= OnGameSettingsChanged;
        }

        private List<UseAnimationStep> CollectUseSteps(int eventStart)
        {
            var result = new List<UseAnimationStep>();
            var groups = new Dictionary<string, List<UseAnimationStep>>();
            var actorOrder = new List<string>();

            for (int i = eventStart; i < _engine.Events.Count; i++)
            {
                CombatEvent ev = _engine.Events[i];
                if (ev.Type != CombatEventType.ItemUsed)
                {
                    continue;
                }

                BagBinding source = FindBindingFor(ev.ActorId, ev.InstanceId);
                if (source == null)
                {
                    // 用掉之后连背包里都没有了（修复包这类），但玩家确实点了这一下：
                    // 用点击瞬间的快照补一条，动画才不会整段消失。
                    if (ev.InstanceId != _usedItemInstance || ev.ActorId != _usedItemActorId)
                    {
                        continue;
                    }

                    source = new BagBinding
                    {
                        Owner = _player,
                        Bag = _player.Bag,
                        Rect = _playerBagRect,
                        View = _playerView
                    };
                }

                var step = new UseAnimationStep
                {
                    Source = source,
                    InstanceId = ev.InstanceId,
                    Target = _engine.FindActor(ev.TargetId),
                    ActorId = ev.ActorId,
                    DefinitionId = ev.ItemId
                };

                List<UseAnimationStep> group;
                if (!groups.TryGetValue(ev.ActorId, out group))
                {
                    group = new List<UseAnimationStep>();
                    groups[ev.ActorId] = group;
                    actorOrder.Add(ev.ActorId);
                }

                group.Add(step);
            }

            for (int i = 0; i < actorOrder.Count; i++)
            {
                List<UseAnimationStep> group = groups[actorOrder[i]];
                group.Sort(CompareByReadingOrder);
                result.AddRange(group);
            }

            return result;
        }

        private BagBinding FindBindingFor(string actorId, int instanceId)
        {
            for (int i = 0; i < _bags.Count; i++)
            {
                BagBinding binding = _bags[i];
                if (binding.Owner == null || binding.Owner.Id != actorId || binding.View == null)
                {
                    continue;
                }

                // 保险箱（以及将来任何"不能用来使用"的背包）不参与：编号是按背包各自起的，
                // 保险箱里的 #1 和背包里的 #1 是两件不同的东西，让它参与就会从保险箱飘出光点。
                if (!binding.IsUseSource)
                {
                    continue;
                }

                ItemView view;
                if (binding.View.TryGetItemView(instanceId, out view))
                {
                    return binding;
                }
            }

            // 视图里没有 ≠ 这一下没发生：消耗品用掉后视图已经刷新过了。
            // 退一步按背包数据找，这样这一步仍然会播动画（起点走点击快照）。
            for (int i = 0; i < _bags.Count; i++)
            {
                BagBinding binding = _bags[i];
                if (binding.Owner == null || binding.Owner.Id != actorId || binding.Bag == null)
                {
                    continue;
                }

                if (!binding.IsUseSource)
                {
                    continue;
                }

                PlacedItem item;
                if (binding.Bag.TryGet(instanceId, out item))
                {
                    return binding;
                }
            }

            return null;
        }

        private int CompareByReadingOrder(UseAnimationStep a, UseAnimationStep b)
        {
            RectTransform rectA = ItemRectOf(a);
            RectTransform rectB = ItemRectOf(b);
            if (rectA == null)
            {
                return rectB == null ? a.InstanceId.CompareTo(b.InstanceId) : 1;
            }

            if (rectB == null)
            {
                return -1;
            }

            Vector2 posA = rectA.anchoredPosition;
            Vector2 posB = rectB.anchoredPosition;
            int row = posB.y.CompareTo(posA.y);
            return row != 0 ? row : posA.x.CompareTo(posB.x);
        }

        private RectTransform ItemRectOf(UseAnimationStep step)
        {
            if (step == null || step.Source == null || step.Source.View == null)
            {
                return null;
            }

            ItemView view;
            return step.Source.View.TryGetItemView(step.InstanceId, out view)
                ? view.transform as RectTransform
                : null;
        }

        /// <summary>
        /// 动画落点。玩家没有独立面板（数字都在外壳左栏），闪光落在左栏整块上；
        /// 敌人落在它自己那张卡上——卡有 300×168，闪光看得见，也是"这只挨打了"最直接的表达。
        /// </summary>
        private RectTransform PanelRectFor(CombatActor actor)
        {
            if (actor == null)
            {
                return null;
            }

            if (actor.IsPlayer)
            {
                return _shell != null ? _shell.StatusRect : null;
            }

            int index = _enemies.IndexOf(actor);
            return index >= 0 && index < _enemyCards.Count ? _enemyCards[index] : null;
        }

        private Image PanelImageFor(CombatActor actor)
        {
            if (actor == null)
            {
                return null;
            }

            if (actor.IsPlayer)
            {
                return _shell != null && _shell.StatusRect != null
                    ? _shell.StatusRect.GetComponent<Image>()
                    : null;
            }

            int index = _enemies.IndexOf(actor);
            return index >= 0 && index < _enemyCardImages.Count ? _enemyCardImages[index] : null;
        }

        private bool StartAnimations(List<UseAnimationStep> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return false;
            }

            _animSteps.Clear();
            _animSteps.AddRange(steps);
            _animStepIndex = 0;
            _animTime = 0f;
            _animating = true;
            if (_playerInteraction != null)
            {
                _playerInteraction.enabled = false;
            }

            _tooltipPanel.SetActive(false);
            BeginAnimationStep(0);
            return true;
        }

        /// <summary>
        /// 记住"这次要点的是哪个格子"。必须在 <see cref="CombatEngine.TryUseItem"/> 之前调用：
        /// 消耗品用掉后会从背包移除，动画开始时已经找不到 ItemView 了。
        ///
        /// 除了 RectTransform 本身，还把当时的画布坐标和尺寸抄一份：格子被 Refresh 销毁之后
        /// 那个 RectTransform 就成了空引用，但光点的起点仍然应该是玩家点的那一格。
        /// </summary>
        private void SnapshotUsedItem(int instanceId)
        {
            _usedItemInstance = instanceId;
            _usedItemActorId = _player != null ? _player.Id : null;
            _usedItemRect = null;
            _usedItemSprite = null;
            _usedItemPointValid = false;

            ItemView view;
            if (_playerView != null && _playerView.TryGetItemView(instanceId, out view))
            {
                _usedItemRect = view.transform as RectTransform;
                _usedItemSprite = view.Image != null ? view.Image.sprite : null;
                if (_usedItemRect != null)
                {
                    _usedItemPoint = CanvasPoint(_usedItemRect);
                    _usedItemSize = _usedItemRect.rect.size;
                    _usedItemPointValid = true;
                }
            }
        }

        private void BeginAnimationStep(int index)
        {
            UseAnimationStep step = _animSteps[index];
            _animHasItem = false;
            _animTargetRect = PanelRectFor(step.Target);
            _animTargetImage = PanelImageFor(step.Target);
            _animTargetBaseColor = _animTargetImage != null ? _animTargetImage.color : Color.white;

            Sprite sprite = null;
            ItemView view;
            if (step.InstanceId == _usedItemInstance && step.ActorId == _usedItemActorId)
            {
                // 玩家自己刚点的那一格是**唯一权威**的起点：编号按背包各自起，
                // 反查视图有可能落到别的背包里编号相同的物品上（保险箱里的那件）。
                sprite = _usedItemSprite;
                if (_usedItemRect != null)
                {
                    _animItemPoint = CanvasPoint(_usedItemRect);
                    _animItemSize = _usedItemRect.rect.size;
                    _animHasItem = true;
                }
                else if (_usedItemPointValid)
                {
                    _animItemPoint = _usedItemPoint;
                    _animItemSize = _usedItemSize;
                    _animHasItem = true;
                }
            }

            if (!_animHasItem && step.Source != null && step.Source.View != null
                && step.Source.View.TryGetItemView(step.InstanceId, out view))
            {
                var itemRect = view.transform as RectTransform;
                if (itemRect != null)
                {
                    _animItemPoint = CanvasPoint(itemRect);
                    _animItemSize = itemRect.rect.size;
                    _animHasItem = true;
                    sprite = view.Image != null ? view.Image.sprite : null;
                }
            }

            if (_animHasItem)
            {
                _fxPulse.sprite = sprite != null ? sprite : SpriteFactory.White;
                _fxPulse.rectTransform.sizeDelta = _animItemSize + new Vector2(3f, 3f);
            }

            _fxPulse.gameObject.SetActive(false);
            _fxSpark.gameObject.SetActive(false);
        }

        private void UpdateAnimation()
        {
            _animTime += Time.unscaledDeltaTime * _animationSpeed;
            while (_animTime >= AnimStepSeconds)
            {
                FinishAnimationStep();
                _animStepIndex++;
                if (_animStepIndex >= _animSteps.Count)
                {
                    EndAnimation();
                    return;
                }

                _animTime -= AnimStepSeconds;
                BeginAnimationStep(_animStepIndex);
            }

            UpdateAnimationVisuals();
        }

        private void UpdateAnimationVisuals()
        {
            float t = _animTime;
            if (_animHasItem && t <= AnimPulseSeconds)
            {
                float p = Mathf.Clamp01(t / AnimPulseSeconds);
                _fxPulse.gameObject.SetActive(true);
                _fxPulse.rectTransform.anchoredPosition = _animItemPoint;
                float scale = 1f + 0.24f * Mathf.Sin(Mathf.PI * p);
                _fxPulse.rectTransform.localScale = new Vector3(scale, scale, 1f);
                Color color = _fxPulse.color;
                color.a = 0.6f * (1f - p);
                _fxPulse.color = color;
            }
            else
            {
                _fxPulse.gameObject.SetActive(false);
            }

            if (_animHasItem && _animTargetRect != null
                && t >= AnimSparkStart && t <= AnimSparkStart + AnimSparkSeconds)
            {
                float p = Mathf.Clamp01((t - AnimSparkStart) / AnimSparkSeconds);
                _fxSpark.gameObject.SetActive(true);
                Vector2 from = _animItemPoint;
                Vector2 to = CanvasPoint(_animTargetRect);
                _fxSpark.rectTransform.anchoredPosition = Vector2.Lerp(from, to, p);
            }
            else
            {
                _fxSpark.gameObject.SetActive(false);
            }

            if (_animTargetImage != null && t >= AnimFlashStart && t <= AnimFlashStart + AnimFlashSeconds)
            {
                float p = Mathf.Clamp01((t - AnimFlashStart) / AnimFlashSeconds);
                _animTargetImage.color = Color.Lerp(_animTargetBaseColor, FlashColor, Mathf.Sin(Mathf.PI * p) * AnimFlashStrength);
            }
            else if (_animTargetImage != null)
            {
                _animTargetImage.color = _animTargetBaseColor;
            }
        }

        private Vector2 CanvasPoint(RectTransform rect)
        {
            Vector3 world = rect.TransformPoint(rect.rect.center);
            return _canvasRect.InverseTransformPoint(world);
        }

        private void FinishAnimationStep()
        {
            _fxPulse.gameObject.SetActive(false);
            _fxSpark.gameObject.SetActive(false);
            if (_animTargetImage != null)
            {
                _animTargetImage.color = _animTargetBaseColor;
            }
        }

        private void EndAnimation()
        {
            _animating = false;
            _animSteps.Clear();
            FinishAnimationStep();
            if (_playerInteraction != null)
            {
                _playerInteraction.enabled = true;
            }

            RefreshAll();
        }

        private void UpdateTooltip(Vector2 screen)
        {
            if (_tooltipPanel == null || _ended)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                _tooltipPanel.SetActive(false);
                return;
            }

            BagBinding hovered = null;
            PlacedItem hoveredItem = null;

            for (int i = 0; i < _bags.Count; i++)
            {
                Cell cell;
                if (!TryScreenToCell(_bags[i].Rect, _bags[i].Bag, screen, out cell))
                {
                    continue;
                }

                int instanceId = _bags[i].Bag.Grid.GetOccupant(cell.X, cell.Y);
                if (instanceId == 0)
                {
                    continue;
                }

                PlacedItem item;
                if (_bags[i].Bag.TryGet(instanceId, out item))
                {
                    hovered = _bags[i];
                    hoveredItem = item;
                    break;
                }
            }

            if (hovered == null)
            {
                _tooltipPanel.SetActive(false);
                return;
            }

            ItemDefinition definition = _catalog.Get(hoveredItem.DefinitionId);
            int damage = _engine.GetDamage(hovered.Owner, hoveredItem);
            int block = _engine.GetShield(hovered.Owner, hoveredItem);
            _tooltipText.text = ItemText.Describe(
                definition,
                hoveredItem,
                _engine.GetAmmoCount(hovered.Owner, definition),
                damage,
                block);
            const float tooltipWidth = 240f;
            _tooltipText.rectTransform.sizeDelta = new Vector2(tooltipWidth - 16f, 200f);
            float tooltipHeight = Mathf.Clamp(_tooltipText.preferredHeight + 14f, 120f, 300f);
            _tooltipRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
            _tooltipText.rectTransform.sizeDelta = new Vector2(tooltipWidth - 16f, tooltipHeight - 12f);

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local);
            float x = local.x + 14f;
            if (x + tooltipWidth > 312f)
            {
                x = local.x - tooltipWidth - 14f;
            }

            x = Mathf.Clamp(x, -312f, 312f - tooltipWidth);
            float y = Mathf.Clamp(local.y - 14f, -170f + tooltipHeight, 168f);
            _tooltipRect.anchoredPosition = new Vector2(x, y);
            _tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// 敌人意图只给模糊类别，避免预览动作和实际动作不一致（例如能量不足显示待机却用了道具）。
        /// 类别：突击 / 治愈 / 防守 / 准备撤离。
        /// </summary>
        private string IntentFor(CombatActor enemy)
        {
            return "意图：" + IntentWord(enemy);
        }

        private string IntentWord(CombatActor enemy)
        {
            if (!_engine.CanActorAttack(enemy))
            {
                return "准备撤离";
            }

            bool wounded = enemy.Hp * 2 <= enemy.MaxHp;
            bool hasHeal = false;
            bool hasGuard = false;
            IReadOnlyList<PlacedItem> items = enemy.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = _catalog.Get(items[i].DefinitionId);
                if (definition == null || !definition.IsActive)
                {
                    continue;
                }

                if (definition.Heal > 0)
                {
                    hasHeal = true;
                }

                if (definition.Shield > 0)
                {
                    hasGuard = true;
                }
            }

            if (wounded && hasHeal)
            {
                return "治愈";
            }

            if (wounded && hasGuard)
            {
                return "防守";
            }

            return "突击";
        }

        private int MaxEnergy(CombatActor actor)
        {
            return actor.BaseEnergy + _engine.GetPassiveEnergy(actor);
        }

        private string ArmorLine(CombatActor actor)
        {
            var parts = new List<string>();
            AppendArmor(parts, "近", _engine.GetArmor(actor, DamageType.Melee));
            AppendArmor(parts, "抛", _engine.GetArmor(actor, DamageType.Projectile));
            AppendArmor(parts, "烧", _engine.GetArmor(actor, DamageType.Burn));
            AppendArmor(parts, "毒", _engine.GetArmor(actor, DamageType.Toxin));
            AppendArmor(parts, "窒", _engine.GetArmor(actor, DamageType.Asphyxiation));

            return parts.Count == 0 ? "护甲 -" : "护甲 " + string.Join(" ", parts);
        }

        private static void AppendArmor(List<string> parts, string name, int value)
        {
            if (value > 0)
            {
                parts.Add(name + value);
            }
        }

        private string ReductionLine(CombatActor actor)
        {
            var parts = new List<string>();
            AppendReduction(parts, "近", _engine.GetArmor(actor, DamageType.Melee));
            AppendReduction(parts, "抛", _engine.GetArmor(actor, DamageType.Projectile));
            AppendReduction(parts, "烧", _engine.GetArmor(actor, DamageType.Burn));
            AppendReduction(parts, "毒", _engine.GetArmor(actor, DamageType.Toxin));
            AppendReduction(parts, "窒", _engine.GetArmor(actor, DamageType.Asphyxiation));

            return parts.Count == 0 ? "减伤 -" : "减伤 " + string.Join(" ", parts);
        }

        private static void AppendReduction(List<string> parts, string name, int armor)
        {
            if (armor > 0)
            {
                parts.Add(name + ArmorMath.ReductionPercent(armor) + "%");
            }
        }

        private static string StatusSuffix(CombatActor actor)
        {
            var builder = new StringBuilder();
            IReadOnlyList<StatusEffect> statuses = actor.Statuses;

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusEffect status = statuses[i];
                if (status.Type == StatusType.None || status.Stacks <= 0)
                {
                    continue;
                }

                builder.Append("  ").Append(ItemText.StatusName(status.Type)).Append(status.Stacks);
                if (BuffLibrary.Decays(status.Type) && status.Turns > 1)
                {
                    builder.Append("(").Append(status.Turns).Append(")");
                }
            }

            return builder.ToString();
        }

        private CombatActor CurrentTarget()
        {
            if (_selectedEnemy >= 0 && _selectedEnemy < _enemies.Count
                && _enemies[_selectedEnemy].IsAlive && !_enemies[_selectedEnemy].HasFled)
            {
                return _enemies[_selectedEnemy];
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive && !_enemies[i].HasFled)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        private void EnsureSelection()
        {
            if (_selectedEnemy >= 0 && _selectedEnemy < _enemies.Count
                && _enemies[_selectedEnemy].IsAlive && !_enemies[_selectedEnemy].HasFled)
            {
                return;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive && !_enemies[i].HasFled)
                {
                    _selectedEnemy = i;
                    return;
                }
            }
        }

        private void SetMessage(string text)
        {
            if (_messageText != null)
            {
                _messageText.text = text;
            }
        }

        private static bool TryScreenToCell(RectTransform rect, BagInventory bag, Vector2 screen, out Cell cell)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out local))
            {
                cell = Cell.Zero;
                return false;
            }

            float halfWidth = bag.Grid.Width * BagView.CellSize * 0.5f;
            float halfHeight = bag.Grid.Height * BagView.CellSize * 0.5f;

            int x = Mathf.FloorToInt((local.x + halfWidth) / BagView.CellSize);
            int y = Mathf.FloorToInt((halfHeight - local.y) / BagView.CellSize);

            cell = new Cell(x, y);
            return x >= 0 && x < bag.Grid.Width && y >= 0 && y < bag.Grid.Height;
        }
    }
}

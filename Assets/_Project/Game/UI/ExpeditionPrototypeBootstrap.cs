using System;
using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static BitRunner.Game.UiFactory;
using static BitRunner.Game.UiPalette;
using static BitRunner.Game.UiTheme;

namespace BitRunner.Game
{
    public sealed class ExpeditionPrototypeBootstrap : MonoBehaviour
    {

        private ExpeditionScenario _scenario;
        private ItemCatalog _catalog;
        private BagInventory _bag;
        private BagInventory _secure;
        private Dictionary<string, Color> _palette;

        private RectTransform _canvasRect;
        private GameShell _shell;
        private RectTransform _sellBinRect;
        private Text _titleText;
        private Text _stageSubtitle;
        private Text _sideFooter;
        private string _lastZoneId;
        private Text _narrationText;
        private Text _detailText;
        private Text _selectedLabel;
        private Text _selectedRisk;
        private Text _confirmText;
        private RectTransform _confirmRect;
        private RectTransform _discardRect;

        private ItemCatalog _actionCatalog;
        private ItemCatalog _combinedCatalog;
        private BagInventory _actionBag;
        private BagInventory _selectedActionBag;
        private Dictionary<string, Color> _actionPalette;
        private BagView _actionView;
        private BagView _selectedActionView;
        private BagView _playerActionView;
        private RectTransform _actionBagRect;
        private RectTransform _playerActionBagRect;
        private RectTransform _selectedActionBagRect;
        private RectTransform _tabActionRect;
        private RectTransform _tabBagRect;
        private RectTransform _sidePanelRect;
        private BaseDragInteraction _actionDrag;
        private string _selectedActionId;
        private Text _assistHint;

        /// <summary>已选行动最多携带几个辅助标志。</summary>
        private const int MaxAssists = 3;
        private bool _showPlayerBag;
        private bool _ended;

        // ---- 压在基地上面的一屏 ----
        /// <summary>本屏在 <see cref="GameShell"/> 屏幕栈里的那一组容器。弹栈时连同它一起销毁。</summary>
        private ShellScreen _screen;

        /// <summary>退出回调。基地据此恢复自己的那一屏。</summary>
        private Action<ExpeditionExit> _onExit;

        private bool _initialized;

        /// <summary>
        /// 主界面的悬停源。= 行动拖拽那套 + 外壳的 F1 背包 / F3 保险箱。
        ///
        /// 外壳那两个是三种模式共用的控件，从来没进过行动拖拽的源列表；
        /// 但玩家就会直接去悬停它们（背包里的装备、保险箱里的保底物资），
        /// 少这两条就变成"主界面什么都悬停不出来"。这里只用于查信息，不参与拖拽。
        /// </summary>
        private readonly List<BaseBagSource> _mainHoverSources = new List<BaseBagSource>();

        /// <summary>缓存悬停源时用的背包引用，用来发现"行动袋被换成新实例了"。见 <see cref="EnsureMainHoverSources"/>。</summary>
        private BagInventory _hoverActionBag;
        private BagInventory _hoverSelectedBag;
        private BagInventory _hoverPack;
        private BagInventory _hoverSecure;
        private bool _hoverShowPlayerBag;

        /// <summary>
        /// 压栈那一帧的点击不能算数。
        ///
        /// 「出征」和「执行」在主按钮上是同一个矩形（455,14,160,28）——基地的 Update 处理完这次按下、
        /// 立刻把探索拉起来，同一帧里探索的 Update 也会看到 wasPressedThisFrame=true，
        /// 于是刚进探索就自己执行了一次行动。这里把发起压栈的那一帧整帧屏蔽掉。
        /// </summary>
        private int _ignoreInputUntilFrame = -1;

        private GameObject _bagOverlay;
        private RectTransform _bagCloseRect;
        private RectTransform _bagDiscardRect;
        private RectTransform _bagPlayerRect;
        private RectTransform _bagSecureRect;
        private BagView _bagView;
        private BagView _secureView;
        private Text _bagSecureTitle;
        private Text _bagHint;
        private BaseDragInteraction _bagDrag;
        private bool _bagOpen;

        private GameObject _lootPanel;
        private RectTransform _lootPanelRect;
        private RectTransform _lootCloseRect;
        private RectTransform _lootAllRect;
        private RectTransform _lootDiscardRect;
        private RectTransform _lootBoxRect;
        private BagView _lootBoxView;
        private Text _lootTitle;
        private Text _lootHint;
        private Text _lootAllText;
        private BaseDragInteraction _lootDrag;
        private FloatingWindowManager _containerWindows;
        private BagInventory _lootBag;
        private int _lastPlayerItemCount;
        private bool _lootOpen;

        private GameObject _itemTooltipPanel;
        private RectTransform _itemTooltipRect;
        private Text _itemTooltipText;

        /// <summary>这一趟探索是怎么结束的。基地据此决定刷新什么、要不要弹结局。</summary>
        public enum ExpeditionExit
        {
            /// <summary>正常撤出 / 走完节点，包裹已入库。</summary>
            Completed,

            /// <summary>倒下，背包丢了，本次出征结束。</summary>
            Defeated,

            /// <summary>没有进行中的探索，什么都没做。</summary>
            Aborted
        }

        /// <summary>
        /// 由基地压栈启动探索。与 <see cref="CombatPrototypeBootstrap.Launch"/> 同一个套路：
        /// 显式 Initialize，不等 Unity 的 Start——压栈是在别的组件的调用栈里发起的，
        /// 等 Start 有时会被推迟到下一帧，那时界面已经晚了。
        /// </summary>
        public static ExpeditionPrototypeBootstrap Launch(GameShell shell, Action<ExpeditionExit> onExit)
        {
            var host = new GameObject("ExpeditionScreen");
            var expedition = host.AddComponent<ExpeditionPrototypeBootstrap>();
            expedition._shell = shell;
            expedition._onExit = onExit;
            expedition.Initialize();
            return expedition;
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
            _bag = GameSession.PlayerBag;
            _secure = GameSession.Secure;
            _scenario = GameSession.Scenario;
            _palette = BuildPalette(_catalog);
            _actionCatalog = ExpeditionActionItemLibrary.CreateCatalog();
            _combinedCatalog = BuildCombinedCatalog(_catalog, _actionCatalog);
            _actionPalette = BuildActionPalette(_combinedCatalog);

            if (_scenario == null)
            {
                GameSession.LastMessage = "没有进行中的探索。";
                Exit(ExpeditionExit.Aborted);
                return;
            }

            // 相机、画布、左栏、账本、背包、保险箱、日志全部来自外壳实例——
            // 与基地是同一批控件，切模式不重建。
            if (_shell == null)
            {
                _shell = GameShell.Ensure(_bag, _secure, _palette);
            }

            _canvasRect = _shell.Root;

            // 压栈：基地那一屏整组隐藏（不销毁），探索这一屏新开四个容器。
            // A 账本 / E 左栏 / F1 背包 / F3 保险箱 / G 日志不入栈，所以两屏之间是同一批控件。
            _screen = _shell.PushScreen("Expedition");
            _shell.SetLedgerVisible(true);

            BuildStage(_screen.Region(ShellRegionId.Stage));
            BuildSide(_screen.Region(ShellRegionId.Side));
            BuildPrimary(_screen.Region(ShellRegionId.Primary));
            BuildDock(_screen.Region(ShellRegionId.Dock));
            BuildActionDrag();
            RebuildMainHoverSources();
            BuildBagOverlay(_shell.OverlayLayer);
            BuildLootPanel();
            BuildItemTooltip(_shell.TooltipLayer);
            BuildContainerWindows();

            _titleText.text = _scenario.Title + " · " + _scenario.ZoneName;
            _lastZoneId = _scenario.ZoneId;
            RebuildActionBags();
            RefreshAll();
            string opening = _scenario.Stage == 0
                ? _scenario.IntroText
                : string.IsNullOrEmpty(GameSession.LastMessage)
                    ? _scenario.IntroText
                    : GameSession.LastMessage;
            SetNarration(opening);
            AppendLog(string.IsNullOrEmpty(GameSession.LastMessage) ? opening : GameSession.LastMessage);
            AppendLog("【" + _scenario.ZoneName + "】" + _scenario.ZoneBlurb);
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput || Time.frameCount <= _ignoreInputUntilFrame)
            {
                return;
            }

            if (_ended || _scenario == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            Vector2 screen = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            UpdateItemTooltip(screen);

            if (_lootOpen)
            {
                // 搜刮面板不是遮罩：背包（F1）和保险箱（F3）一直露在外面可以直接拖进去，
                // 所以这里只处理面板自己的两颗按钮和 Esc，别的一概不碰。
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    CloseLoot();
                    return;
                }

                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    if (Contains(_lootCloseRect, screen))
                    {
                        CloseLoot();
                        return;
                    }

                    if (Contains(_lootAllRect, screen))
                    {
                        TakeAllLoot();
                        return;
                    }

                    // 搜刮没结束就想去点行动：不静默吞掉，直接告诉他为什么点不动。
                    // 这三块是玩家最可能去点的：主按钮（执行）、行动/背包菜单、已选行动。
                    ShowLootBlocksHint(screen);
                }

                return;
            }

            if (_bagOpen)
            {
                if ((mouse != null && mouse.leftButton.wasPressedThisFrame
                     && RectTransformUtility.RectangleContainsScreenPoint(_bagCloseRect, screen, null))
                    || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
                {
                    CloseBag();
                }

                return;
            }

            if (_actionDrag != null && _actionDrag.IsDragging)
            {
                return;
            }

            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    ConfirmSelected();
                    return;
                }

                if (keyboard.bKey.wasPressedThisFrame)
                {
                    OpenBag();
                    return;
                }
            }

            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(_tabActionRect, screen, null))
            {
                SetBackpackTab(false);
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(_tabBagRect, screen, null))
            {
                SetBackpackTab(true);
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(_confirmRect, screen, null))
            {
                ConfirmSelected();
                return;
            }

        }

        private void ConfirmSelected()
        {
            if (_ended)
            {
                return;
            }

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                _scenario,
                SelectedPrimaryItems(),
                SelectedAssistItems(),
                _combinedCatalog,
                GameSession.ExpeditionHp,
                GameSession.ExpeditionMaxHp);

            if (!selection.IsValid)
            {
                SetNarration(selection.Reason);
                return;
            }

            if (selection.IsItemUse)
            {
                string message;
                if (!GameSession.UseExpeditionItem(_selectedActionBag, selection.Item.InstanceId, out message))
                {
                    SetNarration(message);
                    return;
                }

                _selectedActionId = null;
                RebuildActionBags();
                AppendLog(message);
                SetNarration(message);
                RefreshAll();
                return;
            }

            ExpeditionActionOption action = selection.Action;
            if (action == null)
            {
                SetNarration("这个行动已经无法执行。");
                return;
            }

            ExpeditionResolution result = GameSession.ResolveExpeditionAction(action.Id, selection.Assists, _selectedActionBag);
            if (result == null)
            {
                SetNarration("这个行动已经无法执行。");
                return;
            }

            if (result.IsExit || _scenario.IsComplete)
            {
                // 这一趟走完了：辅助标志是玩家的东西，先收回背包再走。
                ReturnSelectedAssistsToBag();
            }

            _selectedActionId = null;
            RebuildActionBags();
            string resultMessage = GameSession.LastMessage;
            AppendLog(resultMessage);
            SetNarration(resultMessage);
            RefreshAll();

            if (result.Kind == ExpeditionOutcomeKind.Damage && GameSession.ExpeditionHp <= 0)
            {
                _ended = true;
                GameSession.CompleteExpedition(false);
                Exit(ExpeditionExit.Defeated);
                return;
            }

            if (result.Kind == ExpeditionOutcomeKind.Combat)
            {
                EnterCombat();
                return;
            }

            if (result.IsExit)
            {
                _ended = true;
                GameSession.CompleteExpedition(true);
                Exit(ExpeditionExit.Completed);
                return;
            }

            if (result.LootIds != null && result.LootIds.Length > 0)
            {
                OpenLoot(result.LootIds);
            }
        }

        /// <summary>
        /// 进入战斗：压栈，不换场景。
        ///
        /// 老做法是 LoadScene("CombatPrototype")，回来时再 LoadScene("ExpeditionPrototype")，
        /// 于是两边的 2122 + 1518 行 UI 各重建一遍，背包里物品的格子位置、日志、账本全断。
        /// 现在战斗整屏压在探索上面，探索这一屏只是被隐藏——回来时连选中高亮都还在。
        ///
        /// 把整个 GameObject 停掉，是为了让探索这边的拖拽/弹窗/浮动窗口在这期间完全静默；
        /// 战斗的退出回调是普通委托，不受 GameObject 激活状态影响。
        /// </summary>
        private void EnterCombat()
        {
            if (_shell == null)
            {
                // 没有外壳说明这一屏还没建起来，宁可留在原地也不要把战斗压到空气中。
                SetNarration("界面尚未就绪，稍后再试。");
                return;
            }

            if (_containerWindows != null)
            {
                _containerWindows.CloseAll();
            }

            // 搜刮面板挂在外壳的 ScreenLayer 上，压栈不会自动收——进战前必须自己关掉。
            if (_lootOpen)
            {
                CloseLoot();
            }

            gameObject.SetActive(false);
            CombatPrototypeBootstrap.Launch(_shell, OnCombatExit);
        }

        private void OnCombatExit(CombatPrototypeBootstrap.CombatExit exit)
        {
            gameObject.SetActive(true);

            if (exit == CombatPrototypeBootstrap.CombatExit.Defeated)
            {
                _ended = true;
                GameSession.LastMessage = "你倒下了，背包丢在了那里。";
                Exit(ExpeditionExit.Defeated);
                return;
            }

            _selectedActionId = null;
            RebuildActionBags();
            RefreshAll();

            string message = exit == CombatPrototypeBootstrap.CombatExit.Looted
                ? "战斗胜利，战利品已带走。"
                : "你脱离了战斗，回到" + (_scenario != null ? _scenario.ZoneName : "原地") + "。";
            SetNarration(message);
            AppendLog(message);
        }

        /// <summary>
        /// 离场：收干净挂在外壳公共层上的东西，弹掉自己这一屏，然后通知基地。
        ///
        /// 老做法是 LoadScene("BasePrototype")——基地整屏重建，背包、日志、账本全断。
        /// 现在只是把上面这一层揭掉，下面基地那一屏一直原样待着。
        /// </summary>
        private void Exit(ExpeditionExit exit)
        {
            Action<ExpeditionExit> callback = _onExit;
            _onExit = null;

            // 已选行动是个临时格子：里面的辅助标志是玩家的东西，离场前必须收回背包。
            ReturnSelectedAssistsToBag();

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
        /// 收掉建在外壳公共层上的东西。Screen 那四个容器由 PopScreen 销毁，
        /// 但遮罩、物品提示、浮动窗口挂在 OverlayLayer / TooltipLayer 上，不清理就会跟着外壳活到下一屏。
        /// </summary>
        private void TearDown()
        {
            if (_containerWindows != null)
            {
                _containerWindows.CloseAll();
            }

            if (_bagOverlay != null)
            {
                Destroy(_bagOverlay);
                _bagOverlay = null;
            }

            if (_lootPanel != null)
            {
                Destroy(_lootPanel);
                _lootPanel = null;
                _lootPanelRect = null;
                _lootBoxRect = null;
                _lootBoxView = null;
            }

            _lootOpen = false;

            if (_itemTooltipPanel != null)
            {
                Destroy(_itemTooltipPanel);
                _itemTooltipPanel = null;
            }
        }

        private void RefreshAll()
        {
            RefreshStatus();
            if (_actionView != null)
            {
                _actionView.Refresh();
            }

            if (_selectedActionView != null)
            {
                _selectedActionView.Refresh();
            }

            if (_playerActionView != null)
            {
                _playerActionView.Refresh();
            }

            RefreshSelected();

            if (_bagView != null)
            {
                _bagView.Refresh();
            }

            if (_secureView != null)
            {
                _secureView.Refresh();
            }

            if (_bagSecureTitle != null)
            {
                _bagSecureTitle.text = "保险箱 " + _secure.Count + "/4";
            }

            // 弹窗里的 BagView 与外壳的 F1/F3 是两份实例，改完数据要通知外壳重画。
            _shell.RefreshContainers();

            if (_containerWindows != null)
            {
                _containerWindows.RefreshAll();
            }
        }

        private void RefreshStatus()
        {
            if (_scenario == null)
            {
                return;
            }

            // A 区账本 + E 区左栏都由外壳持有，探索这边只负责填数。
            // 这也是探索第一次能看见 bit 与债务条（原先只有基地有）。
            _shell.SetLedger(GameSession.Campaign);

            int maxHp = Mathf.Max(1, GameSession.ExpeditionMaxHp);
            int hp = GameSession.ExpeditionHp;
            float hpRatio = Mathf.Clamp01(hp / (float)maxHp);
            int energy = GameSession.BaseEnergyPerTurn + GameSession.CpuModifiers.BaseEnergy;

            _shell.SetStatus(
                null,
                GameShell.StatusRow.Bar("生命", hp + "/" + maxHp, hpRatio, HpBar(hpRatio)),
                GameShell.StatusRow.Text("能量", energy + "/回合"),
                GameShell.StatusRow.Bar("警戒", _scenario.Alert.ToString(), _scenario.Alert / 4f, AlertColor),
                GameShell.StatusRow.Text("阶段", (_scenario.Stage + 1) + "/" + _scenario.StageCount),
                GameShell.StatusRow.Text("天数", GameSession.Campaign.Day + "/7"));

            _titleText.text = _scenario.Title + " · " + _scenario.ZoneName;
            if (_stageSubtitle != null)
            {
                _stageSubtitle.text = "第 " + (_scenario.Stage + 1) + "/" + _scenario.StageCount
                                      + " 段 · " + (_scenario.IsComplete ? "可撤离" : "未完成");
            }

            if (_sideFooter != null)
            {
                _sideFooter.text = "可用 " + _scenario.CurrentOptions.Count + " 个行动";
            }

            AnnounceZoneIfChanged();
        }

        /// <summary>换区域时把新地点的名字和一句环境描述写进记录，避免玩家不知道自己在哪。</summary>
        private void AnnounceZoneIfChanged()
        {
            string zoneId = _scenario.ZoneId;
            if (string.IsNullOrEmpty(zoneId) || zoneId == _lastZoneId)
            {
                return;
            }

            _lastZoneId = zoneId;
            if (_scenario.Stage > 0)
            {
                AppendLog("【" + _scenario.ZoneName + "】" + _scenario.ZoneBlurb);
            }
        }

        private void RebuildActionBags()
        {
            _selectedActionId = null;
            _actionBag = new BagInventory(6, 4);
            for (int i = 0; i < _scenario.CurrentOptions.Count; i++)
            {
                ExpeditionActionOption option = _scenario.CurrentOptions[i];
                ItemDefinition definition;
                if (!_actionCatalog.TryGet(option.Id, out definition))
                {
                    continue;
                }

                PlacedItem placed;
                _actionBag.TryAdd(option.Id, definition.Shape, false, out placed);
            }

            // 已选行动那一格**不再整个换新**：里面除了这次要执行的行动，还放着玩家的辅助标志（实物）。
            // 换实例等于把标志连同耐久一起丢掉。这里只把"行动/道具"那一件取走，辅助原样留着。
            if (_selectedActionBag == null)
            {
                _selectedActionBag = new BagInventory(4, 3);
            }
            else
            {
                ClearPrimaryFromSelected();
            }

            if (_actionView != null)
            {
                _actionView.Rebind(_actionBag);
            }

            if (_selectedActionView != null)
            {
                _selectedActionView.Refresh();
            }

            if (_actionDrag != null)
            {
                _actionDrag.SetSources(ActionSources());
            }

            // 行动袋换了实例，悬停源里的 ACTIONS / SELECTED 必须跟着指向新的那两个背包。
            RebuildMainHoverSources();

            RefreshSelected();
        }

        private BaseBagSource ActionQuickMoveTarget(BaseBagSource source, PlacedItem item)
        {
            if (source == null || item == null || _actionDrag == null)
            {
                return null;
            }

            if (source.Name == "SELECTED")
            {
                // 双击取回：行动物品回行动袋，普通物品回背包。
                // 背包页签没打开时侧栏没有 PACK 源，但外壳的 F1 一定在——退到它，取回不会失效。
                bool isAction = _scenario != null && _scenario.Find(item.DefinitionId) != null;
                BaseBagSource target = _actionDrag.FindSource(isAction ? "ACTIONS" : "PACK");
                return target != null ? target : _actionDrag.FindSource("SHELL_PACK");
            }

            if (source.Name != "ACTIONS" && source.Name != "PACK" && source.Name != "SHELL_PACK")
            {
                return null;
            }

            // 辅助标志：已选行动里可以放好几件（最多 3 个），具体规则交给 TrySpecialDrop。
            ItemDefinition quickDefinition;
            if (_combinedCatalog.TryGet(item.DefinitionId, out quickDefinition) && quickDefinition.Assist != null)
            {
                return _actionDrag.FindSource("SELECTED");
            }

            // 行动 / 待用道具：一格只放一件。
            if (PrimarySelectedItem() != null)
            {
                return null;
            }

            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                _scenario,
                new[] { item },
                _combinedCatalog,
                GameSession.ExpeditionHp,
                GameSession.ExpeditionMaxHp);
            if (!selection.IsValid)
            {
                return null;
            }

            return _actionDrag.FindSource("SELECTED");
        }

        private List<BaseBagSource> ActionSources()
        {
            var sources = new List<BaseBagSource>();
            if (_showPlayerBag)
            {
                sources.Add(new BaseBagSource
                {
                    Name = "PACK",
                    Bag = _bag,
                    Rect = _playerActionBagRect,
                    View = _playerActionView,
                    AcceptsActionItems = false
                });
            }
            else
            {
                sources.Add(new BaseBagSource
                {
                    Name = "ACTIONS",
                    Bag = _actionBag,
                    Rect = _actionBagRect,
                    View = _actionView,
                    AllowedCategory = ItemCategory.Action
                });
            }

            sources.Add(new BaseBagSource
            {
                Name = "SELECTED",
                Bag = _selectedActionBag,
                Rect = _selectedActionBagRect,
                View = _selectedActionView
            });

            // 外壳的 F1 背包 / F3 保险箱也是拖拽源，不只是悬停源。
            //
            // 它们三种模式常驻在屏幕最下方，是最显眼的那块"背包"；玩家想直接从这里
            // 拖一件消耗品到「已选行动」里用掉，如果它们不在源列表里，拖拽根本起不来
            // ——表现为"拖进已选行动毫无反应"。放在最后，命中判定优先于侧栏那两个。
            if (_shell != null)
            {
                sources.Add(new BaseBagSource
                {
                    Name = "SHELL_PACK",
                    Bag = _bag,
                    Rect = _shell.PackBagRect,
                    View = _shell.PackView,
                    AcceptsActionItems = false
                });

                sources.Add(new BaseBagSource
                {
                    Name = "SHELL_SECURE",
                    Bag = _secure,
                    Rect = _shell.SecureBagRect,
                    View = _shell.SecureView,
                    IsSecure = true
                });
            }

            return sources;
        }

        private void SetBackpackTab(bool showPlayerBag)
        {
            _showPlayerBag = showPlayerBag;
            _actionView.gameObject.SetActive(!showPlayerBag);
            _playerActionView.gameObject.SetActive(showPlayerBag);
            if (showPlayerBag)
            {
                _playerActionView.Refresh();
            }

            if (_actionDrag != null)
            {
                _actionDrag.SetSources(ActionSources());
            }

            RebuildMainHoverSources();
            RefreshTabs();
        }

        private void RefreshTabs()
        {
            if (_tabActionRect == null || _tabBagRect == null)
            {
                return;
            }

            _tabActionRect.GetComponent<Image>().color = _showPlayerBag ? new Color32(42, 48, 57, 255) : SelectedColor;
            _tabBagRect.GetComponent<Image>().color = _showPlayerBag ? SelectedColor : new Color32(42, 48, 57, 255);
            Text actionLabel = _tabActionRect.GetComponentInChildren<Text>();
            Text bagLabel = _tabBagRect.GetComponentInChildren<Text>();
            if (actionLabel != null) actionLabel.color = _showPlayerBag ? MutedColor : TextColor;
            if (bagLabel != null) bagLabel.color = _showPlayerBag ? TextColor : MutedColor;
        }

        private void OnActionBagChanged()
        {
            // 格子里现在可能同时有"行动 + 辅助标志"，取主物品的定义 id，别把辅助当成行动。
            PlacedItem primary = PrimarySelectedItem();
            _selectedActionId = primary != null ? primary.DefinitionId : null;

            RefreshSelected();
            RefreshStatus();
        }

        private void OnActionItemClicked(BaseBagSource source, int instanceId)
        {
            if (source == null || source.Bag == null)
            {
                return;
            }

            PlacedItem item;
            if (!source.Bag.TryGet(instanceId, out item))
            {
                return;
            }

            ExpeditionActionOption option = _scenario.Find(item.DefinitionId);
            if (option == null)
            {
                return;
            }

            SetNarration(option.Description);
        }

        private void RefreshSelected()
        {
            List<PlacedItem> assists = SelectedAssistItems();
            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                _scenario,
                SelectedPrimaryItems(),
                assists,
                _combinedCatalog,
                GameSession.ExpeditionHp,
                GameSession.ExpeditionMaxHp);

            RefreshAssistHint();

            if (_selectedActionView != null)
            {
                _selectedActionView.ItemEnabledPredicate = IsExpeditionUsableItem;
                _selectedActionView.Refresh();
            }

            if (!selection.IsValid)
            {
                if (selection.Item != null)
                {
                    ItemDefinition definition;
                    _combinedCatalog.TryGet(selection.Item.DefinitionId, out definition);
                    _selectedLabel.text = definition != null ? definition.DisplayName : "无效物品";
                    _selectedLabel.color = TextColor;
                    _selectedRisk.text = "无法使用";
                    _selectedRisk.color = DangerColor;
                    _detailText.text = selection.Reason;
                }
                else
                {
                    _selectedLabel.text = "拖入一个行动";
                    _selectedLabel.color = MutedColor;
                    _selectedRisk.text = selection.Reason;
                    _selectedRisk.color = DangerColor;
                    _detailText.text = selection.Reason;
                }

                _confirmText.text = "执行";
                _confirmText.color = MutedColor;
                _confirmRect.GetComponent<Image>().color = DisabledColor;
                return;
            }

            if (selection.IsAction && selection.Action != null)
            {
                ExpeditionActionOption action = selection.Action;
                _selectedLabel.text = action.Label;
                _selectedLabel.color = TextColor;
                _selectedRisk.text = "风险：" + ExpeditionText.RiskName(action.Risk);
                _selectedRisk.color = RiskColor(action.Risk);
                _detailText.text = BuildOutcomePreview(action, selection);
                _confirmText.text = action.IsExit ? "撤离" : "执行";
                _confirmText.color = TextColor;
                _confirmRect.GetComponent<Image>().color = AccentColor;
                return;
            }

            if (selection.IsItemUse && selection.Item != null)
            {
                ItemDefinition definition = _combinedCatalog.Get(selection.Item.DefinitionId);
                _selectedLabel.text = definition.DisplayName;
                _selectedLabel.color = TextColor;
                _selectedRisk.text = "可使用物品";
                _selectedRisk.color = SystemColor;
                _detailText.text = "使用 " + definition.DisplayName + "。\n"
                                   + "推进 1 个探索阶段并消耗该物品。\n"
                                   + "辅助标志不生效。";
                _confirmText.text = "使用";
                _confirmText.color = TextColor;
                _confirmRect.GetComponent<Image>().color = AccentColor;
                return;
            }
        }

        private string BuildOutcomePreview(ExpeditionActionOption action, ExpeditionSelection selection)
        {
            int projectedAlert = _scenario.Alert + selection.AssistAlertDelta;
            var context = new ExpeditionContext
            {
                Hp = GameSession.ExpeditionHp,
                MaxHp = GameSession.ExpeditionMaxHp,
                BagFree = _bag != null ? _bag.FreeCellCount : 0,
                Alert = projectedAlert
            };

            var basePct = new Dictionary<ExpeditionOutcomeKind, double>();
            var finalPct = new Dictionary<ExpeditionOutcomeKind, double>();
            var order = new List<ExpeditionOutcomeKind>();

            var baseContext = context;
            baseContext.Alert = _scenario.Alert;
            AddOddsByKind(ExpeditionOdds.Compute(action, baseContext, null), basePct, order);
            AddOddsByKind(ExpeditionOdds.Compute(action, context, selection.AssistSpecs), finalPct, order);

            order.Sort((a, b) => finalPct[b].CompareTo(finalPct[a]));

            var builder = new StringBuilder();
            builder.Append("警戒 ").Append(_scenario.Alert);
            if (selection.AssistAlertDelta != 0)
            {
                builder.Append(" → ").Append(projectedAlert);
            }

            int shown = 0;
            for (int i = 0; i < order.Count && shown < 5; i++)
            {
                ExpeditionOutcomeKind kind = order[i];
                if (finalPct[kind] < 0.5)
                {
                    continue;
                }

                int before = Mathf.RoundToInt((float)basePct[kind]);
                int after = Mathf.RoundToInt((float)finalPct[kind]);
                int aid = AssistDeltaFor(kind, selection);
                builder.Append('\n').Append(ExpeditionText.OutcomeName(kind)).Append(' ');
                if (aid != 0)
                {
                    builder.Append(before).Append("% → ").Append(after).Append('%');
                    int delta = after - before;
                    if (delta != 0)
                    {
                        builder.Append(" (").Append(delta > 0 ? "+" : string.Empty).Append(delta).Append(')');
                    }
                }
                else
                {
                    builder.Append(after).Append('%');
                }

                ExpeditionOutcomeEntry sample = FirstOutcome(action, kind);
                if (kind == ExpeditionOutcomeKind.Loot && sample != null && sample.LootIds != null)
                {
                    builder.Append(" · ").Append(sample.LootIds.Length).Append("组");
                }
                else if (kind == ExpeditionOutcomeKind.Combat)
                {
                    builder.Append(" · 遭遇敌人");
                }
                else if (kind == ExpeditionOutcomeKind.Damage && sample != null)
                {
                    builder.Append(" · ").Append(sample.Damage).Append("伤害");
                }
                else if (kind == ExpeditionOutcomeKind.Alert && sample != null)
                {
                    builder.Append(" · 警戒+").Append(sample.AlertDelta);
                }

                shown++;
            }

            if (selection.Assists.Count > 0)
            {
                builder.Append('\n').Append("辅助：").Append(AssistNames(selection));
            }

            return builder.ToString();
        }

        private static void AddOddsByKind(
            List<ExpeditionOddsEntry> odds,
            Dictionary<ExpeditionOutcomeKind, double> target,
            List<ExpeditionOutcomeKind> order)
        {
            for (int i = 0; i < odds.Count; i++)
            {
                ExpeditionOutcomeKind kind = odds[i].Entry.Kind;
                if (!target.ContainsKey(kind))
                {
                    target[kind] = 0;
                    if (!order.Contains(kind))
                    {
                        order.Add(kind);
                    }
                }

                target[kind] += odds[i].FinalPercent;
            }
        }

        private static ExpeditionOutcomeEntry FirstOutcome(ExpeditionActionOption action, ExpeditionOutcomeKind kind)
        {
            if (action == null || action.Outcomes == null)
            {
                return null;
            }

            for (int i = 0; i < action.Outcomes.Count; i++)
            {
                if (action.Outcomes[i].Kind == kind)
                {
                    return action.Outcomes[i];
                }
            }

            return null;
        }

        private static int AssistDeltaFor(ExpeditionOutcomeKind kind, ExpeditionSelection selection)
        {
            switch (kind)
            {
                case ExpeditionOutcomeKind.Loot:
                    return selection.LootBonus;
                case ExpeditionOutcomeKind.Combat:
                    return selection.EnemyBonus
                           + Mathf.RoundToInt(selection.AssistAlertDelta * (float)ExpeditionOdds.AlertPercentPerPoint);
                case ExpeditionOutcomeKind.Damage:
                    return selection.DamageBonus;
                default:
                    return 0;
            }
        }

        private string AssistNames(ExpeditionSelection selection)
        {
            var parts = new List<string>();
            for (int i = 0; i < selection.Assists.Count; i++)
            {
                PlacedItem item = selection.Assists[i];
                ItemDefinition definition;
                if (_combinedCatalog.TryGet(item.DefinitionId, out definition))
                {
                    parts.Add(definition.DisplayName);
                }
            }

            return string.Join("、", parts.ToArray());
        }

        /// <summary>
        /// 拖放到「已选行动」的特殊规则。辅助标志是**实物**：拖进来就从背包搬进格子，
        /// 拖出去（或拖回背包）就搬回去——不再只是记一个 id、物品还留在背包里。
        /// </summary>
        private bool TryAssistDrop(BaseBagSource source, PlacedItem item, BaseBagSource target)
        {
            if (target == null || target.Name != "SELECTED" || item == null || _selectedActionBag == null)
            {
                return false;
            }

            ItemDefinition definition;
            if (_combinedCatalog == null || !_combinedCatalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            // 在同一个袋子里挪格子（调整摆放位置）不算"拖出"，交给普通拖放处理。
            if (source == target)
            {
                return false;
            }

            // 从已选行动里拖到它自己身上 = 取回背包。
            if (source != null && source.Name == "SELECTED")
            {
                if (definition.Assist == null)
                {
                    return false;
                }

                string backMessage;
                ReturnAssist(item, definition, out backMessage);
                SetNarration(backMessage);
                RefreshSelected();
                RefreshStatus();
                return true;
            }

            if (definition.Assist == null)
            {
                // 行动 / 待用道具：一格只放一件，第二件原样退回并说明原因。
                if (PrimarySelectedItem() != null)
                {
                    SetNarration("已选行动只能放一个物品，先把原来的拖出来。");
                    return true;
                }

                return false;
            }

            string message;
            TakeAssist(item, definition, out message);
            SetNarration(message);
            RefreshSelected();
            RefreshStatus();
            return true;
        }

        /// <summary>把辅助标志从背包搬进已选行动格子。失败时 message 说明原因。</summary>
        private bool TakeAssist(PlacedItem item, ItemDefinition definition, out string message)
        {
            if (item == null || definition == null || definition.Assist == null || _selectedActionBag == null)
            {
                message = "这不是探索辅助标志。";
                return false;
            }

            List<PlacedItem> current = SelectedAssistItems();
            if (current.Count >= MaxAssists)
            {
                message = "已选行动最多放 " + MaxAssists + " 个辅助标志。";
                return false;
            }

            for (int i = 0; i < current.Count; i++)
            {
                ItemDefinition other;
                if (_combinedCatalog.TryGet(current[i].DefinitionId, out other)
                    && other.Assist != null
                    && !string.IsNullOrEmpty(definition.Assist.Family)
                    && other.Assist.Family == definition.Assist.Family)
                {
                    message = "同系列辅助只能携带一个。";
                    return false;
                }
            }

            int durability = ItemDurability.Current(item, definition);
            if (durability <= 0)
            {
                message = definition.DisplayName + " 已耗尽。";
                return false;
            }

            PlacedItem placed;
            string error;
            if (!_selectedActionBag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                message = "已选行动格子里放不下了。";
                return false;
            }

            ItemTransfer.PreserveAll(item, placed);
            if (_bag != null)
            {
                _bag.Remove(item.InstanceId);
            }

            message = "已把 " + definition.DisplayName + " 放进已选行动（耐久 "
                      + ItemDurability.Current(placed, definition) + "/" + definition.DurabilityMax + "）。";
            return true;
        }

        /// <summary>把辅助标志从已选行动搬回背包（背包满了退到保险箱）。</summary>
        private bool ReturnAssist(PlacedItem item, ItemDefinition definition, out string message)
        {
            if (item == null || _selectedActionBag == null)
            {
                message = "物品不存在。";
                return false;
            }

            PlacedItem placed;
            string error;
            if (_bag != null && _bag.TryAddOrStack(
                    item.DefinitionId,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition != null ? definition.MaxStack : 1,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveAll(item, placed);
                _selectedActionBag.Remove(item.InstanceId);
                message = "已取回 " + (definition != null ? definition.DisplayName : item.DefinitionId) + "。";
                return true;
            }

            if (definition != null
                && _secure != null
                && SecureContainerRules.IsEligible(definition)
                && _secure.TryAddOrStack(
                    item.DefinitionId,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveAll(item, placed);
                _selectedActionBag.Remove(item.InstanceId);
                message = "背包满了，" + definition.DisplayName + " 放进了保险箱。";
                return true;
            }

            message = "背包和保险箱都没有空位，"
                      + (definition != null ? definition.DisplayName : "辅助标志") + " 先留在已选行动里。";
            return false;
        }

        /// <summary>离场 / 通关前把已选行动里的辅助标志全部收回背包，免得跟着临时格子一起消失。</summary>
        private void ReturnSelectedAssistsToBag()
        {
            if (_selectedActionBag == null)
            {
                return;
            }

            var snapshot = new List<PlacedItem>(_selectedActionBag.Items);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ItemDefinition definition;
                if (!_combinedCatalog.TryGet(snapshot[i].DefinitionId, out definition) || definition.Assist == null)
                {
                    continue;
                }

                string ignored;
                ReturnAssist(snapshot[i], definition, out ignored);
            }
        }

        /// <summary>已选行动格子里的"主物品"：行动或待用道具。辅助标志不算。</summary>
        private PlacedItem PrimarySelectedItem()
        {
            if (_selectedActionBag == null || _combinedCatalog == null)
            {
                return null;
            }

            PlacedItem fallback = null;
            IReadOnlyList<PlacedItem> items = _selectedActionBag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (_combinedCatalog.TryGet(items[i].DefinitionId, out definition) && definition.Assist != null)
                {
                    continue;
                }

                if (_scenario != null && _scenario.Find(items[i].DefinitionId) != null)
                {
                    return items[i];
                }

                if (fallback == null)
                {
                    fallback = items[i];
                }
            }

            return fallback;
        }

        /// <summary><see cref="ExpeditionSelectionRules"/> 只认"一个物品"，所以单独包成数组。</summary>
        private IReadOnlyList<PlacedItem> SelectedPrimaryItems()
        {
            PlacedItem primary = PrimarySelectedItem();
            return primary == null ? null : new[] { primary };
        }

        /// <summary>把已选行动里除辅助标志以外的东西清掉（执行完一次行动后调用）。</summary>
        private void ClearPrimaryFromSelected()
        {
            if (_selectedActionBag == null || _combinedCatalog == null)
            {
                return;
            }

            var snapshot = new List<PlacedItem>(_selectedActionBag.Items);
            for (int i = 0; i < snapshot.Count; i++)
            {
                ItemDefinition definition;
                if (_combinedCatalog.TryGet(snapshot[i].DefinitionId, out definition) && definition.Assist != null)
                {
                    continue;
                }

                _selectedActionBag.Remove(snapshot[i].InstanceId);
            }
        }

        /// <summary>已选行动里的辅助标志（实物，最多 <see cref="MaxAssists"/> 个）。</summary>
        private List<PlacedItem> SelectedAssistItems()
        {
            var result = new List<PlacedItem>();
            if (_selectedActionBag == null || _combinedCatalog == null)
            {
                return result;
            }

            IReadOnlyList<PlacedItem> items = _selectedActionBag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!_combinedCatalog.TryGet(items[i].DefinitionId, out definition) || definition.Assist == null)
                {
                    continue;
                }

                if (ItemDurability.Current(items[i], definition) <= 0)
                {
                    continue;
                }

                result.Add(items[i]);
            }

            return result;
        }

        private void RefreshAssistHint()
        {
            if (_assistHint == null)
            {
                return;
            }

            int count = SelectedAssistItems().Count;
            _assistHint.text = "辅助 " + count + "/" + MaxAssists + " · 拖入标志";
            _assistHint.color = count >= MaxAssists ? WarningColor : MutedColor;
        }

        private bool IsExpeditionUsableItem(PlacedItem item)
        {
            if (item == null || _combinedCatalog == null)
            {
                return false;
            }

            ItemDefinition definition;
            if (!_combinedCatalog.TryGet(item.DefinitionId, out definition) || definition == null)
            {
                return false;
            }

            if (definition.Assist != null)
            {
                return true;
            }

            if (_scenario != null && _scenario.Find(item.DefinitionId) != null)
            {
                return true;
            }

            return ItemUse.CanUseOutsideCombat(definition);
        }

        private static string OutcomeSummary(ExpeditionActionOption action)
        {
            var kinds = new List<string>();
            for (int i = 0; i < action.Outcomes.Count; i++)
            {
                string name = ExpeditionText.OutcomeName(action.Outcomes[i].Kind);
                if (!kinds.Contains(name))
                {
                    kinds.Add(name);
                }
            }

            return string.Join(" / ", kinds.ToArray());
        }

        /// <summary>
        /// C 舞台。上半是地点与叙事，下半是"已选行动"——一块 4×3 的拖放袋 + 名称/风险/辅助。
        /// 这个分区在基地是"立绘 + 台词"，同一个位置换内容，不改位置。
        /// </summary>
        private void BuildStage(RectTransform region)
        {
            Rect stage = ShellRegions.Stage;
            RectTransform panel = CreateCenteredPanel(region, "StagePanel", Vector2.zero, ShellRegions.SizeOf(stage), WindowColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            panel.GetComponent<Image>().raycastTarget = false;
            panel.gameObject.AddComponent<RectMask2D>();

            _titleText = CreateText(
                panel, "Title", string.Empty, FontTitle, TextColor,
                new Vector2(10f, -8f), new Vector2(250f, 14f), TextAnchor.UpperLeft);

            _stageSubtitle = CreateText(
                panel, "Subtitle", string.Empty, FontCaption, MutedColor,
                new Vector2(10f, -24f), new Vector2(250f, 10f), TextAnchor.UpperLeft);

            _narrationText = CreateText(
                panel, "Body", string.Empty, FontBody, MutedColor,
                new Vector2(10f, -38f), new Vector2(250f, 62f), TextAnchor.UpperLeft);
            _narrationText.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform divider = CreateAnchoredPanel(
                panel, "Divider", new Vector2(10f, -104f), new Vector2(250f, 1f), new Color32(50, 58, 68, 255));
            divider.GetComponent<Image>().raycastTarget = false;

            CreateText(
                panel, "SelectedTitle", "已选行动", FontCaption, MutedColor,
                new Vector2(10f, -108f), new Vector2(120f, 10f), TextAnchor.UpperLeft);

            Vector2 chipSize = new Vector2(4f * BagView.CellSize, 3f * BagView.CellSize);
            _selectedActionBag = new BagInventory(4, 3);
            _selectedActionBagRect = CreateBagRect(
                panel,
                ShellRegions.LocalIn(stage, 10f, 120f, chipSize.x, chipSize.y),
                chipSize);
            _selectedActionView = _selectedActionBagRect.gameObject.AddComponent<BagView>();
            _selectedActionView.Initialize(_selectedActionBag, _selectedActionBagRect, _actionPalette, _combinedCatalog);
            _selectedActionView.ItemEnabledPredicate = IsExpeditionUsableItem;

            _selectedLabel = CreateText(
                panel, "SelectedLabel", "拖入一个行动", FontBody, MutedColor,
                new Vector2(80f, -120f), new Vector2(180f, 12f), TextAnchor.UpperLeft);

            _selectedRisk = CreateText(
                panel, "SelectedRisk", string.Empty, FontCaption, MutedColor,
                new Vector2(80f, -134f), new Vector2(180f, 10f), TextAnchor.UpperLeft);

            _assistHint = CreateText(
                panel, "AssistHint", "辅助 0/3", FontCaption, MutedColor,
                new Vector2(80f, -146f), new Vector2(180f, 10f), TextAnchor.UpperLeft);
        }

        /// <summary>B 主行动条：整个区域就是一个按钮。位置、尺寸、字号与基地的「出征」逐像素相同。</summary>
        private void BuildPrimary(RectTransform region)
        {
            _confirmRect = CreateCenteredPanel(
                region, "Execute", Vector2.zero, ShellRegions.SizeOf(ShellRegions.PrimaryAction), AccentColor);
            AddBorder(_confirmRect, new Color32(96, 142, 186, 255));
            _confirmText = CreateCenteredText(
                _confirmRect, "Label", "执行", FontTitle, MutedColor, Vector2.zero, new Vector2(166f, 22f));
        }

        /// <summary>
        /// D 侧栏：页签行（y 与基地完全相同）+ 行动袋 6×4。行动/背包两个页签直接复用原来那套拖放源切换。
        /// </summary>
        private void BuildSide(RectTransform region)
        {
            Rect side = ShellRegions.Side;
            Rect content = ShellRegions.SideContent;

            // 与基地 RightPanel 同一块底板：同色、同描边、同裁剪。
            RectTransform panel = CreateCenteredPanel(
                region, "SidePanel", Vector2.zero, ShellRegions.SizeOf(side), PanelColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            panel.gameObject.AddComponent<RectMask2D>();
            _sidePanelRect = panel;

            _tabActionRect = CreateAnchoredPanel(panel, "TabAction", new Vector2(4f, -2f), new Vector2(82f, 18f), SelectedColor);
            CreateCenteredText(_tabActionRect, "Label", "行动", FontCaption, TextColor, Vector2.zero, new Vector2(80f, 16f));

            _tabBagRect = CreateAnchoredPanel(panel, "TabBag", new Vector2(88f, -2f), new Vector2(82f, 18f), new Color32(38, 44, 52, 255));
            CreateCenteredText(_tabBagRect, "Label", "背包", FontCaption, MutedColor, Vector2.zero, new Vector2(80f, 16f));

            RectTransform body = CreateAnchoredRect(
                panel, "Content", new Vector2(0f, -(content.y - side.y)), ShellRegions.SizeOf(content));

            CreateText(
                body, "Hint", "拖动 / 双击 / Shift+左键", FontCaption, MutedColor,
                new Vector2(4f, -4f), new Vector2(162f, 10f), TextAnchor.UpperLeft);

            Vector2 actionSize = new Vector2(6f * BagView.CellSize, 4f * BagView.CellSize);
            _actionBag = new BagInventory(6, 4);
            _actionBagRect = CreateBagRect(body, ShellRegions.LocalIn(content, 37f, 32f, actionSize.x, actionSize.y), actionSize);
            _actionView = _actionBagRect.gameObject.AddComponent<BagView>();
            _actionView.Initialize(_actionBag, _actionBagRect, _actionPalette, _combinedCatalog);

            Vector2 bagSize = new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize);
            _playerActionBagRect = CreateBagRect(body, ShellRegions.LocalIn(content, 37f, 24f, bagSize.x, bagSize.y), bagSize);
            _playerActionView = _playerActionBagRect.gameObject.AddComponent<BagView>();
            _playerActionView.Initialize(_bag, _playerActionBagRect, _actionPalette, _combinedCatalog);
            _playerActionView.ItemEnabledPredicate = IsExpeditionUsableItem;
            _playerActionView.gameObject.SetActive(false);

            _sideFooter = CreateText(
                body, "Footer", string.Empty, FontCaption, MutedColor,
                new Vector2(4f, -106f), new Vector2(162f, 10f), TextAnchor.UpperLeft);

            RefreshTabs();
        }

        /// <summary>F2 中段大板：结果预估。基地这里是仓库，尺寸完全相同（184×124）。</summary>
        private void BuildDock(RectTransform region)
        {
            RectTransform panel = CreateCenteredPanel(
                region, "Estimate", Vector2.zero, ShellRegions.SizeOf(ShellRegions.BenchMid), PanelColor);
            panel.gameObject.AddComponent<RectMask2D>();

            CreateText(
                panel, "Title", "结果预估", FontPanelTitle, TextColor,
                new Vector2(8f, -6f), new Vector2(168f, 12f), TextAnchor.UpperLeft);

            _detailText = CreateText(
                panel, "Body", string.Empty, FontSecondary, MutedColor,
                new Vector2(8f, -24f), new Vector2(168f, 94f), TextAnchor.UpperLeft);
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;

            // 丢弃区：保险箱正上方那块空位（F 带里唯一没被占的角落）。
            // 挂在 Dock 容器下，压栈进战斗时随整屏一起隐藏、离场时随容器销毁。
            Rect bin = ShellRegions.DiscardBin;
            Rect dock = ShellRegions.BenchMid;
            _discardRect = CreateCenteredPanel(
                region, "DiscardBin",
                ShellRegions.LocalIn(dock, bin.x - dock.x, bin.y - dock.y, bin.width, bin.height),
                ShellRegions.SizeOf(bin), DiscardColor);
            AddBorder(_discardRect, DiscardHighlightColor);
            CreateCenteredText(_discardRect, "Label", "丢弃", FontCaption, TextColor, Vector2.zero, new Vector2(58f, 18f));
        }

        private void BuildContainerWindows()
        {
            _containerWindows = gameObject.AddComponent<FloatingWindowManager>();
            _containerWindows.Initialize(_shell.OverlayLayer, _catalog, _palette);
            _containerWindows.AttachDrag(_actionDrag);
            _containerWindows.AttachDrag(_bagDrag);
            _actionDrag.TryActivateItem = (source, instanceId) =>
                _containerWindows != null && _containerWindows.TryOpen(source, instanceId);
            _bagDrag.TryActivateItem = (source, instanceId) =>
                _containerWindows != null && _containerWindows.TryOpen(source, instanceId);
            _actionDrag.PointerBlocked = screen =>
                _containerWindows != null && _containerWindows.IsPointerOverChrome(screen);
            _bagDrag.PointerBlocked = screen =>
                _containerWindows != null && _containerWindows.IsPointerOverChrome(screen);
        }

        private void BuildActionDrag()
        {
            _actionDrag = gameObject.AddComponent<BaseDragInteraction>();
            _actionDrag.Initialize(
                ActionSources(),
                _combinedCatalog,
                null,
                GameSession.Campaign,
                _actionPalette,
                _shell.DragLayer,
                null,
                _discardRect);
            _actionDrag.Changed += OnActionBagChanged;
            _actionDrag.ItemClicked += OnActionItemClicked;
            _actionDrag.TrySpecialDrop = TryAssistDrop;
            _actionDrag.Message += SetNarration;
            _actionDrag.QuickMoveTarget = ActionQuickMoveTarget;
        }

        private void BuildBagOverlay(Transform canvas)
        {
            _bagOverlay = new GameObject("BagOverlay", typeof(RectTransform), typeof(Image));
            _bagOverlay.transform.SetParent(canvas, false);
            var root = (RectTransform)_bagOverlay.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            _bagOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
            _bagOverlay.GetComponent<Image>().raycastTarget = true;

            RectTransform box = CreateCenteredPanel(_bagOverlay.transform, "Box", Vector2.zero, new Vector2(550f, 300f), PanelColor);
            CreateCenteredText(box, "Title", "背包", 14, TextColor, new Vector2(0f, 126f), new Vector2(510f, 20f));
            _bagHint = CreateCenteredText(box, "Hint", "拖动物品整理；右键旋转；拖到丢弃区移除。", 8, MutedColor, new Vector2(0f, 105f), new Vector2(510f, 12f));

            RectTransform playerPanel = CreateCenteredPanel(box, "Player", new Vector2(-120f, -12f), new Vector2(220f, 200f), new Color32(20, 24, 30, 255));
            CreateText(playerPanel, "Title", "携带", 9, TextColor, new Vector2(8f, -6f), new Vector2(200f, 12f), TextAnchor.UpperLeft);
            _bagPlayerRect = CreateBagRect(playerPanel, new Vector2(0f, -14f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _bagView = _bagPlayerRect.gameObject.AddComponent<BagView>();
            _bagView.Initialize(_bag, _bagPlayerRect, _palette);

            RectTransform securePanel = CreateCenteredPanel(box, "Secure", new Vector2(100f, 20f), new Vector2(150f, 154f), new Color32(20, 24, 30, 255));
            _bagSecureTitle = CreateText(securePanel, "Title", "保险箱", 9, TextColor, new Vector2(8f, -6f), new Vector2(134f, 12f), TextAnchor.UpperLeft);
            _bagSecureRect = CreateBagRect(securePanel, new Vector2(0f, -16f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _secureView = _bagSecureRect.gameObject.AddComponent<BagView>();
            _secureView.Initialize(_secure, _bagSecureRect, _palette);

            _bagDiscardRect = CreateCenteredPanel(box, "Discard", new Vector2(100f, -116f), new Vector2(120f, 24f), DiscardColor);
            CreateCenteredText(_bagDiscardRect, "Label", "丢弃区", 8, TextColor, Vector2.zero, new Vector2(116f, 22f));

            _bagCloseRect = CreateCenteredPanel(box, "Close", new Vector2(215f, -126f), new Vector2(90f, 24f), new Color32(44, 48, 56, 255));
            CreateCenteredText(_bagCloseRect, "Label", "关闭", 9, TextColor, Vector2.zero, new Vector2(86f, 22f));

            _sellBinRect = CreateCenteredPanel(canvas, "NoSell", new Vector2(10000f, 10000f), Vector2.zero, PanelColor);
            _sellBinRect.gameObject.SetActive(false);

            var sources = new List<BaseBagSource>
            {
                new BaseBagSource { Name = "PACK", Bag = _bag, Rect = _bagPlayerRect, View = _bagView },
                new BaseBagSource { Name = "SECURE", Bag = _secure, Rect = _bagSecureRect, View = _secureView, IsSecure = true }
            };

            _bagDrag = _bagOverlay.AddComponent<BaseDragInteraction>();
            _bagDrag.Initialize(sources, _catalog, GameSession.Merchant, GameSession.Campaign, _palette, _shell.DragLayer, _sellBinRect, _bagDiscardRect);
            _bagDrag.ItemUsed += OnUseItem;
            _bagDrag.Changed += OnBagChanged;
            _bagDrag.Message += SetNarration;
            _bagOverlay.SetActive(false);
        }

        private void BuildLootPanel()
        {
            Rect dock = ShellRegions.LootDock;

            // 挂在 ScreenLayer 上：LootDock 这块位置不属于任何一个 Screen 洞（它在 C 舞台左边），
            // 所以和战斗的播报行一样自己建、自己在 TearDown 里收。
            _lootPanel = new GameObject("LootPanel", typeof(RectTransform), typeof(Image));
            _lootPanel.transform.SetParent(_shell.ScreenLayer, false);

            _lootPanelRect = (RectTransform)_lootPanel.transform;
            _lootPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _lootPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _lootPanelRect.pivot = new Vector2(0.5f, 0.5f);
            _lootPanelRect.anchoredPosition = ShellRegions.CenterOf(dock);
            _lootPanelRect.sizeDelta = ShellRegions.SizeOf(dock);
            _lootPanel.GetComponent<Image>().color = new Color32(26, 31, 39, 255);
            AddBorder(_lootPanelRect, new Color32(67, 59, 47, 255));

            _lootTitle = CreateText(_lootPanelRect, "Title", "搜刮", FontPanelTitle, TextColor,
                new Vector2(8f, -6f), new Vector2(160f, 12f), TextAnchor.UpperLeft);

            _lootHint = CreateText(_lootPanelRect, "Hint", string.Empty, FontCaption, MutedColor,
                new Vector2(8f, -20f), new Vector2(160f, 12f), TextAnchor.UpperLeft);

            // 容器格子（4×3，与背包里的零件箱一致）在 OpenLoot 里按这一场的内容重建。
            _lootDiscardRect = CreateCenteredPanel(
                _lootPanelRect, "Discard",
                ShellRegions.LocalIn(dock, 8f, 128f, 160f, 14f),
                new Vector2(160f, 14f), DiscardColor);
            CreateCenteredText(_lootDiscardRect, "Label", "丢弃区", FontCaption, TextColor, Vector2.zero, new Vector2(156f, 12f));

            _lootAllRect = CreateCenteredPanel(
                _lootPanelRect, "LootAll",
                ShellRegions.LocalIn(dock, 8f, 102f, 84f, 20f),
                new Vector2(84f, 20f), AccentColor);
            AddBorder(_lootAllRect, new Color32(96, 142, 186, 255));
            _lootAllText = CreateCenteredText(
                _lootAllRect, "Label", "全部拿走", FontBody, TextColor, Vector2.zero, new Vector2(80f, 18f));

            _lootCloseRect = CreateCenteredPanel(
                _lootPanelRect, "Close",
                ShellRegions.LocalIn(dock, 96f, 102f, 72f, 20f),
                new Vector2(72f, 20f), new Color32(44, 48, 56, 255));
            CreateCenteredText(_lootCloseRect, "Label", "关闭", FontBody, TextColor, Vector2.zero, new Vector2(68f, 18f));

            _lootPanel.SetActive(false);
        }

        private void OpenBag()
        {
            if (_bagOpen || _lootOpen || _ended)
            {
                return;
            }

            _bagOpen = true;
            SettingsOverlay.PushModal();
            _bagOverlay.SetActive(true);
            if (_actionDrag != null)
            {
                _actionDrag.enabled = false;
            }

            _bagDrag.enabled = true;
            RefreshAll();
            SetNarration("整理背包。拖动物品调整位置，右键旋转，拖到丢弃区移除。");
            _bagDrag.SetSources(new List<BaseBagSource>
            {
                new BaseBagSource { Name = "PACK", Bag = _bag, Rect = _bagPlayerRect, View = _bagView },
                new BaseBagSource { Name = "SECURE", Bag = _secure, Rect = _bagSecureRect, View = _secureView, IsSecure = true }
            });
        }

        private void CloseBag()
        {
            if (_bagOpen)
            {
                SettingsOverlay.PopModal();
                SettingsOverlay.ConsumeEscape();
            }

            _bagOpen = false;
            _bagOverlay.SetActive(false);
            _bagDrag.enabled = false;
            if (_actionDrag != null)
            {
                _actionDrag.enabled = true;
            }
            RefreshAll();
            SetNarration("你收起背包，重新评估眼前的路线。");
        }

        /// <summary>
        /// 打开搜刮面板。**不再是全屏遮罩**：
        ///
        /// 遮罩把整屏压黑，而物品提示挂在 TooltipLayer、排在 OverlayLayer 下面，
        /// 于是"在搜刮窗口里悬停什么都看不到"。现在面板坐在左边那块本来空着的位置
        /// （<see cref="ShellRegions.LootDock"/>），F1 背包 / F3 保险箱原样露在外面，
        /// 直接拖过去就行，提示照常弹。
        /// </summary>
        private void OpenLoot(string[] lootIds)
        {
            if (_lootOpen)
            {
                return;
            }

            _lootOpen = true;
            SettingsOverlay.PushModal();
            _lootPanel.SetActive(true);
            if (_actionDrag != null)
            {
                _actionDrag.enabled = false;
            }

            if (_lootBoxRect != null)
            {
                Destroy(_lootBoxRect.gameObject);
                _lootBoxRect = null;
            }

            _lootBag = new BagInventory(4, 3);
            var lootRandom = new System.Random((_scenario != null ? _scenario.Seed : 17) + lootIds.Length * 101);
            for (int i = 0; i < lootIds.Length; i++)
            {
                ItemDefinition definition = _catalog.Get(lootIds[i]);
                if (definition == null)
                {
                    continue;
                }

                int count = LootStackRules.Roll(definition, lootRandom.Next());
                PlacedItem placed;
                string error;
                if (!_lootBag.TryAddOrStack(lootIds[i], definition.Shape, true, count, definition.MaxStack, out placed, out error))
                {
                    _lootBag.TryAdd(lootIds[i], definition.Shape, out placed);
                }
            }

            _lootBoxRect = CreateBagRect(
                _lootPanelRect,
                ShellRegions.LocalIn(ShellRegions.LootDock, 8f, 34f, 4f * BagView.CellSize, 3f * BagView.CellSize),
                new Vector2(4f * BagView.CellSize, 3f * BagView.CellSize));
            _lootBoxView = _lootBoxRect.gameObject.AddComponent<BagView>();
            _lootBoxView.Initialize(_lootBag, _lootBoxRect, _palette);
            _lootBoxView.Refresh();

            if (_lootDrag != null)
            {
                _lootDrag.enabled = false;
                Destroy(_lootDrag);
                _lootDrag = null;
            }

            // 拖拽源直接指向外壳那两个常驻背包——拖进去的就是玩家看见的那一格。
            var sources = new List<BaseBagSource>
            {
                new BaseBagSource
                {
                    Name = "PACK", Bag = _bag, Rect = _shell.PackBagRect, View = _shell.PackView, AcceptsActionItems = false
                },
                new BaseBagSource
                {
                    Name = "SECURE", Bag = _secure, Rect = _shell.SecureBagRect, View = _shell.SecureView, IsSecure = true
                },
                new BaseBagSource { Name = "LOOT", Bag = _lootBag, Rect = _lootBoxRect, View = _lootBoxView, IsLoot = true }
            };

            _lootDrag = _lootPanel.AddComponent<BaseDragInteraction>();
            _lootDrag.Initialize(sources, _catalog, GameSession.Merchant, GameSession.Campaign, _palette, _shell.DragLayer, _sellBinRect, _lootDiscardRect);
            _lootDrag.QuickMoveTarget = ExpeditionLootQuickTarget;
            _lootDrag.TryActivateItem = (source, instanceId) =>
                _containerWindows != null && _containerWindows.TryOpen(source, instanceId);
            _lootDrag.PointerBlocked = screen =>
                _containerWindows != null && _containerWindows.IsPointerOverChrome(screen);
            if (_containerWindows != null)
            {
                _containerWindows.AttachDrag(_lootDrag);
            }
            _lootDrag.Changed += OnLootChanged;
            _lootDrag.ItemUsed += OnUseItem;
            _lootDrag.Message += SetNarration;

            _lastPlayerItemCount = _bag.Count + _secure.Count;
            RefreshLootPanel();
        }

        /// <summary>面板上的字跟着内容走：容器剩几件、背包还剩几格、按钮上的数量。</summary>
        private void RefreshLootPanel()
        {
            if (!_lootOpen || _lootBag == null)
            {
                return;
            }

            int count = _lootBag.Count;
            _lootTitle.text = "搜刮";
            _lootHint.text = count > 0
                ? "容器 " + count + " 件 · 背包空 " + _bag.FreeCellCount + " 格"
                : "容器空了 · 背包空 " + _bag.FreeCellCount + " 格";
            _lootAllText.text = count > 0 ? "全部拿走（" + count + "）" : "全部拿走";
        }

        /// <summary>一把抓走容器里所有还能装下的东西（先背包，装不下再试保险箱）。</summary>
        private void TakeAllLoot()
        {
            if (_lootBag == null)
            {
                return;
            }

            int taken = 0;
            int value = 0;
            var snapshot = new List<PlacedItem>(_lootBag.Items);
            for (int i = 0; i < snapshot.Count; i++)
            {
                PlacedItem current;
                if (!_lootBag.TryGet(snapshot[i].InstanceId, out current))
                {
                    continue;
                }

                ItemDefinition definition;
                if (!_catalog.TryGet(current.DefinitionId, out definition))
                {
                    continue;
                }

                if (!TryStoreLoot(current, definition))
                {
                    continue;
                }

                _lootBag.Remove(current.InstanceId);
                GameSession.AddLootTaken();
                taken++;
                value += definition.Value;
            }

            _shell.RefreshContainers();
            if (_lootBoxView != null)
            {
                _lootBoxView.Refresh();
            }

            if (taken == 0)
            {
                SetNarration("背包和保险箱都装不下了。");
                RefreshLootPanel();
                RefreshStatus();
                return;
            }

            string message = "拿走了 " + taken + " 件物资（估 ¥" + value + "）。";
            AppendLog(message);
            SetNarration(message);
            RefreshLootPanel();
            RefreshStatus();

            // 搬空了就自己收摊：留一块空面板在那儿等玩家再点一次「关闭」没有意义。
            if (_lootBag != null && _lootBag.Count == 0)
            {
                CloseLoot(true);
            }
        }

        private bool TryStoreLoot(PlacedItem item, ItemDefinition definition)
        {
            PlacedItem placed;
            string error;
            if (_bag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveAll(item, placed);
                return true;
            }

            if (SecureContainerRules.IsEligible(definition)
                && _secure.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveAll(item, placed);
                return true;
            }

            return false;
        }

        /// <summary>关闭搜刮面板。<paramref name="auto"/> = 搬空了自动收摊，不再补一遍"已清空"的日志。</summary>
        private void CloseLoot(bool auto = false)
        {
            if (_lootOpen)
            {
                SettingsOverlay.PopModal();
                SettingsOverlay.ConsumeEscape();
            }

            _lootOpen = false;
            _lootPanel.SetActive(false);
            if (_lootBoxRect != null)
            {
                Destroy(_lootBoxRect.gameObject);
                _lootBoxRect = null;
                _lootBoxView = null;
            }

            if (_lootDrag != null)
            {
                _lootDrag.enabled = false;
                if (_containerWindows != null)
                {
                    _containerWindows.DetachDrag(_lootDrag);
                }
            }

            if (_actionDrag != null)
            {
                _actionDrag.enabled = true;
            }

            if (!auto)
            {
                AppendLog(_lootBag != null && _lootBag.Count > 0
                    ? "你离开了容器，剩下的东西留在原地。"
                    : "容器已经被清空。");
            }

            _lootBag = null;
            RefreshAll();
            SetNarration(auto
                ? "搜刮完了，容器空了。下一步该往哪里走？"
                : "物资已经处理完。下一步该往哪里走？");
        }

        private void OnLootChanged()
        {
            int playerCount = _bag.Count + _secure.Count;
            int gained = playerCount - _lastPlayerItemCount;
            for (int i = 0; i < gained; i++)
            {
                GameSession.AddLootTaken();
            }

            _lastPlayerItemCount = playerCount;

            // 拖拽用的就是外壳的 F1 / F3，所以拖动完必须让那两个视图重画一遍。
            _shell.RefreshContainers();
            if (_lootBoxView != null)
            {
                _lootBoxView.Refresh();
            }

            RefreshLootPanel();
            RefreshStatus();
        }

        private void OnBagChanged()
        {
            RefreshStatus();
            if (_bagSecureTitle != null)
            {
                _bagSecureTitle.text = "保险箱 " + _secure.Count + "/4";
            }

            _shell.RefreshContainers();
        }

        private void OnUseItem(int instanceId)
        {
            PlacedItem item;
            if (!_bag.TryGet(instanceId, out item))
            {
                PlacedItem secureItem;
                if (_secure.TryGet(instanceId, out secureItem))
                {
                    SetNarration("只能在战斗外使用背包中的物品。");
                }

                return;
            }

            ItemDefinition definition = _catalog.Get(item.DefinitionId);
            int newHp;
            string message;
            if (!ItemUse.TryUseOutsideCombat(
                    definition,
                    _bag,
                    instanceId,
                    GameSession.ExpeditionHp,
                    GameSession.ExpeditionMaxHp,
                    out newHp,
                    out message))
            {
                SetNarration(message);
                return;
            }

            GameSession.ExpeditionHp = newHp;
            GameSession.LastMessage = message;
            AppendLog(message);
            SetNarration(message);
            RefreshAll();
            _shell.RefreshContainers();
        }

        private PlacedItem FindItemAt(RectTransform rect, BagInventory bag, Vector2 screen)
        {
            Cell cell;
            if (rect == null || bag == null || !TryBagCell(rect, bag, screen, out cell))
            {
                return null;
            }

            int instanceId = bag.Grid.GetOccupant(cell.X, cell.Y);
            if (instanceId == 0)
            {
                return null;
            }

            PlacedItem item;
            return bag.TryGet(instanceId, out item) ? item : null;
        }

        private static bool TryBagCell(RectTransform rect, BagInventory bag, Vector2 screen, out Cell cell)
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

        private BaseBagSource ExpeditionLootQuickTarget(BaseBagSource source, PlacedItem item)
        {
            if (_lootDrag == null || source == null || source.Name != "LOOT")
            {
                return null;
            }

            return _lootDrag.FindSource("PACK");
        }

        private void BuildItemTooltip(Transform canvas)
        {
            _itemTooltipPanel = new GameObject("ItemTooltip", typeof(RectTransform), typeof(Image));
            _itemTooltipPanel.transform.SetParent(canvas, false);
            _itemTooltipPanel.transform.SetAsLastSibling();

            _itemTooltipRect = (RectTransform)_itemTooltipPanel.transform;
            _itemTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            _itemTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _itemTooltipRect.pivot = new Vector2(0f, 1f);
            _itemTooltipRect.sizeDelta = new Vector2(240f, 180f);
            _itemTooltipPanel.GetComponent<Image>().color = new Color32(12, 14, 18, 245);
            _itemTooltipPanel.GetComponent<Image>().raycastTarget = false;

            _itemTooltipText = CreateText(_itemTooltipPanel.transform, "Text", string.Empty, 9, TextColor, new Vector2(8f, -6f), new Vector2(224f, 168f), TextAnchor.UpperLeft);
            _itemTooltipPanel.SetActive(false);
        }

        private void UpdateItemTooltip(Vector2 screen)
        {
            EnsureMainHoverSources();
            IReadOnlyList<BaseBagSource> sources = CurrentItemSources();
            if (_itemTooltipPanel == null || _ended
                || (_actionDrag != null && _actionDrag.IsDragging)
                || (Mouse.current != null && Mouse.current.leftButton.isPressed))
            {
                if (_itemTooltipPanel != null)
                {
                    _itemTooltipPanel.SetActive(false);
                }

                return;
            }

            BaseBagSource hoveredSource;
            PlacedItem item = FindHoveredItem(sources, screen, out hoveredSource);
            if (item == null)
            {
                _itemTooltipPanel.SetActive(false);
                return;
            }

            ItemDefinition definition;
            if (!_combinedCatalog.TryGet(item.DefinitionId, out definition))
            {
                _itemTooltipPanel.SetActive(false);
                return;
            }

            ShowItemTooltip(item, definition, screen);
        }

        private void ShowItemTooltip(PlacedItem item, ItemDefinition definition, Vector2 screen)
        {
            _itemTooltipText.text = ItemText.Describe(definition, item, _bag.CountOf(definition.AmmoItemId));
            const float tooltipWidth = 240f;
            _itemTooltipText.rectTransform.sizeDelta = new Vector2(tooltipWidth - 16f, 200f);
            float tooltipHeight = Mathf.Clamp(_itemTooltipText.preferredHeight + 14f, 120f, 300f);
            _itemTooltipRect.sizeDelta = new Vector2(tooltipWidth, tooltipHeight);
            _itemTooltipText.rectTransform.sizeDelta = new Vector2(tooltipWidth - 16f, tooltipHeight - 12f);

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local);
            float x = local.x + 14f;
            if (x + tooltipWidth > 312f)
            {
                x = local.x - tooltipWidth - 14f;
            }

            x = Mathf.Clamp(x, -312f, 312f - tooltipWidth);
            float y = Mathf.Clamp(local.y - 14f, -170f + tooltipHeight, 168f);
            _itemTooltipRect.anchoredPosition = new Vector2(x, y);
            _itemTooltipPanel.transform.SetAsLastSibling();
            _itemTooltipPanel.SetActive(true);
        }

        private IReadOnlyList<BaseBagSource> CurrentItemSources()
        {
            if (_lootOpen && _lootDrag != null)
            {
                return _lootDrag.Sources;
            }

            if (_bagOpen && _bagDrag != null)
            {
                return _bagDrag.Sources;
            }

            return _mainHoverSources;
        }

        /// <summary>
        /// 主界面悬停源必须跟着「当前装的是哪个背包」走。
        ///
        /// 行动袋（<c>_actionBag</c>）和已选行动（<c>_selectedActionBag</c>）每执行一次行动就会被
        /// <see cref="RebuildActionBags"/> 换成新实例——缓存下来的旧源指向一个已经没人看的空背包，
        /// 于是"悬停行动物品什么都不弹"。这里在每次查询前比一次引用，换了就重建。
        /// </summary>
        private void EnsureMainHoverSources()
        {
            if (_hoverActionBag == _actionBag
                && _hoverSelectedBag == _selectedActionBag
                && _hoverPack == _bag
                && _hoverSecure == _secure
                && _hoverShowPlayerBag == _showPlayerBag)
            {
                return;
            }

            RebuildMainHoverSources();
        }

        /// <summary>重建主界面悬停源。切页签（行动 / 背包）、换行动袋时要重来一次。</summary>
        private void RebuildMainHoverSources()
        {
            _mainHoverSources.Clear();
            _hoverActionBag = _actionBag;
            _hoverSelectedBag = _selectedActionBag;
            _hoverPack = _bag;
            _hoverSecure = _secure;
            _hoverShowPlayerBag = _showPlayerBag;

            if (_actionDrag != null && _actionDrag.Sources != null)
            {
                _mainHoverSources.AddRange(_actionDrag.Sources);
            }

            if (_shell == null)
            {
                return;
            }

            _mainHoverSources.Add(new BaseBagSource
            {
                Name = "SHELL_PACK",
                Bag = _bag,
                Rect = _shell.PackBagRect,
                View = _shell.PackView,
                AcceptsActionItems = false
            });

            _mainHoverSources.Add(new BaseBagSource
            {
                Name = "SHELL_SECURE",
                Bag = _secure,
                Rect = _shell.SecureBagRect,
                View = _shell.SecureView,
                IsSecure = true
            });
        }

        private PlacedItem FindHoveredItem(
            IReadOnlyList<BaseBagSource> sources,
            Vector2 screen,
            out BaseBagSource hoveredSource)
        {
            hoveredSource = null;
            if (sources == null)
            {
                return null;
            }

            for (int i = sources.Count - 1; i >= 0; i--)
            {
                BaseBagSource source = sources[i];
                if (source == null || source.Bag == null)
                {
                    continue;
                }

                PlacedItem item = FindItemAt(source.Rect, source.Bag, screen);
                if (item != null)
                {
                    hoveredSource = source;
                    return item;
                }
            }

            return null;
        }

        private string DescribeItem(ItemDefinition definition, PlacedItem item = null)
        {
            if (definition == null)
            {
                return "未知物品";
            }

            var parts = new List<string>();
            parts.Add(ItemText.CategoryName(definition.Category));
            parts.Add("价值 " + definition.Value);

            if (definition.Damage > 0)
            {
                parts.Add("伤害 " + definition.Damage + " " + ItemText.DamageTypeName(definition.Type));
            }

            string armor = ItemText.ArmorSummary(definition);
            if (!string.IsNullOrEmpty(armor))
            {
                parts.Add("护甲 " + armor);
            }

            if (definition.Heal > 0)
            {
                parts.Add("治疗 " + definition.Heal);
            }

            if (definition.Activation == ItemActivation.Active && definition.EnergyCost > 0)
            {
                parts.Add("消耗 " + definition.EnergyCost + " 能量");
            }

            IReadOnlyList<string> traits = ItemText.TraitLines(definition);
            for (int i = 0; i < traits.Count; i++)
            {
                parts.Add(traits[i]);
            }

            parts.Add("尺寸 " + definition.Shape.Width + "x" + definition.Shape.Height);
            int durability = ItemDurability.Current(item, definition);
            if (durability > 0)
            {
                parts.Add("耐久 " + durability + "/" + definition.DurabilityMax);
            }

            return definition.DisplayName + "　" + string.Join("　", parts);
        }

        private void SetNarration(string text)
        {
            if (_narrationText != null)
            {
                _narrationText.text = text;
            }
        }

        private void AppendLog(string text)
        {
            // 日志归外壳持有：与基地同一块面板、同一份内容，切模式连续。
            _shell.AppendLog(text);
        }

        private static bool Contains(RectTransform rect, Vector2 screen)
        {
            return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
        }

        /// <summary>
        /// 搜刮还开着的时候点到"要做决定的地方"（执行 / 行动菜单 / 已选行动），
        /// 就在旁白里说明为什么点不动。原来这种情况是完全静默的，玩家只会觉得界面卡住了。
        /// </summary>
        private void ShowLootBlocksHint(Vector2 screen)
        {
            if (Contains(_confirmRect, screen)
                || Contains(_sidePanelRect, screen)
                || Contains(_selectedActionBagRect, screen))
            {
                SetNarration("先结束搜刮：点「关闭」或「全部拿走」，再选行动。");
            }
        }

        private static bool IsPointerOver(RectTransform rect)
        {
            if (Mouse.current == null || rect == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), null);
        }

        private static Color RiskColor(ExpeditionRisk risk)
        {
            switch (risk)
            {
                case ExpeditionRisk.Low:
                    return new Color32(120, 190, 155, 255);
                case ExpeditionRisk.Medium:
                    return new Color32(220, 184, 103, 255);
                default:
                    return DangerColor;
            }
        }

        private static Dictionary<string, Color> BuildActionPalette(ItemCatalog catalog)
        {
            var palette = new Dictionary<string, Color>();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                palette[definition.Id] = definition.Category == ItemCategory.Action
                    ? ActionColor(definition.Id)
                    : ColorFor(definition.Category);
            }

            return palette;
        }

        private static ItemCatalog BuildCombinedCatalog(ItemCatalog gameCatalog, ItemCatalog actionCatalog)
        {
            var definitions = new List<ItemDefinition>();
            foreach (ItemDefinition definition in gameCatalog.Definitions)
            {
                definitions.Add(definition);
            }

            foreach (ItemDefinition definition in actionCatalog.Definitions)
            {
                definitions.Add(definition);
            }

            return new ItemCatalog(definitions);
        }

        private static Color ActionColor(string id)
        {
            switch (id)
            {
                case "scan":
                case "decode":
                case "listen":
                    return new Color32(70, 150, 160, 255);
                case "sneak":
                case "slip":
                case "hide":
                    return new Color32(70, 110, 175, 255);
                case "force":
                case "breach":
                case "rush":
                case "suppress":
                case "dive":
                    return new Color32(170, 76, 62, 255);
                case "search":
                case "forage":
                case "hack":
                    return new Color32(170, 140, 55, 255);
                case "detour":
                case "trace":
                case "jam":
                case "climb":
                    return new Color32(112, 92, 150, 255);
                default:
                    return new Color32(78, 118, 88, 255);
            }
        }

    }
}

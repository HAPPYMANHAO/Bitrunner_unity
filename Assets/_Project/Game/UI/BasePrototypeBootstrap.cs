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
    public sealed class BasePrototypeBootstrap : MonoBehaviour
    {

        private enum BaseTab
        {
            Merchant,
            Pod,
            Mine,
            Craft,
            Cpu,
            Insurance,
            Storage,
            Comms
        }

        /// <summary>标签只有 20px 宽，所以一律两个字。</summary>
        private static readonly string[] TabNames = { "商人", "潜网", "矿场", "制作", "芯片", "保险", "仓储", "通讯" };

        private ItemCatalog _catalog;
        private CampaignState _campaign;
        private Merchant _merchant;
        private DivePod _pod;
        private BitMine _mine;
        private CraftingStation _craft;
        private CpuCore _cpu;
        private InsuranceHub _insurance;
        private StorageRack _rack;
        private CommsHub _comms;
        private BagInventory _bag;
        private BagInventory _stash;
        private Dictionary<string, Color> _palette;

        private RectTransform _canvasRect;
        private Text _hudText;
        private Text _statsText;
        private RectTransform _progressFill;
        private Text _logText;
        private readonly List<string> _logEntries = new List<string>();
        private const int MaxLogEntries = 5;

        // 中央艺术台：立绘位 + 台词。NPC 接通后这里换人，属性数字全部挪到背包左侧。
        private RectTransform _artStage;
        private Text _artTitle;
        private Text _artSubtitle;
        private Text _artDialogue;
        private Text _artPortraitInitial;
        private string _activeNpcId;
        private string _npcLineOverride;
        private float _npcLineUntil;
        private NpcBagWindow _npcWindow;
        private bool _npcModalPushed;
        private NpcMarket _npcStocks;
        private QuestLog _quests;
        private RectTransform _deployRect;
        private Image _deployImage;
        private Text _deployText;
        private RectTransform _discardBinRect;

        private BagView _bagView;
        private BagView _stashView;
        private BagView _secureView;
        private RectTransform _bagRect;
        private RectTransform _stashRect;
        private RectTransform _stashViewport;
        private ScrollRect _stashScrollRect;
        private StashScrollView _stashScroll;
        private RectTransform _secureRect;
        private Text _secureTitle;
        private BagInventory _secure;

        private BaseTab _activeTab;
        private GameObject[] _tabContents;
        private RectTransform[] _tabRects;
        private Text[] _tabLabels;
        private Image[] _tabImages;

        private BagView _merchantView;
        private RectTransform _merchantRect;

        private BagView _podView;
        private RectTransform _podRect;
        private Text _podInfo;
        private RectTransform _podSelectRect;
        private RectTransform _podUpgradeRect;
        private Text _podUpgradeText;
        private Image _podUpgradeImage;

        private RectTransform _mineGridRect;
        private Text _mineInfo;
        private Text _mineCollectText;
        private Image _mineCollectImage;
        private RectTransform _mineCollectRect;
        private RectTransform _mineUpgradeRect;
        private Text _mineUpgradeText;
        private Image _mineUpgradeImage;
        private readonly List<Image> _mineCellImages = new List<Image>();
        private readonly List<Text> _mineCellTexts = new List<Text>();
        private Sprite _bitSprite;

        private BagView _craftView;
        private RectTransform _craftRect;
        private Text _craftInfo;
        private RectTransform _craftCraftRect;
        private Text _craftCraftText;
        private Image _craftCraftImage;
        private RectTransform _craftUpgradeRect;
        private Text _craftUpgradeText;
        private Image _craftUpgradeImage;

        private BagView _cpuView;
        private RectTransform _cpuRect;
        private Text _cpuInfo;
        private RectTransform _cpuUpgradeRect;
        private Text _cpuUpgradeText;
        private Image _cpuUpgradeImage;

        private Text _insuranceInfo;
        private RectTransform _insuranceUpgradeRect;
        private Text _insuranceUpgradeText;
        private Image _insuranceUpgradeImage;

        private Text _storageInfo;
        private RectTransform _storageUpgradeRect;
        private Text _storageUpgradeText;
        private Image _storageUpgradeImage;

        // ---------- 通讯中枢 ----------
        private RectTransform _commsRect;
        private BagView _commsView;
        private Text _commsInfo;
        private RectTransform _commsUpgradeRect;
        private Text _commsUpgradeText;
        private Image _commsUpgradeImage;


        private readonly List<BaseBagSource> _sources = new List<BaseBagSource>();        private BaseDragInteraction _drag;
        private FloatingWindowManager _containerWindows;

        private GameObject _tooltipPanel;
        private RectTransform _tooltipRect;
        private Text _tooltipText;

        private GameObject _endOverlay;
        private Text _endTitle;
        private Text _endSubtitle;
        private bool _ended;

        /// <summary>基地里有任何状态变化就标脏，由 <see cref="TickAutoSave"/> 攒一小段再落盘。</summary>
        private const float AutoSaveSeconds = 4f;

        private bool _saveDirty;
        private float _autoSaveTimer;

        private void Start()
        {
            GameSession.EnsureInitialized();
            GameSession.EnsureMerchantStockForDay();
            GameSession.EnsureFacilitiesForDay();

            _catalog = GameSession.Catalog;
            _campaign = GameSession.Campaign;
            _merchant = GameSession.Merchant;
            _pod = GameSession.Pod;
            _mine = GameSession.Mine;
            _craft = GameSession.Craft;
            _cpu = GameSession.Cpu;
            _insurance = GameSession.Insurance;
            _rack = GameSession.Rack;
            _comms = GameSession.Comms;
            _quests = GameSession.Quests;
            _npcStocks = GameSession.NpcStocks;
            _bag = GameSession.PlayerBag;
            _stash = GameSession.Stash;
            _secure = GameSession.Secure;
            _palette = BuildPalette(_catalog);
            _activeTab = BaseTab.Merchant;

            Canvas canvas = CreateCanvas();
            _canvasRect = (RectTransform)canvas.transform;

            BuildHud(canvas.transform);
            BuildLeftStats(canvas.transform);
            BuildBagPanels(canvas.transform);
            BuildRightPanel(canvas.transform);
            BuildDeploy(canvas.transform);
            BuildArtStage(canvas.transform);
            BuildLog(canvas.transform);
            BuildDrag();
            BuildTooltip(canvas.transform);
            BuildContainerWindows();
            BuildNpcWindow(canvas.transform);
            BuildEndOverlay(canvas.transform);

            SetActiveTab(BaseTab.Merchant, true);
            RefreshAll();

            if (!_campaign.IsActive)
            {
                ShowEnd();
            }

            // 进入基地即落盘：这一笔同时覆盖「新开局第一次进基地」和「出征回来」。
            SaveSystem.Save();
            _saveDirty = false;
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveSystem.Save();
            }
        }

        private void TickAutoSave()
        {
            if (!_saveDirty)
            {
                return;
            }

            _autoSaveTimer -= Time.unscaledDeltaTime;
            if (_autoSaveTimer > 0f)
            {
                return;
            }

            _saveDirty = false;
            SaveSystem.Save();
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

            TickAutoSave();

            if (!string.IsNullOrEmpty(_npcLineOverride) && Time.unscaledTime >= _npcLineUntil)
            {
                _npcLineOverride = null;
                RefreshArtStage();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (_ended)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    GameSession.StartNewGame();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }

                return;
            }

            UpdateTooltip(screen);

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (HandleTabClick(screen))
            {
                return;
            }

            if (HandleFacilityClick(screen))
            {
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(_deployRect, screen, null))
            {
                Deploy();
            }
        }

        private bool HandleTabClick(Vector2 screen)
        {
            if (_tabRects == null)
            {
                return false;
            }

            for (int i = 0; i < _tabRects.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_tabRects[i], screen, null))
                {
                    SetActiveTab((BaseTab)i);
                    return true;
                }
            }

            return false;
        }

        private bool HandleFacilityClick(Vector2 screen)
        {
            if (_activeTab == BaseTab.Pod)
            {
                if (_podSelectRect != null && RectTransformUtility.RectangleContainsScreenPoint(_podSelectRect, screen, null))
                {
                    _pod.SelectNextKey(_catalog);
                    RefreshPod();
                    RefreshDeploy();
                    return true;
                }

                if (_podUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_podUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradePod(out error))
                    {
                        _podView.Rebind(_pod.Grid);
                        _podRect.sizeDelta = new Vector2(_pod.Width * BagView.CellSize, _pod.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("潜网舱升级到 Lv." + _pod.Level + "（" + _pod.Width + "×" + _pod.Height + "）。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _pod.UpgradeCost, _pod.UpgradeMaterialIds, _pod.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Mine)
            {
                if (_mineCollectRect != null && RectTransformUtility.RectangleContainsScreenPoint(_mineCollectRect, screen, null))
                {
                    int amount = GameSession.CollectMine();
                    RefreshAll();
                    if (amount > 0)
                    {
                        SetMessage("收取比特矿场 " + amount + " bit。");
                    }

                    return true;
                }

                if (_mineUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_mineUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradeMine(out error))
                    {
                        RefreshAll();
                        SetMessage("比特矿场升级到 Lv." + _mine.Level + "。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _mine.UpgradeCost, _mine.UpgradeMaterialIds, _mine.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Craft)
            {
                if (_craftCraftRect != null && RectTransformUtility.RectangleContainsScreenPoint(_craftCraftRect, screen, null))
                {
                    PlacedItem placed;
                    string error;
                    if (GameSession.CraftItem(out placed, out error))
                    {
                        _craftView.Refresh();
                        _stashView.Refresh();
                        RefreshHud();
                        RefreshTabs();
                        RefreshCraft();
                        SetMessage("制作完成：" + _catalog.Get(placed.DefinitionId).DisplayName + "。");
                    }
                    else
                    {
                        SetMessage(error);
                    }

                    return true;
                }

                if (_craftUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_craftUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradeCraft(out error))
                    {
                        _craftView.Rebind(_craft.Grid);
                        _craftRect.sizeDelta = new Vector2(_craft.Width * BagView.CellSize, _craft.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("制作站升级到 Lv." + _craft.Level + "（" + _craft.Width + "×" + _craft.Height + "）。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _craft.UpgradeCost, _craft.UpgradeMaterialIds, _craft.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Cpu)
            {
                if (_cpuUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_cpuUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradeCpu(out error))
                    {
                        // 网格尺寸变了，必须 Rebind 才会重画格子。
                        _cpuView.Rebind(_cpu.Grid);
                        _cpuRect.sizeDelta = new Vector2(_cpu.Width * BagView.CellSize, _cpu.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("中央处理器升级到 Lv." + _cpu.Level + "（" + _cpu.Width + "×" + _cpu.Height + "）。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _cpu.UpgradeCost, _cpu.UpgradeMaterialIds, _cpu.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Comms)
            {
                if (_commsUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_commsUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradeComms(out error))
                    {
                        _commsView.Rebind(_comms.Grid);
                        _commsRect.sizeDelta = new Vector2(
                            _comms.Width * BagView.CellSize,
                            _comms.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("通讯中枢升级到 Lv." + _comms.Level + "（" + _comms.Width + "×" + _comms.Height + "）。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _comms.UpgradeCost, _comms.UpgradeMaterialIds, _comms.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Insurance)
            {
                if (_insuranceUpgradeRect != null && RectTransformUtility.RectangleContainsScreenPoint(_insuranceUpgradeRect, screen, null))
                {
                    string error;
                    if (GameSession.UpgradeInsurance(out error))
                    {
                        _secureView.Rebind(_secure);
                        _secureRect.sizeDelta = new Vector2(
                            _secure.Grid.Width * BagView.CellSize,
                            _secure.Grid.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("保险网络中枢升级到 Lv." + _insurance.Level + "。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _insurance.UpgradeCost, _insurance.UpgradeMaterialIds, _insurance.CanUpgrade));
                    }

                    return true;
                }
            }

            if (_activeTab == BaseTab.Storage)
            {
                if (_storageUpgradeRect != null && RectTransformRectangleContains(_storageUpgradeRect, screen))
                {
                    string error;
                    if (GameSession.UpgradeStorage(out error))
                    {
                        _stashView.Rebind(_stash);
                        _stashRect.sizeDelta = new Vector2(
                            _stash.Grid.Width * BagView.CellSize,
                            _stash.Grid.Height * BagView.CellSize);
                        RebuildSources();
                        RefreshAll();
                        SetMessage("仓储设备升级到 Lv." + _rack.Level + "（" + _stash.Grid.Height + " 行）。");
                    }
                    else
                    {
                        SetMessage(UpgradeFailureText(error, _rack.UpgradeCost, _rack.UpgradeMaterialIds, _rack.CanUpgrade));
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool RectTransformRectangleContains(RectTransform rect, Vector2 screen)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null);
        }

        private void Deploy()
        {
            if (!_campaign.IsActive)
            {
                return;
            }

            if (GameSession.ExpeditionActive)
            {
                SetMessage("已有探索进行中。");
                return;
            }

            DivePlan plan;
            string error;
            if (!_pod.TryResolveDive(_catalog, out plan, out error))
            {
                SetMessage(error);
                return;
            }

            if (GameSession.BeginExpedition())
            {
                SceneManager.LoadScene("ExpeditionPrototype");
            }
            else
            {
                SetMessage(GameSession.LastMessage);
            }
        }

        private void RefreshAll()
        {
            _bagView.Refresh();
            _stashView.Refresh();
            _secureView.Refresh();
            _merchantView.Refresh();
            _podView.Refresh();
            _craftView.Refresh();
            _cpuView.Refresh();
            RefreshMineCells();
            RefreshHud();
            RefreshTabs();
            RefreshPod();
            RefreshMine();
            RefreshCraft();
            RefreshCpu();
            RefreshInsurance();
            RefreshStorage();
            RefreshComms();
            RefreshNpcBoard();
            RefreshDeploy();

            SetMessage(GameSession.LastMessage);

            // 每次刷新都可能跟在一次状态变化之后（拖拽、买卖、升级、接任务）。
            // 这里只标脏，真正写盘由 TickAutoSave 去抖——否则每拖一次就写一个几十 KB 的文件。
            _saveDirty = true;
            _autoSaveTimer = AutoSaveSeconds;
        }

        private void RefreshTabs()
        {
            if (_tabLabels == null)
            {
                return;
            }

            for (int i = 0; i < _tabLabels.Length; i++)
            {
                _tabLabels[i].text = TabNames[i];
                bool active = (int)_activeTab == i;
                _tabImages[i].color = active ? AccentColor : new Color32(38, 44, 52, 255);
                _tabLabels[i].color = active ? TextColor : MutedColor;
            }
        }

        private void RefreshPod()
        {
            if (_podInfo == null)
            {
                return;
            }

            _pod.EnsureSelection(_catalog);
            DivePlan plan;
            string error;
            bool ready = _pod.TryResolveDive(_catalog, out plan, out error);
            string keyName = _pod.SelectedKeyName(_catalog);
            _podInfo.text = "密钥：" + keyName
                            + "\n副本：" + (ready ? plan.Name : "无")
                            + "    Lv." + _pod.Level + "  " + _pod.Width + "×" + _pod.Height
                            + "\n升级：" + UpgradeText(_pod.UpgradeCost, _pod.UpgradeMaterialIds, _pod.CanUpgrade);

            bool max = !_pod.CanUpgrade;
            _podUpgradeText.text = max ? "已满级" : "升级";
            _podUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshMine()
        {
            if (_mineInfo == null)
            {
                return;
            }

            _mineInfo.text = "Lv." + _mine.Level + "  " + _mine.Width + "×" + _mine.Height
                             + "  ·  每格 " + BitMine.YieldPerCell + " bit/天"
                             + "\n可收取 " + _mine.StoredBit + " bit"
                             + "\n升级：" + UpgradeText(_mine.UpgradeCost, _mine.UpgradeMaterialIds, _mine.CanUpgrade);

            _mineCollectText.text = _mine.StoredBit > 0 ? "收取 " + _mine.StoredBit : "已收取";
            _mineCollectImage.color = _mine.StoredBit > 0 ? AccentColor : DisabledColor;

            bool max = !_mine.CanUpgrade;
            _mineUpgradeText.text = max ? "已满级" : "升级";
            _mineUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshCraft()
        {
            if (_craftInfo == null)
            {
                return;
            }

            CraftRecipe recipe = _craft.MatchingRecipe;
            CraftRecipe locked = recipe == null ? _craft.LockedRecipe : null;

            // 「材料够了但站不够」和「材料不够」是两件事，按钮文案必须分开说，
            // 否则玩家会一直往工作台上堆材料却不知道为什么没反应。
            string recipeLine = recipe != null
                ? recipe.Name
                : locked != null
                    ? locked.Name + "（需 Lv." + locked.MinStationLevel + "）"
                    : "无匹配";

            _craftInfo.text = "Lv." + _craft.Level + "  "
                              + _craft.Grid.Grid.Width + "×" + _craft.Grid.Grid.Height
                              + "\n配方：" + recipeLine
                              + "\n升级：" + UpgradeText(_craft.UpgradeCost, _craft.UpgradeMaterialIds, _craft.CanUpgrade);

            bool canCraft = recipe != null;
            _craftCraftText.text = canCraft ? "制作" : locked != null ? "站不够" : "材料不足";
            _craftCraftImage.color = canCraft ? AccentColor : DisabledColor;

            bool max = !_craft.CanUpgrade;
            _craftUpgradeText.text = max ? "已满级" : "升级";
            _craftUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshCpu()
        {
            if (_cpuInfo == null)
            {
                return;
            }

            GlobalModifiers modifiers = _cpu.EvaluateModifiers(_catalog);
            _cpuInfo.text = "Lv." + _cpu.Level + "  " + _cpu.Width + "×" + _cpu.Height
                            + "   芯片 " + _cpu.Grid.Count + "/" + _cpu.Grid.Capacity
                            + "\n加成：" + modifiers.Describe()
                            + "\n可插 T1–T" + _cpu.MaxChipTier
                            + "\n升级：" + UpgradeText(_cpu.UpgradeCost, _cpu.UpgradeMaterialIds, _cpu.CanUpgrade);

            bool max = !_cpu.CanUpgrade;
            _cpuUpgradeText.text = max ? "已满级" : "升级";
            _cpuUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshInsurance()
        {
            if (_insuranceInfo == null)
            {
                return;
            }

            _insuranceInfo.text = "Lv." + _insurance.Level + "  "
                                  + InsuranceHub.WidthForLevel(_insurance.Level) + "×"
                                  + InsuranceHub.HeightForLevel(_insurance.Level)
                                  + "（" + _insurance.CapacityFor(_insurance.Level) + " 格）"
                                  + "\n保险箱是唯一阵亡也不会丢的格子，"
                                  + "\n只收 1×1 的非武器 / 护甲 / 比特。"
                                  + "\n升级：" + UpgradeText(_insurance.UpgradeCost, _insurance.UpgradeMaterialIds, _insurance.CanUpgrade);

            bool max = !_insurance.CanUpgrade;
            _insuranceUpgradeText.text = max ? "已满级" : "升级";
            _insuranceUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshStorage()
        {
            if (_storageInfo == null)
            {
                return;
            }

            _storageInfo.text = "Lv." + _rack.Level + "  "
                                + StorageRack.Width + "×" + StorageRack.HeightForLevel(_rack.Level)
                                + "（" + (_stash.Grid.Width * _stash.Grid.Height) + " 格，空 " + _stash.FreeCellCount + "）"
                                + "\n仓库用于囤材料与备用装备。"
                                + "\n升级：" + UpgradeText(_rack.UpgradeCost, _rack.UpgradeMaterialIds, _rack.CanUpgrade);

            bool max = !_rack.CanUpgrade;
            _storageUpgradeText.text = max ? "已满级" : "升级";
            _storageUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshComms()
        {
            if (_commsView != null)
            {
                _commsView.Refresh();
            }

            if (_commsInfo == null)
            {
                return;
            }

            IReadOnlyList<string> contacted = _comms.ContactedNpcIds;
            string names = "无";
            for (int i = 0; i < contacted.Count; i++)
            {
                NpcDefinition npc;
                if (!NpcLibrary.TryGet(contacted[i], out npc))
                {
                    continue;
                }

                // 只取冒号后的名字，面板太窄放不下「回收商 · 老陈」。
                string shortName = npc.DisplayName;
                int dot = shortName.LastIndexOf('·');
                if (dot >= 0 && dot + 1 < shortName.Length)
                {
                    shortName = shortName.Substring(dot + 1).Trim();
                }

                names = i == 0 ? shortName : names + "、" + shortName;
            }

            _commsInfo.text = "已联系 " + contacted.Count + " 人  Lv." + _comms.Level
                              + "  " + _comms.Width + "×" + _comms.Height
                              + "\n点击链路接通：" + names
                              + "\n升级：" + UpgradeText(_comms.UpgradeCost, _comms.UpgradeMaterialIds, _comms.CanUpgrade);

            bool max = !_comms.CanUpgrade;
            _commsUpgradeText.text = max ? "已满级" : "升级";
            _commsUpgradeImage.color = max ? DisabledColor : AccentColor;
        }

        private void RefreshDeploy()
        {
            if (!_campaign.IsActive)
            {
                _deployImage.color = DisabledColor;
                _deployText.text = "战役结束";
                return;
            }

            DivePlan plan;
            string error;
            if (!_pod.TryResolveDive(_catalog, out plan, out error))
            {
                _deployImage.color = DisabledColor;
                _deployText.text = _pod.LastResolveBlockedByTier ? "潜网舱等级不足" : "需要潜网密钥";
                return;
            }

            _deployImage.color = AccentColor;
            _deployText.text = "下潜 · " + plan.Name;
        }

        private void SetActiveTab(BaseTab tab, bool force = false)
        {
            if (!force && _activeTab == tab)
            {
                return;
            }

            _activeTab = tab;
            for (int i = 0; i < _tabContents.Length; i++)
            {
                _tabContents[i].SetActive(i == (int)tab);
            }

            RefreshTabs();
            RebuildSources();
            RefreshPod();
            RefreshMine();
            RefreshCraft();
            RefreshCpu();
            RefreshInsurance();
            RefreshStorage();
            RefreshComms();
            RefreshNpcBoard();
            RefreshDeploy();
        }

        private void RebuildSources()
        {
            _sources.Clear();
            _sources.Add(new BaseBagSource { Name = "PACK", Bag = _bag, Rect = _bagRect, View = _bagView });
            _sources.Add(new BaseBagSource { Name = "STASH", Bag = _stash, Rect = _stashRect, Viewport = _stashViewport, View = _stashView, IsPurchaseTarget = true });
            _sources.Add(new BaseBagSource { Name = "SECURE", Bag = _secure, Rect = _secureRect, View = _secureView, IsSecure = true });

            if (_activeTab == BaseTab.Merchant)
            {
                _sources.Add(new BaseBagSource { Name = "MERCHANT", Bag = _merchant.Stock, Rect = _merchantRect, View = _merchantView, IsMerchant = true });
            }
            else if (_activeTab == BaseTab.Pod)
            {
                // 潜网舱只收关卡密钥：任务契约、装备、材料都不该占船位。
                _sources.Add(new BaseBagSource
                {
                    Name = "POD",
                    Bag = _pod.Grid,
                    Rect = _podRect,
                    View = _podView,
                    ItemFilter = definition => definition.Category == ItemCategory.Key
                });
            }
            else if (_activeTab == BaseTab.Craft)
            {
                // 制作台只收材料：配方按材料匹配，别的放进去只会白占格子。
                _sources.Add(new BaseBagSource
                {
                    Name = "CRAFT",
                    Bag = _craft.Grid,
                    Rect = _craftRect,
                    View = _craftView,
                    ItemFilter = definition => definition.Category == ItemCategory.Material
                });
            }
            else if (_activeTab == BaseTab.Cpu)
            {
                // 只收芯片，且档位不能超过 CPU 等级——否则低级 CPU 也能插 T5 芯片，
                // 「升级扩容」就只剩下格子数量一个收益了。
                _sources.Add(new BaseBagSource
                {
                    Name = "CPU",
                    Bag = _cpu.Grid,
                    Rect = _cpuRect,
                    View = _cpuView,
                    ItemFilter = definition => _cpu.Accepts(definition)
                });
            }
            else if (_activeTab == BaseTab.Comms)
            {
                // 只收通讯上行链路：关卡密钥、芯片、装备都进不来。
                _sources.Add(new BaseBagSource
                {
                    Name = "COMMS",
                    Bag = _comms.Grid,
                    Rect = _commsRect,
                    View = _commsView,
                    ItemFilter = definition => CommsHub.CanAccept(definition)
                });
            }

            if (_drag != null)
            {
                _drag.SetSources(_sources);
            }
        }

        private void RefreshMineCells()
        {
            if (_mineGridRect == null)
            {
                return;
            }

            for (int i = 0; i < _mineCellImages.Count; i++)
            {
                Destroy(_mineCellImages[i].gameObject);
            }

            _mineCellImages.Clear();
            _mineCellTexts.Clear();

            if (_bitSprite == null)
            {
                ItemDefinition bit = _catalog.Get(ItemBitWallet.BitItemId);
                Color fill;
                if (!_palette.TryGetValue(ItemBitWallet.BitItemId, out fill))
                {
                    fill = new Color32(230, 196, 70, 255);
                }

                Color border = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 1f);
                _bitSprite = SpriteFactory.CreateShapeSprite(bit.Shape, fill, border);
            }

            float cell = BagView.CellSize;
            for (int y = 0; y < _mine.Height; y++)
            {
                for (int x = 0; x < _mine.Width; x++)
                {
                    var go = new GameObject("MineCell_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(_mineGridRect, false);

                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(cell, cell);
                    rect.anchoredPosition = new Vector2(
                        -_mine.Width * cell * 0.5f + x * cell + cell * 0.5f,
                        _mine.Height * cell * 0.5f - y * cell - cell * 0.5f);

                    Image image = go.GetComponent<Image>();
                    image.sprite = SpriteFactory.CellBackground;
                    image.color = Color.white;

                    if (_mine.IsFilled(x, y))
                    {
                        var iconGo = new GameObject("Bit", typeof(RectTransform), typeof(Image));
                        iconGo.transform.SetParent(go.transform, false);
                        var iconRect = (RectTransform)iconGo.transform;
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0f, 1f);
                        iconRect.sizeDelta = new Vector2(cell, cell);
                        iconRect.anchoredPosition = new Vector2(-cell * 0.5f, cell * 0.5f);

                        Image icon = iconGo.GetComponent<Image>();
                        icon.sprite = _bitSprite;
                        icon.color = Color.white;
                    }

                    Text label = CreateCenteredText(go.transform, "Value", _mine.IsFilled(x, y) ? BitMine.YieldPerCell.ToString() : string.Empty, 10, TextColor, Vector2.zero, new Vector2(cell - 4f, cell - 4f));
                    _mineCellImages.Add(image);
                    _mineCellTexts.Add(label);
                }
            }
        }

        private void RefreshHud()
        {
            _hudText.text = "第 " + _campaign.Week + " 周 · 第 " + _campaign.Day + "/7 天"
                            + "\n应缴 " + _campaign.CurrentDue + "   已缴 " + _campaign.TotalPaid
                            + "\nbit " + _campaign.Bit;

            _hudText.color = _campaign.IsActive && _campaign.Bit < _campaign.CurrentDue
                ? WarningColor
                : TextColor;

            if (_statsText != null)
            {
                if (_campaign.IsActive)
                {
                    _statsText.text = "生命 " + GameSession.ExpeditionMaxHp
                                      + "\n背包 " + _bag.FreeCellCount + " 格"
                                      + "\n仓库 " + _stash.FreeCellCount + " 格"
                                      + "\n能量 " + (GameSession.BaseEnergyPerTurn + GameSession.CpuModifiers.BaseEnergy);
                }
                else
                {
                    _statsText.text = _campaign.Status == CampaignStatus.Victory
                        ? EndingLabel(_campaign.Ending)
                        : "债务违约";
                }
            }

            if (_secureTitle != null)
            {
                _secureTitle.text = "保险箱 " + _secure.Count + "/" + _secure.Grid.Capacity;
            }

            if (_progressFill != null)
            {
                float ratio = _campaign.CurrentDue <= 0
                    ? 1f
                    : Mathf.Clamp01((float)_campaign.Bit / _campaign.CurrentDue);
                _progressFill.sizeDelta = new Vector2(150f * ratio, 5f);
                _progressFill.GetComponent<Image>().color = ratio >= 1f ? AccentColor : WarningColor;
            }
        }

        private void ShowEnd()
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            if (_containerWindows != null)
            {
                _containerWindows.CloseAll();
            }

            _endOverlay.SetActive(true);

            if (_drag != null)
            {
                _drag.enabled = false;
            }

            if (_campaign.Status == CampaignStatus.Victory)
            {
                _endTitle.text = EndingLabel(_campaign.Ending);
                _endSubtitle.text = "撑到第 " + _campaign.Week + " 周，累计上缴 "
                                    + _campaign.TotalPaid + " bit。\n点击开始新游戏。";
            }
            else
            {
                _endTitle.text = "债务违约";
                _endSubtitle.text = "第 " + _campaign.Week + " 周欠下 "
                                    + _campaign.CurrentDue + " bit 无法偿还。\n点击开始新游戏。";
            }
        }

        private static string EndingLabel(CampaignEnding ending)
        {
            switch (ending)
            {
                case CampaignEnding.BossSlain:
                    return "机甲已倒下";
                case CampaignEnding.Escaped:
                    return "你逃了出去";
                default:
                    return "游戏结束";
            }
        }

        private void SetMessage(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_logEntries.Count > 0 && _logEntries[_logEntries.Count - 1] == text)
            {
                return;
            }

            _logEntries.Add(text);
            while (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }

            if (_logText != null)
            {
                _logText.text = string.Join("\n", _logEntries.ToArray());
            }
        }

        /// <summary>
        /// 债务只在这一个面板出现：周/天、应缴/已缴、bit 与进度条。
        /// 左栏是角色属性（生命/空间/能量），不再重复任何债务数字。
        /// </summary>
        private void BuildHud(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Hud", new Vector2(-227f, 141f), new Vector2(170f, 74f), PanelColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            _hudText = CreateText(panel, "Stats", string.Empty, 10, TextColor, new Vector2(10f, -6f), new Vector2(150f, 48f), TextAnchor.UpperLeft);

            RectTransform bg = CreateAnchoredPanel(panel, "DebtBar", new Vector2(10f, -58f), new Vector2(150f, 5f), new Color32(38, 44, 54, 255));
            bg.GetComponent<Image>().raycastTarget = false;
            _progressFill = CreateAnchoredPanel(bg, "Fill", Vector2.zero, new Vector2(0f, 5f), AccentColor);
            _progressFill.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildBagPanels(Transform canvas)
        {
            RectTransform bench = CreateCenteredPanel(canvas, "Workbench", new Vector2(-36f, -104f), new Vector2(398f, 132f), BenchColor);
            bench.GetComponent<Image>().raycastTarget = false;
            AddBorder(bench, BenchRailColor);
            RectTransform benchRail = CreateAnchoredPanel(bench, "TopRail", Vector2.zero, new Vector2(398f, 6f), BenchRailColor);
            benchRail.GetComponent<Image>().raycastTarget = false;
            CreateText(bench, "BenchLabel", "WORKBENCH", 7, new Color32(120, 104, 78, 255), new Vector2(318f, -10f), new Vector2(76f, 10f), TextAnchor.UpperLeft);

            RectTransform bagPanel = CreateCenteredPanel(canvas, "PackPanel", new Vector2(-172f, -108f), new Vector2(110f, 124f), PanelColor);
            bagPanel.gameObject.AddComponent<RectMask2D>();
            CreateText(bagPanel, "Title", "背包", 10, TextColor, new Vector2(8f, -6f), new Vector2(124f, 14f), TextAnchor.UpperLeft);

            _bagRect = CreateBagRect(bagPanel, new Vector2(0f, 2f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _bagView = _bagRect.gameObject.AddComponent<BagView>();
            _bagView.Initialize(_bag, _bagRect, _palette);

            RectTransform stashPanel = CreateCenteredPanel(canvas, "StashPanel", new Vector2(-19f, -108f), new Vector2(184f, 124f), PanelColor);
            stashPanel.gameObject.AddComponent<RectMask2D>();
            CreateText(stashPanel, "Title", "仓库", 10, TextColor, new Vector2(8f, -6f), new Vector2(168f, 14f), TextAnchor.UpperLeft);

            _stashViewport = CreateCenteredPanel(stashPanel, "StashViewport", new Vector2(-4f, -6f), new Vector2(StorageRack.Width * BagView.CellSize, 96f), Color.clear);
            var viewportImage = _stashViewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = false;
            }

            _stashViewport.gameObject.AddComponent<RectMask2D>();
            _stashRect = CreateBagRect(_stashViewport, Vector2.zero, new Vector2(StorageRack.Width * BagView.CellSize, _stash.Grid.Height * BagView.CellSize));
            _stashView = _stashRect.gameObject.AddComponent<BagView>();
            _stashView.Initialize(_stash, _stashRect, _palette);

            RectTransform scrollbarRect = CreateCenteredPanel(stashPanel, "StashScrollbar", new Vector2(80f, -6f), new Vector2(8f, 96f), new Color32(38, 44, 54, 255));
            var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            RectTransform scrollbarHandle = CreateCenteredPanel(scrollbarRect, "Handle", Vector2.zero, new Vector2(8f, 40f), new Color32(92, 108, 132, 255));
            scrollbar.handleRect = scrollbarHandle;
            scrollbar.targetGraphic = scrollbarHandle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;

            _stashScrollRect = stashPanel.gameObject.AddComponent<ScrollRect>();
            _stashScrollRect.content = _stashRect;
            _stashScrollRect.viewport = _stashViewport;
            _stashScrollRect.horizontal = false;
            _stashScrollRect.vertical = true;
            _stashScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _stashScrollRect.inertia = false;
            _stashScrollRect.scrollSensitivity = 20f;
            _stashScrollRect.verticalScrollbar = scrollbar;
            _stashScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            _stashScroll = stashPanel.gameObject.AddComponent<StashScrollView>();
            _stashScroll.Scroll = _stashScrollRect;
            _stashScroll.Viewport = _stashViewport;
            _stashScroll.Track = scrollbarRect;
            _stashScroll.Handle = scrollbarHandle;

            RectTransform securePanel = CreateCenteredPanel(canvas, "SecurePanel", new Vector2(117f, -128f), new Vector2(76f, 84f), PanelColor);
            _secureTitle = CreateText(securePanel, "Title", "保险箱 0/4", 9, TextColor, new Vector2(6f, -4f), new Vector2(64f, 12f), TextAnchor.UpperLeft);
            _secureRect = CreateBagRect(securePanel, new Vector2(0f, -8f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _secureView = _secureRect.gameObject.AddComponent<BagView>();
            _secureView.Initialize(_secure, _secureRect, _palette);
        }

        private void BuildRightPanel(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "RightPanel", new Vector2(230f, 45f), new Vector2(170f, 160f), PanelColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            panel.gameObject.AddComponent<RectMask2D>();

            int tabCount = TabNames.Length;
            _tabContents = new GameObject[tabCount];
            _tabRects = new RectTransform[tabCount];
            _tabLabels = new Text[tabCount];
            _tabImages = new Image[tabCount];

            // 8 个标签挤在 170px 里：每个 20px、间距 20.6px，两个字在字号 7 下刚好放得下。
            // 总量 4 + 7×20.6 + 20 = 168.2，留 1.8px 余量。
            const float tabWidth = 20f;
            const float tabStep = 20.6f;
            for (int i = 0; i < tabCount; i++)
            {
                RectTransform tab = CreateAnchoredPanel(
                    panel,
                    "Tab_" + i,
                    new Vector2(4f + i * tabStep, -2f),
                    new Vector2(tabWidth, 18f),
                    new Color32(38, 44, 52, 255));
                _tabRects[i] = tab;
                _tabImages[i] = tab.GetComponent<Image>();
                _tabLabels[i] = CreateCenteredText(tab, "Label", TabNames[i], 7, TextColor, Vector2.zero, new Vector2(tabWidth - 2f, 16f));
            }

            RectTransform content = CreateAnchoredRect(panel, "Content", new Vector2(0f, -22f), new Vector2(170f, 138f));
            for (int i = 0; i < tabCount; i++)
            {
                var go = new GameObject("TabContent_" + i, typeof(RectTransform));
                go.transform.SetParent(content, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _tabContents[i] = go;
            }

            BuildMerchantContent(_tabContents[(int)BaseTab.Merchant].transform);
            BuildPodContent(_tabContents[(int)BaseTab.Pod].transform);
            BuildMineContent(_tabContents[(int)BaseTab.Mine].transform);
            BuildCraftContent(_tabContents[(int)BaseTab.Craft].transform);
            BuildCpuContent(_tabContents[(int)BaseTab.Cpu].transform);
            BuildInsuranceContent(_tabContents[(int)BaseTab.Insurance].transform);
            BuildStorageContent(_tabContents[(int)BaseTab.Storage].transform);
            BuildCommsContent(_tabContents[(int)BaseTab.Comms].transform);
        }

        private void BuildMerchantContent(Transform content)
        {
            _merchantRect = CreateBagRect(content, new Vector2(0f, 0f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _merchantView = _merchantRect.gameObject.AddComponent<BagView>();
            _merchantView.Initialize(_merchant.Stock, _merchantRect, _palette);

            _discardBinRect = CreateAnchoredPanel(content, "DiscardBin", new Vector2(46f, -108f), new Vector2(78f, 22f), SellColor);
            CreateCenteredText(_discardBinRect, "Label", "丢弃", 9, MutedColor, Vector2.zero, new Vector2(84f, 22f));
        }

        /// <summary>
        /// 潜网舱面板。舱体宽 = 等级（1–5），密钥体积随档递增（1/2/4/6/9 格），
        /// 所以升到 Lv.5 才能把 3×3 的核心密钥连同催化剂一起摆进去。
        /// 网格高度封顶 3 行 —— 再高就会压到下面的说明文字。
        /// </summary>
        private void BuildPodContent(Transform content)
        {
            _podRect = CreateBagRect(
                content,
                new Vector2(0f, 32f),
                new Vector2(_pod.Width * BagView.CellSize, _pod.Height * BagView.CellSize));
            _podView = _podRect.gameObject.AddComponent<BagView>();
            _podView.Initialize(_pod.Grid, _podRect, _palette);

            _podInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -62f), new Vector2(154f, 36f), TextAnchor.UpperLeft);
            _podSelectRect = CreateButton(content, "PodSelect", "换密钥", new Vector2(8f, -112f), new Vector2(73f, 22f), new Color32(38, 44, 52, 255));
            _podUpgradeRect = CreateButton(content, "PodUpgrade", "升级", new Vector2(89f, -112f), new Vector2(73f, 22f), AccentColor);
            _podUpgradeImage = _podUpgradeRect.GetComponent<Image>();
            _podUpgradeText = _podUpgradeRect.Find("Label").GetComponent<Text>();
        }

        private void BuildMineContent(Transform content)
        {
            _mineGridRect = CreateBagRect(content, new Vector2(0f, 15f), new Vector2(3f * BagView.CellSize, 2f * BagView.CellSize));
            _mineInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -58f), new Vector2(154f, 32f), TextAnchor.UpperLeft);

            _mineCollectRect = CreateButton(content, "MineCollect", "收取", new Vector2(8f, -108f), new Vector2(73f, 22f), AccentColor);
            _mineCollectImage = _mineCollectRect.GetComponent<Image>();
            _mineCollectText = _mineCollectRect.Find("Label").GetComponent<Text>();

            _mineUpgradeRect = CreateButton(content, "MineUpgrade", "升级", new Vector2(89f, -108f), new Vector2(73f, 22f), AccentColor);
            _mineUpgradeImage = _mineUpgradeRect.GetComponent<Image>();
            _mineUpgradeText = _mineUpgradeRect.Find("Label").GetComponent<Text>();
        }

        private void BuildCraftContent(Transform content)
        {
            _craftRect = CreateBagRect(
                content,
                new Vector2(0f, 32f),
                new Vector2(_craft.Width * BagView.CellSize, _craft.Height * BagView.CellSize));
            _craftView = _craftRect.gameObject.AddComponent<BagView>();
            _craftView.Initialize(_craft.Grid, _craftRect, _palette);

            _craftInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -62f), new Vector2(154f, 36f), TextAnchor.UpperLeft);

            _craftCraftRect = CreateButton(content, "Craft", "制作", new Vector2(8f, -112f), new Vector2(73f, 22f), AccentColor);
            _craftCraftImage = _craftCraftRect.GetComponent<Image>();
            _craftCraftText = _craftCraftRect.Find("Label").GetComponent<Text>();

            _craftUpgradeRect = CreateButton(content, "CraftUpgrade", "升级", new Vector2(89f, -112f), new Vector2(73f, 22f), AccentColor);
            _craftUpgradeImage = _craftUpgradeRect.GetComponent<Image>();
            _craftUpgradeText = _craftUpgradeRect.Find("Label").GetComponent<Text>();
        }

        /// <summary>
        /// 中央处理器面板。芯片网格是「背包之外的第二块格子」，所以换入换出全部用拖拽。
        /// 网格尺寸随 CPU 等级变化，升级后要 Rebind 才能重画格子。
        /// </summary>
        private void BuildCpuContent(Transform content)
        {
            _cpuRect = CreateBagRect(
                content,
                new Vector2(0f, 30f),
                new Vector2(_cpu.Width * BagView.CellSize, _cpu.Height * BagView.CellSize));
            _cpuView = _cpuRect.gameObject.AddComponent<BagView>();
            _cpuView.Initialize(_cpu.Grid, _cpuRect, _palette, _catalog);

            _cpuInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -72f), new Vector2(154f, 42f), TextAnchor.UpperLeft);

            _cpuUpgradeRect = CreateButton(content, "CpuUpgrade", "升级", new Vector2(8f, -114f), new Vector2(154f, 22f), AccentColor);
            _cpuUpgradeImage = _cpuUpgradeRect.GetComponent<Image>();
            _cpuUpgradeText = _cpuUpgradeRect.Find("Label").GetComponent<Text>();
        }

        /// <summary>保险中枢只改容量，格子本身在主界面「保险箱」面板上，所以这里只放说明与升级。</summary>
        private void BuildInsuranceContent(Transform content)
        {
            _insuranceInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -10f), new Vector2(154f, 90f), TextAnchor.UpperLeft);

            _insuranceUpgradeRect = CreateButton(content, "InsuranceUpgrade", "升级", new Vector2(8f, -114f), new Vector2(154f, 22f), AccentColor);
            _insuranceUpgradeImage = _insuranceUpgradeRect.GetComponent<Image>();
            _insuranceUpgradeText = _insuranceUpgradeRect.Find("Label").GetComponent<Text>();
        }

        /// <summary>仓储设备同理：仓库在主界面上，这里只放容量与升级。</summary>
        private void BuildStorageContent(Transform content)
        {
            _storageInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -10f), new Vector2(154f, 90f), TextAnchor.UpperLeft);

            _storageUpgradeRect = CreateButton(content, "StorageUpgrade", "升级", new Vector2(8f, -114f), new Vector2(154f, 22f), AccentColor);
            _storageUpgradeImage = _storageUpgradeRect.GetComponent<Image>();
            _storageUpgradeText = _storageUpgradeRect.Find("Label").GetComponent<Text>();
        }

        /// <summary>
        /// 通讯中枢面板。格子装的是**通讯上行链路**（1 / 2 / 4 / 6 格），
        /// 一条链路 = 一位能联系的 NPC = 一条能挂任务的信道。
        /// 具体谁在货架上、任务怎么接，都放在弹层里——138px 的面板塞不下十行。
        /// </summary>
        private void BuildCommsContent(Transform content)
        {
            _commsRect = CreateBagRect(
                content,
                new Vector2(0f, 30f),
                new Vector2(_comms.Width * BagView.CellSize, _comms.Height * BagView.CellSize));
            _commsView = _commsRect.gameObject.AddComponent<BagView>();
            _commsView.Initialize(_comms.Grid, _commsRect, _palette);

            _commsInfo = CreateText(content, "Info", string.Empty, 8, MutedColor, new Vector2(8f, -72f), new Vector2(154f, 40f), TextAnchor.UpperLeft);

            RectTransform upgrade = CreateButton(content, "CommsUpgrade", "升级", new Vector2(89f, -114f), new Vector2(73f, 22f), AccentColor);
            _commsUpgradeImage = upgrade.GetComponent<Image>();
            _commsUpgradeText = upgrade.Find("Label").GetComponent<Text>();
            _commsUpgradeRect = upgrade;
        }

        private void BuildDeploy(Transform canvas)        {
            _deployRect = CreateCenteredPanel(canvas, "Deploy", new Vector2(215f, 152f), new Vector2(160f, 28f), AccentColor);
            AddBorder(_deployRect, new Color32(96, 142, 186, 255));
            _deployImage = _deployRect.GetComponent<Image>();
            _deployText = CreateCenteredText(_deployRect, "Label", "出征", 11, TextColor, Vector2.zero, new Vector2(156f, 26f));
        }

        /// <summary>
        /// 背包左侧的窄属性栏。中央整块让给立绘/台词，所以战斗与空间相关的数字只放这里；
        /// 债务信息仍留在左上角 HUD，避免同一组数字出现两次。
        /// </summary>
        private void BuildLeftStats(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "LeftStats", new Vector2(-276f, -108f), new Vector2(84f, 124f), PanelColor);
            panel.gameObject.AddComponent<RectMask2D>();
            _statsText = CreateText(panel, "Stats", string.Empty, 8, TextColor, new Vector2(6f, -6f), new Vector2(72f, 104f), TextAnchor.UpperLeft);
        }

        /// <summary>
        /// 中央艺术台：顶部台词 + 左下立绘位。未接通联系人时显示基地提示。
        /// 立绘先用名字首字兜底，接美术时替换 Portrait 下的 Image 即可。
        /// </summary>
        private void BuildArtStage(Transform canvas)
        {
            _artStage = CreateCenteredPanel(canvas, "ArtStage", new Vector2(0f, 50f), new Vector2(270f, 170f), WindowColor);
            AddBorder(_artStage, new Color32(67, 59, 47, 255));
            _artStage.GetComponent<Image>().raycastTarget = false;

            _artTitle = CreateText(_artStage, "Title", "基地", 10, TextColor, new Vector2(10f, -6f), new Vector2(250f, 13f), TextAnchor.UpperLeft);
            _artSubtitle = CreateText(_artStage, "Subtitle", "基础潜网 // BASE LINK", 7, new Color32(164, 146, 112, 255), new Vector2(10f, -20f), new Vector2(250f, 11f), TextAnchor.UpperLeft);
            _artDialogue = CreateText(_artStage, "Dialogue", string.Empty, 8, MutedColor, new Vector2(10f, -34f), new Vector2(250f, 26f), TextAnchor.UpperLeft);

            RectTransform portrait = CreateCenteredPanel(_artStage, "Portrait", new Vector2(-58f, -32f), new Vector2(84f, 84f), new Color32(28, 33, 41, 255));
            portrait.GetComponent<Image>().raycastTarget = false;
            AddBorder(portrait, new Color32(52, 60, 72, 255));
            _artPortraitInitial = CreateCenteredText(portrait, "Initial", string.Empty, 30, new Color32(120, 132, 150, 255), Vector2.zero, new Vector2(80f, 80f));

            RefreshArtStage();
        }

        /// <summary>
        /// 消息日志：贴在仓库右侧（工作台右边、设施面板下方），保留最近几条记录。
        /// 中央区域整块留给立绘，不再被单行提示占用。
        /// </summary>
        private void BuildLog(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Log", new Vector2(241f, -108f), new Vector2(150f, 124f), new Color32(22, 26, 32, 255));
            AddBorder(panel, new Color32(50, 58, 68, 255));
            panel.GetComponent<Image>().raycastTarget = false;
            panel.gameObject.AddComponent<RectMask2D>();
            CreateText(panel, "Header", "LOG", 7, new Color32(164, 146, 112, 255), new Vector2(8f, -6f), new Vector2(134f, 10f), TextAnchor.UpperLeft);
            _logText = CreateText(panel, "Lines", string.Empty, 7, MutedColor, new Vector2(8f, -20f), new Vector2(134f, 98f), TextAnchor.UpperLeft);
        }

        private void BuildNpcWindow(Transform canvas)
        {
            var go = new GameObject("NpcBagWindow", typeof(RectTransform), typeof(Image), typeof(NpcBagWindow));
            go.transform.SetParent(canvas, false);
            _npcWindow = go.GetComponent<NpcBagWindow>();
            _npcWindow.Initialize(_canvasRect, _catalog, _palette, _ => CloseNpcWindow());
            _npcWindow.SetPosition(new Vector2(60f, 40f));
            go.SetActive(false);
        }

        /// <summary>点击通讯网格里的上行链路 = 接通。中心画面与柜台窗口都跟着这个人换。</summary>
        private void ConnectNpc(NpcDefinition npc)
        {
            if (npc == null || _npcWindow == null)
            {
                return;
            }

            _activeNpcId = npc.Id;
            _npcLineOverride = null;
            NpcBagFlow.SyncNpc(npc, _npcStocks, _quests, _comms, QuestLog.Today(_campaign), _bag, _stash, _catalog);
            _npcWindow.Bind(_npcStocks.StockFor(npc.Id), npc.DisplayName, "拖出＝买 / 接单　拖入＝交付");
            _npcWindow.gameObject.SetActive(true);
            SettingsOverlay.PushModal();
            _npcModalPushed = true;

            if (_drag != null)
            {
                _drag.RemovePersistentSource("NPCBAG");
                _drag.AddPersistentSource(_npcWindow.Source);
            }

            RefreshArtStage();
            SetMessage("已接通 " + npc.DisplayName + "。");
        }

        private void CloseNpcWindow()
        {
            _activeNpcId = null;
            _npcLineOverride = null;
            if (_npcModalPushed)
            {
                SettingsOverlay.PopModal();
                _npcModalPushed = false;
            }

            if (_npcWindow != null)
            {
                _npcWindow.gameObject.SetActive(false);
            }

            if (_drag != null)
            {
                _drag.RemovePersistentSource("NPCBAG");
            }

            RefreshArtStage();
        }

        private void RefreshNpcBoard()
        {
            if (_npcWindow == null || !_npcWindow.gameObject.activeSelf || string.IsNullOrEmpty(_activeNpcId))
            {
                return;
            }

            NpcDefinition npc;
            if (!NpcLibrary.TryGet(_activeNpcId, out npc))
            {
                CloseNpcWindow();
                return;
            }

            NpcBagFlow.SyncNpc(npc, _npcStocks, _quests, _comms, QuestLog.Today(_campaign), _bag, _stash, _catalog);
            _npcWindow.Refresh();
            RefreshArtStage();
        }

        private void RefreshArtStage()
        {
            if (_artTitle == null)
            {
                return;
            }

            NpcDefinition npc = null;
            if (!string.IsNullOrEmpty(_activeNpcId))
            {
                NpcLibrary.TryGet(_activeNpcId, out npc);
            }

            if (npc == null)
            {
                _artTitle.text = "基地";
                _artSubtitle.text = "基础潜网 // BASE LINK";
                _artDialogue.text = "点击通讯页里的上行链路，接通联系人。";
                if (_artPortraitInitial != null)
                {
                    _artPortraitInitial.text = string.Empty;
                }

                return;
            }

            _artTitle.text = npc.DisplayName;
            _artSubtitle.text = npc.Faction;
            _artDialogue.text = CurrentNpcLine(npc);
            if (_artPortraitInitial != null)
            {
                _artPortraitInitial.text = ShortNpcName(npc.DisplayName);
            }
        }

        private string CurrentNpcLine(NpcDefinition npc)
        {
            if (!string.IsNullOrEmpty(_npcLineOverride) && Time.unscaledTime < _npcLineUntil)
            {
                return _npcLineOverride;
            }

            _npcLineOverride = null;
            List<QuestDefinition> quests = QuestLibrary.ForNpc(npc.Id);
            bool ready = false;
            bool active = false;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestState state = _quests.StateOf(quests[i].Id);
                if (state == QuestState.Ready)
                {
                    ready = true;
                }
                else if (state == QuestState.Active)
                {
                    active = true;
                }
            }

            if (ready)
            {
                return "办妥了就把它拿回来，我只看结果。";
            }

            if (active)
            {
                return "契约在你手上。别死在外面。";
            }

            return npc.Greeting;
        }

        private void SetNpcLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            _npcLineOverride = line;
            _npcLineUntil = Time.unscaledTime + 4.5f;
            RefreshArtStage();
        }

        private static string ShortNpcName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return string.Empty;
            }

            int dot = displayName.LastIndexOf('\u00b7');
            string name = dot >= 0 && dot + 1 < displayName.Length
                ? displayName.Substring(dot + 1).Trim()
                : displayName;
            return name.Length == 0 ? string.Empty : name.Substring(0, 1);
        }

        private BagInventory NpcBoard()
        {
            if (string.IsNullOrEmpty(_activeNpcId) || _npcStocks == null)
            {
                return null;
            }

            return _npcStocks.StockFor(_activeNpcId);
        }

        /// <summary>
        /// NPC 柜台的拖拽落点。两个方向都走这里，返回 true 表示「已处理，不要再移动物品」。
        /// 拖出：商品=买、契约=接单；拖入：契约=交付，其它一律拒绝。
        /// </summary>
        private bool TryNpcDrop(BaseBagSource source, PlacedItem item, BaseBagSource target)
        {
            if (source == null || target == null || item == null)
            {
                return false;
            }

            BagInventory board = NpcBoard();
            if (board == null)
            {
                return false;
            }

            NpcDefinition npc;
            if (!NpcLibrary.TryGet(_activeNpcId, out npc))
            {
                return false;
            }

            ItemDefinition definition = _catalog.Get(item.DefinitionId);
            bool isCard = definition.Category == ItemCategory.Quest;

            if (source.Bag == board && target.Bag != board)
            {
                if (target.Bag != _bag && target.Bag != _stash)
                {
                    SetMessage("拖到背包或仓库才能拿走。");
                    return true;
                }

                string message;
                bool ok = isCard
                    ? NpcBagFlow.TryAccept(npc, _npcStocks, _quests, _comms, QuestLog.Today(_campaign), item.InstanceId, target.Bag, _catalog, out message)
                    : NpcBagFlow.TryBuy(npc, _npcStocks, item.InstanceId, _campaign, target.Bag, _catalog, out message);

                SetMessage(message);
                SetNpcLine(message);
                if (ok)
                {
                    RefreshAll();
                }

                return true;
            }

            if (target.Bag == board && source.Bag != board)
            {
                if (source.Bag != _bag && source.Bag != _stash)
                {
                    SetMessage("从背包或仓库里拿契约来交付。");
                    return true;
                }

                if (!isCard)
                {
                    SetMessage("他不收这个。");
                    SetNpcLine("我只做契约和买卖，别的东西别往柜台上放。");
                    return true;
                }

                string message;
                bool turnedIn = NpcBagFlow.TryTurnIn(
                    npc,
                    _npcStocks,
                    _quests,
                    _comms,
                    _campaign,
                    item.InstanceId,
                    source.Bag,
                    _bag,
                    _stash,
                    _catalog,
                    out message);

                SetMessage(message);
                SetNpcLine(message);
                if (turnedIn)
                {
                    RefreshAll();
                }

                return true;
            }

            return false;
        }

        private void BuildDrag()
        {
            RebuildSources();

            _drag = gameObject.AddComponent<BaseDragInteraction>();
            _drag.Initialize(_sources, _catalog, _merchant, _campaign, _palette, _canvasRect, null, _discardBinRect);

            // NPC 柜台：拖出=买/接单，拖入=交付。全部在特殊落点里处理，不走常规移动。
            _drag.TrySpecialDrop = TryNpcDrop;

            // 被任务占用的上行链路必须留在通讯中枢里：拖拽、快速移动、右键旋转全部拦下。
            _drag.CanTake = (source, item) =>
            {
                if (source == null || source.Bag != _comms.Grid)
                {
                    return null;
                }

                string blocked;
                return _comms.CanModify(item.InstanceId, out blocked) ? null : blocked;
            };

            _drag.Message += OnDragMessage;
            _drag.Changed += OnDragChanged;
            _drag.ItemClicked += OnItemClicked;
            _drag.QuickMoveTarget = (source, item) =>            {
                if (source == null)
                {
                    return null;
                }

                if (source.Name == "PACK")
                {
                    return _drag.FindSource("STASH");
                }

                if (source.Name == "STASH")
                {
                    return _drag.FindSource("PACK");
                }

                return null;
            };
        }


        private void BuildTooltip(Transform canvas)
        {
            _tooltipPanel = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            _tooltipPanel.transform.SetParent(canvas, false);

            _tooltipRect = (RectTransform)_tooltipPanel.transform;
            _tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            _tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            _tooltipRect.pivot = new Vector2(0f, 1f);
            _tooltipRect.sizeDelta = new Vector2(240f, 180f);
            _tooltipPanel.GetComponent<Image>().color = new Color32(12, 14, 18, 245);
            _tooltipPanel.GetComponent<Image>().raycastTarget = false;

            _tooltipText = CreateText(_tooltipPanel.transform, "Text", string.Empty, 9, TextColor, new Vector2(8f, -6f), new Vector2(224f, 168f), TextAnchor.UpperLeft);
            _tooltipPanel.SetActive(false);
        }

        private void UpdateTooltip(Vector2 screen)
        {
            IReadOnlyList<BaseBagSource> tooltipSources = _drag != null ? _drag.Sources : _sources;
            if (_tooltipPanel == null || _ended || (_drag != null && _drag.IsDragging))
            {
                if (_tooltipPanel != null)
                {
                    _tooltipPanel.SetActive(false);
                }

                return;
            }

            PlacedItem hoveredItem = null;
            BaseBagSource hoveredSource = null;
            bool hoveredFromMerchant = false;
            for (int i = tooltipSources.Count - 1; i >= 0; i--)
            {
                Cell cell;
                if (!TryScreenToCell(tooltipSources[i].Rect, tooltipSources[i].Bag, screen, out cell))
                {
                    continue;
                }

                int instanceId = tooltipSources[i].Bag.Grid.GetOccupant(cell.X, cell.Y);
                if (instanceId == 0)
                {
                    continue;
                }

                if (tooltipSources[i].Bag.TryGet(instanceId, out hoveredItem))
                {
                    hoveredSource = tooltipSources[i];
                    hoveredFromMerchant = tooltipSources[i].IsMerchant;
                    break;
                }
            }

            if (hoveredItem == null)
            {
                _tooltipPanel.SetActive(false);
                return;
            }

            ItemDefinition definition = _catalog.Get(hoveredItem.DefinitionId);
            string text = ItemText.Describe(definition, hoveredItem, _bag.CountOf(definition.AmmoItemId));
            if (hoveredFromMerchant)
            {
                int offerIndex = _merchant.FindOfferIndexByInstance(hoveredItem.InstanceId);
                if (offerIndex >= 0)
                {
                    MerchantOffer offer = _merchant.Offers[offerIndex];
                    text += "\n" + (offer.IsBuyback ? "回收价 " : "售价 ") + offer.Price + " bit";
                    if (offer.Count > 1)
                    {
                        text += " ×" + offer.Count;
                    }

                    text += "   左键从仓库购买";
                }
            }
            else if (ItemUse.CanUseOutsideCombat(definition))
            {
                text += "\n探索中可以左键使用";
            }

            _tooltipText.text = text;
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
            _tooltipPanel.transform.SetAsLastSibling();
            _tooltipPanel.SetActive(true);
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

        private void OnItemClicked(BaseBagSource source, int instanceId)
        {
            PlacedItem item;
            if (source == null || !source.Bag.TryGet(instanceId, out item))
            {
                return;
            }

            ItemDefinition definition = _catalog.Get(item.DefinitionId);

            if (source.Bag == _comms.Grid)
            {
                NpcDefinition contact;
                if (NpcLibrary.TryFindByUplink(definition.Id, out contact))
                {
                    ConnectNpc(contact);
                }
                else
                {
                    SetMessage("通讯中枢只收上行链路。");
                }

                return;
            }

            if (definition.Category == ItemCategory.Quest)
            {
                SetMessage("把契约拖到联系人的柜台上交付。");
                return;
            }

            if (source.Name == "NPCBAG")
            {
                SetMessage(definition.Category == ItemCategory.Quest
                    ? "把契约拖到背包或仓库，就算接单。"
                    : "拖到背包或仓库买下，" + NpcMarket.PriceOf(definition) + " bit。");
                return;
            }

            if (source.IsMerchant)
            {
                SetMessage("拖入仓库购买。");
                return;
            }

            if (source.Name == "POD")
            {
                if (_pod.SelectKey(_catalog, instanceId))
                {
                    RefreshPod();
                    RefreshDeploy();
                    SetMessage("已选择 " + definition.DisplayName + "。");
                }
                else
                {
                    SetMessage("潜网舱里只能选中密钥。");
                }

                return;
            }

            if (source.Name == "CRAFT")
            {
                SetMessage("制作站：放入材料后点击制作。");
                return;
            }

            if (source.Name == "CPU")
            {
                SetMessage("中央处理器：芯片可随时拔插，效果从下一场战斗生效。"
                           + " 当前加成 " + GameSession.CpuModifiers.Describe() + "。");
                return;
            }

            if (source.Bag != _bag)
            {
                SetMessage("只能使用背包中的物品。");
            }
            else if (ItemUse.CanUseOutsideCombat(definition))
            {
                SetMessage(definition.DisplayName + " 只能在探索中使用。");
            }
            else if (definition.Activation == ItemActivation.Passive)
            {
                SetMessage("被动物品无需使用。");
            }
            else
            {
                SetMessage("该物品只能在战斗中使用。");
            }
        }

        private void OnDragMessage(string message)
        {
            SetMessage(message);
        }

        private void OnDragChanged()
        {
            _bagView.Refresh();
            _stashView.Refresh();
            _secureView.Refresh();
            _merchantView.Refresh();
            _podView.Refresh();
            _craftView.Refresh();
            _pod.EnsureSelection(_catalog);
            RefreshHud();
            RefreshTabs();
            RefreshPod();
            RefreshCraft();
            RefreshDeploy();
            if (_containerWindows != null)
            {
                _containerWindows.RefreshAll();
            }
        }

        private string UpgradeText(int bitCost, string[] materials, bool canUpgrade)
        {
            if (!canUpgrade)
            {
                return "已满级";
            }

            return FacilityUpgrade.RequirementText(_catalog, bitCost, materials);
        }

        /// <summary>
        /// 升级失败时的提示。需求行放在底部消息栏而不是设施信息框——信息框只有 154×36，
        /// 追加「(有 N)」会把文字挤到按钮上。
        /// </summary>
        private string UpgradeFailureText(string error, int bitCost, string[] materials, bool canUpgrade)
        {
            if (!canUpgrade || string.IsNullOrEmpty(error))
            {
                return error;
            }

            return error + " 需求：" + FacilityUpgrade.RequirementText(_catalog, GameSession.Stash, bitCost, materials);
        }

        private void BuildContainerWindows()
        {
            _containerWindows = gameObject.AddComponent<FloatingWindowManager>();
            _containerWindows.Initialize(_canvasRect, _catalog, _palette);
            _containerWindows.AttachDrag(_drag);
            _drag.TryActivateItem = (source, instanceId) =>
                _containerWindows != null && _containerWindows.TryOpen(source, instanceId);
            _drag.PointerBlocked = screen =>
                _containerWindows != null && _containerWindows.IsPointerOverChrome(screen);
        }

        private void BuildEndOverlay(Transform canvas)
        {
            _endOverlay = new GameObject("EndOverlay", typeof(RectTransform), typeof(Image));
            _endOverlay.transform.SetParent(canvas, false);

            var rect = (RectTransform)_endOverlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _endOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            RectTransform box = CreateCenteredPanel(_endOverlay.transform, "Box", Vector2.zero, new Vector2(360f, 160f), PanelColor);
            _endTitle = CreateCenteredText(box, "Title", string.Empty, 26, TextColor, new Vector2(0f, 30f), new Vector2(340f, 38f));
            _endSubtitle = CreateCenteredText(box, "Subtitle", string.Empty, 10, MutedColor, new Vector2(0f, -22f), new Vector2(330f, 46f));

            _endOverlay.SetActive(false);
        }

        private Canvas CreateCanvas()
        {
            if (Camera.main == null)
            {
                var cameraGo = new GameObject("Main Camera", typeof(Camera));
                cameraGo.tag = "MainCamera";
                Camera camera = cameraGo.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BackgroundColor;
                camera.orthographic = true;
            }

            var go = new GameObject("BaseCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreateCenteredPanel(canvas.transform, "Background", Vector2.zero, new Vector2(640f, 360f), BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;
            BackgroundArt.Apply(background);

            return canvas;
        }
    }
}

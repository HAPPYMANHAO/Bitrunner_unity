using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class BasePrototypeBootstrap : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color32(18, 21, 26, 255);
        private static readonly Color PanelColor = new Color32(27, 31, 38, 255);
        private static readonly Color SellColor = new Color32(58, 46, 34, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color WarningColor = new Color32(208, 128, 80, 255);
        private static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        private static readonly Color DisabledColor = new Color32(44, 48, 56, 255);
        private static readonly Color BenchColor = new Color32(22, 26, 32, 255);
        private static readonly Color BenchRailColor = new Color32(63, 54, 43, 255);
        private static readonly Color WindowColor = new Color32(16, 20, 26, 255);
        private static readonly Color WindowScreenColor = new Color32(23, 29, 36, 255);

        private enum BaseTab
        {
            Merchant,
            Pod,
            Mine,
            Craft
        }

        private ItemCatalog _catalog;
        private CampaignState _campaign;
        private Merchant _merchant;
        private DivePod _pod;
        private BitMine _mine;
        private CraftingStation _craft;
        private BagInventory _bag;
        private BagInventory _stash;
        private Dictionary<string, Color> _palette;

        private RectTransform _canvasRect;
        private Text _hudText;
        private Text _statusText;
        private Text _progressLabel;
        private RectTransform _progressFill;
        private Text _messageText;
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

        private readonly List<BaseBagSource> _sources = new List<BaseBagSource>();
        private BaseDragInteraction _drag;
        private FloatingWindowManager _containerWindows;

        private GameObject _tooltipPanel;
        private RectTransform _tooltipRect;
        private Text _tooltipText;

        private GameObject _endOverlay;
        private Text _endTitle;
        private Text _endSubtitle;
        private bool _ended;

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
            _bag = GameSession.PlayerBag;
            _stash = GameSession.Stash;
            _secure = GameSession.Secure;
            _palette = BuildPalette(_catalog);
            _activeTab = BaseTab.Merchant;

            Canvas canvas = CreateCanvas();
            _canvasRect = (RectTransform)canvas.transform;

            BuildHud(canvas.transform);
            BuildBagPanels(canvas.transform);
            BuildRightPanel(canvas.transform);
            BuildDeploy(canvas.transform);
            BuildMessage(canvas.transform);
            BuildDrag();
            BuildTooltip(canvas.transform);
            BuildContainerWindows();
            BuildEndOverlay(canvas.transform);

            SetActiveTab(BaseTab.Merchant, true);
            RefreshAll();

            if (!_campaign.IsActive)
            {
                ShowEnd();
            }
        }

        private void Update()
        {
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
                        RebuildSources();
                        RefreshAll();
                        SetMessage("潜网舱升级到 Lv." + _pod.Level + "。");
                    }
                    else
                    {
                        SetMessage(error);
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
                        SetMessage(error);
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
                        RebuildSources();
                        RefreshAll();
                        SetMessage("制作站升级到 Lv." + _craft.Level + "。");
                    }
                    else
                    {
                        SetMessage(error);
                    }

                    return true;
                }
            }

            return false;
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
            RefreshMineCells();
            RefreshHud();
            RefreshTabs();
            RefreshPod();
            RefreshMine();
            RefreshCraft();
            RefreshDeploy();

            if (_messageText != null)
            {
                _messageText.text = GameSession.LastMessage;
            }
        }

        private void RefreshTabs()
        {
            if (_tabLabels == null)
            {
                return;
            }

            _tabLabels[0].text = "商人 " + _merchant.Stock.Count;
            _tabLabels[1].text = "潜网舱";
            _tabLabels[2].text = "矿场";
            _tabLabels[3].text = "制作站";

            for (int i = 0; i < _tabImages.Length; i++)
            {
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
            _craftInfo.text = "Lv." + _craft.Level + "  "
                              + _craft.Grid.Grid.Width + "×" + _craft.Grid.Grid.Height
                              + "\n配方：" + (recipe != null ? recipe.Name : "无匹配")
                              + "\n升级：" + UpgradeText(_craft.UpgradeCost, _craft.UpgradeMaterialIds, _craft.CanUpgrade);

            bool canCraft = recipe != null;
            _craftCraftText.text = canCraft ? "制作" : "材料不足";
            _craftCraftImage.color = canCraft ? AccentColor : DisabledColor;

            bool max = !_craft.CanUpgrade;
            _craftUpgradeText.text = max ? "已满级" : "升级";
            _craftUpgradeImage.color = max ? DisabledColor : AccentColor;
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
                _deployText.text = "需要潜网密钥";
                return;
            }

            _deployImage.color = AccentColor;
            _deployText.text = "下潜 · " + plan.Name + " · 第 " + _campaign.Day + "/7 天";
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
                _sources.Add(new BaseBagSource { Name = "POD", Bag = _pod.Grid, Rect = _podRect, View = _podView });
            }
            else if (_activeTab == BaseTab.Craft)
            {
                _sources.Add(new BaseBagSource { Name = "CRAFT", Bag = _craft.Grid, Rect = _craftRect, View = _craftView });
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
            _hudText.text = "第 " + _campaign.Week + " 周"
                            + "\n应缴 " + _campaign.CurrentDue + "    bit " + _campaign.Bit
                            + "\n负债 " + _campaign.DebtRemaining;

            _hudText.color = _campaign.IsActive && _campaign.Bit < _campaign.CurrentDue
                ? WarningColor
                : TextColor;

            if (_campaign.IsActive)
            {
                _statusText.text = "距缴款 " + _campaign.DaysRemaining + " 次探索"
                                   + "\n背包 " + _bag.FreeCellCount + " 格 · 仓库 " + _stash.FreeCellCount + " 格";
            }
            else
            {
                _statusText.text = _campaign.Status == CampaignStatus.Victory
                    ? "债务已清"
                    : "游戏结束";
            }

            if (_secureTitle != null)
            {
                _secureTitle.text = "保险箱 " + _secure.Count + "/4";
            }

            if (_progressFill != null && _progressLabel != null)
            {
                float ratio = _campaign.CurrentDue <= 0
                    ? 1f
                    : Mathf.Clamp01((float)_campaign.Bit / _campaign.CurrentDue);
                _progressFill.sizeDelta = new Vector2(238f * ratio, 6f);
                _progressFill.GetComponent<Image>().color = ratio >= 1f ? AccentColor : WarningColor;
                _progressLabel.text = "已筹 " + _campaign.Bit + " / " + _campaign.CurrentDue + " bit";
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
                _endTitle.text = "债务已清";
                _endSubtitle.text = "你还清了 " + _campaign.TotalDebt + " bit。\n点击开始新游戏。";
            }
            else
            {
                _endTitle.text = "游戏结束";
                _endSubtitle.text = "债务到期了。\n点击开始新游戏。";
            }
        }

        private void SetMessage(string text)
        {
            if (_messageText != null)
            {
                _messageText.text = text;
            }
        }

        private void BuildHud(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Hud", new Vector2(-227f, 148f), new Vector2(170f, 56f), PanelColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            _hudText = CreateText(panel, "Stats", string.Empty, 10, TextColor, new Vector2(10f, -6f), new Vector2(150f, 46f), TextAnchor.UpperLeft);
        }

        private void BuildBagPanels(Transform canvas)
        {
            RectTransform bench = CreateCenteredPanel(canvas, "Workbench", new Vector2(-30f, -104f), new Vector2(380f, 132f), BenchColor);
            bench.GetComponent<Image>().raycastTarget = false;
            AddBorder(bench, BenchRailColor);
            RectTransform benchRail = CreateAnchoredPanel(bench, "TopRail", Vector2.zero, new Vector2(380f, 6f), BenchRailColor);
            benchRail.GetComponent<Image>().raycastTarget = false;
            CreateText(bench, "BenchLabel", "BASE / WORKBENCH", 7, new Color32(120, 104, 78, 255), new Vector2(268f, -10f), new Vector2(104f, 10f), TextAnchor.UpperLeft);

            RectTransform bagPanel = CreateCenteredPanel(canvas, "PackPanel", new Vector2(-160f, -108f), new Vector2(110f, 124f), PanelColor);
            bagPanel.gameObject.AddComponent<RectMask2D>();
            CreateText(bagPanel, "Title", "背包", 10, TextColor, new Vector2(8f, -6f), new Vector2(124f, 14f), TextAnchor.UpperLeft);

            _bagRect = CreateBagRect(bagPanel, new Vector2(0f, 2f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _bagView = _bagRect.gameObject.AddComponent<BagView>();
            _bagView.Initialize(_bag, _bagRect, _palette);

            RectTransform stashPanel = CreateCenteredPanel(canvas, "StashPanel", new Vector2(-35f, -108f), new Vector2(150f, 124f), PanelColor);
            stashPanel.gameObject.AddComponent<RectMask2D>();
            CreateText(stashPanel, "Title", "仓库", 10, TextColor, new Vector2(8f, -6f), new Vector2(134f, 14f), TextAnchor.UpperLeft);

            _stashViewport = CreateCenteredPanel(stashPanel, "StashViewport", new Vector2(0f, -6f), new Vector2(6f * BagView.CellSize, 96f), Color.clear);
            var viewportImage = _stashViewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.raycastTarget = false;
            }

            _stashViewport.gameObject.AddComponent<RectMask2D>();
            _stashRect = CreateBagRect(_stashViewport, Vector2.zero, new Vector2(6f * BagView.CellSize, _stash.Grid.Height * BagView.CellSize));
            _stashView = _stashRect.gameObject.AddComponent<BagView>();
            _stashView.Initialize(_stash, _stashRect, _palette);

            RectTransform scrollbarRect = CreateCenteredPanel(stashPanel, "StashScrollbar", new Vector2(65f, -6f), new Vector2(8f, 96f), new Color32(38, 44, 54, 255));
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

            RectTransform securePanel = CreateCenteredPanel(canvas, "SecurePanel", new Vector2(120f, -135f), new Vector2(80f, 70f), PanelColor);
            _secureTitle = CreateText(securePanel, "Title", "保险箱 0/4", 9, TextColor, new Vector2(6f, -4f), new Vector2(68f, 12f), TextAnchor.UpperLeft);
            _secureRect = CreateBagRect(securePanel, new Vector2(0f, -8f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _secureView = _secureRect.gameObject.AddComponent<BagView>();
            _secureView.Initialize(_secure, _secureRect, _palette);
        }

        private void BuildRightPanel(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "RightPanel", new Vector2(230f, 45f), new Vector2(170f, 160f), PanelColor);
            AddBorder(panel, new Color32(67, 59, 47, 255));
            panel.gameObject.AddComponent<RectMask2D>();

            _tabContents = new GameObject[4];
            _tabRects = new RectTransform[4];
            _tabLabels = new Text[4];
            _tabImages = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                RectTransform tab = CreateAnchoredPanel(
                    panel,
                    "Tab_" + i,
                    new Vector2(4f + i * 40f, -2f),
                    new Vector2(38f, 18f),
                    new Color32(38, 44, 52, 255));
                _tabRects[i] = tab;
                _tabImages[i] = tab.GetComponent<Image>();
                _tabLabels[i] = CreateCenteredText(tab, "Label", string.Empty, 7, TextColor, Vector2.zero, new Vector2(36f, 16f));
            }

            RectTransform content = CreateAnchoredRect(panel, "Content", new Vector2(0f, -22f), new Vector2(170f, 138f));
            for (int i = 0; i < 4; i++)
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

            BuildMerchantContent(_tabContents[0].transform);
            BuildPodContent(_tabContents[1].transform);
            BuildMineContent(_tabContents[2].transform);
            BuildCraftContent(_tabContents[3].transform);
        }

        private void BuildMerchantContent(Transform content)
        {
            _merchantRect = CreateBagRect(content, new Vector2(0f, 0f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _merchantView = _merchantRect.gameObject.AddComponent<BagView>();
            _merchantView.Initialize(_merchant.Stock, _merchantRect, _palette);

            _discardBinRect = CreateAnchoredPanel(content, "DiscardBin", new Vector2(46f, -108f), new Vector2(78f, 22f), SellColor);
            CreateCenteredText(_discardBinRect, "Label", "丢弃", 9, MutedColor, Vector2.zero, new Vector2(84f, 22f));
        }

        private void BuildPodContent(Transform content)
        {
            _podRect = CreateBagRect(content, new Vector2(0f, 15f), new Vector2(3f * BagView.CellSize, 3f * BagView.CellSize));
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
            _craftRect = CreateBagRect(content, new Vector2(0f, 15f), new Vector2(3f * BagView.CellSize, 3f * BagView.CellSize));
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

        private void BuildDeploy(Transform canvas)
        {
            _deployRect = CreateCenteredPanel(canvas, "Deploy", new Vector2(215f, 152f), new Vector2(160f, 28f), AccentColor);
            AddBorder(_deployRect, new Color32(96, 142, 186, 255));
            _deployImage = _deployRect.GetComponent<Image>();
            _deployText = CreateCenteredText(_deployRect, "Label", "出征", 11, TextColor, Vector2.zero, new Vector2(156f, 26f));
        }

        private void BuildMessage(Transform canvas)
        {
            RectTransform window = CreateCenteredPanel(canvas, "ViewWindow", new Vector2(0f, 68f), new Vector2(270f, 150f), WindowColor);
            AddBorder(window, new Color32(67, 59, 47, 255));

            CreateText(window, "Header", "基础潜网 // BASE LINK", 9, new Color32(164, 146, 112, 255), new Vector2(10f, -5f), new Vector2(240f, 12f), TextAnchor.UpperLeft);

            RectTransform screen = CreateAnchoredPanel(window, "Screen", new Vector2(8f, -22f), new Vector2(254f, 120f), WindowScreenColor);
            screen.GetComponent<Image>().raycastTarget = false;

            RectTransform rule = CreateAnchoredPanel(screen, "Rule", new Vector2(8f, -46f), new Vector2(238f, 1f), new Color32(46, 53, 63, 255));
            rule.GetComponent<Image>().raycastTarget = false;

            _statusText = CreateText(screen, "Status", string.Empty, 10, TextColor, new Vector2(8f, -8f), new Vector2(238f, 34f), TextAnchor.UpperLeft);
            _messageText = CreateText(screen, "Message", "欢迎回到基础潜网。", 8, MutedColor, new Vector2(8f, -56f), new Vector2(238f, 32f), TextAnchor.UpperLeft);

            _progressLabel = CreateText(screen, "ProgressLabel", string.Empty, 8, MutedColor, new Vector2(8f, -92f), new Vector2(238f, 10f), TextAnchor.UpperLeft);
            RectTransform progressBg = CreateAnchoredPanel(screen, "ProgressBg", new Vector2(8f, -106f), new Vector2(238f, 6f), new Color32(38, 44, 54, 255));
            progressBg.GetComponent<Image>().raycastTarget = false;
            _progressFill = CreateAnchoredPanel(progressBg, "Fill", Vector2.zero, new Vector2(0f, 6f), AccentColor);
            _progressFill.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildDrag()
        {
            RebuildSources();

            _drag = gameObject.AddComponent<BaseDragInteraction>();
            _drag.Initialize(_sources, _catalog, _merchant, _campaign, _palette, _canvasRect, null, _discardBinRect);
            _drag.Message += OnDragMessage;
            _drag.Changed += OnDragChanged;
            _drag.ItemClicked += OnItemClicked;
            _drag.QuickMoveTarget = (source, item) =>
            {
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


        private void UpdateSynergyHighlights(
            IReadOnlyList<BaseBagSource> sources,
            BaseBagSource hovered,
            PlacedItem item)
        {
            if (hovered == null || item == null)
            {
                ClearSynergyHighlights(sources);
                return;
            }

            IReadOnlyList<int> linked = SynergyResolver.FindLinked(
                hovered.Bag,
                _catalog,
                SynergyResolver.DefaultRules,
                item.InstanceId);

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == null || sources[i].View == null)
                {
                    continue;
                }

                if (sources[i] == hovered)
                {
                    sources[i].View.SetHighlightedItems(linked);
                }
                else
                {
                    sources[i].View.ClearHighlights();
                }
            }
        }

        private static void ClearSynergyHighlights(IReadOnlyList<BaseBagSource> sources)
        {
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null && sources[i].View != null)
                {
                    sources[i].View.ClearHighlights();
                }
            }
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
                ClearSynergyHighlights(tooltipSources);
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
                ClearSynergyHighlights(tooltipSources);
                return;
            }

            UpdateSynergyHighlights(tooltipSources, hoveredSource, hoveredItem);
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
            float tooltipHeight = Mathf.Clamp(_tooltipText.preferredHeight + 14f, 120f, 250f);
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

            return canvas;
        }

        private static void AddBorder(RectTransform rect, Color color)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        private static RectTransform CreateAnchoredRect(Transform parent, string name, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform CreateButton(Transform parent, string name, string label, Vector2 topLeft, Vector2 size, Color color)
        {
            RectTransform rect = CreateAnchoredPanel(parent, name, topLeft, size, color);
            CreateCenteredText(rect, "Label", label, 9, TextColor, Vector2.zero, new Vector2(size.x - 4f, size.y - 2f));
            return rect;
        }

        private static RectTransform CreateBagRect(Transform parent, Vector2 center)
        {
            return CreateBagRect(parent, center, new Vector2(192f, 160f));
        }

        private static RectTransform CreateBagRect(Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject("Bag", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform CreateCenteredPanel(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static RectTransform CreateAnchoredPanel(Transform parent, string name, Vector2 topLeft, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateCenteredText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Font GetFont()
        {
            return GameFonts.Ui;
        }

        private static Dictionary<string, Color> BuildPalette(ItemCatalog catalog)
        {
            var palette = new Dictionary<string, Color>();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                palette[definition.Id] = ColorFor(definition.Category);
            }

            return palette;
        }

        private static Color ColorFor(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon:
                    return new Color32(232, 110, 64, 255);
                case ItemCategory.Armor:
                    return new Color32(80, 140, 220, 255);
                case ItemCategory.Tool:
                    return new Color32(79, 184, 200, 255);
                case ItemCategory.Consumable:
                    return new Color32(90, 200, 130, 255);
                case ItemCategory.Loot:
                    return new Color32(220, 200, 70, 255);
                case ItemCategory.Material:
                    return new Color32(150, 128, 100, 255);
                case ItemCategory.Key:
                    return new Color32(190, 120, 235, 255);
                case ItemCategory.Currency:
                    return new Color32(230, 196, 70, 255);
                case ItemCategory.Ammo:
                    return new Color32(200, 170, 120, 255);
                case ItemCategory.Container:
                    return new Color32(150, 118, 82, 255);
                default:
                    return new Color32(140, 150, 160, 255);
            }
        }
    }
}

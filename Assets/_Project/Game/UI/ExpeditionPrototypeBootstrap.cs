using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class ExpeditionPrototypeBootstrap : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color32(18, 21, 26, 255);
        private static readonly Color PanelColor = new Color32(27, 31, 38, 255);
        private static readonly Color SlotColor = new Color32(16, 19, 24, 255);
        private static readonly Color SlotHoverColor = new Color32(34, 40, 49, 255);
        private static readonly Color SelectedColor = new Color32(74, 60, 32, 255);
        private static readonly Color DisabledColor = new Color32(23, 26, 31, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color LogColor = new Color32(123, 196, 127, 255);
        private static readonly Color SystemColor = new Color32(127, 199, 217, 255);
        private static readonly Color DangerColor = new Color32(226, 107, 91, 255);
        private static readonly Color LootColor = new Color32(220, 200, 70, 255);
        private static readonly Color HpColor = new Color32(214, 92, 82, 255);
        private static readonly Color BagColor = new Color32(91, 160, 210, 255);
        private static readonly Color AlertColor = new Color32(224, 164, 79, 255);
        private static readonly Color ConfirmColor = new Color32(58, 74, 70, 255);
        private static readonly Color ConfirmReadyColor = new Color32(94, 132, 103, 255);

        private ExpeditionScenario _scenario;
        private ItemCatalog _catalog;
        private BagInventory _bag;
        private BagInventory _secure;
        private Dictionary<string, Color> _palette;

        private RectTransform _canvasRect;
        private RectTransform _sellBinRect;
        private Text _titleText;
        private Text _narrationText;
        private Text _logText;
        private Text _detailText;
        private Text _selectedLabel;
        private Text _selectedRisk;
        private Text _confirmText;
        private RectTransform _confirmRect;
        private RectTransform _bagButtonRect;

        private Image _hpFill;
        private Image _bagFill;
        private Image _alertFill;
        private Text _hpValue;
        private Text _bagValue;
        private Text _alertValue;
        private Text _stageValue;
        private Text _dayValue;

        private readonly List<string> _logLines = new List<string>();
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
        private BaseDragInteraction _actionDrag;
        private string _selectedActionId;
        private bool _showPlayerBag;
        private bool _ended;

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

        private GameObject _lootOverlay;
        private RectTransform _lootCloseRect;
        private RectTransform _lootDiscardRect;
        private RectTransform _lootBoxPanel;
        private RectTransform _lootBoxRect;
        private BagView _lootBoxView;
        private RectTransform _lootPlayerRect;
        private BagView _lootPlayerView;
        private RectTransform _lootSecureRect;
        private BagView _lootSecureView;
        private Text _lootHint;
        private Text _lootItemInfo;
        private BaseDragInteraction _lootDrag;
        private FloatingWindowManager _containerWindows;
        private BagInventory _lootBag;
        private int _lastPlayerItemCount;
        private bool _lootOpen;

        private GameObject _itemTooltipPanel;
        private RectTransform _itemTooltipRect;
        private Text _itemTooltipText;

        private void Start()
        {
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
                SceneManager.LoadScene("BasePrototype");
                return;
            }

            Canvas canvas = CreateCanvas();
            _canvasRect = (RectTransform)canvas.transform;

            BuildNarrative(canvas.transform);
            BuildStatus(canvas.transform);
            BuildActions(canvas.transform);
            BuildSelection(canvas.transform);
            BuildActionDrag();
            BuildLog(canvas.transform);
            BuildBagOverlay(canvas.transform);
            BuildLootOverlay(canvas.transform);
            BuildItemTooltip(canvas.transform);
            BuildContainerWindows();

            _titleText.text = _scenario.Title;
            RebuildActionBags();
            RefreshAll();
            string opening = _scenario.Stage == 0
                ? _scenario.IntroText
                : string.IsNullOrEmpty(GameSession.LastMessage)
                    ? _scenario.IntroText
                    : GameSession.LastMessage;
            SetNarration(opening);
            AppendLog(string.IsNullOrEmpty(GameSession.LastMessage) ? opening : GameSession.LastMessage);
        }

        private void Update()
        {
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
                if (mouse != null && mouse.leftButton.wasPressedThisFrame
                    && RectTransformUtility.RectangleContainsScreenPoint(_lootCloseRect, screen, null))
                {
                    CloseLoot();
                    return;
                }

                UpdateLootHover(screen);
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

            if (RectTransformUtility.RectangleContainsScreenPoint(_bagButtonRect, screen, null))
            {
                OpenBag();
            }
        }

        private void ConfirmSelected()
        {
            if (_ended)
            {
                return;
            }

            IReadOnlyList<PlacedItem> items = _selectedActionBag != null ? _selectedActionBag.Items : null;
            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                _scenario,
                items,
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

            ExpeditionResolution result = GameSession.ResolveExpeditionAction(action.Id);
            if (result == null)
            {
                SetNarration("这个行动已经无法执行。");
                return;
            }

            _selectedActionId = null;
            RebuildActionBags();
            AppendLog(result.Text);
            SetNarration(result.Text);
            RefreshAll();

            if (result.Kind == ExpeditionOutcomeKind.Damage && GameSession.ExpeditionHp <= 0)
            {
                _ended = true;
                GameSession.CompleteExpedition(false);
                SceneManager.LoadScene("BasePrototype");
                return;
            }

            if (result.Kind == ExpeditionOutcomeKind.Combat)
            {
                _ended = true;
                SceneManager.LoadScene("CombatPrototype");
                return;
            }

            if (result.IsExit)
            {
                _ended = true;
                GameSession.CompleteExpedition(true);
                SceneManager.LoadScene("BasePrototype");
                return;
            }

            if (result.LootIds != null && result.LootIds.Length > 0)
            {
                OpenLoot(result.LootIds);
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

            if (_lootPlayerView != null)
            {
                _lootPlayerView.Refresh();
            }

            if (_lootSecureView != null)
            {
                _lootSecureView.Refresh();
            }

            if (_bagSecureTitle != null)
            {
                _bagSecureTitle.text = "保险箱 " + _secure.Count + "/4";
            }

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

            _hpValue.text = GameSession.ExpeditionHp + "/" + GameSession.ExpeditionMaxHp;
            _bagValue.text = _bag.FreeCellCount + " 格";
            _alertValue.text = _scenario.Alert.ToString();
            _stageValue.text = _scenario.Stage + "/" + ExpeditionScenario.MaxStages;
            _dayValue.text = GameSession.Campaign.Day + "/7";

            SetFill(_hpFill, GameSession.ExpeditionHp / (float)GameSession.ExpeditionMaxHp, HpColor);
            SetFill(_bagFill, _bag.FreeCellCount / (float)(_bag.Grid.Width * _bag.Grid.Height), BagColor);
            SetFill(_alertFill, _scenario.Alert / 4f, AlertColor);
        }

        private static void SetFill(Image fill, float ratio, Color color)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform rect = (RectTransform)fill.transform;
            float max = rect.parent != null ? ((RectTransform)rect.parent).sizeDelta.x - 8f : 52f;
            rect.sizeDelta = new Vector2(Mathf.Clamp01(ratio) * max, rect.sizeDelta.y);
            fill.color = color;
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

            _selectedActionBag = new BagInventory(2, 2);
            if (_actionView != null)
            {
                _actionView.Rebind(_actionBag);
            }

            if (_selectedActionView != null)
            {
                _selectedActionView.Rebind(_selectedActionBag);
            }

            if (_actionDrag != null)
            {
                _actionDrag.SetSources(ActionSources());
            }

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
                bool isAction = _scenario != null && _scenario.Find(item.DefinitionId) != null;
                return _actionDrag.FindSource(isAction ? "ACTIONS" : "PACK");
            }

            if (source.Name != "ACTIONS" && source.Name != "PACK")
            {
                return null;
            }

            if (_selectedActionBag == null || _selectedActionBag.Count > 0)
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
            if (_selectedActionBag != null && _selectedActionBag.Count > 0)
            {
                _selectedActionId = _selectedActionBag.Items[0].DefinitionId;
            }
            else
            {
                _selectedActionId = null;
            }

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
            IReadOnlyList<PlacedItem> items = _selectedActionBag != null ? _selectedActionBag.Items : null;
            ExpeditionSelection selection = ExpeditionSelectionRules.Evaluate(
                _scenario,
                items,
                _combinedCatalog,
                GameSession.ExpeditionHp,
                GameSession.ExpeditionMaxHp);

            if (selection.IsAction && selection.Action != null)
            {
                ExpeditionActionOption action = selection.Action;
                _selectedLabel.text = action.Label;
                _selectedLabel.color = TextColor;
                _selectedRisk.text = "风险：" + ExpeditionText.RiskName(action.Risk);
                _selectedRisk.color = RiskColor(action.Risk);
                _detailText.text = "风险：" + ExpeditionText.RiskName(action.Risk)
                                   + "\n" + action.Description
                                   + "\n可能的回声：\n" + OutcomeSummary(action);
                _confirmText.text = action.IsExit ? "确认撤离" : "确认行动";
                _confirmText.color = TextColor;
                _confirmRect.GetComponent<Image>().color = ConfirmReadyColor;
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
                                   + "这会推进一个探索阶段，并消耗该物品。";
                _confirmText.text = "确认使用";
                _confirmText.color = TextColor;
                _confirmRect.GetComponent<Image>().color = ConfirmReadyColor;
                return;
            }

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
                _selectedLabel.text = "拖入一个物品";
                _selectedLabel.color = MutedColor;
                _selectedRisk.text = selection.Reason;
                _selectedRisk.color = DangerColor;
                _detailText.text = selection.Reason;
            }
            _confirmText.text = "确认";
            _confirmText.color = MutedColor;
            _confirmRect.GetComponent<Image>().color = ConfirmColor;
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

        private void BuildNarrative(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Narrative", new Vector2(25f, 128f), new Vector2(510f, 64f), PanelColor);
            _titleText = CreateText(panel, "Title", string.Empty, 11, TextColor, new Vector2(10f, -8f), new Vector2(490f, 14f), TextAnchor.UpperLeft);
            _narrationText = CreateText(panel, "Body", string.Empty, 9, MutedColor, new Vector2(10f, -26f), new Vector2(490f, 32f), TextAnchor.UpperLeft);
            _narrationText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuildStatus(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Status", new Vector2(-288f, 18f), new Vector2(64f, 242f), PanelColor);
            CreateText(panel, "Title", "状态", 9, TextColor, new Vector2(8f, -7f), new Vector2(48f, 12f), TextAnchor.UpperLeft);

            _hpValue = AddStatusRow(panel, "生命", -26f, HpColor, out _hpFill);
            _bagValue = AddStatusRow(panel, "背包", -72f, BagColor, out _bagFill);
            _alertValue = AddStatusRow(panel, "警戒", -118f, AlertColor, out _alertFill);
            _stageValue = AddStatusValue(panel, "阶段", -164f);
            _dayValue = AddStatusValue(panel, "天数", -202f);
        }

        private Text AddStatusRow(RectTransform panel, string label, float top, Color color, out Image fill)
        {
            CreateText(panel, label, label, 8, MutedColor, new Vector2(7f, top), new Vector2(50f, 10f), TextAnchor.UpperLeft);
            fill = CreateFill(panel, new Vector2(7f, top - 14f), new Vector2(50f, 4f), color);
            return CreateText(panel, label + "Value", string.Empty, 7, color, new Vector2(7f, top - 22f), new Vector2(50f, 10f), TextAnchor.UpperLeft);
        }

        private Text AddStatusValue(RectTransform panel, string label, float top)
        {
            CreateText(panel, label, label, 8, MutedColor, new Vector2(7f, top), new Vector2(50f, 10f), TextAnchor.UpperLeft);
            return CreateText(panel, label + "Value", string.Empty, 8, TextColor, new Vector2(7f, top - 13f), new Vector2(50f, 10f), TextAnchor.UpperLeft);
        }

        private Image CreateFill(RectTransform parent, Vector2 topLeft, Vector2 size, Color color)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;

            var background = new GameObject("Track", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            var bg = (RectTransform)background.transform;
            bg.SetSiblingIndex(go.transform.GetSiblingIndex());
            bg.anchorMin = new Vector2(0f, 1f);
            bg.anchorMax = new Vector2(0f, 1f);
            bg.pivot = new Vector2(0f, 1f);
            bg.anchoredPosition = new Vector2(topLeft.x - 1f, topLeft.y + 1f);
            bg.sizeDelta = new Vector2(size.x + 2f, size.y + 2f);
            background.GetComponent<Image>().color = new Color32(11, 13, 17, 255);

            go.transform.SetAsLastSibling();
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private void BuildActions(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "Actions", new Vector2(-90f, 25f), new Vector2(240f, 200f), PanelColor);
            CreateText(panel, "Title", "可选行动", 10, TextColor, new Vector2(10f, -7f), new Vector2(150f, 14f), TextAnchor.UpperLeft);
            CreateText(panel, "Hint", "拖动 / 双击 / Shift+左键", 8, MutedColor, new Vector2(10f, -22f), new Vector2(150f, 10f), TextAnchor.UpperLeft);

            _bagButtonRect = CreateCenteredPanel(panel, "BagButton", new Vector2(84f, 83f), new Vector2(56f, 18f), new Color32(42, 48, 57, 255));
            CreateCenteredText(_bagButtonRect, "Label", "背包 B", 8, TextColor, Vector2.zero, new Vector2(54f, 16f));

            _actionBag = new BagInventory(6, 4);
            _actionBagRect = CreateBagRect(panel, new Vector2(0f, -10f), new Vector2(6f * BagView.CellSize, 4f * BagView.CellSize));
            _actionView = _actionBagRect.gameObject.AddComponent<BagView>();
            _actionView.Initialize(_actionBag, _actionBagRect, _actionPalette, _combinedCatalog);

            _playerActionBagRect = CreateBagRect(panel, new Vector2(0f, -10f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _playerActionView = _playerActionBagRect.gameObject.AddComponent<BagView>();
            _playerActionView.Initialize(_bag, _playerActionBagRect, _actionPalette, _combinedCatalog);
            _playerActionView.gameObject.SetActive(false);

            _tabActionRect = CreateCenteredPanel(panel, "TabAction", new Vector2(-70f, -106f), new Vector2(58f, 18f), SelectedColor);
            CreateCenteredText(_tabActionRect, "Label", "行动", 8, TextColor, Vector2.zero, new Vector2(54f, 16f));
            _tabBagRect = CreateCenteredPanel(panel, "TabBag", new Vector2(-8f, -106f), new Vector2(58f, 18f), new Color32(42, 48, 57, 255));
            CreateCenteredText(_tabBagRect, "Label", "背包", 8, MutedColor, Vector2.zero, new Vector2(54f, 16f));
        }

        private void BuildSelection(Transform canvas)
        {
            RectTransform selected = CreateCenteredPanel(canvas, "Selected", new Vector2(125f, 38f), new Vector2(180f, 130f), PanelColor);
            CreateCenteredText(selected, "Title", "已选行动", 9, TextColor, new Vector2(0f, 54f), new Vector2(164f, 12f));

            _selectedActionBag = new BagInventory(2, 2);
            _selectedActionBagRect = CreateBagRect(selected, new Vector2(-52f, 15f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _selectedActionView = _selectedActionBagRect.gameObject.AddComponent<BagView>();
            _selectedActionView.Initialize(_selectedActionBag, _selectedActionBagRect, _actionPalette, _combinedCatalog);

            _selectedLabel = CreateText(selected, "Label", "拖入一个行动", 9, MutedColor, new Vector2(70f, -20f), new Vector2(92f, 12f), TextAnchor.UpperLeft);
            _selectedRisk = CreateText(selected, "Risk", string.Empty, 7, MutedColor, new Vector2(70f, -35f), new Vector2(92f, 10f), TextAnchor.UpperLeft);

            _confirmRect = CreateCenteredPanel(selected, "Confirm", new Vector2(0f, -46f), new Vector2(150f, 22f), ConfirmColor);
            _confirmText = CreateCenteredText(_confirmRect, "Label", "确认", 8, MutedColor, Vector2.zero, new Vector2(146f, 20f));

            RectTransform detail = CreateCenteredPanel(canvas, "Detail", new Vector2(125f, -65f), new Vector2(180f, 110f), PanelColor);
            CreateText(detail, "Title", "行动详情", 9, TextColor, new Vector2(8f, -7f), new Vector2(164f, 12f), TextAnchor.UpperLeft);
            _detailText = CreateText(detail, "Body", string.Empty, 8, MutedColor, new Vector2(8f, -24f), new Vector2(164f, 78f), TextAnchor.UpperLeft);
            _detailText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuildContainerWindows()
        {
            _containerWindows = gameObject.AddComponent<FloatingWindowManager>();
            _containerWindows.Initialize(_canvasRect, _catalog, _palette);
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
                _canvasRect,
                null,
                null);
            _actionDrag.Changed += OnActionBagChanged;
            _actionDrag.ItemClicked += OnActionItemClicked;
            _actionDrag.Message += SetNarration;
            _actionDrag.QuickMoveTarget = ActionQuickMoveTarget;
        }

        private void BuildLog(Transform canvas)
        {            RectTransform panel = CreateCenteredPanel(canvas, "Log", new Vector2(-125f, -148f), new Vector2(350f, 44f), new Color32(9, 12, 16, 255));
            _logText = CreateText(panel, "Lines", string.Empty, 8, LogColor, new Vector2(8f, -5f), new Vector2(334f, 36f), TextAnchor.UpperLeft);
            _logText.verticalOverflow = VerticalWrapMode.Truncate;
            CreateCenteredText(canvas, "Help", "拖动行动 · 空格确认 · B 背包", 7, MutedColor, new Vector2(155f, -170f), new Vector2(240f, 10f));
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

            _bagDiscardRect = CreateCenteredPanel(box, "Discard", new Vector2(100f, -116f), new Vector2(120f, 24f), new Color32(58, 46, 34, 255));
            CreateCenteredText(_bagDiscardRect, "Label", "丢弃区", 8, MutedColor, Vector2.zero, new Vector2(116f, 22f));

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
            _bagDrag.Initialize(sources, _catalog, GameSession.Merchant, GameSession.Campaign, _palette, _canvasRect, _sellBinRect, _bagDiscardRect);
            _bagDrag.ItemUsed += OnUseItem;
            _bagDrag.Changed += OnBagChanged;
            _bagDrag.Message += SetNarration;
            _bagOverlay.SetActive(false);
        }

        private void BuildLootOverlay(Transform canvas)
        {
            _lootOverlay = new GameObject("LootOverlay", typeof(RectTransform), typeof(Image));
            _lootOverlay.transform.SetParent(canvas, false);
            var root = (RectTransform)_lootOverlay.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            _lootOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
            _lootOverlay.GetComponent<Image>().raycastTarget = true;

            RectTransform box = CreateCenteredPanel(_lootOverlay.transform, "Box", Vector2.zero, new Vector2(600f, 300f), PanelColor);
            CreateCenteredText(box, "Title", "发现物资", 14, TextColor, new Vector2(0f, 126f), new Vector2(560f, 20f));
            _lootHint = CreateCenteredText(box, "Hint", string.Empty, 8, MutedColor, new Vector2(0f, 112f), new Vector2(500f, 12f));
            _lootItemInfo = CreateCenteredText(box, "ItemInfo", string.Empty, 8, TextColor, new Vector2(-80f, -128f), new Vector2(420f, 12f));

            _lootBoxPanel = CreateCenteredPanel(box, "LootBox", new Vector2(-205f, -10f), new Vector2(150f, 150f), new Color32(20, 24, 30, 255));
            CreateText(_lootBoxPanel, "Title", "容器", 9, TextColor, new Vector2(8f, -6f), new Vector2(134f, 12f), TextAnchor.UpperLeft);

            RectTransform playerPanel = CreateCenteredPanel(box, "PlayerBag", new Vector2(-20f, -10f), new Vector2(220f, 200f), new Color32(20, 24, 30, 255));
            CreateText(playerPanel, "Title", "携带", 9, TextColor, new Vector2(8f, -6f), new Vector2(200f, 12f), TextAnchor.UpperLeft);
            _lootPlayerRect = CreateBagRect(playerPanel, new Vector2(0f, -14f), new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _lootPlayerView = _lootPlayerRect.gameObject.AddComponent<BagView>();
            _lootPlayerView.Initialize(_bag, _lootPlayerRect, _palette);

            RectTransform securePanel = CreateCenteredPanel(box, "SecureBag", new Vector2(165f, 24f), new Vector2(150f, 160f), new Color32(20, 24, 30, 255));
            CreateText(securePanel, "Title", "保险箱", 9, TextColor, new Vector2(8f, -6f), new Vector2(134f, 12f), TextAnchor.UpperLeft);
            _lootSecureRect = CreateBagRect(securePanel, new Vector2(0f, -16f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _lootSecureView = _lootSecureRect.gameObject.AddComponent<BagView>();
            _lootSecureView.Initialize(_secure, _lootSecureRect, _palette);

            _lootDiscardRect = CreateCenteredPanel(box, "Discard", new Vector2(165f, -100f), new Vector2(120f, 24f), new Color32(58, 46, 34, 255));
            CreateCenteredText(_lootDiscardRect, "Label", "丢弃区", 8, MutedColor, Vector2.zero, new Vector2(116f, 22f));

            _lootCloseRect = CreateCenteredPanel(box, "Close", new Vector2(215f, -126f), new Vector2(90f, 24f), new Color32(44, 48, 56, 255));
            CreateCenteredText(_lootCloseRect, "Label", "关闭", 9, TextColor, Vector2.zero, new Vector2(86f, 22f));

            _lootOverlay.SetActive(false);
        }

        private void OpenBag()
        {
            if (_bagOpen || _lootOpen || _ended)
            {
                return;
            }

            _bagOpen = true;
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

        private void OpenLoot(string[] lootIds)
        {
            _lootOpen = true;
            _lootOverlay.SetActive(true);
            if (_actionDrag != null)
            {
                _actionDrag.enabled = false;
            }

            if (_lootBoxRect != null)
            {
                Destroy(_lootBoxRect.gameObject);
            }

            _lootBag = new BagInventory(4, 3);
            for (int i = 0; i < lootIds.Length; i++)
            {
                ItemDefinition definition = _catalog.Get(lootIds[i]);
                if (definition == null)
                {
                    continue;
                }

                PlacedItem placed;
                _lootBag.TryAdd(lootIds[i], definition.Shape, out placed);
            }

            _lootBoxRect = CreateBagRect(_lootBoxPanel, new Vector2(0f, -18f), new Vector2(4f * BagView.CellSize, 3f * BagView.CellSize));
            _lootBoxView = _lootBoxRect.gameObject.AddComponent<BagView>();
            _lootBoxView.Initialize(_lootBag, _lootBoxRect, _palette);
            _lootBoxView.Refresh();
            _lootPlayerView.Refresh();
            _lootSecureView.Refresh();

            if (_lootDrag != null)
            {
                _lootDrag.enabled = false;
                Destroy(_lootDrag);
            }

            var sources = new List<BaseBagSource>
            {
                new BaseBagSource { Name = "PACK", Bag = _bag, Rect = _lootPlayerRect, View = _lootPlayerView },
                new BaseBagSource { Name = "SECURE", Bag = _secure, Rect = _lootSecureRect, View = _lootSecureView, IsSecure = true },
                new BaseBagSource { Name = "LOOT", Bag = _lootBag, Rect = _lootBoxRect, View = _lootBoxView, IsLoot = true }
            };

            _lootDrag = _lootOverlay.AddComponent<BaseDragInteraction>();
            _lootDrag.Initialize(sources, _catalog, GameSession.Merchant, GameSession.Campaign, _palette, _canvasRect, _sellBinRect, _lootDiscardRect);
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

            _lootHint.text = "拖动物资到背包或保险箱；关闭后剩余物资留在原地。";
            _lootItemInfo.text = string.Empty;
            _lastPlayerItemCount = _bag.Count + _secure.Count;
        }

        private void CloseLoot()
        {
            _lootOpen = false;
            _lootOverlay.SetActive(false);
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

            if (_lootBag != null && _lootBag.Count > 0)
            {
                AppendLog("你离开了容器，剩下的东西留在原地。");
            }
            else
            {
                AppendLog("容器已经被清空。");
            }

            _lootBag = null;
            RefreshAll();
            SetNarration("物资已经处理完。下一步该往哪里走？");
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
            if (_lootHint != null && _lootBag != null)
            {
                _lootHint.text = "容器 " + _lootBag.Count + " 件　·　携带 " + _bag.FreeCellCount + " 格空闲";
            }

            RefreshStatus();
        }

        private void OnBagChanged()
        {
            RefreshStatus();
            if (_bagSecureTitle != null)
            {
                _bagSecureTitle.text = "保险箱 " + _secure.Count + "/4";
            }
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
            _lootPlayerView.Refresh();
            _lootSecureView.Refresh();
        }

        private void UpdateLootHover(Vector2 screen)
        {
            if (!_lootOpen || _lootBag == null)
            {
                return;
            }

            PlacedItem item = FindItemAt(_lootBoxRect, _lootBag, screen);
            if (item == null)
            {
                item = FindItemAt(_lootPlayerRect, _bag, screen);
            }

            if (item == null)
            {
                item = FindItemAt(_lootSecureRect, _secure, screen);
            }

            if (item == null)
            {
                _lootItemInfo.text = "拖动物品到背包或保险箱";
                return;
            }

            _lootItemInfo.text = "悬停查看完整物品信息";
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
            IReadOnlyList<BaseBagSource> sources = CurrentItemSources();
            if (_itemTooltipPanel == null || _ended
                || (_actionDrag != null && _actionDrag.IsDragging)
                || (Mouse.current != null && Mouse.current.leftButton.isPressed))
            {
                ClearSynergyHighlights(sources);
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
                ClearSynergyHighlights(sources);
                return;
            }

            ItemDefinition definition;
            if (!_combinedCatalog.TryGet(item.DefinitionId, out definition))
            {
                _itemTooltipPanel.SetActive(false);
                ClearSynergyHighlights(sources);
                return;
            }

            UpdateSynergyHighlights(sources, hoveredSource, item);
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

            return _actionDrag != null ? _actionDrag.Sources : null;
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
                _combinedCatalog,
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
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _logLines.Add(text);
            while (_logLines.Count > 3)
            {
                _logLines.RemoveAt(0);
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _logLines.Count; i++)
            {
                builder.Append("> ").Append(_logLines[i]);
                if (i < _logLines.Count - 1)
                {
                    builder.Append('\n');
                }
            }

            _logText.text = builder.ToString();
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

            var go = new GameObject("ExpeditionCanvas", typeof(Canvas), typeof(CanvasScaler));
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

        private static Text CreateText(Transform parent, string name, string content, int size, Color color, Vector2 topLeft, Vector2 box, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = box;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = GameFonts.Ui;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateCenteredText(Transform parent, string name, string content, int size, Color color, Vector2 center, Vector2 box)
        {
            Text text = CreateText(parent, name, content, size, color, Vector2.zero, box, TextAnchor.MiddleCenter);
            var rect = (RectTransform)text.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            return text;
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
                    return new Color32(155, 155, 160, 255);
            }
        }

    }
}

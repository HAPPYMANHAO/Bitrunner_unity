using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class CombatPrototypeBootstrap : MonoBehaviour
    {
        private sealed class BagBinding
        {
            public CombatActor Owner;
            public BagInventory Bag;
            public RectTransform Rect;
            public BagView View;
        }


        private static readonly Color BackgroundColor = new Color32(18, 21, 26, 255);
        private static readonly Color PanelColor = new Color32(27, 31, 38, 255);
        private static readonly Color PanelSelectedColor = new Color32(42, 51, 64, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        private static readonly Color DisabledColor = new Color32(44, 48, 56, 255);
        private static readonly Color HpColor = new Color32(111, 191, 115, 255);
        private static readonly Color EnemyHpColor = new Color32(200, 96, 96, 255);

        private ItemCatalog _catalog;
        private CombatEngine _engine;
        private CombatActor _player;
        private List<CombatActor> _enemies;

        private readonly List<BagBinding> _bags = new List<BagBinding>();
        private readonly List<RectTransform> _enemyPanels = new List<RectTransform>();
        private readonly List<Image> _enemyPanelImages = new List<Image>();
        private readonly List<Image> _enemyHpFills = new List<Image>();
        private readonly List<Text> _enemyHpTexts = new List<Text>();
        private readonly List<Text> _enemyApTexts = new List<Text>();
        private readonly List<Text> _enemyIntentTexts = new List<Text>();
        private readonly List<Text> _enemyArmorTexts = new List<Text>();
        private readonly List<BagView> _enemyViews = new List<BagView>();

        private Dictionary<string, Color> _palette;
        private BagView _playerView;
        private BagView _secureView;
        private RectTransform _playerBagRect;
        private RectTransform _secureRect;
        private RectTransform _discardRect;
        private Text _playerTitle;

        private Image _playerHpFill;
        private Text _playerHpText;
        private Text _playerApText;
        private Text _playerArmorText;
        private Text _playerReductionText;
        private Text _targetText;
        private Text _logText;
        private Text _messageText;
        private Text _endTurnText;
        private Image _endTurnImage;
        private RectTransform _endTurnRect;
        private RectTransform _lootAllRect;
        private Text _lootAllText;

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

        private int _selectedEnemy;
        private bool _ended;

        private void Start()
        {
            GameSession.EnsureInitialized();
            _catalog = GameSession.Catalog;
            _palette = BuildPalette(_catalog);
            ItemDurability.EnsureBag(GameSession.PlayerBag, _catalog);
            _player = GameSession.CreatePlayerActor();
            if (GameSession.ExpeditionHp > 0)
            {
                _player.SetHp(GameSession.ExpeditionHp);
            }

            _enemies = CreateEnemies();

            _engine = new CombatEngine(_player, _enemies, _catalog);
            _engine.Start();

            Canvas canvas = CreateCanvas();
            _canvasRect = (RectTransform)canvas.transform;
            BuildHud(canvas.transform);
            RefreshAll();
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
                    SceneManager.LoadScene("BasePrototype");
                }

                return;
            }

            if (_lootPhase)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    && _lootAllRect != null
                    && _lootAllRect.gameObject.activeSelf
                    && RectTransformUtility.RectangleContainsScreenPoint(_lootAllRect, screen, null))
                {
                    LootAll();
                    return;
                }

                if (mouse.leftButton.wasPressedThisFrame
                    && RectTransformUtility.RectangleContainsScreenPoint(_endTurnRect, screen, null))
                {
                    FinishLoot();
                    return;
                }

                UpdateTooltip(screen);
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_endTurnRect != null
                    && _engine.Phase == CombatPhase.PlayerTurn
                    && RectTransformUtility.RectangleContainsScreenPoint(_endTurnRect, screen, null))
                {
                    _engine.EndTurn();
                    SetMessage("敌方回合结束");
                    RefreshAll();
                    return;
                }

                for (int i = 0; i < _enemyPanels.Count; i++)
                {
                    if (_enemies[i].IsAlive
                        && RectTransformUtility.RectangleContainsScreenPoint(_enemyPanels[i], screen, null))
                    {
                        _selectedEnemy = i;
                        RefreshAll();
                        return;
                    }
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

            var go = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler));
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

        private void BuildHud(Transform canvas)
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                BuildEnemyPanel(canvas, _enemies[i], i);
            }

            RectTransform securePanel = CreateCenteredPanel(canvas, "SecurePanel", new Vector2(230f, 100f), new Vector2(84f, 68f), PanelColor);
            CreateText(securePanel, "Title", "保险箱", 9, TextColor, new Vector2(6f, -4f), new Vector2(72f, 12f), TextAnchor.UpperLeft);
            _secureRect = CreateBagRect(securePanel, new Vector2(0f, -8f), new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            _secureView = _secureRect.gameObject.AddComponent<BagView>();
            _secureView.Initialize(GameSession.Secure, _secureRect, _palette);
            _secureView.ItemEnabledPredicate = item => false;
            _bags.Add(new BagBinding { Owner = _player, Bag = GameSession.Secure, Rect = _secureRect, View = _secureView });

            _discardRect = CreateCenteredPanel(canvas, "Discard", new Vector2(260f, 22f), new Vector2(80f, 24f), new Color32(58, 46, 34, 255));
            CreateCenteredText(_discardRect, "Label", "丢弃", 9, MutedColor, Vector2.zero, new Vector2(76f, 22f));

            RectTransform playerPanel = CreateCenteredPanel(canvas, "PlayerBagPanel", new Vector2(-215f, -90f), new Vector2(140f, 120f), PanelColor);
            playerPanel.gameObject.AddComponent<RectMask2D>();

            _playerBagRect = CreateBagRect(playerPanel, Vector2.zero, new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            _playerView = _playerBagRect.gameObject.AddComponent<BagView>();
            _playerView.Initialize(_player.Bag, _playerBagRect, _palette);
            _playerView.ItemEnabledPredicate = IsPlayerItemAvailable;
            _bags.Add(new BagBinding { Owner = _player, Bag = _player.Bag, Rect = _playerBagRect, View = _playerView });

            var interaction = _playerBagRect.gameObject.AddComponent<BagInteraction>();
            interaction.Initialize(_player.Bag, _playerView, _playerBagRect, null, _discardRect);
            interaction.ItemClicked = OnPlayerItemClicked;
            interaction.ItemDiscarded += OnPlayerItemDiscarded;

            BuildStatusPanel(canvas);
            BuildLootButton(canvas);
            BuildLogPanel(canvas);
            BuildTooltip(canvas);
            BuildEndOverlay(canvas);
            BuildLootInteraction();
        }

        private void BuildEnemyPanel(Transform canvas, CombatActor enemy, int index)
        {
            var center = new Vector2(-185f + index * 190f, 105f);
            RectTransform panel = CreateCenteredPanel(canvas, "Enemy_" + enemy.Id, center, new Vector2(170f, 130f), PanelColor);
            _enemyPanels.Add(panel);
            _enemyPanelImages.Add(panel.GetComponent<Image>());

            CreateText(panel, "Name", enemy.DisplayName, 12, TextColor, new Vector2(8f, -6f), new Vector2(100f, 16f), TextAnchor.UpperLeft);
            Text hpText = CreateText(panel, "Hp", enemy.Hp + "/" + enemy.MaxHp, 10, MutedColor, new Vector2(108f, -6f), new Vector2(54f, 16f), TextAnchor.UpperRight);
            _enemyHpTexts.Add(hpText);

            Image hpFill = CreateBar(panel, "HpBar", new Vector2(8f, -24f), new Vector2(154f, 8f), EnemyHpColor);
            _enemyHpFills.Add(hpFill);

            RectTransform bagRect = CreateBagRect(panel, new Vector2(-28f, -2f), new Vector2(4f * BagView.CellSize, 3f * BagView.CellSize));
            BagView view = bagRect.gameObject.AddComponent<BagView>();
            view.Initialize(enemy.Bag, bagRect, _palette);
            view.ItemEnabledPredicate = item => IsEnemyItemAvailable(enemy, item);
            _bags.Add(new BagBinding { Owner = enemy, Bag = enemy.Bag, Rect = bagRect, View = view });
            _enemyViews.Add(view);
            _enemyBagRects.Add(bagRect);

            Text apText = CreateText(panel, "Ap", string.Empty, 10, TextColor, new Vector2(8f, -94f), new Vector2(154f, 12f), TextAnchor.UpperLeft);
            _enemyApTexts.Add(apText);

            Text intent = CreateText(panel, "Intent", string.Empty, 10, MutedColor, new Vector2(8f, -106f), new Vector2(154f, 12f), TextAnchor.UpperLeft);
            _enemyIntentTexts.Add(intent);
            Text armor = CreateText(panel, "Armor", string.Empty, 8, MutedColor, new Vector2(8f, -118f), new Vector2(154f, 12f), TextAnchor.UpperLeft);
            _enemyArmorTexts.Add(armor);
        }

        private void BuildStatusPanel(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "StatusPanel", new Vector2(-20f, -88f), new Vector2(150f, 176f), PanelColor);

            _playerTitle = CreateText(panel, "Title", "跑者", 12, TextColor, new Vector2(8f, -8f), new Vector2(134f, 16f), TextAnchor.UpperLeft);

            _playerHpText = CreateText(panel, "Hp", string.Empty, 11, TextColor, new Vector2(8f, -28f), new Vector2(134f, 14f), TextAnchor.UpperLeft);
            _playerHpFill = CreateBar(panel, "HpBar", new Vector2(8f, -45f), new Vector2(134f, 8f), HpColor);

            _playerApText = CreateText(panel, "Ap", string.Empty, 10, TextColor, new Vector2(8f, -58f), new Vector2(134f, 14f), TextAnchor.UpperLeft);
            _playerArmorText = CreateText(panel, "Armor", string.Empty, 8, MutedColor, new Vector2(8f, -71f), new Vector2(134f, 12f), TextAnchor.UpperLeft);
            _playerReductionText = CreateText(panel, "Reduction", string.Empty, 8, MutedColor, new Vector2(8f, -82f), new Vector2(134f, 12f), TextAnchor.UpperLeft);
            _targetText = CreateText(panel, "Target", string.Empty, 9, MutedColor, new Vector2(8f, -94f), new Vector2(134f, 14f), TextAnchor.UpperLeft);

            _endTurnRect = CreateCenteredPanel(panel, "EndTurn", new Vector2(0f, -33f), new Vector2(134f, 26f), AccentColor);
            _endTurnImage = _endTurnRect.GetComponent<Image>();
            _endTurnText = CreateCenteredText(_endTurnRect, "Label", "结束回合", 12, TextColor, Vector2.zero, new Vector2(130f, 24f));

            _messageText = CreateText(panel, "Message", "点击物品使用", 9, MutedColor, new Vector2(8f, -136f), new Vector2(134f, 28f), TextAnchor.UpperLeft);
        }

        private void BuildLootButton(Transform canvas)
        {
            _lootAllRect = CreateCenteredPanel(canvas, "LootAll", new Vector2(0f, -160f), new Vector2(150f, 26f), AccentColor);
            _lootAllText = CreateCenteredText(_lootAllRect, "Label", "搜刮全部", 11, TextColor, Vector2.zero, new Vector2(146f, 24f));
            _lootAllRect.gameObject.SetActive(false);
        }

        private void BuildLogPanel(Transform canvas)
        {
            RectTransform panel = CreateCenteredPanel(canvas, "LogPanel", new Vector2(190f, -88f), new Vector2(250f, 160f), PanelColor);
            CreateText(panel, "Title", "战斗日志", 10, TextColor, new Vector2(8f, -8f), new Vector2(234f, 14f), TextAnchor.UpperLeft);
            _logText = CreateText(panel, "Log", string.Empty, 9, MutedColor, new Vector2(8f, -26f), new Vector2(234f, 128f), TextAnchor.UpperLeft);
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

        private void BuildEndOverlay(Transform canvas)
        {
            _endOverlay = new GameObject("EndOverlay", typeof(RectTransform), typeof(Image));
            _endOverlay.transform.SetParent(canvas, false);

            var rect = (RectTransform)_endOverlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _endOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            RectTransform box = CreateCenteredPanel(_endOverlay.transform, "Box", Vector2.zero, new Vector2(320f, 150f), PanelColor);
            _endTitle = CreateCenteredText(box, "Title", string.Empty, 26, TextColor, new Vector2(0f, 28f), new Vector2(300f, 36f));
            _endSubtitle = CreateCenteredText(box, "Subtitle", string.Empty, 10, MutedColor, new Vector2(0f, -18f), new Vector2(290f, 44f));

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
            if (_engine == null || enemy == null || item == null || !enemy.IsAlive)
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
            string error;
            if (_engine.TryUseItem(_player, instanceId, target, out error))
            {
                SetMessage("已使用 " + definition.DisplayName + "。");
                EnsureSelection();
                RefreshAll();
            }
            else
            {
                SetMessage(error);
            }
        }

        private void RefreshAll()
        {
            EnsureSelection();

            _playerView.Refresh();
            _secureView.Refresh();
            _playerTitle.text = "跑者  弹 " + _player.Bag.CountOf("ammo");

            for (int i = 0; i < _enemyViews.Count; i++)
            {
                _enemyViews[i].Refresh();
            }

            UpdatePlayerHud();
            UpdateEnemyHud();
            UpdateLog();
            UpdateEndTurnButton();
            CheckEnd();
        }

        private void UpdatePlayerHud()
        {
            _playerHpText.text = "生命 " + _player.Hp + "/" + _player.MaxHp;
            _playerHpFill.fillAmount = _player.MaxHp > 0 ? (float)_player.Hp / _player.MaxHp : 0f;
            _playerApText.text = "能量 " + _player.Energy + "/" + MaxEnergy(_player)
                                 + "   护盾 " + _player.Shield
                                 + StatusSuffix(_player);
            _playerArmorText.text = ArmorLine(_player);
            _playerReductionText.text = ReductionLine(_player);

            CombatActor target = CurrentTarget();
            _targetText.text = target == null
                ? "目标 -"
                : "目标 " + target.DisplayName + " " + target.Hp + "/" + target.MaxHp;
        }

        private void UpdateEnemyHud()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                CombatActor enemy = _enemies[i];

                _enemyHpTexts[i].text = enemy.Hp + "/" + enemy.MaxHp;
                _enemyHpFills[i].fillAmount = enemy.MaxHp > 0 ? (float)enemy.Hp / enemy.MaxHp : 0f;
                _enemyApTexts[i].text = "能量 " + MaxEnergy(enemy)
                                        + "   护盾 " + enemy.Shield
                                        + StatusSuffix(enemy);
                _enemyIntentTexts[i].text = enemy.IsAlive ? IntentFor(enemy) : "已倒下";
                _enemyArmorTexts[i].text = ReductionLine(enemy);
                _enemyPanelImages[i].color = i == _selectedEnemy && enemy.IsAlive ? PanelSelectedColor : PanelColor;
            }
        }

        private void UpdateLog()
        {
            var builder = new StringBuilder();
            int start = Mathf.Max(0, _engine.Events.Count - 10);

            for (int i = start; i < _engine.Events.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(_engine.Events[i].Message);
            }

            _logText.text = builder.ToString();
        }

        private void UpdateEndTurnButton()
        {
            if (_lootPhase)
            {
                _endTurnImage.color = AccentColor;
                _endTurnText.text = GameSession.ExpeditionActive && GameSession.Scenario != null
                    ? "返回地点"
                    : "离开";
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
            _endTurnText.text = playerTurn ? "结束回合" : "敌方回合";
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

            GameSession.CompleteExpedition(false);
            _ended = true;
            _endOverlay.SetActive(true);
            _endTitle.text = "失败";
            _endSubtitle.text = "背包已丢失。\n点击返回基地。";
        }

        private void BeginLoot()
        {
            _lootPhase = true;
            _takenValue = 0;
            _tooltipPanel.SetActive(false);
            _lootInteraction.enabled = true;
            if (_lootAllRect != null)
            {
                _lootAllRect.gameObject.SetActive(true);
            }

            SetMessage("把战利品收进背包。");
            _targetText.text = "已取 0  剩余 " + _player.Bag.FreeCellCount;
            UpdateEndTurnButton();
        }

        private void LootAll()
        {
            if (_lootInteraction == null)
            {
                return;
            }

            _lootInteraction.TakeAll();
            _targetText.text = "已取 " + _takenValue + "  剩余 " + _player.Bag.FreeCellCount;
            _playerTitle.text = "跑者  弹 " + _player.Bag.CountOf("ammo");
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

        private void OnLootTaken(string displayName, int value)
        {
            _takenValue += value;
            SetMessage("获得 " + displayName + "（+" + value + "）");
            _targetText.text = "已取 " + _takenValue + "  剩余 " + _player.Bag.FreeCellCount;
            _playerTitle.text = "跑者  弹 " + _player.Bag.CountOf("ammo");
        }

        private void FinishLoot()
        {
            _lootPhase = false;
            _lootInteraction.enabled = false;
            if (_lootAllRect != null)
            {
                _lootAllRect.gameObject.SetActive(false);
            }

            if (GameSession.ExpeditionActive && GameSession.Scenario != null)
            {
                GameSession.CompleteEncounter(_player.Hp);
                _ended = true;
                SceneManager.LoadScene("ExpeditionPrototype");
                return;
            }

            GameSession.CompleteExpedition(true);
            _ended = true;
            _endOverlay.SetActive(true);
            _endTitle.text = "已撤离";
            _endSubtitle.text = "带出战利品：" + _takenValue + "\n点击返回基地。";
        }

        private void UpdateTooltip(Vector2 screen)
        {
            if (_tooltipPanel == null || _ended)
            {
                ClearSynergyHighlights();
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                _tooltipPanel.SetActive(false);
                ClearSynergyHighlights();
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
                ClearSynergyHighlights();
                return;
            }

            UpdateSynergyHighlights(hovered, hoveredItem);
            ItemDefinition definition = _catalog.Get(hoveredItem.DefinitionId);
            int damage = _engine.GetDamage(hovered.Owner, hoveredItem);
            int block = _engine.GetShield(hovered.Owner, hoveredItem);
            SynergyBonus synergy = _engine.GetSynergyBonus(hovered.Owner, hoveredItem.InstanceId);
            _tooltipText.text = ItemText.Describe(
                definition,
                hoveredItem,
                _engine.GetAmmoCount(hovered.Owner, definition),
                damage,
                block,
                synergy);
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

        private void UpdateSynergyHighlights(BagBinding hovered, PlacedItem item)
        {
            if (hovered == null || item == null)
            {
                ClearSynergyHighlights();
                return;
            }

            IReadOnlyList<int> linked = SynergyResolver.FindLinked(
                hovered.Bag,
                _catalog,
                SynergyResolver.DefaultRules,
                item.InstanceId);

            for (int i = 0; i < _bags.Count; i++)
            {
                if (_bags[i] == hovered)
                {
                    _bags[i].View.SetHighlightedItems(linked);
                }
                else
                {
                    _bags[i].View.ClearHighlights();
                }
            }
        }

        private void ClearSynergyHighlights()
        {
            for (int i = 0; i < _bags.Count; i++)
            {
                _bags[i].View.ClearHighlights();
            }
        }

        private string IntentFor(CombatActor enemy)
        {
            EnemyBrain brain = enemy.Brain as EnemyBrain;
            if (brain == null)
            {
                return "下一步：待机";
            }

            CombatAction action = brain.PreviewNextAction(_engine, enemy);
            if (action.Type == CombatActionType.EndTurn)
            {
                return "下一步：待机";
            }

            PlacedItem item = _engine.GetItem(enemy, action.ItemInstanceId);
            ItemDefinition definition = _engine.GetDefinition(item);
            if (definition == null)
            {
                return "下一步：？";
            }

            int damage = _engine.GetDamageAfterShieldAndArmor(_player, _engine.GetDamage(enemy, item), definition.Type);
            return damage > 0
                ? "下一步：" + definition.DisplayName + "（" + damage + "）"
                : "下一步：" + definition.DisplayName;
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
            if (_selectedEnemy >= 0 && _selectedEnemy < _enemies.Count && _enemies[_selectedEnemy].IsAlive)
            {
                return _enemies[_selectedEnemy];
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        private void EnsureSelection()
        {
            if (_selectedEnemy >= 0 && _selectedEnemy < _enemies.Count && _enemies[_selectedEnemy].IsAlive)
            {
                return;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive)
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

        private static Image CreateBar(Transform parent, string name, Vector2 topLeft, Vector2 size, Color fillColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color32(12, 14, 18, 255);
            go.GetComponent<Image>().raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);

            var fillRect = (RectTransform)fillGo.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fill = fillGo.GetComponent<Image>();
            fill.sprite = SpriteFactory.White;
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
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

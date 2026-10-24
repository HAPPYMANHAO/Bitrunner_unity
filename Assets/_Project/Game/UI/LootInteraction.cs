using System;
using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class LootSource
    {
        public BagInventory Bag;
        public RectTransform Rect;
        public BagView View;
    }

    public sealed class LootInteraction : MonoBehaviour
    {
        private readonly List<LootSource> _sources = new List<LootSource>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private BagInventory _playerBag;
        private RectTransform _playerRect;
        private BagView _playerView;
        private BagInventory _secureBag;
        private RectTransform _secureRect;
        private BagView _secureView;
        private bool _targetSecure;
        private ItemCatalog _catalog;
        private Dictionary<string, Color> _palette;
        private RectTransform _canvasRect;

        private RectTransform _floatRect;
        private Image _floatImage;

        private bool _dragging;
        private LootSource _source;
        private PlacedItem _item;
        private int _rotation;
        private Cell _grabOffset;
        private Cell _target;
        private bool _validTarget;
        private Vector2 _pressScreen;
        private bool _clickCandidate;
        private float _lastClickTime = -1f;
        private LootSource _lastClickSource;
        private int _lastClickId;

        public event Action<string, int> ItemTaken;
        public event Action<string> Message;

        public void Initialize(
            BagInventory playerBag,
            RectTransform playerRect,
            BagView playerView,
            BagInventory secureBag,
            RectTransform secureRect,
            BagView secureView,
            IEnumerable<LootSource> sources,
            ItemCatalog catalog,
            Dictionary<string, Color> palette,
            RectTransform canvasRect)
        {
            _playerBag = playerBag;
            _playerRect = playerRect;
            _playerView = playerView;
            _secureBag = secureBag;
            _secureRect = secureRect;
            _secureView = secureView;
            _catalog = catalog;
            _palette = palette;
            _canvasRect = canvasRect;

            _sources.Clear();
            _sources.AddRange(sources);

            var go = new GameObject("LootDragGhost", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvasRect, false);
            go.transform.SetAsLastSibling();

            _floatRect = (RectTransform)go.transform;
            _floatRect.anchorMin = new Vector2(0.5f, 0.5f);
            _floatRect.anchorMax = new Vector2(0.5f, 0.5f);
            _floatRect.pivot = new Vector2(0.5f, 0.5f);
            _floatRect.sizeDelta = new Vector2(BagView.CellSize, BagView.CellSize);

            _floatImage = go.GetComponent<Image>();
            _floatImage.raycastTarget = false;
            go.SetActive(false);
        }

        private void OnDisable()
        {
            CancelDrag();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (!_dragging && mouse.leftButton.wasPressedThisFrame)
            {
                BeginDrag(screen);
            }

            if (_dragging && mouse.leftButton.isPressed)
            {
                if ((screen - _pressScreen).sqrMagnitude > 36f)
                {
                    _clickCandidate = false;
                }

                UpdateDrag(screen);
            }

            if (_dragging && mouse.rightButton.wasPressedThisFrame)
            {
                RotateDrag(screen);
            }

            if (_dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                if (_clickCandidate)
                {
                    int clickedId = _item.InstanceId;
                    LootSource clickedSource = _source;
                    bool shift = Keyboard.current != null
                                 && (Keyboard.current.leftShiftKey.isPressed
                                     || Keyboard.current.rightShiftKey.isPressed);
                    float now = Time.unscaledTime;
                    bool doubleClick = _lastClickSource == clickedSource
                                       && _lastClickId == clickedId
                                       && now - _lastClickTime <= 0.5f;
                    _lastClickTime = now;
                    _lastClickSource = clickedSource;
                    _lastClickId = clickedId;

                    CancelDrag();
                    if (shift || doubleClick)
                    {
                        TryQuickTransfer(clickedSource, clickedId);
                    }

                    return;
                }

                UpdateDrag(screen);
                EndDrag();
            }
        }

        private void BeginDrag(Vector2 screen)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                LootSource source = _sources[i];

                Cell cell;
                if (!TryScreenToCell(source.Rect, source.Bag, screen, out cell))
                {
                    continue;
                }

                int instanceId = source.Bag.Grid.GetOccupant(cell.X, cell.Y);
                if (instanceId == 0)
                {
                    continue;
                }

                PlacedItem item;
                if (!source.Bag.TryGet(instanceId, out item))
                {
                    continue;
                }

                _dragging = true;
                _source = source;
                _item = item;
                _rotation = item.Rotation;
                _grabOffset = new Cell(cell.X - item.Origin.X, cell.Y - item.Origin.Y);
                _target = item.Origin;
                _validTarget = false;
                _pressScreen = screen;
                _clickCandidate = true;

                source.View.SetItemAlpha(instanceId, 0.2f);
                ShowFloating(screen);
                return;
            }
        }

        private void UpdateDrag(Vector2 screen)
        {
            if (_item == null)
            {
                return;
            }

            Cell cell;

            if (TryScreenToCell(_playerRect, _playerBag, screen, out cell))
            {
                _targetSecure = false;
                _target = new Cell(cell.X - _grabOffset.X, cell.Y - _grabOffset.Y);
                ItemShape shape = _item.BaseShape.Rotate(_rotation);
                ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                bool canPlace = _playerBag.Grid.CanPlace(shape, _target);
                bool canStack = _playerBag.CanStackAt(_item.DefinitionId, _item.BaseShape, _target, _rotation, _item.Count, definition.MaxStack);
                _validTarget = canPlace || canStack;
                _secureView.HideGhost();
                _playerView.ShowGhost(shape, _target, _validTarget);
                _floatRect.gameObject.SetActive(false);
                return;
            }

            if (_secureBag != null && TryScreenToCell(_secureRect, _secureBag, screen, out cell))
            {
                _targetSecure = true;
                _target = new Cell(cell.X - _grabOffset.X, cell.Y - _grabOffset.Y);
                ItemShape shape = _item.BaseShape.Rotate(_rotation);
                ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                bool eligible = SecureContainerRules.IsEligible(definition);
                bool canPlace = _secureBag.Grid.CanPlace(shape, _target);
                bool canStack = _secureBag.CanStackAt(_item.DefinitionId, _item.BaseShape, _target, _rotation, _item.Count, definition.MaxStack);
                _validTarget = eligible && (canPlace || canStack);
                _playerView.HideGhost();
                _secureView.ShowGhost(shape, _target, _validTarget);
                _floatRect.gameObject.SetActive(false);
                return;
            }

            _validTarget = false;
            _targetSecure = false;
            _playerView.HideGhost();
            _secureView.HideGhost();
            ShowFloating(screen);
        }

        private void RotateDrag(Vector2 screen)
        {
            if (_item == null || !_item.Rotatable)
            {
                return;
            }

            _rotation = (_rotation + 1) % 4;
            UpdateDrag(screen);
        }

        public int TakeAll()
        {
            int total = 0;
            int taken = 0;

            for (int i = 0; i < _sources.Count; i++)
            {
                LootSource source = _sources[i];
                IReadOnlyList<PlacedItem> items = source.Bag.Items;
                total += items.Count;
                var snapshot = new List<PlacedItem>(items);

                for (int j = 0; j < snapshot.Count; j++)
                {
                    PlacedItem current;
                    if (!source.Bag.TryGet(snapshot[j].InstanceId, out current))
                    {
                        continue;
                    }

                    ItemDefinition definition;
                    if (!_catalog.TryGet(current.DefinitionId, out definition))
                    {
                        continue;
                    }

                    if (!TryStore(current, definition))
                    {
                        continue;
                    }

                    source.Bag.Remove(current.InstanceId);
                    NotifyTaken(definition);
                    taken++;
                }

                source.View.Refresh();
            }

            RefreshViews();
            if (Message != null)
            {
                if (taken == 0)
                {
                    Message("没有可搜刮的物品。");
                }
                else if (taken < total)
                {
                    Message("已搜刮 " + taken + " 件，" + (total - taken) + " 件空间不足。");
                }
                else
                {
                    Message("已搜刮全部 " + taken + " 件物品。");
                }
            }

            return taken;
        }

        private bool TryQuickTransfer(LootSource source, int instanceId)
        {
            if (source == null || source.Bag == null)
            {
                return false;
            }

            PlacedItem item;
            if (!source.Bag.TryGet(instanceId, out item))
            {
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            if (!TryStore(item, definition))
            {
                if (Message != null)
                {
                    Message("背包和保险箱空间不足。");
                }

                return false;
            }

            source.Bag.Remove(instanceId);
            source.View.Refresh();
            RefreshViews();
            NotifyTaken(definition);
            if (Message != null)
            {
                Message("已搜刮 " + definition.DisplayName + "。");
            }

            _lastClickTime = -1f;
            return true;
        }

        private bool TryStore(PlacedItem item, ItemDefinition definition)
        {
            PlacedItem placed;
            string error;
            if (_playerBag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveContainer(item, placed);
                return true;
            }

            if (_secureBag != null
                && SecureContainerRules.IsEligible(definition)
                && _secureBag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                ItemTransfer.PreserveContainer(item, placed);
                return true;
            }

            return false;
        }

        private void NotifyTaken(ItemDefinition definition)
        {
            if (ItemTaken != null)
            {
                ItemTaken(definition.DisplayName, definition.Value);
            }
        }

        private void RefreshViews()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].View.Refresh();
            }

            _playerView.Refresh();
            _secureView.Refresh();
            _playerView.HideGhost();
            _secureView.HideGhost();
        }

        private void EndDrag()
        {
            if (_item != null)
            {
                BagInventory targetBag = _targetSecure ? _secureBag : _playerBag;
                PlacedItem placed = null;
                string error;
                ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                bool stored = _validTarget
                              && targetBag.TryStackAt(
                                  _item.DefinitionId,
                                  _item.BaseShape,
                                  _target,
                                  _rotation,
                                  _item.Count,
                                  definition.MaxStack,
                                  out placed,
                                  out error);

                if (!stored && _targetSecure && SecureContainerRules.IsEligible(definition))
                {
                    stored = _secureBag.TryAddOrStack(
                        definition.Id,
                        _item.BaseShape,
                        _item.Rotatable,
                        _item.Count,
                        definition.MaxStack,
                        out placed,
                        out error);
                }

                if (stored)
                {
                    ItemTransfer.PreserveContainer(_item, placed);
                    _source.Bag.Remove(_item.InstanceId);
                    if (ItemTaken != null)
                    {
                        ItemTaken(definition.DisplayName, definition.Value);
                    }
                }
                else if (_targetSecure && Message != null)
                {
                    Message(SecureContainerRules.IsEligible(definition)
                        ? "保险箱空间不足。"
                        : "保险箱只接受 1×1 非武器/护甲物品。");
                }
            }

            if (_source != null && _source.View != null)
            {
                _source.View.Refresh();
            }

            _playerView.Refresh();
            _secureView.Refresh();
            _playerView.HideGhost();
            _secureView.HideGhost();
            _floatRect.gameObject.SetActive(false);

            _dragging = false;
            _source = null;
            _item = null;
            _validTarget = false;
            _targetSecure = false;
            _target = Cell.Zero;
        }

        private void CancelDrag()
        {
            if (_source != null && _source.View != null)
            {
                _source.View.Refresh();
            }

            if (_playerView != null)
            {
                _playerView.Refresh();
                _playerView.HideGhost();
            }

            if (_secureView != null)
            {
                _secureView.Refresh();
                _secureView.HideGhost();
            }

            if (_floatRect != null)
            {
                _floatRect.gameObject.SetActive(false);
            }

            _dragging = false;
            _source = null;
            _item = null;
            _validTarget = false;
        }

        private void ShowFloating(Vector2 screen)
        {
            if (_item == null)
            {
                return;
            }

            ItemDefinition definition = _catalog.Get(_item.DefinitionId);
            ItemShape shape = _item.BaseShape.Rotate(_rotation);

            _floatImage.sprite = GetSprite(definition, shape);
            _floatImage.color = new Color(1f, 1f, 1f, 0.72f);
            _floatRect.sizeDelta = new Vector2(shape.Width * BagView.CellSize, shape.Height * BagView.CellSize);

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local);
            _floatRect.anchoredPosition = local;
            _floatRect.gameObject.SetActive(true);
        }

        private Sprite GetSprite(ItemDefinition definition, ItemShape shape)
        {
            string key = definition.Id + "|" + shape.ToPattern();
            Sprite sprite;
            if (_spriteCache.TryGetValue(key, out sprite))
            {
                return sprite;
            }

            Color fill;
            if (!_palette.TryGetValue(definition.Id, out fill))
            {
                fill = Color.magenta;
            }

            Color border = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 1f);
            sprite = SpriteFactory.CreateShapeSprite(shape, fill, border);
            _spriteCache[key] = sprite;
            return sprite;
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

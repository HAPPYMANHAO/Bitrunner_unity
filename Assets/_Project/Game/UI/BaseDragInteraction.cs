using System;
using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class BaseBagSource
    {
        public string Name;
        public BagInventory Bag;
        public RectTransform Rect;
        public RectTransform Viewport;
        public BagView View;
        public bool IsSecure;
        public bool IsLoot;
        public bool IsMerchant;
        public bool IsPurchaseTarget;
        public bool IsContainerStorage;
        public bool CanReceive = true;
        public bool AcceptsActionItems = true;
        public ItemCategory? AllowedCategory;
        public Func<ItemDefinition, bool> ItemFilter;
    }

    public sealed class BaseDragInteraction : MonoBehaviour
    {
        private static readonly Color SellBinColor = new Color32(58, 46, 34, 255);
        private static readonly Color SellBinHighlightColor = new Color32(112, 84, 46, 255);

        private readonly List<BaseBagSource> _sources = new List<BaseBagSource>();
        private readonly List<BaseBagSource> _persistentSources = new List<BaseBagSource>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private ItemCatalog _catalog;
        private Merchant _merchant;
        private CampaignState _campaign;
        private Dictionary<string, Color> _palette;
        private RectTransform _canvasRect;
        private RectTransform _sellBin;
        private Image _sellBinImage;
        private RectTransform _discardBin;
        private Image _discardBinImage;

        private RectTransform _floatRect;
        private Image _floatImage;

        private bool _dragging;
        private BaseBagSource _source;
        private PlacedItem _item;
        private Cell _originalOrigin;
        private int _originalRotation;
        private int _rotation;
        private Cell _grabOffset;
        private BaseBagSource _targetBag;
        private Cell _target;
        private bool _validTarget;
        private bool _overSell;
        private bool _overDiscard;
        private Vector2 _pressScreen;
        private bool _clickCandidate;
        private float _lastClickTime = -1f;
        private BaseBagSource _lastClickSource;
        private int _lastClickId;

        public Func<BaseBagSource, PlacedItem, BaseBagSource> QuickMoveTarget;
        public Func<BaseBagSource, int, bool> TryActivateItem;

        /// <summary>
        /// 返回非 null 表示这件物品**不允许被拿走**，返回值直接当提示文案。
        /// 通讯中枢里被任务占用的上行链路走这里拦下——它必须一直待在格子里。
        /// </summary>
        public Func<BaseBagSource, PlacedItem, string> CanTake;

        public Func<Vector2, bool> PointerBlocked;
        public event Action<string> Message;
        public event Action Changed;
        public event Action<int> ItemUsed;
        public event Action<BaseBagSource, int> ItemClicked;

        public bool IsDragging
        {
            get { return _dragging; }
        }

        public void AddPersistentSource(BaseBagSource source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = _persistentSources.Count - 1; i >= 0; i--)
            {
                if (_persistentSources[i] == null || _persistentSources[i].Name == source.Name)
                {
                    _persistentSources.RemoveAt(i);
                }
            }

            _persistentSources.Add(source);
            AddPersistentSourcesToSourceList();
        }

        public void RemovePersistentSource(string name)
        {
            for (int i = _persistentSources.Count - 1; i >= 0; i--)
            {
                if (_persistentSources[i] != null && _persistentSources[i].Name == name)
                {
                    _persistentSources.RemoveAt(i);
                }
            }

            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] != null && _sources[i].Name == name)
                {
                    _sources.RemoveAt(i);
                }
            }
        }

        public IReadOnlyList<BaseBagSource> Sources
        {
            get { return _sources; }
        }

        private void AddPersistentSourcesToSourceList()
        {
            for (int i = 0; i < _persistentSources.Count; i++)
            {
                BaseBagSource source = _persistentSources[i];
                if (source == null || source.Bag == null)
                {
                    continue;
                }

                bool exists = false;
                for (int j = 0; j < _sources.Count; j++)
                {
                    if (_sources[j] != null && _sources[j].Name == source.Name)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _sources.Add(source);
                }
            }
        }

        public BaseBagSource FindSource(string name)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].Name == name)
                {
                    return _sources[i];
                }
            }

            return null;
        }

        public void SetSources(List<BaseBagSource> sources)
        {
            if (_dragging)
            {
                CancelDrag();
            }

            _sources.Clear();
            _sources.AddRange(sources);
            AddPersistentSourcesToSourceList();
        }

        public void Initialize(
            List<BaseBagSource> sources,
            ItemCatalog catalog,
            Merchant merchant,
            CampaignState campaign,
            Dictionary<string, Color> palette,
            RectTransform canvasRect,
            RectTransform sellBin,
            RectTransform discardBin = null)
        {
            _sources.Clear();
            _sources.AddRange(sources);

            _catalog = catalog;
            _merchant = merchant;
            _campaign = campaign;
            _palette = palette;
            _canvasRect = canvasRect;
            _sellBin = sellBin;
            _sellBinImage = sellBin != null ? sellBin.GetComponent<Image>() : null;
            if (_sellBinImage != null)
            {
                _sellBinImage.color = SellBinColor;
            }

            _discardBin = discardBin;
            _discardBinImage = discardBin != null ? discardBin.GetComponent<Image>() : null;
            if (_discardBinImage != null)
            {
                _discardBinImage.color = SellBinColor;
            }

            var go = new GameObject("BaseDragGhost", typeof(RectTransform), typeof(Image));
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
            if (!_dragging && PointerBlocked != null && PointerBlocked(screen))
            {
                return;
            }

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

            if (!_dragging && mouse.rightButton.wasPressedThisFrame)
            {
                RotateUnderCursor(screen);
            }

            if (_dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                if (_clickCandidate)
                {
                    int clickedId = _item.InstanceId;
                    BaseBagSource clickedSource = _source;
                    bool merchantItem = _source != null && _source.IsMerchant;
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
                    if (merchantItem)
                    {
                        if (Message != null)
                        {
                            Message("拖入仓库购买。");
                        }
                    }
                    else if (doubleClick && !shift && TryActivateItem != null
                             && TryActivateItem(clickedSource, clickedId))
                    {
                    }
                    else if (shift || doubleClick)
                    {
                        if (QuickMoveTarget != null)
                        {
                            TryQuickMove(clickedSource, clickedId);
                        }
                    }
                    else if (ItemClicked != null)
                    {
                        ItemClicked(clickedSource, clickedId);
                    }
                    else if (ItemUsed != null)
                    {
                        ItemUsed(clickedId);
                    }
                }
                else
                {
                    UpdateDrag(screen);
                    EndDrag();
                }
            }
        }

        private void BeginDrag(Vector2 screen)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                BaseBagSource source = _sources[i];

                if (!PointerInsideSource(source, screen))
                {
                    continue;
                }

                Cell cell;
                if (!TryScreenToCell(source.Rect, source.Bag, source.View, screen, out cell))
                {
                    continue;
                }

                int instanceId = source.Bag.Grid.GetOccupant(cell.X, cell.Y);
                if (instanceId == 0)
                {
                    if (source.IsContainerStorage)
                    {
                        return;
                    }

                    continue;
                }

                PlacedItem item;
                if (!source.Bag.TryGet(instanceId, out item))
                {
                    continue;
                }

                if (CanTake != null)
                {
                    string blocked = CanTake(source, item);
                    if (blocked != null)
                    {
                        if (Message != null)
                        {
                            Message(blocked);
                        }

                        return;
                    }
                }

                _dragging = true;
                _source = source;
                _item = item;
                _originalOrigin = item.Origin;
                _originalRotation = item.Rotation;
                _rotation = item.Rotation;
                _grabOffset = new Cell(cell.X - item.Origin.X, cell.Y - item.Origin.Y);
                _targetBag = null;
                _target = item.Origin;
                _validTarget = false;
                _overSell = false;
                _overDiscard = false;
                _pressScreen = screen;
                _clickCandidate = true;

                source.View.SetItemAlpha(instanceId, 0.2f);
                ShowFloating(screen, false);
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
            BaseBagSource bagUnder = FindBagAt(screen, out cell);

            if (bagUnder != null)
            {
                _targetBag = bagUnder;
                _target = new Cell(cell.X - _grabOffset.X, cell.Y - _grabOffset.Y);

                ItemShape shape = _item.BaseShape.Rotate(_rotation);
                int ignoreId = bagUnder == _source ? _item.InstanceId : 0;

                bool eligible = true;
                if (_source != null && _source.IsMerchant)
                {
                    if (bagUnder == _source)
                    {
                        eligible = true;
                    }
                    else if (bagUnder.IsPurchaseTarget)
                    {
                        eligible = CanAffordMerchantItem();
                    }
                    else
                    {
                        eligible = false;
                    }
                }
                else if (bagUnder.IsMerchant)
                {
                    eligible = true;
                }
                else if (bagUnder.IsSecure)
                {
                    eligible = SecureContainerRules.IsEligible(_catalog.Get(_item.DefinitionId));
                }

                if (bagUnder.IsLoot && bagUnder != _source)
                {
                    eligible = false;
                }

                ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                if (!bagUnder.CanReceive)
                {
                    eligible = false;
                }

                if (bagUnder.AllowedCategory.HasValue && definition.Category != bagUnder.AllowedCategory.Value)
                {
                    eligible = false;
                }

                if (bagUnder.ItemFilter != null && !bagUnder.ItemFilter(definition))
                {
                    eligible = false;
                }

                if (!bagUnder.AcceptsActionItems && definition.Category == ItemCategory.Action)
                {
                    eligible = false;
                }

                if (definition.Category == ItemCategory.Currency && bagUnder.Name != "STASH")
                {
                    eligible = false;
                }

                bool canPlace = bagUnder.Bag.Grid.CanPlace(shape, _target, ignoreId);
                bool canStack = bagUnder != _source
                                && definition.MaxStack > 1
                                && bagUnder.Bag.CanStackAt(_item.DefinitionId, _item.BaseShape, _target, _rotation, _item.Count, definition.MaxStack);
                _validTarget = eligible && (canPlace || canStack);
                _overSell = false;
                _overDiscard = false;
                ResetDiscardBin();

                HideAllGhosts();
                bagUnder.View.ShowGhost(shape, _target, _validTarget);
                _floatRect.gameObject.SetActive(false);
                ResetSellBin();
                return;
            }

            _targetBag = null;
            _validTarget = false;

            if (IsOverDiscardBin(screen))
            {
                _overDiscard = true;
                _overSell = false;
                HideAllGhosts();
                ShowFloating(screen, true);
                ResetSellBin();

                if (_discardBinImage != null)
                {
                    _discardBinImage.color = SellBinHighlightColor;
                }

                return;
            }

            _overDiscard = false;
            ResetDiscardBin();

            if (IsOverSellBin(screen))
            {
                _overSell = true;
                HideAllGhosts();
                ShowFloating(screen, true);

                if (_sellBinImage != null)
                {
                    _sellBinImage.color = SellBinHighlightColor;
                }

                return;
            }

            _overSell = false;
            _overDiscard = false;
            HideAllGhosts();
            ShowFloating(screen, false);
            ResetSellBin();
            ResetDiscardBin();
        }

        private void RotateUnderCursor(Vector2 screen)
        {
            Cell cell;
            BaseBagSource source = FindBagAt(screen, out cell);
            if (source == null)
            {
                return;
            }

            int instanceId = source.Bag.Grid.GetOccupant(cell.X, cell.Y);
            if (instanceId == 0)
            {
                return;
            }

            string error;
            if (!source.Bag.TryRotate(instanceId, out error))
            {
                if (Message != null)
                {
                    Message("空间不足，无法旋转。");
                }

                return;
            }

            source.View.Refresh();
            if (Changed != null)
            {
                Changed();
            }

            PlacedItem item;
            if (Message != null && source.Bag.TryGet(instanceId, out item))
            {
                Message("已旋转 " + _catalog.Get(item.DefinitionId).DisplayName + "。");
            }
        }

        private bool TryQuickMove(BaseBagSource source, int instanceId)
        {
            if (QuickMoveTarget == null || source == null || source.Bag == null)
            {
                return false;
            }

            PlacedItem item;
            if (!source.Bag.TryGet(instanceId, out item))
            {
                return false;
            }

            if (CanTake != null)
            {
                string blocked = CanTake(source, item);
                if (blocked != null)
                {
                    if (Message != null)
                    {
                        Message(blocked);
                    }

                    return false;
                }
            }

            BaseBagSource target = QuickMoveTarget(source, item);
            if (target == null || target == source || target.Bag == null || !target.CanReceive)
            {
                if (Message != null)
                {
                    Message("无法快速移动。");
                }

                return false;
            }

            ItemDefinition definition = _catalog.Get(item.DefinitionId);
            if (target.IsLoot
                || (target.AllowedCategory.HasValue && definition.Category != target.AllowedCategory.Value)
                || (target.ItemFilter != null && !target.ItemFilter(definition))
                || (!target.AcceptsActionItems && definition.Category == ItemCategory.Action)
                || (target.IsSecure && !SecureContainerRules.IsEligible(definition))
                || (definition.Category == ItemCategory.Currency && target.Name != "STASH"))
            {
                return false;
            }

            PlacedItem placed;
            string error;
            if (!target.Bag.TryAddOrStack(
                    definition.Id,
                    item.BaseShape,
                    item.Rotatable,
                    item.Count,
                    definition.MaxStack,
                    out placed,
                    out error))
            {
                if (Message != null)
                {
                    Message(error);
                }

                return false;
            }

            ItemTransfer.PreserveAll(item, placed);
            source.Bag.Remove(instanceId);
            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].View.Refresh();
            }

            if (Changed != null)
            {
                Changed();
            }

            if (Message != null)
            {
                Message("已快速移动 " + definition.DisplayName + "。");
            }

            _lastClickTime = -1f;
            return true;
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

        private void EndDrag()
        {
            if (_item == null)
            {
                CancelDrag();
                return;
            }

            if (_source != null && _source.IsMerchant)
            {
                EndMerchantDrag();
                return;
            }

            bool changed = false;
            string message = null;
            bool sealedContainer = ContainerOps.HasContents(_item);

            if (_overDiscard)
            {
                ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                if (sealedContainer)
                {
                    message = "请先清空容器再丢弃。";
                }
                else if (definition.Category == ItemCategory.Key || definition.Category == ItemCategory.Currency)
                {
                    message = definition.Category == ItemCategory.Key
                        ? "潜网密钥不能丢弃。"
                        : "比特不能丢弃。";
                }
                else
                {
                    _source.Bag.Remove(_item.InstanceId);
                    changed = true;
                    message = "已丢弃 " + definition.DisplayName + "。";
                }
            }
            else if (_overSell)
            {
                if (sealedContainer)
                {
                    message = "请先清空容器再出售。";
                }
                else
                {
                int value;
                string error;
                if (_merchant.TrySell(_source.Bag, _item.InstanceId, _campaign, out value, out error))
                {
                    message = "卖出 " + _catalog.Get(_item.DefinitionId).DisplayName + "，获得 " + value + " bit。";
                    changed = true;
                }
                else
                {
                    message = error;
                }
                }
            }
            else if (_targetBag != null && _targetBag.IsMerchant && _source != null && !_source.IsMerchant && _validTarget)
            {
                if (sealedContainer)
                {
                    message = "请先清空容器再出售。";
                }
                else
                {
                    int value;
                    string error;
                    if (_merchant.TrySellToStock(
                            _source.Bag,
                            _item.InstanceId,
                            _campaign,
                            _target,
                            _rotation,
                            out value,
                            out error))
                    {
                        message = "出售 " + _catalog.Get(_item.DefinitionId).DisplayName
                                  + "，获得 " + value + " bit，商人物资已回收。";
                        changed = true;
                    }
                    else
                    {
                        message = error;
                    }
                }
            }
            else if (_targetBag != null && _targetBag.IsMerchant && _source != null && !_source.IsMerchant)
            {
                message = sealedContainer ? "请先清空容器再出售。" : "仓库空间不足，无法出售。";
            }
            else if (_targetBag != null && _validTarget)
            {
                if (_targetBag == _source)
                {
                    if (_rotation == _item.Rotation)
                    {
                        string error;
                        changed = _source.Bag.TryMove(_item.InstanceId, _target, out error);
                        if (!changed)
                        {
                            message = error;
                        }
                    }
                    else
                    {
                        changed = ReaddToSameBag();
                        if (!changed)
                        {
                            message = "这里放不下旋转后的物品。";
                        }
                    }
                }
                else
                {
                    PlacedItem placed;
                    string error;
                    ItemDefinition definition = _catalog.Get(_item.DefinitionId);
                    if (_targetBag.Bag.TryStackAt(_item.DefinitionId, _item.BaseShape, _target, _rotation, _item.Count, definition.MaxStack, out placed, out error))
                    {
                        ItemTransfer.PreserveAll(_item, placed);
                        _source.Bag.Remove(_item.InstanceId);
                        changed = true;
                    }
                    else
                    {
                        message = error;
                    }
                }
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].View.Refresh();
            }

            HideAllGhosts();
            _floatRect.gameObject.SetActive(false);
            ResetSellBin();
            ResetDiscardBin();

            _dragging = false;
            _source = null;
            _item = null;
            _targetBag = null;
            _validTarget = false;
            _overSell = false;
            _overDiscard = false;

            if (message != null && Message != null)
            {
                Message(message);
            }

            if (changed && Changed != null)
            {
                Changed();
            }
        }

        private bool CanAffordMerchantItem()
        {
            if (_merchant == null || _campaign == null || _item == null)
            {
                return false;
            }

            int offerIndex = _merchant.FindOfferIndexByInstance(_item.InstanceId);
            return offerIndex >= 0 && _campaign.Bit >= _merchant.Offers[offerIndex].Price;
        }

        private void EndMerchantDrag()
        {
            string message = null;
            bool changed = false;

            if (_targetBag != null && _targetBag == _source)
            {
                string error;
                changed = _source.Bag.TryTransform(_item.InstanceId, _target, _rotation, out error);
                if (!changed)
                {
                    message = "货架空间不足。";
                }
            }
            else if (_targetBag != null && _validTarget && _targetBag.IsPurchaseTarget)
            {
                int offerIndex = _merchant.FindOfferIndexByInstance(_item.InstanceId);
                if (offerIndex < 0)
                {
                    message = "商品已售罄。";
                }
                else
                {
                    MerchantOffer offer = _merchant.Offers[offerIndex];
                    if (_campaign.Bit < offer.Price)
                    {
                        message = "bit 不足，无法放入仓库。";
                    }
                    else
                    {
                        PlacedItem placed;
                        string error;
                        if (_merchant.TryBuyAt(offerIndex, _campaign, _targetBag.Bag, _target, _rotation, out placed, out error))
                        {
                            message = "买入 " + _catalog.Get(_item.DefinitionId).DisplayName
                                      + "，花费 " + offer.Price + " bit。";
                            changed = true;
                        }
                        else
                        {
                            message = error;
                        }
                    }
                }
            }
            else if (_targetBag != null && _targetBag.IsPurchaseTarget)
            {
                message = CanAffordMerchantItem()
                    ? "仓库空间不足。"
                    : "bit 不足，无法放入仓库。";
            }
            else if (_targetBag != null && _targetBag != _source)
            {
                message = "拖入仓库才能购买。";
            }

            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].View != null)
                {
                    _sources[i].View.Refresh();
                }
            }

            HideAllGhosts();
            _floatRect.gameObject.SetActive(false);
            ResetSellBin();
            ResetDiscardBin();

            _dragging = false;
            _source = null;
            _item = null;
            _targetBag = null;
            _validTarget = false;
            _overSell = false;
            _overDiscard = false;

            if (message != null && Message != null)
            {
                Message(message);
            }

            if (changed && Changed != null)
            {
                Changed();
            }
        }

        private bool ReaddToSameBag()
        {
            string definitionId = _item.DefinitionId;
            ItemShape baseShape = _item.BaseShape;
            bool rotatable = _item.Rotatable;

            _source.Bag.Remove(_item.InstanceId);

            int count = _item.Count;
            PlacedItem placed;
            string error;
            if (_source.Bag.TryAddAt(definitionId, baseShape, _target, _rotation, rotatable, count, out placed, out error))
            {
                ItemTransfer.PreserveAll(_item, placed);
                return true;
            }

            _source.Bag.TryAddAt(definitionId, baseShape, _originalOrigin, _originalRotation, rotatable, count, out placed, out error);
            ItemTransfer.PreserveAll(_item, placed);
            return false;
        }

        private void CancelDrag()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i].View == null)
                {
                    continue;
                }

                _sources[i].View.Refresh();
                _sources[i].View.HideGhost();
            }

            if (_floatRect != null)
            {
                _floatRect.gameObject.SetActive(false);
            }

            ResetSellBin();
            ResetDiscardBin();

            _dragging = false;
            _source = null;
            _item = null;
            _targetBag = null;
            _validTarget = false;
            _overSell = false;
            _overDiscard = false;
        }

        private BaseBagSource FindBagAt(Vector2 screen, out Cell cell)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (!PointerInsideSource(_sources[i], screen))
                {
                    continue;
                }

                Cell candidate;
                if (TryScreenToCell(_sources[i].Rect, _sources[i].Bag, _sources[i].View, screen, out candidate))
                {
                    cell = candidate;
                    return _sources[i];
                }
            }

            cell = Cell.Zero;
            return null;
        }

        private bool IsOverSellBin(Vector2 screen)
        {
            return _sellBin != null && RectTransformUtility.RectangleContainsScreenPoint(_sellBin, screen, null);
        }

        private bool IsOverDiscardBin(Vector2 screen)
        {
            return _discardBin != null && RectTransformUtility.RectangleContainsScreenPoint(_discardBin, screen, null);
        }

        private void ResetDiscardBin()
        {
            if (_discardBinImage != null)
            {
                _discardBinImage.color = SellBinColor;
            }
        }

        private void HideAllGhosts()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                _sources[i].View.HideGhost();
            }
        }

        private void ResetSellBin()
        {
            if (_sellBinImage != null)
            {
                _sellBinImage.color = SellBinColor;
            }
        }

        private void ShowFloating(Vector2 screen, bool overSell)
        {
            if (_item == null)
            {
                return;
            }

            ItemDefinition definition = _catalog.Get(_item.DefinitionId);
            ItemShape shape = _item.BaseShape.Rotate(_rotation);

            _floatImage.sprite = GetSprite(definition, shape);
            _floatImage.color = overSell
                ? new Color(1f, 0.86f, 0.45f, 0.8f)
                : new Color(1f, 1f, 1f, 0.72f);
            int cellSize = _source != null && _source.View != null
                ? _source.View.ViewCellSize
                : BagView.CellSize;
            _floatRect.sizeDelta = new Vector2(shape.Width * cellSize, shape.Height * cellSize);

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

        private static bool PointerInsideSource(BaseBagSource source, Vector2 screen)
        {
            return source == null
                   || source.Viewport == null
                   || RectTransformUtility.RectangleContainsScreenPoint(source.Viewport, screen, null);
        }

        private static bool TryScreenToCell(
            RectTransform rect,
            BagInventory bag,
            BagView view,
            Vector2 screen,
            out Cell cell)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out local))
            {
                cell = Cell.Zero;
                return false;
            }

            int cellSize = view != null ? view.ViewCellSize : BagView.CellSize;
            float halfWidth = bag.Grid.Width * cellSize * 0.5f;
            float halfHeight = bag.Grid.Height * cellSize * 0.5f;

            int x = Mathf.FloorToInt((local.x + halfWidth) / cellSize);
            int y = Mathf.FloorToInt((halfHeight - local.y) / cellSize);

            cell = new Cell(x, y);
            return x >= 0 && x < bag.Grid.Width && y >= 0 && y < bag.Grid.Height;
        }
    }
}

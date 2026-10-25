using System;
using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class ItemView : MonoBehaviour
    {
        public int InstanceId;
        public Image Image;
        public Text CountText;
        public Text Glyph;
        public GameObject DisabledOverlay;
        public Outline Highlight;
    }

    public sealed class BagView : MonoBehaviour
    {
        public const int CellSize = 16;

        private readonly Dictionary<int, ItemView> _views = new Dictionary<int, ItemView>();
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _ghostCache = new Dictionary<string, Sprite>();

        private BagInventory _inventory;
        private RectTransform _bagRect;
        private RectTransform _itemLayer;
        private RectTransform _ghostRect;
        private Image _ghostImage;
        private Dictionary<string, Color> _palette;
        private ItemCatalog _catalog;
        private readonly HashSet<int> _highlighted = new HashSet<int>();

        public Func<PlacedItem, bool> ItemEnabledPredicate;

        public void Initialize(BagInventory inventory, RectTransform bagRect, Dictionary<string, Color> palette, ItemCatalog catalog = null)
        {
            _inventory = inventory;
            _bagRect = bagRect;
            _palette = palette;
            _catalog = catalog;

            BuildGrid();
            BuildLayers();
        }

        public void Rebind(BagInventory inventory)
        {
            _inventory = inventory;
            for (int i = _bagRect.childCount - 1; i >= 0; i--)
            {
                Destroy(_bagRect.GetChild(i).gameObject);
            }

            _views.Clear();
            _spriteCache.Clear();
            _ghostCache.Clear();
            _ghostRect = null;
            _ghostImage = null;
            _itemLayer = null;

            BuildGrid();
            BuildLayers();
        }

        public void Refresh()
        {
            if (_itemLayer == null)
            {
                return;
            }

            foreach (Transform child in _itemLayer)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            _views.Clear();

            IReadOnlyList<PlacedItem> items = _inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                CreateItemView(items[i]);
            }

            ApplyHighlights();
        }

        public void SetHighlightedItems(IReadOnlyList<int> instanceIds)
        {
            bool changed = false;
            if (instanceIds == null || instanceIds.Count == 0)
            {
                if (_highlighted.Count > 0)
                {
                    _highlighted.Clear();
                    changed = true;
                }
            }
            else
            {
                var next = new HashSet<int>();
                for (int i = 0; i < instanceIds.Count; i++)
                {
                    next.Add(instanceIds[i]);
                }

                if (!next.SetEquals(_highlighted))
                {
                    _highlighted.Clear();
                    foreach (int id in next)
                    {
                        _highlighted.Add(id);
                    }

                    changed = true;
                }
            }

            if (changed)
            {
                ApplyHighlights();
            }
        }

        public void ClearHighlights()
        {
            SetHighlightedItems(null);
        }

        private void ApplyHighlights()
        {
            foreach (ItemView view in _views.Values)
            {
                bool highlighted = _highlighted.Contains(view.InstanceId);
                if (highlighted && view.Highlight == null)
                {
                    Outline outline = view.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.78f, 0.25f, 0.95f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    view.Highlight = outline;
                }
                else if (!highlighted && view.Highlight != null)
                {
                    Destroy(view.Highlight);
                    view.Highlight = null;
                }
            }
        }

        /// <summary>按实例 ID 取物品视图；用于战斗动画定位被使用/消耗的道具。</summary>
        public bool TryGetItemView(int instanceId, out ItemView view)
        {
            return _views.TryGetValue(instanceId, out view);
        }

        public void SetItemAlpha(int instanceId, float alpha)
        {
            ItemView view;
            if (!_views.TryGetValue(instanceId, out view))
            {
                return;
            }

            Color color = view.Image.color;
            color.a = alpha;
            view.Image.color = color;
        }

        public void ShowGhost(ItemShape shape, Cell origin, bool valid)
        {
            _ghostImage.sprite = GetGhostSprite(shape);
            _ghostRect.sizeDelta = new Vector2(shape.Width * CellSize, shape.Height * CellSize);
            _ghostRect.anchoredPosition = AnchoredFor(origin);
            _ghostImage.color = valid
                ? new Color(0.35f, 1f, 0.45f, 0.55f)
                : new Color(1f, 0.3f, 0.3f, 0.55f);
            _ghostRect.gameObject.SetActive(true);
        }

        public void HideGhost()
        {
            if (_ghostRect != null)
            {
                _ghostRect.gameObject.SetActive(false);
            }
        }

        public Vector2 AnchoredFor(Cell origin)
        {
            float width = _inventory.Grid.Width * CellSize;
            float height = _inventory.Grid.Height * CellSize;
            return new Vector2(-width * 0.5f + origin.X * CellSize, height * 0.5f - origin.Y * CellSize);
        }

        private void BuildGrid()
        {
            for (int y = 0; y < _inventory.Grid.Height; y++)
            {
                for (int x = 0; x < _inventory.Grid.Width; x++)
                {
                    var cellGo = new GameObject("Cell_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    cellGo.transform.SetParent(_bagRect, false);

                    var rect = (RectTransform)cellGo.transform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.sizeDelta = new Vector2(CellSize, CellSize);
                    rect.anchoredPosition = AnchoredFor(new Cell(x, y));

                    Image image = cellGo.GetComponent<Image>();
                    image.sprite = SpriteFactory.CellBackground;
                    image.raycastTarget = false;
                }
            }
        }

        private void BuildLayers()
        {
            _itemLayer = CreateStretchLayer("Items");
            _ghostRect = CreateCenteredLayer("Ghost");
            _ghostImage = _ghostRect.gameObject.AddComponent<Image>();
            _ghostImage.raycastTarget = false;
            _ghostRect.gameObject.SetActive(false);
        }

        private RectTransform CreateStretchLayer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_bagRect, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform CreateCenteredLayer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_bagRect, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(CellSize, CellSize);
            return rect;
        }

        private void CreateItemView(PlacedItem item)
        {
            var go = new GameObject("Item_" + item.InstanceId, typeof(RectTransform), typeof(Image), typeof(ItemView));
            go.transform.SetParent(_itemLayer, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = AnchoredFor(item.Origin);
            rect.sizeDelta = new Vector2(item.Shape.Width * CellSize, item.Shape.Height * CellSize);

            Image image = go.GetComponent<Image>();
            image.sprite = GetItemSprite(item);
            image.raycastTarget = false;

            var view = go.GetComponent<ItemView>();
            view.InstanceId = item.InstanceId;
            view.Image = image;

            if (item.Count > 1)
            {
                var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
                countGo.transform.SetParent(go.transform, false);

                var countRect = (RectTransform)countGo.transform;
                countRect.anchorMin = new Vector2(0.5f, 0.5f);
                countRect.anchorMax = new Vector2(0.5f, 0.5f);
                countRect.pivot = new Vector2(1f, 0f);
                countRect.sizeDelta = new Vector2(rect.sizeDelta.x - 4f, 12f);
                countRect.anchoredPosition = new Vector2(rect.sizeDelta.x * 0.5f - 2f, -rect.sizeDelta.y * 0.5f + 1f);

                var count = countGo.GetComponent<Text>();
                count.font = GameFonts.Ui;
                count.fontSize = 8;
                count.fontStyle = FontStyle.Bold;
                count.color = Color.white;
                count.alignment = TextAnchor.LowerRight;
                count.text = item.Count.ToString();
                count.raycastTarget = false;
                count.horizontalOverflow = HorizontalWrapMode.Overflow;
                count.verticalOverflow = VerticalWrapMode.Overflow;
                view.CountText = count;
            }

            if (_catalog != null)
            {
                ItemDefinition definition;
                if (_catalog.TryGet(item.DefinitionId, out definition)
                    && (definition.Category == ItemCategory.Action || definition.Assist != null))
                {
                    var glyphGo = new GameObject("Glyph", typeof(RectTransform), typeof(Text));
                    glyphGo.transform.SetParent(go.transform, false);

                    var glyphRect = (RectTransform)glyphGo.transform;
                    glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
                    glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
                    glyphRect.pivot = new Vector2(0.5f, 0.5f);
                    glyphRect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y);

                    var glyph = glyphGo.GetComponent<Text>();
                    glyph.font = GameFonts.Ui;
                    glyph.fontSize = definition.Assist != null ? 10 : 20;
                    glyph.color = new Color32(240, 244, 250, 255);
                    glyph.alignment = TextAnchor.MiddleCenter;
                    glyph.text = definition.Assist != null
                        ? (definition.DisplayName.Length > 0 ? definition.DisplayName.Substring(0, 1) : "辅")
                        : ExpeditionActionItemLibrary.Glyph(item.DefinitionId);
                    glyph.raycastTarget = false;
                    glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
                    glyph.verticalOverflow = VerticalWrapMode.Overflow;
                    view.Glyph = glyph;
                }
            }

            var disabledGo = new GameObject("Disabled", typeof(RectTransform), typeof(Image));
            disabledGo.transform.SetParent(go.transform, false);

            var disabledRect = (RectTransform)disabledGo.transform;
            disabledRect.anchorMin = Vector2.zero;
            disabledRect.anchorMax = Vector2.one;
            disabledRect.offsetMin = Vector2.zero;
            disabledRect.offsetMax = Vector2.zero;

            Image disabledImage = disabledGo.GetComponent<Image>();
            disabledImage.sprite = SpriteFactory.White;
            disabledImage.color = new Color(0.06f, 0.07f, 0.08f, 0.62f);
            disabledImage.raycastTarget = false;
            disabledGo.SetActive(ItemEnabledPredicate != null && !ItemEnabledPredicate(item));
            view.DisabledOverlay = disabledGo;

            _views[item.InstanceId] = view;
        }

        private Sprite GetItemSprite(PlacedItem item)
        {
            string key = item.DefinitionId + "|" + item.Rotation;
            Sprite sprite;
            if (_spriteCache.TryGetValue(key, out sprite))
            {
                return sprite;
            }

            Color fill;
            if (!_palette.TryGetValue(item.DefinitionId, out fill))
            {
                fill = Color.magenta;
            }

            Color border = new Color(fill.r * 0.45f, fill.g * 0.45f, fill.b * 0.45f, 1f);
            sprite = SpriteFactory.CreateShapeSprite(item.Shape, fill, border);
            _spriteCache[key] = sprite;
            return sprite;
        }

        private Sprite GetGhostSprite(ItemShape shape)
        {
            string key = shape.ToPattern();
            Sprite sprite;
            if (!_ghostCache.TryGetValue(key, out sprite))
            {
                sprite = SpriteFactory.CreateShapeSprite(shape, Color.white, Color.white);
                _ghostCache[key] = sprite;
            }

            return sprite;
        }
    }
}

using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class ContainerWindow : MonoBehaviour
    {
        private FloatingWindowManager _manager;
        private RectTransform _rect;
        private RectTransform _canvas;
        private RectTransform _titleBar;
        private RectTransform _closeRect;
        private ItemContainer _container;
        private bool _draggingWindow;
        private Vector2 _windowDragOffset;

        public int ContainerId
        {
            get { return _container == null ? 0 : _container.Id; }
        }

        public ItemContainer Container
        {
            get { return _container; }
        }

        public BaseBagSource Source { get; private set; }

        public BagView View { get; private set; }

        public void Initialize(
            FloatingWindowManager manager,
            ItemContainer container,
            ItemDefinition definition,
            RectTransform canvas,
            ItemCatalog catalog,
            System.Collections.Generic.Dictionary<string, Color> palette)
        {
            _manager = manager;
            _container = container;
            _rect = (RectTransform)transform;
            _canvas = canvas;

            int innerWidth = Mathf.Max(1, container.Spec.Width) * BagView.CellSize;
            int innerHeight = Mathf.Max(1, container.Spec.Height) * BagView.CellSize;
            float windowWidth = Mathf.Max(124f, innerWidth + 16f);
            float windowHeight = innerHeight + 34f;
            _rect.sizeDelta = new Vector2(windowWidth, windowHeight);

            Image background = GetComponent<Image>();
            background.color = new Color32(18, 22, 28, 248);
            background.raycastTarget = true;

            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(92, 74, 50, 255);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            RectTransform titleBar = CreateAnchored(
                transform,
                "TitleBar",
                Vector2.zero,
                new Vector2(windowWidth, 22f),
                new Color32(48, 40, 31, 255));
            titleBar.GetComponent<Image>().raycastTarget = true;
            _titleBar = titleBar;

            Text title = CreateText(
                titleBar,
                "Title",
                definition.DisplayName + "  " + container.Spec.Width + "×" + container.Spec.Height,
                9,
                new Color32(236, 222, 190, 255),
                new Vector2(7f, -4f),
                new Vector2(windowWidth - 34f, 14f));
            title.raycastTarget = false;

            RectTransform close = CreateAnchored(
                titleBar,
                "Close",
                Vector2.zero,
                new Vector2(18f, 18f),
                new Color32(78, 48, 43, 255));
            close.anchorMin = new Vector2(1f, 1f);
            close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-3f, -2f);
            _closeRect = close;
            CreateCenteredText(close, "Label", "×", 12, Color.white, Vector2.zero, new Vector2(18f, 18f));

            RectTransform content = CreateAnchored(
                transform,
                "Content",
                new Vector2(8f, -26f),
                new Vector2(innerWidth, innerHeight),
                Color.clear);
            content.GetComponent<Image>().raycastTarget = false;

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(content, false);
            RectTransform grid = (RectTransform)gridGo.transform;
            grid.anchorMin = new Vector2(0.5f, 0.5f);
            grid.anchorMax = new Vector2(0.5f, 0.5f);
            grid.pivot = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2(innerWidth, innerHeight);
            grid.anchoredPosition = Vector2.zero;

            View = gridGo.AddComponent<BagView>();
            View.Initialize(container.Bag, grid, palette, catalog);

            Source = new BaseBagSource
            {
                Name = "CONTAINER_" + container.Id,
                Bag = container.Bag,
                Rect = grid,
                View = View,
                IsContainerStorage = true,
                AcceptsActionItems = false,
                ItemFilter = container.CanAccept
            };

        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || _manager == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            if (!_manager.IsTopmost(this, screen))
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_closeRect, screen, null))
                {
                    _manager.Close(ContainerId);
                    return;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(_titleBar, screen, null))
                {
                    _manager.BringToFront(this);
                    Vector2 local;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out local))
                    {
                        _draggingWindow = true;
                        _windowDragOffset = _rect.anchoredPosition - local;
                    }
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(_rect, screen, null))
                {
                    _manager.BringToFront(this);
                }
            }

            if (_draggingWindow && mouse.leftButton.isPressed)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out local))
                {
                    Vector2 halfCanvas = _canvas.rect.size * 0.5f;
                    Vector2 halfWindow = _rect.sizeDelta * 0.5f;
                    Vector2 position = local + _windowDragOffset;
                    position.x = Mathf.Clamp(position.x, -halfCanvas.x + halfWindow.x, halfCanvas.x - halfWindow.x);
                    position.y = Mathf.Clamp(position.y, -halfCanvas.y + halfWindow.y, halfCanvas.y - halfWindow.y);
                    _rect.anchoredPosition = position;
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _draggingWindow = false;
            }
        }

        public bool IsChromeAt(Vector2 screen)
        {
            return (_titleBar != null
                    && RectTransformUtility.RectangleContainsScreenPoint(_titleBar, screen, null))
                   || (_closeRect != null
                       && RectTransformUtility.RectangleContainsScreenPoint(_closeRect, screen, null));
        }

        public void Refresh()
        {
            if (View != null)
            {
                View.Refresh();
            }
        }

        public void SetPosition(Vector2 position)
        {
            if (_rect != null)
            {
                _rect.anchoredPosition = position;
            }
        }

        public void ClampTo(RectTransform canvas)
        {
            if (_rect == null || canvas == null)
            {
                return;
            }

            Vector2 halfCanvas = canvas.rect.size * 0.5f;
            Vector2 halfWindow = _rect.sizeDelta * 0.5f;
            Vector2 position = _rect.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -halfCanvas.x + halfWindow.x, halfCanvas.x - halfWindow.x);
            position.y = Mathf.Clamp(position.y, -halfCanvas.y + halfWindow.y, halfCanvas.y - halfWindow.y);
            _rect.anchoredPosition = position;
        }

        private static RectTransform CreateAnchored(
            Transform parent,
            string name,
            Vector2 topLeft,
            Vector2 size,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int size,
            Color color,
            Vector2 position,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Ui;
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateCenteredText(
            Transform parent,
            string name,
            string content,
            int size,
            Color color,
            Vector2 position,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Ui;
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}

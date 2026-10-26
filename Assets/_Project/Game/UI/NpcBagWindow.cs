using System;
using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    /// <summary>
    /// NPC 柜台：一个可拖动的小悬浮窗，里面就是那位联系人的格子。
    ///
    /// 它复用 <see cref="BagView"/> 与 <see cref="BaseDragInteraction"/>：
    /// 拖出 = 购买 / 接单，拖入 = 交付，规则都在 <see cref="NpcBagFlow"/> 里，
    /// 这里只负责画面与「窗口自己的拖动/关闭」。
    /// </summary>
    public sealed class NpcBagWindow : MonoBehaviour
    {
        private const string SourceName = "NPCBAG";

        private RectTransform _rect;
        private RectTransform _canvas;
        private RectTransform _titleBar;
        private RectTransform _closeRect;
        private Text _titleText;
        private BagInventory _bag;
        private Action<string> _onClose;
        private bool _draggingWindow;
        private Vector2 _dragOffset;

        public BaseBagSource Source { get; private set; }

        public BagView View { get; private set; }

        public void Initialize(
            RectTransform canvas,
            ItemCatalog catalog,
            Dictionary<string, Color> palette,
            Action<string> onClose)
        {
            _canvas = canvas;
            _onClose = onClose;
            _rect = (RectTransform)transform;
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(140f, 88f);

            Image background = GetComponent<Image>();
            background.color = new Color32(18, 22, 28, 250);
            background.raycastTarget = true;

            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(92, 74, 50, 255);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            // 紧凑尺寸：柜台要挤进艺术台右下的空档，不能盖住上面的台词。
            RectTransform titleBar = CreateAnchored(
                transform, "TitleBar", Vector2.zero, new Vector2(140f, 18f), new Color32(48, 40, 31, 255));
            titleBar.GetComponent<Image>().raycastTarget = true;
            _titleBar = titleBar;

            _titleText = CreateText(
                titleBar, "Title", string.Empty, 8, new Color32(236, 222, 190, 255),
                new Vector2(7f, -2f), new Vector2(104f, 14f));
            _titleText.raycastTarget = false;

            RectTransform close = CreateAnchored(
                titleBar, "Close", Vector2.zero, new Vector2(14f, 14f), new Color32(78, 48, 43, 255));
            close.anchorMin = new Vector2(1f, 1f);
            close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-2f, -2f);
            _closeRect = close;
            CreateCenteredText(close, "Label", "×", 11, Color.white, Vector2.zero, new Vector2(16f, 16f));

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(transform, false);
            RectTransform grid = (RectTransform)gridGo.transform;
            grid.anchorMin = new Vector2(0.5f, 0.5f);
            grid.anchorMax = new Vector2(0.5f, 0.5f);
            grid.pivot = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2(6f * BagView.CellSize, 4f * BagView.CellSize);
            grid.anchoredPosition = new Vector2(0f, -6f);

            View = gridGo.AddComponent<BagView>();
            var placeholder = new BagInventory(6, 4);
            Source = new BaseBagSource
            {
                Name = SourceName,
                Bag = placeholder,
                Rect = grid,
                View = View
            };

            View.Initialize(placeholder, grid, palette, catalog);
        }

        public void Bind(BagInventory bag, string title, string hint)
        {
            _bag = bag;
            Source.Bag = bag;
            View.Rebind(bag);
            View.Refresh();

            if (_titleText != null)
            {
                _titleText.text = title;
            }

        }

        public void Refresh()
        {
            if (View != null && _bag != null)
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

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || _rect == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_closeRect, screen, null))
                {
                    if (_onClose != null)
                    {
                        _onClose(SourceName);
                    }

                    return;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(_titleBar, screen, null))
                {
                    Vector2 local;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out local))
                    {
                        _draggingWindow = true;
                        _dragOffset = _rect.anchoredPosition - local;
                    }
                }
            }

            if (_draggingWindow && mouse.leftButton.isPressed)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, screen, null, out local))
                {
                    Vector2 halfCanvas = _canvas.rect.size * 0.5f;
                    Vector2 halfWindow = _rect.sizeDelta * 0.5f;
                    Vector2 position = local + _dragOffset;
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

        private static RectTransform CreateAnchored(
            Transform parent, string name, Vector2 topLeft, Vector2 size, Color color)
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
            Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
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
            Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
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

using UnityEngine;
using UnityEngine.UI;

namespace BitRunner.Game
{
    /// <summary>
    /// UI 控件构造的唯一定义处。
    ///
    /// 这些方法原先在 Base / Expedition / Combat 三个 Bootstrap 里各写一遍（共 7 个方法 × 3 份，
    /// 逐字一致）。改一处颜色或尺寸要改三处，是界面漂移的根源。
    ///
    /// 坐标语义（全项目统一）：
    ///   CreateCenteredPanel(center)  —— center 是相对屏幕中心 (320,180) 的偏移，pivot 居中
    ///   CreateAnchoredPanel(topLeft) —— topLeft 是相对父物体左上角的位置，pivot 左上
    /// 换算：cx = x0 + w/2 − 320，cy = 180 − (y0 + h/2)
    /// </summary>
    public static class UiFactory
    {
        public static Font GetFont()
        {
            return GameFonts.Ui;
        }

        /// <summary>1px 描边。用 Outline 而不是 Image 叠层，改色时只需改一处。</summary>
        public static void AddBorder(RectTransform rect, Color color)
        {
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        /// <summary>纯布局容器，无 Image，不可交互。</summary>
        public static RectTransform CreateAnchoredRect(Transform parent, string name, Vector2 topLeft, Vector2 size)
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

        /// <summary>左上角锚定的面板。</summary>
        public static RectTransform CreateAnchoredPanel(Transform parent, string name, Vector2 topLeft, Vector2 size, Color color)
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

        /// <summary>居中的面板：所有主界面面板都用这个，方便按屏幕中心算坐标。</summary>
        public static RectTransform CreateCenteredPanel(Transform parent, string name, Vector2 center, Vector2 size, Color color)
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

        /// <summary>带居中文字的面板，按钮基本都用它。</summary>
        public static RectTransform CreateButton(Transform parent, string name, string label, Vector2 topLeft, Vector2 size, Color color)
        {
            RectTransform rect = CreateAnchoredPanel(parent, name, topLeft, size, color);
            CreateCenteredText(rect, "Label", label, UiTheme.FontBody, UiTheme.TextColor, Vector2.zero, new Vector2(size.x - 4f, size.y - 2f));
            return rect;
        }

        /// <summary>物品格容器，中心锚定。默认尺寸是 12×10 格。</summary>
        public static RectTransform CreateBagRect(Transform parent, Vector2 center)
        {
            return CreateBagRect(parent, center, new Vector2(192f, 160f));
        }

        public static RectTransform CreateBagRect(Transform parent, Vector2 center, Vector2 size)
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

        /// <summary>左上角锚定的文本，默认自动换行、纵向溢出。</summary>
        public static Text CreateText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta, TextAnchor alignment)
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

        /// <summary>居中对齐的文本。</summary>
        public static Text CreateCenteredText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
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

        /// <summary>
        /// 进度条：深底 + 水平填充。返回 fill 的 Image，改 fillAmount 即改长度。
        /// </summary>
        public static Image CreateBar(Transform parent, string name, Vector2 topLeft, Vector2 size, Color fillColor)
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

        /// <summary>建一张铺满父物体的全屏遮罩，用于弹窗背景。</summary>
        public static RectTransform CreateFullScreenRect(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }
    }
}

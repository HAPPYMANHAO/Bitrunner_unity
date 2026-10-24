using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private sealed class MenuButton
        {
            public RectTransform Rect;
            public Image Image;
            public Text Label;
            public Color NormalColor;
        }

        private static readonly Color BackgroundColor = new Color32(16, 19, 24, 255);
        private static readonly Color PanelColor = new Color32(26, 30, 38, 255);
        private static readonly Color PanelLight = new Color32(33, 38, 47, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        private static readonly Color AccentHoverColor = new Color32(78, 138, 198, 255);
        private static readonly Color DangerColor = new Color32(95, 58, 54, 255);

        private readonly List<MenuButton> _buttons = new List<MenuButton>();
        private RectTransform _canvasRect;
        private GameObject _helpOverlay;
        private RectTransform _backRect;
        private Image _backImage;
        private bool _helpOpen;

        private void Start()
        {
            Canvas canvas = CreateCanvas();
            _canvasRect = (RectTransform)canvas.transform;

            BuildBackground(canvas.transform);
            BuildShowcase(canvas.transform);
            BuildTitle(canvas.transform);
            BuildButtons(canvas.transform);
            BuildFooter(canvas.transform);
            BuildHelp(canvas.transform);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (_helpOpen)
            {
                bool overBack = RectTransformUtility.RectangleContainsScreenPoint(_backRect, screen, null);
                _backImage.color = overBack ? AccentHoverColor : AccentColor;
                if (mouse.leftButton.wasPressedThisFrame && overBack)
                {
                    _helpOpen = false;
                    _helpOverlay.SetActive(false);
                }

                return;
            }

            for (int i = 0; i < _buttons.Count; i++)
            {
                MenuButton button = _buttons[i];
                bool over = RectTransformUtility.RectangleContainsScreenPoint(button.Rect, screen, null);
                button.Image.color = over ? AccentHoverColor : button.NormalColor;

                if (over && mouse.leftButton.wasPressedThisFrame)
                {
                    Activate(button);
                    return;
                }
            }
        }

        private void Activate(MenuButton button)
        {
            if (button.Label.text == "开始游戏")
            {
                GameSession.StartNewGame();
                SceneManager.LoadScene("BasePrototype");
            }
            else if (button.Label.text == "玩法说明")
            {
                _helpOpen = true;
                _helpOverlay.SetActive(true);
            }
            else if (button.Label.text == "退出游戏")
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void BuildBackground(Transform canvas)
        {
            RectTransform background = CreatePanel(canvas, "Background", Vector2.zero, new Vector2(640f, 360f), BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 22; x++)
                {
                    var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(background, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(2f, 2f);
                    rect.anchoredPosition = new Vector2(-308f + x * 28f, -156f + y * 28f);
                    Image image = go.GetComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.045f);
                    image.raycastTarget = false;
                }
            }
        }

        private void BuildShowcase(Transform canvas)
        {
            RectTransform panel = CreatePanel(canvas, "Showcase", new Vector2(-192f, 2f), new Vector2(184f, 184f), PanelColor);
            panel.GetComponent<Image>().raycastTarget = false;

            RectTransform grid = CreatePanel(panel, "Grid", new Vector2(0f, -8f), new Vector2(132f, 132f), new Color32(20, 24, 30, 255));
            grid.GetComponent<Image>().raycastTarget = false;

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    var go = new GameObject("Cell", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(grid, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(31f, 31f);
                    rect.anchoredPosition = new Vector2(-49.5f + x * 33f, 49.5f - y * 33f);
                    Image image = go.GetComponent<Image>();
                    image.color = new Color32(36, 42, 52, 255);
                    image.raycastTarget = false;
                }
            }

            AddItem(grid, new Vector2(-49.5f, 49.5f), new Vector2(64f, 64f), new Color32(80, 140, 220, 255));
            AddItem(grid, new Vector2(16.5f, 49.5f), new Vector2(64f, 31f), new Color32(232, 110, 64, 255));
            AddItem(grid, new Vector2(49.5f, 16.5f), new Vector2(31f, 31f), new Color32(79, 184, 200, 255));
            AddItem(grid, new Vector2(16.5f, 16.5f), new Vector2(31f, 31f), new Color32(90, 200, 130, 255));
            AddItem(grid, new Vector2(-49.5f, -16.5f), new Vector2(31f, 31f), new Color32(220, 200, 70, 255));
            AddItem(grid, new Vector2(16.5f, -16.5f), new Vector2(64f, 31f), new Color32(232, 110, 64, 255));

            CreateText(panel, "Caption", "PACK / EXTRACT / SURVIVE", 8, MutedColor, new Vector2(10f, -6f), new Vector2(164f, 12f), TextAnchor.UpperLeft);
        }

        private static void AddItem(Transform parent, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject("Item", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = center;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void BuildTitle(Transform canvas)
        {
            RectTransform panel = CreatePanel(canvas, "Title", new Vector2(116f, 118f), new Vector2(300f, 80f), PanelColor);
            panel.GetComponent<Image>().raycastTarget = false;
            CreateText(panel, "Cn", "比特跑者", 30, TextColor, new Vector2(12f, -8f), new Vector2(276f, 40f), TextAnchor.UpperLeft);
            CreateText(panel, "En", "BIT RUNNER", 10, AccentHoverColor, new Vector2(14f, -48f), new Vector2(276f, 18f), TextAnchor.UpperLeft);
            CreateText(panel, "Tag", "背包整理　·　搜打撤　·　还债", 9, MutedColor, new Vector2(14f, -64f), new Vector2(276f, 16f), TextAnchor.UpperLeft);
        }

        private void BuildButtons(Transform canvas)
        {
            AddButton(canvas, "开始游戏", new Vector2(116f, 40f), AccentColor);
            AddButton(canvas, "玩法说明", new Vector2(116f, -4f), AccentColor);
            AddButton(canvas, "退出游戏", new Vector2(116f, -48f), DangerColor);
        }

        private void AddButton(Transform canvas, string label, Vector2 center, Color color)
        {
            RectTransform rect = CreatePanel(canvas, "Button_" + label, center, new Vector2(244f, 32f), color);
            Text text = CreateCenteredText(rect, "Label", label, 13, TextColor, Vector2.zero, new Vector2(236f, 30f));
            _buttons.Add(new MenuButton
            {
                Rect = rect,
                Image = rect.GetComponent<Image>(),
                Label = text,
                NormalColor = color
            });
        }

        private void BuildHelp(Transform canvas)
        {
            _helpOverlay = new GameObject("HelpOverlay", typeof(RectTransform), typeof(Image));
            _helpOverlay.transform.SetParent(canvas, false);

            var rect = (RectTransform)_helpOverlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image shade = _helpOverlay.GetComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.82f);
            shade.raycastTarget = true;

            RectTransform box = CreatePanel(_helpOverlay.transform, "Box", Vector2.zero, new Vector2(540f, 300f), PanelColor);
            CreateCenteredText(box, "Title", "玩法说明", 18, TextColor, new Vector2(0f, 122f), new Vector2(500f, 26f));

            string body =
                "目标\n" +
                "每周 7 次探索，周末缴纳债务：400 / 700 / 1000 / 1300 / 1600 / 2000，之后固定 2000。\n" +
                "缴不上即破产；债务没有终点，只能靠变强撑下去。\n\n" +
                "基地\n" +
                "拖动物品整理背包与仓库；bit 与弹药是堆叠物品。保险箱只收 1×1 的非武器/护甲。\n" +
                "把物品拖到商人货架出售（收购价为价值的 50%，可原价购回）；把货架物品拖进仓库购买。商人每天补货。\n" +
                "潜网舱初始 1×2，密钥越大关卡越危险；矿场每天收取 bit；制作站放入材料后制作。\n" +
                "设施升级消耗 bit 与材料。\n\n" +
                "探索\n" +
                "每个地点会给出若干行动；选择行动并确认，每个行动都可能导向不同结果。\n" +
                "物资会进入格子容器；战斗胜利后回到当前地点，撤退则结束本次探索。\n" +
                "阵亡只丢失本次探索新捡到的战利品，出发时带在身上的装备会保留（耐久不回满）。\n\n" +
                "战斗\n" +
                "点击背包物品使用，结束回合推进；能量驱动行动。\n" +
                "护盾先于护甲吸收伤害；抛射武器消耗背包弹药。胜利后拖走战利品。";

            CreateText(box, "Body", body, 8, MutedColor, new Vector2(22f, -84f), new Vector2(496f, 172f), TextAnchor.UpperLeft);

            _backRect = CreatePanel(box, "Back", new Vector2(0f, -122f), new Vector2(120f, 28f), AccentColor);
            CreateCenteredText(_backRect, "Label", "返回", 11, TextColor, Vector2.zero, new Vector2(116f, 26f));
            _backImage = _backRect.GetComponent<Image>();

            _helpOverlay.SetActive(false);
            _helpOverlay.transform.SetAsLastSibling();
        }

        private void BuildFooter(Transform canvas)
        {
            CreateCenteredText(canvas, "Footer", "原型 v0.1　·　Unity 6", 8, MutedColor, new Vector2(0f, -172f), new Vector2(400f, 12f));
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

            var go = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 center, Vector2 size, Color color)
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
    }
}

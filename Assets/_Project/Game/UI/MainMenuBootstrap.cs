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
            public System.Action Click;
        }

        private const string TitleResourcePath = "Art/bt_title";

        private static readonly Color BackgroundColor = new Color32(16, 19, 24, 255);
        private static readonly Color PanelColor = new Color32(26, 30, 38, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        private static readonly Color AccentHoverColor = new Color32(78, 138, 198, 255);
        private static readonly Color DangerColor = new Color32(95, 58, 54, 255);

        private static Sprite _titleSprite;
        private static bool _titleSpriteLoaded;

        private readonly List<MenuButton> _buttons = new List<MenuButton>();
        private GameObject _helpOverlay;
        private RectTransform _backRect;
        private Image _backImage;
        private bool _helpOpen;

        private void Start()
        {
            Canvas canvas = CreateCanvas();

            BuildBackground(canvas.transform);
            BuildTitle(canvas.transform);
            BuildButtons(canvas.transform);
            BuildHelp(canvas.transform);
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

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
                    button.Click();
                    return;
                }
            }
        }

        private void ContinueOrNewGame()
        {
            string error;
            if (!SaveSystem.TryLoad(out error))
            {
                GameSession.StartNewGame();
                GameSession.LastMessage = "存档读取失败：" + error + " 已开始新战役。";
            }

            SceneManager.LoadScene("BasePrototype");
        }

        private void StartNewGame()
        {
            // 新开局同时清掉旧档，避免「从头开始但继续游戏还能读到上一局」。
            SaveSystem.DeleteSave();
            GameSession.StartNewGame();
            SceneManager.LoadScene("BasePrototype");
        }

        private void BuildBackground(Transform canvas)
        {
            RectTransform background = CreatePanel(canvas, "Background", Vector2.zero, new Vector2(640f, 360f), BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;
            BackgroundArt.Apply(background);

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

        /// <summary>
        /// 标题优先用 Resources/Art/bt_title.png。图被切成多张字形时按原图坐标拼回一行
        /// （保持原图的间距与重叠，整数倍放大保证像素清晰）；单张图直接铺；都没有时退回一行文字。
        /// 美术替换不需要改代码。
        /// </summary>
        private void BuildTitle(Transform canvas)
        {
            Sprite[] glyphs = Resources.LoadAll<Sprite>(TitleResourcePath);
            if (glyphs != null && glyphs.Length > 1)
            {
                // 备用路线：图被切成多张字形时按原图坐标拼回一行（切片需把 Sprite Extrude 设为 0）。
                BuildGlyphTitle(canvas, glyphs);
                return;
            }

            Sprite title = LoadTitleSprite();
            if (title != null)
            {
                var go = new GameObject("Title", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float pxWidth = Mathf.Max(1f, title.rect.width);
                float scale = Mathf.Max(1f, Mathf.FloorToInt(288f / pxWidth));
                rect.sizeDelta = new Vector2(title.rect.width * scale, title.rect.height * scale);
                rect.anchoredPosition = new Vector2(0f, 96f);

                var image = go.GetComponent<Image>();
                image.sprite = title;
                image.preserveAspect = false;
                image.raycastTarget = false;
                return;
            }

            CreateCenteredText(canvas, "Title", "BITRUNNER", 34, TextColor, new Vector2(0f, 96f), new Vector2(420f, 48f));
        }

        /// <summary>
        /// 单张标题图：如果贴图可读，先裁掉四周透明边（标题图常带大片空白），
        /// 这样布局位置和缩放都基于真实画面，不靠手填偏移量。
        /// </summary>
        private static Sprite LoadTitleSprite()
        {
            if (_titleSpriteLoaded)
            {
                return _titleSprite;
            }

            _titleSpriteLoaded = true;
            Sprite sprite = Resources.Load<Sprite>(TitleResourcePath);
            if (sprite == null)
            {
                return null;
            }

            Texture2D texture = sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                _titleSprite = sprite;
                return _titleSprite;
            }

            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a <= 0.01f)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                _titleSprite = sprite;
                return _titleSprite;
            }

            var rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            _titleSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
            _titleSprite.name = "bt_title_trimmed";
            return _titleSprite;
        }

        private void BuildGlyphTitle(Transform canvas, Sprite[] glyphs)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < glyphs.Length; i++)
            {
                Rect r = glyphs[i].rect;
                minX = Mathf.Min(minX, r.xMin);
                minY = Mathf.Min(minY, r.yMin);
                maxX = Mathf.Max(maxX, r.xMax);
                maxY = Mathf.Max(maxY, r.yMax);
            }

            float pixelWidth = Mathf.Max(1f, maxX - minX);
            float pixelHeight = Mathf.Max(1f, maxY - minY);
            float scale = Mathf.Max(1f, Mathf.FloorToInt(288f / pixelWidth));

            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var root = (RectTransform)go.transform;
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(pixelWidth * scale, pixelHeight * scale);
            root.anchoredPosition = new Vector2(0f, 96f);

            // 按原图像素坐标摆放，重叠/间距都与绘制时一致。
            for (int i = 0; i < glyphs.Length; i++)
            {
                Sprite glyph = glyphs[i];
                var part = new GameObject("Glyph_" + i, typeof(RectTransform), typeof(Image));
                part.transform.SetParent(root, false);
                var rect = (RectTransform)part.transform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(glyph.rect.width * scale, glyph.rect.height * scale);
                rect.anchoredPosition = new Vector2(
                    (glyph.rect.xMin - minX) * scale,
                    (glyph.rect.yMin - minY) * scale);

                var image = part.GetComponent<Image>();
                image.sprite = glyph;
                image.raycastTarget = false;
            }
        }

        private void BuildButtons(Transform canvas)
        {
            float y = 26f;
            if (SaveSystem.HasSave())
            {
                AddButton(canvas, "继续游戏", new Vector2(0f, y), AccentColor, ContinueOrNewGame);
                y -= 40f;
                AddButton(canvas, "重新开始", new Vector2(0f, y), AccentColor, StartNewGame);
                y -= 40f;
            }
            else
            {
                AddButton(canvas, "开始游戏", new Vector2(0f, y), AccentColor, StartNewGame);
                y -= 40f;
            }

            AddButton(canvas, "玩法说明", new Vector2(0f, y), AccentColor, OpenHelp);
            y -= 40f;
            AddButton(canvas, "设置", new Vector2(0f, y), AccentColor, SettingsOverlay.OpenPanel);
            y -= 40f;
            AddButton(canvas, "退出游戏", new Vector2(0f, y), DangerColor, QuitGame);
        }

        private void OpenHelp()
        {
            _helpOpen = true;
            _helpOverlay.SetActive(true);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AddButton(Transform canvas, string label, Vector2 center, Color color, System.Action click)
        {
            RectTransform rect = CreatePanel(canvas, "Button_" + label, center, new Vector2(244f, 32f), color);
            Text text = CreateCenteredText(rect, "Label", label, 13, TextColor, Vector2.zero, new Vector2(236f, 30f));
            _buttons.Add(new MenuButton
            {
                Rect = rect,
                Image = rect.GetComponent<Image>(),
                Label = text,
                NormalColor = color,
                Click = click
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
                "拖动物品整理背包与仓库；bit 与弹药是堆叠物品。保险箱收能放进 2×2 的非武器/护甲。\n" +
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

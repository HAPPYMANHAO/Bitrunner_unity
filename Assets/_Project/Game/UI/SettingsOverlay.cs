using System;
using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BitRunner.Game
{
    /// <summary>
    /// 全局设置面板：主菜单按钮或游戏内按 O 打开，Esc/再按 O 关闭。
    /// 面板挂在 DontDestroyOnLoad 的画布上，所有场景共用一份，打开时挡住下层输入。
    /// </summary>
    public sealed class SettingsOverlay : MonoBehaviour
    {
        private sealed class SettingButton
        {
            public RectTransform Rect;
            public Image Image;
            public Color NormalColor;
            public Func<bool> IsActive;
            public Action Click;
        }

        private static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelColor = new Color32(26, 30, 38, 255);
        private static readonly Color RowColor = new Color32(33, 38, 47, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(138, 146, 162, 255);
        private static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        private static readonly Color AccentHoverColor = new Color32(78, 138, 198, 255);

        private static SettingsOverlay _instance;
        private static int _escapeConsumedFrame = -1;
        private static int _modalCount;

        private readonly List<SettingButton> _buttons = new List<SettingButton>();
        private GameObject _root;
        private RectTransform _panelRect;
        private Text _volumeValue;
        private Text _statusText;
        private bool _open;
        private int _blockedUntilFrame = -1;

        /// <summary>设置面板打开（含关闭当帧）时，其他界面的 Update 应直接返回。</summary>
        public static bool BlocksInput
        {
            get
            {
                if (_instance == null)
                {
                    return false;
                }

                return _instance._open || Time.frameCount <= _instance._blockedUntilFrame;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("SettingsOverlay");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SettingsOverlay>();
        }

        /// <summary>标记本帧的 Esc 已被别的界面处理（关背包、关容器窗口等）。</summary>
        public static void ConsumeEscape()
        {
            _escapeConsumedFrame = Time.frameCount;
        }

        /// <summary>打开的模态界面计数：大于 0 时 Esc 不会打开设置。</summary>
        public static void PushModal()
        {
            _modalCount++;
        }

        public static void PopModal()
        {
            if (_modalCount > 0)
            {
                _modalCount--;
            }
        }

        /// <summary>主菜单等界面用按钮打开。</summary>
        public static void OpenPanel()
        {
            if (_instance == null)
            {
                Bootstrap();
            }

            if (_instance != null)
            {
                _instance.Open();
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            GameSettings.EnsureLoaded();
        }

        private void Start()
        {
            if (_instance != this)
            {
                return;
            }

            Build();
            _root.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.oKey.wasPressedThisFrame)
                {
                    if (_open)
                    {
                        Close();
                    }
                    else
                    {
                        Open();
                    }
                }
                else if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (_open)
                    {
                        Close();
                        return;
                    }

                    bool blocked = _escapeConsumedFrame == Time.frameCount
                                   || _modalCount > 0
                                   || FloatingWindowManager.OpenWindowCount > 0;
                    if (!blocked)
                    {
                        Open();
                        return;
                    }
                }
            }

            if (!_open)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            for (int i = 0; i < _buttons.Count; i++)
            {
                SettingButton button = _buttons[i];
                bool over = RectTransformUtility.RectangleContainsScreenPoint(button.Rect, screen, null);
                bool active = button.IsActive != null && button.IsActive();
                button.Image.color = over
                    ? AccentHoverColor
                    : (active ? AccentColor : button.NormalColor);

                if (over && mouse.leftButton.wasPressedThisFrame)
                {
                    if (button.Click != null)
                    {
                        button.Click();
                    }

                    RefreshVisuals();
                    return;
                }
            }
        }

        private void Open()
        {
            if (_root == null)
            {
                return;
            }

            if (_open)
            {
                return;
            }

            _open = true;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            RefreshVisuals();
        }

        private void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            _blockedUntilFrame = Time.frameCount;
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>存档只在基地允许：探索/战斗中保存会留下无法还原的中途状态。</summary>
        private static bool CanSaveNow()
        {
            return SceneManager.GetActiveScene().name == "BasePrototype";
        }

        private void SaveGame()
        {
            if (!CanSaveNow())
            {
                SetStatus("探索或战斗中不能存档，退出会回到本次出征前的基地。");
                return;
            }

            SaveSystem.Save();
            SetStatus("已保存。");
        }

        private void ReturnToMenu()
        {
            if (CanSaveNow())
            {
                SaveSystem.Save();
            }

            Close();
            SceneManager.LoadScene("MainMenu");
        }

        private void QuitGame()
        {
            if (CanSaveNow())
            {
                SaveSystem.Save();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                SettingButton button = _buttons[i];
                bool active = button.IsActive != null && button.IsActive();
                button.Image.color = active ? AccentColor : button.NormalColor;
            }

            if (_volumeValue != null)
            {
                _volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            }
        }

        private void Build()
        {
            _root = new GameObject("SettingsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(_root.transform, false);
            var shadeRect = (RectTransform)shade.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = ShadeColor;

            _panelRect = CreatePanel(_root.transform, "Panel", Vector2.zero, new Vector2(380f, 320f), PanelColor);
            CreateCenteredText(_panelRect, "Title", "设置", 16, TextColor, new Vector2(0f, 126f), new Vector2(200f, 24f));

            CreateRowLabel("SpeedLabel", "动画速度", 88f);
            AddSpeedButton("×1", new Vector2(30f, 88f), 1);
            AddSpeedButton("×2", new Vector2(78f, 88f), 2);
            AddSpeedButton("×4", new Vector2(126f, 88f), 4);

            CreateRowLabel("FullscreenLabel", "全屏", 46f);
            AddToggleButton("开", new Vector2(30f, 46f), true);
            AddToggleButton("关", new Vector2(78f, 46f), false);

            CreateRowLabel("VolumeLabel", "音量", 8f);
            AddButton(_panelRect, "-", new Vector2(30f, 8f), new Vector2(38f, 22f), RowColor, null, delegate { GameSettings.Volume -= 0.1f; });
            _volumeValue = CreateCenteredText(_panelRect, "VolumeValue", "100%", 10, TextColor, new Vector2(78f, 8f), new Vector2(52f, 16f));
            AddButton(_panelRect, "+", new Vector2(126f, 8f), new Vector2(38f, 22f), RowColor, null, delegate { GameSettings.Volume += 0.1f; });

            AddButton(_panelRect, "保存进度", new Vector2(-86f, -36f), new Vector2(150f, 26f), RowColor, null, SaveGame);
            AddButton(_panelRect, "返回主菜单", new Vector2(76f, -36f), new Vector2(150f, 26f), RowColor, null, ReturnToMenu);
            AddButton(_panelRect, "退出游戏", new Vector2(0f, -74f), new Vector2(160f, 26f), AccentColor, null, QuitGame);

            _statusText = CreateCenteredText(_panelRect, "Status", string.Empty, 8, MutedColor, new Vector2(0f, -104f), new Vector2(340f, 14f));
            CreateCenteredText(_panelRect, "Hint", "Esc / O 随时开关设置", 8, MutedColor, new Vector2(0f, -122f), new Vector2(240f, 12f));
            AddButton(_panelRect, "关闭", new Vector2(0f, -142f), new Vector2(120f, 24f), AccentColor, null, Close);
        }

        private void CreateRowLabel(string name, string content, float y)
        {
            Text label = CreateCenteredText(_panelRect, name, content, 10, MutedColor, new Vector2(-120f, y), new Vector2(110f, 16f));
            label.alignment = TextAnchor.MiddleLeft;
        }

        private void AddSpeedButton(string label, Vector2 center, int speed)
        {
            AddButton(
                _panelRect,
                label,
                center,
                new Vector2(42f, 22f),
                RowColor,
                delegate { return GameSettings.AnimationSpeed == speed; },
                delegate { GameSettings.AnimationSpeed = speed; });
        }

        private void AddToggleButton(string label, Vector2 center, bool fullscreen)
        {
            AddButton(
                _panelRect,
                label,
                center,
                new Vector2(42f, 22f),
                RowColor,
                delegate { return GameSettings.Fullscreen == fullscreen; },
                delegate { GameSettings.Fullscreen = fullscreen; });
        }

        private void AddButton(
            RectTransform parent,
            string label,
            Vector2 center,
            Vector2 size,
            Color color,
            Func<bool> isActive,
            Action click)
        {
            RectTransform rect = CreatePanel(parent, "Button_" + label, center, size, color);
            CreateCenteredText(rect, "Label", label, 10, TextColor, Vector2.zero, new Vector2(size.x - 4f, size.y - 2f));
            _buttons.Add(new SettingButton
            {
                Rect = rect,
                Image = rect.GetComponent<Image>(),
                NormalColor = color,
                IsActive = isActive,
                Click = click
            });
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
    }
}

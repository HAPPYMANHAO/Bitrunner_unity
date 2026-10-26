using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.UI;
using static BitRunner.Game.UiFactory;
using static BitRunner.Game.UiTheme;

namespace BitRunner.Game
{
    /// <summary>Screen 可以往哪些洞里画。其余六个区由外壳持有，Screen 拿不到。</summary>
    public enum ShellRegionId
    {
        /// <summary>B 主行动条区：出征 / 执行 / 结束回合。</summary>
        Primary,

        /// <summary>C 舞台：立绘台词 / 叙事 / 焦点敌人。</summary>
        Stage,

        /// <summary>D 侧栏：页签行 + 内容。</summary>
        Side,

        /// <summary>F2 中段大板：仓库 / 结果预估 / 目标背包。</summary>
        Dock
    }

    /// <summary>
    /// 单场景外壳：相机 + 画布 + 七个区域 + 六层容器。
    ///
    /// 这个类存在的唯一理由，是让三种模式共用同一批控件实例。背包、保险箱、日志、账本 HUD
    /// 一旦建立就不再销毁——切模式只是换掉 <see cref="ScreenLayer"/> 里的内容。
    ///
    /// 三条硬约束（写在这里，因为这里是最容易被绕过的地方）：
    ///   1. Screen 只拿得到 <see cref="Region"/> 返回的容器，拿不到 Canvas / RegionLayer；
    ///      要往别处画必须走这里暴露的接口。
    ///   2. 区域矩形来自 <see cref="ShellRegions"/> 的常量，不读配置、不随模式变化。
    ///   3. 颜色只从 <see cref="UiTheme"/> 取。
    ///
    /// 实测 640×360 基线下每个区域的屏幕矩形见 docs/PLAN_UI_SHELL.md §2。
    /// </summary>
    public sealed class GameShell : MonoBehaviour
    {
        /// <summary>
        /// 左栏的一行。三种模式共用同一套行布局，所以"生命""能量"在两处必然对齐。
        ///
        /// <see cref="Ratio"/> >= 0 时画条（行高 20），&lt; 0 时只画文字（行高 12）。
        /// </summary>
        public struct StatusRow
        {
            public string Label;
            public string Value;
            public float Ratio;
            public Color BarColor;

            public static StatusRow Bar(string label, string value, float ratio, Color color)
            {
                return new StatusRow { Label = label, Value = value, Ratio = ratio, BarColor = color };
            }

            public static StatusRow Text(string label, string value)
            {
                return new StatusRow { Label = label, Value = value, Ratio = -1f };
            }
        }

        /// <summary>左栏最多几行。基地 4 行、探索 5 行，留 6 个槽够用且不会重建。</summary>
        private const int StatusSlotCount = 6;

        /// <summary>日志最多保留几行。150×124 的面板在字号 7 下约放得下 9 行。</summary>
        private const int MaxLogEntries = 9;

        /// <summary>日志按字符计的容量上限，超出从最旧的开始丢。见 <see cref="AppendLog"/>。</summary>
        private const int LogCharBudget = 170;

        public static GameShell Instance { get; private set; }

        // ---------- 容器 ----------

        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }

        public RectTransform BackgroundLayer { get; private set; }
        public RectTransform RegionLayer { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public RectTransform TooltipLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }
        public RectTransform EndLayer { get; private set; }

        // ---------- 外壳持有的控件（三种模式共用同一实例）----------

        public RectTransform HudRect { get; private set; }
        public Text HudText { get; private set; }
        public RectTransform DebtFillRect { get; private set; }

        public RectTransform StatusRect { get; private set; }

        /// <summary>整块说明文本（结局、违约这类"没有可分解数字"的状态用它占满左栏）。</summary>
        public Text VitalsText { get; private set; }

        public RectTransform BenchRect { get; private set; }

        public RectTransform PackRect { get; private set; }
        public BagView PackView { get; private set; }

        public RectTransform SecureRect { get; private set; }
        public Text SecureTitle { get; private set; }
        public BagView SecureView { get; private set; }

        public RectTransform LogRect { get; private set; }
        public Text LogText { get; private set; }

        private readonly Dictionary<ShellRegionId, RectTransform> _regions =
            new Dictionary<ShellRegionId, RectTransform>();

        private readonly List<string> _logLines = new List<string>();

        private readonly Text[] _statusLabels = new Text[StatusSlotCount];
        private readonly Text[] _statusValues = new Text[StatusSlotCount];
        private readonly Image[] _statusFills = new Image[StatusSlotCount];
        private readonly RectTransform[] _statusBars = new RectTransform[StatusSlotCount];

        private BagInventory _secureBag;

        /// <summary>区域矩形的运行期快照，供验收脚本比对三模式是否一致。</summary>
        public Rect RectOf(ShellRegionId id)
        {
            switch (id)
            {
                case ShellRegionId.Primary: return ShellRegions.PrimaryAction;
                case ShellRegionId.Stage: return ShellRegions.Stage;
                case ShellRegionId.Side: return ShellRegions.Side;
                default: return ShellRegions.BenchMid;
            }
        }

        public static GameShell Ensure(BagInventory pack, BagInventory secure, Dictionary<string, Color> palette)
        {
            if (Instance != null)
            {
                return Instance;
            }

            var go = new GameObject("GameShell");
            var shell = go.AddComponent<GameShell>();
            shell.Build(pack, secure, palette);
            Instance = shell;
            return shell;
        }

        /// <summary>Screen 唯一的入口：拿到某个洞的容器。容器坐标系原点在区域左上角之外，用 CenterOf 铺满。</summary>
        public RectTransform Region(ShellRegionId id)
        {
            RectTransform rect;
            return _regions.TryGetValue(id, out rect) ? rect : null;
        }

        private void Build(BagInventory pack, BagInventory secure, Dictionary<string, Color> palette)
        {
            EnsureCamera();
            Canvas = CreateCanvas();
            Root = (RectTransform)Canvas.transform;

            BackgroundLayer = CreateLayer("BackgroundLayer");
            RegionLayer = CreateLayer("RegionLayer");
            ScreenLayer = CreateLayer("ScreenLayer");
            DragLayer = CreateLayer("DragLayer");
            TooltipLayer = CreateLayer("TooltipLayer");
            OverlayLayer = CreateLayer("OverlayLayer");
            EndLayer = CreateLayer("EndLayer");

            BuildRegionContainers();
            BuildBenchPlate();
            BuildHud();
            BuildPack(pack, palette);
            BuildSecure(secure, palette);
            BuildStatus();
            BuildLog();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiTheme.BackgroundColor;
            camera.orthographic = true;
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiTheme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreateCenteredPanel(
                canvas.transform,
                "Background",
                Vector2.zero,
                ShellRegions.ReferenceResolution,
                UiTheme.BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;
            BackgroundArt.Apply(background);

            return canvas;
        }

        /// <summary>铺满画布的透明容器层。不带 Image，因此不会挡住射线。</summary>
        private RectTransform CreateLayer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = ShellRegions.ReferenceResolution;
            return rect;
        }

        private void BuildRegionContainers()
        {
            CreateRegion(ShellRegionId.Primary, "Region_Primary", ShellRegions.PrimaryAction);
            CreateRegion(ShellRegionId.Stage, "Region_Stage", ShellRegions.Stage);
            CreateRegion(ShellRegionId.Side, "Region_Side", ShellRegions.Side);
            CreateRegion(ShellRegionId.Dock, "Region_Dock", ShellRegions.BenchMid);
        }

        private void CreateRegion(ShellRegionId id, string name, Rect region)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(ScreenLayer, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = ShellRegions.CenterOf(region);
            rect.sizeDelta = ShellRegions.SizeOf(region);
            _regions[id] = rect;
        }

        /// <summary>工作台底板。放在区域层最底，背包与保险箱压在它上面。</summary>
        private void BuildBenchPlate()
        {
            BenchRect = CreateCenteredPanel(
                RegionLayer,
                "Workbench",
                ShellRegions.CenterOf(ShellRegions.Bench),
                ShellRegions.SizeOf(ShellRegions.Bench),
                UiTheme.BenchColor);
            BenchRect.GetComponent<Image>().raycastTarget = false;
            AddBorder(BenchRect, UiTheme.BenchRailColor);

            RectTransform rail = CreateAnchoredPanel(
                BenchRect,
                "TopRail",
                Vector2.zero,
                new Vector2(ShellRegions.Bench.width, 6f),
                UiTheme.BenchRailColor);
            rail.GetComponent<Image>().raycastTarget = false;

            CreateText(
                BenchRect,
                "BenchLabel",
                "WORKBENCH",
                UiTheme.FontCaption,
                new Color32(120, 104, 78, 255),
                new Vector2(318f, -10f),
                new Vector2(76f, 10f),
                TextAnchor.UpperLeft);
        }

        private void BuildHud()
        {
            HudRect = CreateCenteredPanel(
                RegionLayer,
                "Hud",
                ShellRegions.CenterOf(ShellRegions.Hud),
                ShellRegions.SizeOf(ShellRegions.Hud),
                UiTheme.PanelColor);
            AddBorder(HudRect, new Color32(67, 59, 47, 255));

            HudText = CreateText(
                HudRect,
                "Stats",
                string.Empty,
                UiTheme.FontPanelTitle,
                UiTheme.TextColor,
                new Vector2(10f, -6f),
                new Vector2(150f, 48f),
                TextAnchor.UpperLeft);

            RectTransform bar = CreateAnchoredPanel(
                HudRect,
                "DebtBar",
                new Vector2(10f, -58f),
                new Vector2(150f, 5f),
                new Color32(38, 44, 54, 255));
            bar.GetComponent<Image>().raycastTarget = false;

            DebtFillRect = CreateAnchoredPanel(
                bar,
                "Fill",
                Vector2.zero,
                new Vector2(0f, 5f),
                UiTheme.AccentColor);
            DebtFillRect.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildPack(BagInventory pack, Dictionary<string, Color> palette)
        {
            PackRect = CreateCenteredPanel(
                RegionLayer,
                "PackPanel",
                ShellRegions.CenterOf(ShellRegions.BenchPack),
                ShellRegions.SizeOf(ShellRegions.BenchPack),
                UiTheme.PanelColor);
            PackRect.gameObject.AddComponent<RectMask2D>();

            CreateText(
                PackRect,
                "Title",
                "背包",
                UiTheme.FontPanelTitle,
                UiTheme.TextColor,
                new Vector2(8f, -6f),
                new Vector2(124f, 14f),
                TextAnchor.UpperLeft);

            RectTransform bagRect = CreateBagRect(
                PackRect,
                new Vector2(0f, 2f),
                new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize));
            PackView = bagRect.gameObject.AddComponent<BagView>();
            PackView.Initialize(pack, bagRect, palette);
        }

        private void BuildSecure(BagInventory secure, Dictionary<string, Color> palette)
        {
            _secureBag = secure;

            SecureRect = CreateCenteredPanel(
                RegionLayer,
                "SecurePanel",
                ShellRegions.CenterOf(ShellRegions.BenchSecure),
                ShellRegions.SizeOf(ShellRegions.BenchSecure),
                UiTheme.PanelColor);

            SecureTitle = CreateText(
                SecureRect,
                "Title",
                string.Empty,
                UiTheme.FontBody,
                UiTheme.TextColor,
                new Vector2(6f, -4f),
                new Vector2(64f, 12f),
                TextAnchor.UpperLeft);

            RectTransform secureRect = CreateBagRect(
                SecureRect,
                new Vector2(0f, -8f),
                new Vector2(2f * BagView.CellSize, 2f * BagView.CellSize));
            SecureView = secureRect.gameObject.AddComponent<BagView>();
            SecureView.Initialize(secure, secureRect, palette);
        }

        /// <summary>
        /// 左栏：标题 + 最多 6 行。行内容由当前模式通过 <see cref="SetStatus"/> 填，
        /// 但**行的位置和尺寸是这里的常量**——这是"生命/能量在三种模式下同一坐标"的技术保证。
        /// </summary>
        private void BuildStatus()
        {
            StatusRect = CreateCenteredPanel(
                RegionLayer,
                "LeftStats",
                ShellRegions.CenterOf(ShellRegions.Status),
                ShellRegions.SizeOf(ShellRegions.Status),
                UiTheme.PanelColor);
            StatusRect.gameObject.AddComponent<RectMask2D>();

            CreateText(
                StatusRect,
                "Title",
                "状态",
                UiTheme.FontCaption,
                UiTheme.MutedColor,
                new Vector2(4f, -4f),
                new Vector2(70f, 10f),
                TextAnchor.UpperLeft);

            VitalsText = CreateText(
                StatusRect,
                "Note",
                string.Empty,
                UiTheme.FontSecondary,
                UiTheme.TextColor,
                new Vector2(4f, -20f),
                new Vector2(74f, 96f),
                TextAnchor.UpperLeft);

            for (int i = 0; i < StatusSlotCount; i++)
            {
                _statusLabels[i] = CreateText(
                    StatusRect,
                    "Row" + i + "Label",
                    string.Empty,
                    UiTheme.FontCaption,
                    UiTheme.MutedColor,
                    Vector2.zero,
                    new Vector2(46f, 10f),
                    TextAnchor.UpperLeft);

                _statusValues[i] = CreateText(
                    StatusRect,
                    "Row" + i + "Value",
                    string.Empty,
                    UiTheme.FontCaption,
                    UiTheme.TextColor,
                    Vector2.zero,
                    new Vector2(74f, 10f),
                    TextAnchor.UpperRight);

                _statusFills[i] = CreateBar(
                    StatusRect,
                    "Row" + i + "Bar",
                    Vector2.zero,
                    new Vector2(70f, 5f),
                    UiTheme.HpColor);
                _statusBars[i] = (RectTransform)_statusFills[i].transform.parent;

                _statusLabels[i].gameObject.SetActive(false);
                _statusValues[i].gameObject.SetActive(false);
                _statusBars[i].gameObject.SetActive(false);
            }

            SetStatus("尚未出征");
        }

        // ---------- 外壳接口：Screen 只能通过这些方法改恒定的七个区域 ----------

        /// <summary>
        /// 写左栏。<paramref name="note"/> 非空时占满整块（用于结局/违约这类不可分解的状态），
        /// 此时 <paramref name="rows"/> 全部隐藏。
        /// </summary>
        public void SetStatus(string note, params StatusRow[] rows)
        {
            bool hasNote = !string.IsNullOrEmpty(note);
            VitalsText.text = hasNote ? note : string.Empty;
            VitalsText.gameObject.SetActive(hasNote);

            float y = 16f;
            for (int i = 0; i < StatusSlotCount; i++)
            {
                bool used = !hasNote && rows != null && i < rows.Length;
                _statusLabels[i].gameObject.SetActive(used);
                _statusValues[i].gameObject.SetActive(used);
                _statusBars[i].gameObject.SetActive(used && rows[i].Ratio >= 0f);
                if (!used)
                {
                    continue;
                }

                StatusRow row = rows[i];
                _statusLabels[i].text = row.Label;
                _statusValues[i].text = row.Value;
                _statusLabels[i].rectTransform.anchoredPosition = new Vector2(4f, -y);
                _statusValues[i].rectTransform.anchoredPosition = new Vector2(4f, -y);

                if (row.Ratio >= 0f)
                {
                    _statusBars[i].anchoredPosition = new Vector2(4f, -(y + 10f));
                    _statusFills[i].fillAmount = Mathf.Clamp01(row.Ratio);
                    _statusFills[i].color = row.BarColor;
                    y += 20f;
                }
                else
                {
                    y += 13f;
                }
            }
        }

        /// <summary>写 A 区账本（周·天 / 应缴·已缴 / bit / 债务条）。三种模式共用。</summary>
        public void SetLedger(CampaignState campaign)
        {
            if (campaign == null)
            {
                return;
            }

            HudText.text = "第 " + campaign.Week + " 周 · 第 " + campaign.Day + "/7 天"
                           + "\n应缴 " + campaign.CurrentDue + "   已缴 " + campaign.TotalPaid
                           + "\nbit " + campaign.Bit;

            HudText.color = campaign.IsActive && campaign.Bit < campaign.CurrentDue
                ? UiTheme.WarningColor
                : UiTheme.TextColor;

            float ratio = campaign.CurrentDue <= 0
                ? 1f
                : Mathf.Clamp01((float)campaign.Bit / campaign.CurrentDue);
            DebtFillRect.sizeDelta = new Vector2(150f * ratio, 5f);
            DebtFillRect.GetComponent<Image>().color = ratio >= 1f
                ? UiTheme.AccentColor
                : UiTheme.WarningColor;
        }

        /// <summary>往 G 区追加一行。跨模式连续——进战斗还能看见探索里最后发生了什么。</summary>
        public void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_logLines.Count > 0 && _logLines[_logLines.Count - 1] == text)
            {
                return;
            }

            _logLines.Add(text);
            while (_logLines.Count > MaxLogEntries)
            {
                _logLines.RemoveAt(0);
            }

            // 面板只有 134×98（字号 7 约 170 字），超了会被 RectMask2D 裁掉——
            // 裁掉的恰好是最新的那几行，所以按字符数从最旧的开始丢，保证新消息一定可见。
            while (_logLines.Count > 1 && JoinedLogLength() > LogCharBudget)
            {
                _logLines.RemoveAt(0);
            }

            if (LogText != null)
            {
                LogText.text = string.Join("\n", _logLines.ToArray());
            }
        }

        private int JoinedLogLength()
        {
            int total = 0;
            for (int i = 0; i < _logLines.Count; i++)
            {
                total += _logLines[i].Length + 1;
            }

            return total;
        }

        /// <summary>重画 F1 / F3 与保险箱标题。Screen 的弹窗里若有第二份 BagView 改了同一份数据，要调这个。</summary>
        public void RefreshContainers()
        {
            if (PackView != null)
            {
                PackView.Refresh();
            }

            if (SecureView != null)
            {
                SecureView.Refresh();
            }

            if (SecureTitle != null && _secureBag != null)
            {
                SecureTitle.text = "保险箱 " + _secureBag.Count + "/" + _secureBag.Grid.Capacity;
            }
        }

        private void BuildLog()
        {
            LogRect = CreateCenteredPanel(
                RegionLayer,
                "Log",
                ShellRegions.CenterOf(ShellRegions.Log),
                ShellRegions.SizeOf(ShellRegions.Log),
                new Color32(22, 26, 32, 255));
            AddBorder(LogRect, new Color32(50, 58, 68, 255));
            LogRect.GetComponent<Image>().raycastTarget = false;
            LogRect.gameObject.AddComponent<RectMask2D>();

            CreateText(
                LogRect,
                "Header",
                "LOG",
                UiTheme.FontCaption,
                new Color32(164, 146, 112, 255),
                new Vector2(8f, -6f),
                new Vector2(134f, 10f),
                TextAnchor.UpperLeft);

            LogText = CreateText(
                LogRect,
                "Lines",
                string.Empty,
                UiTheme.FontCaption,
                UiTheme.MutedColor,
                new Vector2(8f, -20f),
                new Vector2(134f, 98f),
                TextAnchor.UpperLeft);
        }
    }
}

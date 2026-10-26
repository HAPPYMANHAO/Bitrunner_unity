using UnityEngine;

namespace BitRunner.Game
{
    /// <summary>
    /// 基地骨架的七个区域，以及三种模式唯一可以填的三个"洞"。
    ///
    /// 这些矩形全部是从现有基地界面代码反算出来的实测值（`CreateCenteredPanel` 的 center 逆运算：
    /// x0 = 320 + cx − w/2，y0 = 180 − cy − h/2），不是设计出来的。**任何模式都不许改这里**——
    /// 它们就是"三种模式看起来是一个场景"的物理基础。
    ///
    /// 坐标系：640×360 基准画布，原点在**左上角**，y 向下为正（与 Rect 的默认 y 向上不同，注意）。
    /// 出处见 docs/PLAN_UI_SHELL.md §2。
    /// </summary>
    public static class ShellRegions
    {
        public const float RefWidth = 640f;
        public const float RefHeight = 360f;

        public static readonly Vector2 ReferenceResolution = new Vector2(RefWidth, RefHeight);

        public static readonly Rect Screen = new Rect(0f, 0f, RefWidth, RefHeight);

        // ---- 外壳持有：三种模式像素级不动 ----

        /// <summary>A 账本：周·天 / 应缴·已缴 / bit / 债务进度条。出自 BuildHud。</summary>
        public static readonly Rect Hud = new Rect(8f, 2f, 170f, 74f);

        /// <summary>
        /// B 主行动条：出征 / 执行 / 结束回合。三种模式共用同一个矩形——这是全屏最常点的一颗。
        ///
        /// 位置在**右侧命令列**：行动/背包菜单正下方、日志正上方。原来它在右上角与日志同高，
        /// 离玩家的视线中心（左中 + 中下）太远；挪到这一列之后，垂直方向从"菜单 → 主按钮 → 日志"
        /// 一路读下来，右下角那片大留白也不再空着。
        ///
        /// 整列又整体上移过一版（侧栏 55→45、按钮 216→209、日志 246→239）：原来按钮贴着底带
        /// 和日志，视觉上像压住了中段。现在三者之间各留 4–6px，读起来是三段而不是一块。
        /// </summary>
        public static readonly Rect PrimaryAction = new Rect(465f, 209f, 170f, 24f);

        /// <summary>战斗用的两枚次要按钮（撤离 / 动画速度），落在主按钮让出来的右上角空位。</summary>
        public static readonly Rect SecondaryAction1 = new Rect(455f, 16f, 74f, 22f);
        public static readonly Rect SecondaryAction2 = new Rect(535f, 16f, 100f, 22f);

        /// <summary>D 侧栏：页签行 + 内容区。出自 BuildRightPanel。</summary>
        public static readonly Rect Side = new Rect(465f, 45f, 170f, 160f);

        /// <summary>D 的页签行（基地 8 个 20px 页签），高度固定，三种模式都有。</summary>
        public static readonly Rect SideTabRow = new Rect(465f, 47f, 170f, 18f);

        /// <summary>D 的内容区（三种模式的 Screen 往这里建）。</summary>
        public static readonly Rect SideContent = new Rect(465f, 67f, 170f, 138f);

        /// <summary>E 角色状态：生命 / 能量 固定在前两组。出自 BuildLeftStats。</summary>
        public static readonly Rect Status = new Rect(2f, 226f, 84f, 124f);

        /// <summary>F 工作台带（底带的底板，只作为背景，不参与交互）。出自 BuildBagPanels。</summary>
        public static readonly Rect Bench = new Rect(85f, 218f, 398f, 132f);

        /// <summary>F1 背包：三种模式下是同一个 BagView 实例，永不重建。</summary>
        public static readonly Rect BenchPack = new Rect(93f, 226f, 110f, 124f);

        /// <summary>F3 保险箱：同上，永不重建。</summary>
        public static readonly Rect BenchSecure = new Rect(399f, 266f, 76f, 84f);

        /// <summary>G 日志：右下角，跨模式连续追加，不清空。出自 BuildLog。让出上方一段给主按钮。</summary>
        public static readonly Rect Log = new Rect(486f, 239f, 150f, 111f);

        /// <summary>
        /// 探索的丢弃区：保险箱正上方那块空位（F 带里唯一没被占的角落）。
        /// 只在探索主界面出现；战斗里的丢弃区在 F2 战利品板内部。
        /// </summary>
        public static readonly Rect DiscardBin = new Rect(399f, 238f, 62f, 20f);

        // ---- Screen 可建：三种模式三选一 ----

        /// <summary>C 舞台：基地=立绘台词，探索=叙事+已选行动，战斗=焦点敌人。出自 BuildArtStage。</summary>
        public static readonly Rect Stage = new Rect(185f, 45f, 270f, 170f);

        /// <summary>F2 中段大板：基地=仓库，探索=结果预估，战斗=战利品板。出自 StashPanel。</summary>
        public static readonly Rect BenchMid = new Rect(209f, 226f, 184f, 124f);

        // ---- 仅战斗使用 ----

        /// <summary>
        /// 战场带：横跨 A 的下半 + C + D。
        ///
        /// 战斗是唯一一个"跨洞"的模式，理由是硬的：敌人卡必须并排比较（DESIGN.md §4.3
        /// 「敌人背包完全可见」），而三块洞各自都装不下能同时显示背包与数字的卡。
        /// A 的账本在战斗中被 <see cref="GameShell.SetLedgerVisible"/> 关掉，正好让出左边这一截。
        /// </summary>
        public static readonly Rect Battlefield = new Rect(4f, 46f, 632f, 168f);

        /// <summary>战斗播报行：A 账本让出来的那一条。原来这句话写在 C 舞台底部。</summary>
        public static readonly Rect MessageLine = new Rect(10f, 12f, 440f, 20f);

        /// <summary>
        /// 左侧空洞 (0,76,185,150) 里的搜刮面板位。探索搜刮时用，平时留白。
        ///
        /// 搜刮不再是全屏弹窗：那块遮罩把整屏压黑，物品提示又被压在遮罩下面，
        /// 于是"在窗口里悬停什么都看不到"。改成坐在左边这块本来空着的地方，
        /// 背包（F1）和保险箱（F3）一直露在外面，拖过去就行。
        /// </summary>
        public static readonly Rect LootDock = new Rect(4f, 78f, 176f, 146f);

        // ---- 已知空洞，本版不动 ----
        // 左中 (0,76,185,150)：基地现在就是留白，填上就不是"零改动"了。
        // 留作后续：本周日程 / 债务预警板 / 联系人列表。

        /// <summary>
        /// 把"相对某个区域左上角的局部矩形"换算成 <see cref="UiFactory.CreateCenteredPanel"/> 需要的 center 参数。
        /// </summary>
        public static Vector2 AnchorIn(Rect region, float localX, float localY, float width, float height)
        {
            return new Vector2(
                region.x + localX + width * 0.5f - RefWidth * 0.5f,
                RefHeight * 0.5f - (region.y + localY + height * 0.5f));
        }

        /// <summary>
        /// 把"相对区域左上角的局部矩形"换算成"相对区域中心的偏移"。
        /// 用于 Screen 已经在区域容器内部（<see cref="GameShell.Region"/> 返回的 rect）建控件时——
        /// 此时 <see cref="CreateCenteredPanel"/> 的 center 要相对容器中心，而不是屏幕中心。
        /// </summary>
        public static Vector2 LocalIn(Rect region, float localX, float localY, float width, float height)
        {
            return new Vector2(
                localX + width * 0.5f - region.width * 0.5f,
                region.height * 0.5f - localY - height * 0.5f);
        }

        /// <summary>整个区域的正中（用于铺满该区域的底板）。</summary>
        public static Vector2 CenterOf(Rect region)
        {
            return AnchorIn(region, 0f, 0f, region.width, region.height);
        }

        /// <summary>区域尺寸，省得每次写 new Vector2(r.width, r.height)。</summary>
        public static Vector2 SizeOf(Rect region)
        {
            return new Vector2(region.width, region.height);
        }
    }
}

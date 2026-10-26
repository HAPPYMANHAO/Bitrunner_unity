# 调研：基地 / 探索 / 战斗 合并为单场景

> 调研对象：BitRnner（Unity 6000.5.7f1 / URP 2D 17.6 / uGUI 2.5 / Input System 1.20）
> 调研方式：源码与场景资产实测 + 官方/社区实践对照
> 结论日期：2026-10-26

---

## 0. 结论摘要

**支持合并。** 但理由不是"三个场景看着乱"，而是一个具体的技术事实：

> 这三个场景里**没有任何内容**。每个场景只有一个空 GameObject 挂着一个 Bootstrap 脚本，相机和 Canvas 都是运行时 `new` 出来的。真正的状态机是 `static class GameSession`。
>
> 也就是说：**场景早就不是场景了，它只是一个"重置 UI 并重建 2000 行界面"的开关。**

合并的方向是 **单场景 + 屏幕栈（Shell + Screen Stack）**，不是重写 UI。

| 指标 | 现状 | 合并后 |
|---|---|---|
| 场景数 | 4（MainMenu + 3 个玩法场景） | 1 |
| UI 构建代码 | 6497 行，分散在 4 个 Bootstrap | 6497 行迁移为 1 Shell + 3 Screen |
| 场景切换点 | 12 处 `LoadScene` + 1 处按场景名分支 | 1 个 Router |
| Core / Save 层 | — | **零改动** |
| 战斗往返成本 | 场景加载 + 全量重建 2122 行 UI | 栈 Pop，UI 不销毁 |

---

## 1. 现状测绘（实测数据）

### 1.1 场景里到底有什么

对四个 `.unity` 做 YAML 对象统计，每个场景都是 8 个对象：

| 对象类型 | 数量 | 说明 |
|---|---|---|
| `!u!29` OcclusionCullingSettings | 1 | 场景设置 |
| `!u!104` RenderSettings | 1 | 场景设置 |
| `!u!157` LightmapSettings | 1 | 场景设置 |
| `!u!196` NavMeshSettings | 1 | 场景设置 |
| `!u!1660057539` SceneRoots | 1 | 场景根列表 |
| `!u!1` GameObject | 1 | 唯一的物体，无名 |
| `!u!4` Transform | 1 | 上面那个物体的 |
| `!u!114` MonoBehaviour | 1 | 挂着 Bootstrap 脚本 |

**`!u!20`（Camera）数量为 0，没有任何 Canvas、EventSystem、Prefab 实例。** 相机由 `BasePrototypeBootstrap.cs:2029` 在运行时创建：

```csharp
// BasePrototypeBootstrap.cs:2027
private Canvas CreateCanvas()
{
    if (Camera.main == null)
    {
        var cameraGo = new GameObject("Main Camera", typeof(Camera));
        ...
```

三个玩法场景除了挂的脚本不同，结构完全相同 —— 它们是同一个空壳的三份拷贝。

### 1.2 场景与脚本映射

| 场景 | 挂载脚本 | 行数 |
|---|---|---|
| `MainMenu.unity` | `MainMenuBootstrap` | 456 |
| `BasePrototype.unity` | `BasePrototypeBootstrap` | 2237 |
| `ExpeditionPrototype.unity` | `ExpeditionPrototypeBootstrap` | 2122 |
| `CombatPrototype.unity` | `CombatPrototypeBootstrap` | 1682 |
| | **合计** | **6497** |

### 1.3 所有场景切换点（12 处 + 1 处隐式依赖）

| 文件:行 | 调用 | 语义 |
|---|---|---|
| `MainMenuBootstrap.cs:101, 109` | `LoadScene("BasePrototype")` | 开始游戏 |
| `BasePrototypeBootstrap.cs:287` | `LoadScene(GetActiveScene().buildIndex)` | 重载自己 |
| `BasePrototypeBootstrap.cs:567` | `LoadScene("ExpeditionPrototype")` | 出发 |
| `ExpeditionPrototypeBootstrap.cs:133, 332, 347` | `LoadScene("BasePrototype")` | 回基地 |
| `ExpeditionPrototypeBootstrap.cs:339` | `LoadScene("CombatPrototype")` | 遭遇战 |
| `CombatPrototypeBootstrap.cs:179` | `LoadScene("BasePrototype")` | 战败 |
| `CombatPrototypeBootstrap.cs:755, 911` | `LoadScene("ExpeditionPrototype")` | 战斗结束回地点 |
| `SettingsOverlay.cs:258` | `LoadScene("MainMenu")` | 回主菜单 |
| `SettingsOverlay.cs:235` | `GetActiveScene().name == "BasePrototype"` | **按场景名分支业务逻辑** |

最后一条是隐性耦合的典型样本：把"当前在哪个界面"编码成了字符串比较，编译器无法校验，改场景名就是运行时 bug。

### 1.4 状态存在哪

`GameSession.cs`（901 行）是 `public static class`，全部游戏状态都是静态字段：
`Campaign` / `Catalog` / `Merchant` / `Pod` / `Mine` / `Craft` / `Cpu` / `Insurance` / `Rack` / `Comms` / `Quests` / `Stash` / `PlayerBag` / `Secure` / `Scenario` / `ExpeditionHp` / `HasPendingEncounter` / `PendingEnemyId` …

跨场景存活**完全依赖 statics**，配合 `:96 ResetOnPlay()` 处理编辑器域重载。

---

## 2. 问题诊断

### 2.1 病灶一：状态有两个"真相"

真实状态在静态类里，场景里只有 UI。所以每次切换场景，代码必须做一遍"把静态状态重新翻译成 UI"的重建。这不是架构，这是把状态机拆成了两半、再用场景加载来同步。

### 2.2 病灶二：一次战斗往返 = 全量重建一次探索界面

现有调用链（实测）：

```
ExpeditionPrototypeBootstrap 确认行动
  └─ GameSession.ResolveExpeditionAction()        // :553
       └─ 命中战斗结果 → PendingEnemyId / HasPendingEncounter = true   // :646-647
  └─ SceneManager.LoadScene("CombatPrototype")    // :339  ← 整个探索界面销毁
       └─ 战斗 → 拾取完毕
       └─ GameSession.CompleteEncounter()          // CombatBootstrap:911
       └─ SceneManager.LoadScene("ExpeditionPrototype")   ← 再加载一次
            └─ _scenario = GameSession.Scenario   // ExpeditionBootstrap:122
            └─ 重新构建 2122 行 UI、重建所有背包视图、重绑所有引用
```

设计文档 `DESIGN.md:281` 写的是「战斗胜利后返回当前地点，不会回到旧地图」，`DESIGN.md:262` 写的是「战斗（进入战斗场景，胜利后回到当前地点）」。

**代码每打一场架，就把"当前地点"整个拆掉重建一次。** 玩家在探索界面里滚到一半的仓库、拖到一半的物品、选到一半的行动，全部丢失。

### 2.3 病灶三：过渡表现被结构性封死

Canvas 随场景销毁，所以做不到：

- 战斗切入的推近/闪白过渡（前一个界面已不存在，没有可淡出的对象）
- 探索与战斗共享背景、共享道具栏、共享 HUD
- 战斗中"整理背包"这种高频动作的低成本往返

### 2.4 病灶四：三份重复的脚手架

三个 Bootstrap 各自 `CreateCanvas`、各自建相机、各自建调色板、各自建 HUD 日志。共享部件（`BagView` / `FloatingWindowManager` / `GameFonts` / `BackgroundArt`）已经抽出来了，但**容器层没抽**。

---

## 3. 方案对比

| 方案 | 做法 | 改动量 | 收益 | 风险 | 结论 |
|---|---|---|---|---|---|
| **A. 单场景 + 屏幕栈** | 1 个 Shell 场景，Base/Expedition/Combat 变成 Screen 压栈 | 中（6497 行迁移，逻辑不动） | 战斗往返零重建；过渡可控；场景名不再进业务 | 中（一次性重构，需分步） | **推荐** |
| B. 单场景 + Additive 叠加 | 保留三个场景，用 `LoadSceneAsync(Additive)` 叠加 | 中 | 可保留"一场景一模块" | 需处理叠加卸载、EventSystem 重复、多 Canvas 排序，**本质仍是伪单场景** | 不推荐 |
| C. 保持三场景 + 上下文传参 | 只把 `PendingEnemyId` 之类显式传参 | 低 | 最小改动 | 治不了重建成本与过渡问题 | 只能当临时缓解 |
| D. 单场景 + UI Toolkit 重写 | 换 UI 体系 | 极高（6497 行重写） | 长期 UI 体验更好 | 与"合并场景"这个目标无关，混在一起必然失控 | 不推荐，另立项 |

**推荐 A。** 依据：这类游戏（面板驱动、无 3D 关卡、无烘焙光照、无流式加载）正是社区结论中单场景最优的形态——界面切换只做激活/停用而非实例化销毁，过渡也更容易做。反例条件（开放世界、超大内存环境、每关独立烘焙光照）本项目一条都不占。

---

## 4. 目标架构

```
Boot.unity                      ← 唯一场景，进游戏就加载，永不卸载
└─ GameRoot
   ├─ Main Camera               ← 一次创建，永久复用
   ├─ EventSystem               ← 一次创建，避免重复
   └─ UIRoot (Canvas 分层)
      ├─ Canvas_Static          背景/装饰，几乎不重绘
      ├─ Canvas_Screen          当前 Screen 主体
      └─ Canvas_Modal           浮动窗口 / 设置 / 确认框（复用现有 FloatingWindowManager）
   └─ GameShell (MonoBehaviour) ← 唯一入口，替代 4 个 Bootstrap
        └─ ScreenRouter         栈式导航
             ├─ MainMenuScreen
             ├─ BaseScreen
             ├─ ExpeditionScreen
             └─ CombatScreen
```

### 4.1 Screen 契约

```csharp
public interface IScreen
{
    string Id { get; }
    void OnEnter(ScreenArgs args);   // 首次进入，构建 UI
    void OnExit();                   // 移出栈，销毁或缓存
    void OnPause();                  // 被上层覆盖（战斗压上来时）
    void OnResume(ScreenArgs args);  // 上层弹出（战斗结束回到探索）
}
```

### 4.2 栈语义

| 操作 | 场景 |
|---|---|
| `Push(Base)` | 开始游戏 |
| `Push(Expedition)` | 出发 |
| `Push(Combat)` | 遭遇战（**Base 与 Expedition 保活，仅 `OnPause`**） |
| `Pop()` | 战斗胜利 → 回到**同一个** Expedition 实例，`OnResume` 只需刷新数值，不重建 UI |
| `ClearTo(Base)` | 撤离 / 战败 |

第 4 行就是这次重构的全部价值所在：`CombatPrototypeBootstrap:911` 那两行「`CompleteEncounter` + `LoadScene("ExpeditionPrototype")`」变成一次 `Router.Pop()`。

### 4.3 分层 Canvas 的作用

uGUI 里只要 Canvas 下一个元素变脏，整个 Canvas 会重算布局并重建合批。常驻单场景后界面元素变多，所以：

- 静态背景/装饰单独一层，永不参与重绘
- 每帧变化的数值（HP、警戒、日志）放 `Canvas_Screen` 的子 Canvas
- 频繁开关的面板用 `CanvasGroup.alpha + blocksRaycasts` 而不是 `SetActive`（后者会清空顶点缓冲，重新显示时触发完整 Rebuild）

---

## 5. 迁移路线（5 步，每步可运行、可回滚）

> 原则：**先建新壳，再逐个搬人，最后拆旧房。** 任何一步中断，项目都处于可运行状态。

### Step 0 · 埋点（半天，不改逻辑）
在 `ResolveExpeditionAction`、`CompleteEncounter`、各 Bootstrap 的 `CreateCanvas` 前后加 `Profiler.BeginSample`，记录：单次场景加载耗时、单次 UI 重建耗时、常驻对象数。
**验收**：有一组基线数字，"合并后更快"才有可验证依据，而不是感觉。

### Step 1 · 建 Shell + Router（空壳，不接业务）
新建 `Boot.unity`：Camera + EventSystem + 三层 Canvas + `GameShell` + `ScreenRouter`。Router 只支持 `Push/Pop/ClearTo`，Screen 先都为空实现。
**验收**：`Boot.unity` 能进，能按栈顺序打出 log，旧四场景仍可正常玩。

### Step 2 · 先搬 MainMenu + Base（最大、最独立）
`MainMenuBootstrap` → `MainMenuScreen`；`BasePrototypeBootstrap` → `BaseScreen`（2237 行基本原样搬，只把 `CreateCanvas` 换成用 Shell 给的 Canvas）。
`MainMenuBootstrap:101,109` → `Router.ClearTo(Base)`；`BasePrototypeBootstrap:287`（重载自己）→ `BaseScreen.Refresh()`。
**验收**：从 Boot 进主菜单 → 进基地 → 八个页签全可用；`GameSession.ExportSave` 存读一致。

### Step 3 · 搬 Expedition + Combat，并把战斗改成压栈
`ExpeditionPrototypeBootstrap` → `ExpeditionScreen`；`CombatPrototypeBootstrap` → `CombatScreen`。
`ExpeditionBootstrap:339` → `Router.Push(Combat)`；`CombatBootstrap:755,911` → `Router.Pop()`（关键一步：不再 `LoadScene`）。
`CombatBootstrap:179`（战败）→ `Router.ClearTo(Base)`。
**验收**：探索中触发战斗 → 战斗 → 拾取 → 回到探索，**探索界面的滚动位置与选中状态仍在**；撤离/战败正常回基地。

### Step 4 · 收尾拆除
`SettingsOverlay:235` 的场景名判断 → 改成查 `Router.Top.Id`；`:258` → `Router.ClearTo(MainMenu)`。
删掉三个 Prototype 场景与 Bootstrap，`EditorBuildSettings` 只留 `Boot.unity`。
**验收**：全流程走通一遍；`grep -r "LoadScene"` 结果为 0；Profiler 对比 Step 0 基线。

---

## 6. 风险与对策

| 风险 | 说明 | 对策 |
|---|---|---|
| 静态状态泄漏 | 单场景下没有"场景卸载"这个天然的复位点，旧的 statics 更容易残留 | 现有 `ResetOnPlay` 继续保留；Router 每次 `ClearTo(Base)` 时显式走一遍 `StartNewGame` 路径 |
| 常驻 UI 内存上升 | 三个界面同时存活，顶点/对象数增加 | 已测：四场景合计 6497 行，且都是运行时构建；用 Step 0 的对象数基线监控，超阈值时对非栈顶 Screen 启 `CanvasGroup` 隐藏 |
| `SetActive` 切界面卡一帧 | 反激活再激活会触发完整 Canvas Rebuild | 频繁切换的面板统一用 `CanvasGroup`；整屏切换才用 `SetActive` |
| 相机/背景需要跟着界面变 | 三个场景原来各有一台运行时相机 | `BackgroundArt.Apply()` 已存在，集中到 Shell 一处调用，Screen 只声明用哪套背景 |
| 存档兼容 | — | **零影响**：`ExportSave`/`ApplySaveData` 只读 `GameSession`，不引用场景 |
| 一次性大重构失控 | 6497 行同时搬 | 严格按 Step 1→4 分批，每步都有独立验收标准 |

---

## 7. 明确不建议做的事

1. **不要顺手把 uGUI 换成 UI Toolkit。** 那是 6497 行重写，与"合并场景"正交。两件事一起做，出问题无法归因。
2. **不要保留三个场景然后用 Additive 拼。** 那只是把"场景切换"换成"场景加载卸载"，仍要处理重复 EventSystem、多 Canvas 排序、卸载时机——成本比真合并高，收益只有一半。
3. **不要借机重构 Core / Save 层。** Core 是纯 C# 且有 EditMode 测试，它是这个项目里最健康的部分。这次只动 `Game/UI` 一层。
4. **不要先改再量。** 没有 Step 0 的基线，重构后"是不是真的更好"就只能靠争论。

---

## 8. 如果只想先解决最痛的一点

不做全量合并的最小缓解：**只把战斗改成在探索界面上叠加**（压栈一个全屏 Combat 覆盖层），基地与探索仍保持现状。这样 `CombatBootstrap:755,911` 的两处 `LoadScene("ExpeditionPrototype")` 立刻消失，最频繁的那次往返先归零，收益/改动比最高。之后要不要连基地一起收，可以再判断。

---

## 附：实测证据索引

- 场景构成：`Assets/_Project/Scenes/*.unity` 各 8 个 YAML 对象、`!u!20` 计数为 0
- 运行时建相机：`Assets/_Project/Game/UI/BasePrototypeBootstrap.cs:2027-2046`
- 状态静态化：`Assets/_Project/Game/GameSession.cs:7-47`、`:96-123`
- 战斗往返链：`GameSession.cs:646` → `ExpeditionPrototypeBootstrap.cs:339` → `CombatPrototypeBootstrap.cs:911` → `ExpeditionPrototypeBootstrap.cs:122`
- 场景名分支：`Assets/_Project/Game/UI/SettingsOverlay.cs:235`
- 设计意图：`docs/DESIGN.md:262`、`docs/DESIGN.md:281`

# 新系统规划：通讯中枢 / 任务 / 隐藏关卡（危机模式）

> 版本：**v2（已按 6 条拍板修订）**
> 前置：C-1…C-7 已落地（96 件物品 / 28 种敌人 / 10 个地点 / 292 测试）；C-9 落地后为 101 件物品 / 330 测试
> v1 → v2 的变化集中在 §0.1，逐条对应你的 6 个决定。

---

## 0. 这套系统的核心张力

三个系统不是并列的三个功能，是一条**「社交 → 信息 → 风险」**的漏斗：

```
通讯中枢（能不能听）→ 任务（愿不愿意做）→ 危机模式（敢不敢去）
```

普通下潜是**赚钱**，危机模式是**拿货**。而危机模式真正的作用，是给「密钥档次跟不上债务曲线」的玩家一条**硬闯的兜底路**。

### 0.1 六条决定带来的设计变化

| # | 你的决定 | 对设计的影响 |
|---|---|---|
| 1 | 「通讯密钥」改名**通讯上行链路** | 枚举 `ItemCategory.Uplink`，物品 id `uplink_*`，类别名「上行链路」。语义上也不再是「钥匙」，而是**一条被占用的信道**——这正好解释了为什么它能被"锁定" |
| 2 | **必须击败指定最终 Boss 才能结束游戏** | 胜利结局收敛为唯一路径；`CampaignEnding.Escaped` 废弃（枚举保留、不再可达）；危机 Boss 分三级，**只有核心潜网的 `corona_sovereign` 是终局** |
| 3 | **同舱即可**，危机模式**极大提升敌人强度** | 删掉 v1 的相邻判定 → **`DivePod` 不用改 `SelectedKeyInstanceId`，风险归零**。危机定位从「高阶挑战」改成「低档密钥的硬闯兜底」：敌人 +120% HP / +1 能量 / 常驻随从，掉落整体上移一档 |
| 4 | 测序器**永久** | 不消耗。但因此它必须是个**开关**而不是**强制**——否则玩家一有测序器就永远只能打危机（见 §3.2） |
| 5 | 接任务即**锁定上行链路**直至完成 | 删掉 `ActiveLimit = 3`。**并行上限 = 中枢物理空间**，一条链路只承载一个任务 |
| 6 | NPC 货架**绕开**周次限制 | 不变，v1 方案成立 |

第 5 条是这次修订里最漂亮的一处：**并行限制从一条硬编码数字变成了格子约束**。玩家想同时做 4 个任务，就得先把中枢升到能放下 4 条链路——限制变得可看见、可投资，而不是一个 UI 上的抽象规则。

---

## 1. 系统一：通讯中枢（CommsHub）

### 1.1 建筑规格

沿用现有设施三件套：格子制 `BagInventory` + `FacilityUpgrade.TryPay` + `FacilityGrid.Resize`。

| Lv | 尺寸 | 格数 | 升级费 | 升级材料 |
|---:|---|---:|---:|---|
| 1 | 2×2 | 4 | — | — |
| 2 | 3×2 | 6 | 250 | scrap, scrap, wire |
| 3 | 3×3 | 9 | 500 | alloy, circuit |
| 4 | 4×3 | 12 | 900 | circuit, servo |
| 5 | 4×4 | 16 | 1500 | superalloy, optic_fiber |

- `MaxLevel = 5`。
- **L5 刻意不用 `crisis_core`**：中枢是危机模式的前置（白鸦卖测序器），升级若需要危机产物就成循环依赖。
- 只收 `ItemCategory.Uplink`。

### 1.2 通讯上行链路：为什么新开一个 `ItemCategory`

现有密钥用 `ItemCategory.Key`，被三处消费：`DivePod.EnsureSelection` / `key_box` 的 `ContainerSpec.AllowedCategories` / `Merchant` 排除列表。

如果上行链路也用 `Key`：

- **潜网舱会把它当关卡密钥**——`EnsureSelection` 只按 `Category == Key` 挑第一个，选中后 `DiveKeyLibrary.TryResolve` 报「未知密钥」，玩家看到「我明明放了密钥为什么不能下潜」。
- **密钥盒会把它收进去**——`ContainerSpec` 只有 `AllowedCategories` / `RequiredAnyTags` / `AllowNestedContainers` 三重**白名单**，**没有排除机制**。靠 `ItemTag` 只能实现「都收」或「都不收」。

新增 `ItemCategory.Uplink`（**追加在枚举末尾**，不打乱现有序号）。代价是 4 处枚举分支要补，收益是三条容器规则天然互斥。

| 物品 | NPC | Level | 形状 | 格数 |
|---|---|---:|---|---:|
| `uplink_junk` 旧频道上行链路 | 老陈 | 1 | `#` | 1 |
| `uplink_arms` 军械上行链路 | K-7 | 2 | `##` | 2 |
| `uplink_intel` 情报上行链路 | 白鸦 | 3 | `##`,`##` | 4 |
| `uplink_seed` 核心上行链路 | SEED | 4 | `###`,`###` | 6 |

体积规则与关卡密钥完全一致（1/2/4/6 格）。四把合计 **13 格**：

- Lv.4（4×3 = 12 格）**差 1 格放不下全部四条**，得升 Lv.5（16 格）。
- 但链路随时能换，这不是硬门控——**Lv.5 买的是「同时挂 4 个任务」**。

`Value = 0`，于是 `Merchant` 天然不卖也不收。上行链路不是商品，是**关系凭证**。

### 1.3 NPC 定义

```csharp
public sealed class NpcDefinition
{
    public string Id;                 // "npc_arms"
    public string DisplayName;        // "军械贩 · K-7"
    public string UplinkItemId;       // "uplink_arms"
    public int Tier;                  // 1–4
    public string Faction;
    public string Greeting;
    public string[] StockPool;        // 专属货架候选
    public int StockSize = 3;
    public int RefreshDays = 2;
    public string[] QuestPool;
}
```

| NPC | 卖什么 | 「更好」体现在哪 | 任务线 |
|---|---|---|---|
| **回收商 · 老陈**（T1） | T1–T2 材料按包卖、修复包 | 材料比商人便宜 25% | 教学链 |
| **军械贩 · K-7**（T2） | T2–T4 武器 / 护甲 | **不吃周次限制**，W1 就能买 T4 | 击杀链 |
| **掮客 · 白鸦**（T3） | 数据核心、超频核心等情报货；**不卖测序器/深空密钥** | 卖别处买不到的情报件；测序器是 `q_intel_1` 奖励，深空密钥走制作站 | 探索链，通往危机 |
| **核心之声 · SEED**（T4） | T5 装备、`crisis_core` | 全场唯一 T5 货架 | 终局链 |

### 1.4 NPC 货架 vs 主商人

| | 主商人 | NPC 货架 |
|---|---|---|
| 件数 | 6 | 3 |
| 刷新 | 每日 | 每 2 天 |
| 档位限制 | `MaxTierForWeek` | **无限制** |
| 价格 | `Value × 3` | `Value × 4` |
| 收购 | 有（50%） | **无** |

「更好的购买选项」= **更高档位 + 更专品类**，不是更便宜。刻意的不对称：贵 33% 是绕过时间门的代价；不收购是为了不让玩家绕开主商人的 50% 折价、把 `Merchant.SellRatioPercent` 的经济设计打穿。

---

## 2. 系统二：任务系统

### 2.1 数据模型

```csharp
public enum QuestObjectiveKind
{
    KillEnemies, KillTagged, ClearDive, CollectItem,
    DefeatCrisisBoss, ClearCrisis, PayDebt
}

public enum QuestState { Offered, Active, Ready, Claimed, Failed }

public sealed class QuestDefinition
{
    public string Id, NpcId, Title, Description;
    public QuestObjectiveKind Objective;
    public string TargetId;
    public int TargetCount = 1;
    public int TimeLimitDays;
    public int RewardBit;
    public string[] RewardItemIds;
    public string RewardUplinkItemId;   // 完成后解锁下一位 NPC
}
```

### 2.2 并行限制：不是数字，是格子

**没有 `ActiveLimit`。** 一条上行链路在 `Active` / `Ready` 期间被**锁定**在中枢格子里：

- 不能移出中枢、不能在格间拖动、不能旋转、不能拖到其他设施或仓库。
- 一个 NPC 有多条任务，但**一条链路同一时间只承载一个任务** → 想接老陈的第二个任务，得先做完第一个。
- 因此并行上限 = 中枢里能同时放下的链路数，且受体积约束：
  - Lv.2（6 格）：老陈 1 + K-7 2 = 3 格，能挂 2 个任务，还剩 3 格放不下白鸦（4 格）
  - Lv.3（9 格）：老陈 + K-7 + 白鸦 = 7 格，能挂 3 个任务
  - Lv.5（16 格）：四条全放（13 格）= 4 个任务全开

**解锁时机**：`Claimed` 之后（不是 `Ready`）。否则玩家可以在「达成但没领奖」时把链路抽出去换别人，等于零成本过桥。

**两个必须自动解锁的意外路径**：

1. `Failed`（超时）→ **立刻解锁**。任务超时还把链路锁着，玩家会被永久卡住，连换人都做不到。
2. `Abandon`（主动放弃）→ 立刻解锁，但该 NPC **冷却 2 天**（`CooldownUntilDay`），防止刷任务池。

### 2.3 追踪点：只有三个

任务进度**不做事件总线**，只在三个天然节点上报：

| 节点 | 上报什么 |
|---|---|
| `GameSession.CompleteEncounter(int hp)` | `KillEnemies` +1；`KillTagged`；危机 Boss 战则 `DefeatCrisisBoss` |
| `GameSession.CompleteExpedition(bool survived)` | `survived` 时 `ClearDive`（比对 `SiteId`）+ `ClearCrisis` |
| 基地页刷新 `QuestLog.RefreshStatic(...)` | `CollectItem`（`ItemSearch.CountAll` 穿透容器）+ `PayDebt`（`Campaign.TotalPaid`） |

两个实现细节：

1. **`CompleteExpedition` 里 `Scenario` 会被置 null**（现有代码第 475 行）。上报必须在 `Scenario = null` **之前**快照 `SiteId` 与 `IsCrisis`，否则 `ClearDive` 永远匹配不上。这是最容易漏的一行。
2. `CollectItem` **不在物品移动时实时判定**——每个格子拖拽都扫整包（含容器穿透）不划算，基地页每次刷新算一次足够。

### 2.4 任务表（10 个，4 条链）

| id | NPC | 标题 | 目标 | 时限 | 奖励 |
|---|---|---|---|---:|---|
| `q_junk_1` | 老陈 | 清一清场 | 通关旷野 1 次 | 0 | 80 bit |
| `q_junk_2` | 老陈 | 收废料 | 交 5 个 `scrap` | 0 | 120 bit + **uplink_arms** |
| `q_junk_3` | 老陈 | 矿场学徒 | 收取矿场 3 次 | 0 | 100 bit + 2×`patch` |
| `q_arms_1` | K-7 | 打得动 | 击杀 10 个敌人 | 0 | 200 bit |
| `q_arms_2` | K-7 | 军用规格 | 通关军用潜网 2 次 | 5 天 | 260 bit + `plasma_cutter` |
| `q_arms_3` | K-7 | 深一点 | 通关深空潜网 1 次 | 0 | 400 bit + **uplink_intel** |
| `q_intel_1` | 白鸦 | 带样本回来 | 交 3 个 `superalloy` | 0 | 350 bit + **`cipher_sequencer`** |
| `q_intel_2` | 白鸦 | 第一次危机 | 通关危机模式 1 次 | 0 | 800 bit + **uplink_seed** |
| `q_seed_1` | SEED | 核心环 | 击败危机 Boss 1 次 | 0 | 1200 bit + `corona_aegis` |
| `q_seed_2` | SEED | 缴清 | 累计缴款 10000 bit | 0 | 2000 bit + `crisis_core` |

- **`uplink_intel` 来自 K-7，测序器来自白鸦**，所以想进危机必须走完 老陈→K-7→白鸦 三段，跳不过去。
- 时限只给 `q_arms_2`（5 天），用来教会玩家「任务会过期」，但不惩罚太狠。
- 因为链路锁定，**老陈的 3 个任务只能顺序做**——这就是并行限制的实际体感。

---

## 3. 系统三：隐藏关卡（危机模式）

### 3.1 密码测序器

| 属性 | 值 |
|---|---|
| id | `cipher_sequencer` |
| 名称 | 密码测序器 |
| Level | 3 |
| 形状 | `ItemShape.Parse("#", "#")` → **1×2 竖放** |
| Category | **`Tool`**（不是 `Key`，否则会被潜网舱当密钥选中） |
| Tags | `Data \| Tool` |
| Value | 0 |
| 消耗 | **永不消耗**（永久工具） |
| 耐久 | 无 |

### 3.2 判定：同舱即可，但它是开关不是强制

`CrisisRules.TryDetect(BagInventory podGrid, ItemCatalog catalog, string selectedKeyId)`：

1. 舱内是否存在任意 `cipher_sequencer` 实例。
2. 存在 → 当前选中密钥对应的地点**解锁**危机模式。

**关键：解锁 ≠ 强制。** 测序器是永久的，如果它在舱里就自动进危机，玩家把测序器放进去一次就再也打不了普通下潜了。所以：

> 测序器负责把「危机下潜」这个**选项**打开，具体走哪条路由玩家每次自己选。

交互：舱内有测序器时，部署按钮的点击会弹出二选一确认层：

```
┌────────────────────────────┐
│  军用潜网 · 第 2/7 天          │
│  ┌──────────┐ ┌────────────┐│
│  │ 普通下潜   │ │ 危机下潜     ││
│  └──────────┘ └────────────┘│
│  危机：无法撤离，必须击败 Boss  │
└────────────────────────────┘
```

不可逆的选择必须有一次确认，这里正好用得上。

`GameSession.BeginExpedition()` 增加 `BeginExpedition(bool crisis)` 重载，`DivePlan` 增加 `bool IsCrisis` / `string CrisisBossId` / `int StageCount`。

同舱判定让 v1 的一个风险直接消失：**不需要给 `DivePod` 加 `SelectedKeyInstanceId`**，`EnsureSelection` / `SelectKey` / `SelectNextKey` / `Consume` 四个方法一行都不用动。

### 3.3 危机模式 = 债务兜底路径

这是这次修订里最重要的定位变化。

原来的设计是「危机 = 高阶挑战」。但按你的说法，它真正的用途是：

> 玩家没拿到高等级密钥，可是时间推进到了需要还清大量负债的时候。

所以危机的核心不是**难度**，是**难度换档位**：

| | 普通 | 危机 |
|---|---|---|
| 阶段数 | 3 | **5** |
| 撤离 | 随时 | **完全没有撤离选项** |
| 结束条件 | 撤离 | **击败 Boss** |
| 敌人 HP | 基础 + 探索层数加成（上限 +15） | **+120%** |
| 敌人能量 | 基础 | **+1** |
| 敌人数量 | 第 2 场起加 1 随从 | **每场都加 1 随从** |
| 掉落档位 | 地点原表 | **整体上移一档** |
| 物资量 | `LootCount` | `LootCount × 1.5` |
| 死亡 | 丢本次战利品 | 同左（不额外惩罚——不能撤离已经是惩罚） |

**掉落档位上移规则**（`CrisisRules.ShiftTierWeights`）：`TierMin + 1`，权重向量丢掉首项、把首项份额并入末项。

| 地点 | 普通档位 | 危机档位 |
|---|---|---|
| 基础潜网 | T1–T3 40/40/20 | **T2–T4 40/20/40** |
| 超频 | T2–T3 50/50 | **T3 100** |
| 军用 / 毒雾 | T2–T4 50/35/15 | **T3–T5 35/15/50** |
| 信号 / 数据金库 | T3–T4 70/30 | **T4–T5 30/70** |
| 深空潜网 | T4–T5 60/40 | **T5 100** + Boss 必掉 |
| 核心潜网 | T5 100 | T5 100 + Boss 必掉 `crisis_core` |

效果：**用一把 T2 军用密钥开危机，就能刷到 T5 掉落**。这就是兜底——不需要先拿到深空密钥，只需要敢打一场 HP 翻倍的 5 阶段硬仗。

敌人强度的实现点很省：`EnemyLibrary.Create(id, catalog, bonusHp, seed)` 的 `bonusHp` 本来就是**加法**（`archetype.MaxHp + bonusHp`），危机只要传 `archetype.MaxHp * 120 / 100` 即可；再给同签名加一个 `int bonusEnergy = 0` 可选参数。

### 3.4 三级危机 Boss，只有一个是终局

| 地点档 | Boss | HP | 能量 | 终局 |
|---|---|---:|---:|---|
| T1–T3 | `breach_hound` 裂隙猎犬 | 120 | 4 | 否 |
| T4 深空 | `void_herald` 虚空先驱 | 170 | 4 | 否 |
| **T5 核心** | **`corona_sovereign` 日冕主宰** | **220** | **5** | **是（唯一）** |

- 三个 Boss 都只由 `CrisisRules` 指定，**不进随机敌人池**。
- **只有核心潜网的危机模式能遇到 `corona_sovereign`**，所以终局路径被锁在：T5 核心密钥（9 格 → 潜网舱 Lv.5）+ 测序器 + 打完 5 阶段 + 击败 220 HP Boss。
- 击败 `corona_sovereign` → **立刻 `CampaignEnding.BossSlain`，游戏结束**。没有「继续经营」选项，按你的要求。
- `breach_hound` / `void_herald` 击败后正常出关，给 `crisis_core` + T5 掉落，可以反复刷。

**`CampaignEnding.Escaped` 的处置**：枚举值保留（不破坏现有测试与序列化），但**不再有任何可达路径**——剧情上「逃出去」已经不是一个结局。`CampaignStateTests.EndCampaign_EscapedMarksVictory` 保留不动（它测的是状态机能力，不是设计意图），只在 UI 文案上不再提供入口。

> 注意：这意味着 **`CampaignStatus.Active` 会永久持续到打赢或破产**。这是无限债务 + 唯一终局的自然结果，符合 C-1 定的「不存在还清即胜利」。

### 3.5 危机专属探索分支

每个主题在危机模式下插入 2 条专属行动（普通模式看不到）。特点：**大方差**，且有一条直通 Boss 的捷径。

| 主题 | 行动 | 倾向 |
|---|---|---|
| 旷野 | 「循着牵引索」/「钻进地缝」 | 高 `Clue`，低 `Damage` |
| 设施 | 「重启分区电源」/「走通风井」 | 高 `Loot`，`Alert` +2 |
| 军事 | 「抢占火力点」/「放倒整排」 | 高 `Combat`，击杀后额外掉落 |
| 信号 | 「伪造身份」/「反向追踪」 | `Clue` 为主，可跳阶段 |
| 深空 | 「断缆加速」/「爬外壁」 | 大 `Loot` + 自伤 8 |
| 核心 | 「同步主序」/「手动泄压」 | 直通 Boss，跳过 1–2 次物资 |

`ExpeditionOutcomeKind` **不新增枚举值**，全部复用现有 7 种——新分支是新的 `ActionTemplate` + `ExpeditionOutcomeEntry` 组合，不是新机制。这是控制复杂度的关键。

### 3.6 结算

| 结果 | 处理 |
|---|---|
| 击败 `breach_hound` / `void_herald` | 出关；`crisis_core` ×1 + 1 件 T5 装备 + 1.5× 物资 |
| 击败 `corona_sovereign` | **`EndCampaign(BossSlain)` → 游戏结束** |
| 阵亡 | 正常阵亡流程（丢本次战利品），战役继续 |

`crisis_core`（T5 材料，价值 320）**只有危机 Boss 掉**，是 T5 专属配方的唯一来源。它让危机模式从「可选的挑战」变成「T5 装备的必经之路」。

---

## 4. 进度闭环

```
开局赠送 uplink_junk
      │
      ▼
   老陈 (T1·1格) ──q_junk_2──▶ uplink_arms
      │                          │
      ▼                          ▼
   （锁链路，一次一个）       K-7 (T2·2格) ──q_arms_3──▶ uplink_intel
                                 │                        │
                                 │ 卖 T2–T4（不吃周次）    ▼
                                 │                   白鸦 (T3·4格)
                                 │                        │
                                 │              q_intel_1 ─┴─▶ cipher_sequencer
                                 │                        │
                                 │                        ▼
                                 │              危机模式（测序器同舱即解锁）
                                 │                        │
                                 │              q_intel_2 ─┴─▶ uplink_seed
                                 │                        │
                                 │                        ▼
                                 │                   SEED (T4·6格) ── 卖 T5
                                 │                        │
                                 └────────────────────────┴──▶ 危机 Boss 三连
                                                                  │
                                          ┌───────────────────────┴──────────────┐
                                          ▼                                      ▼
                              裂隙猎犬 / 虚空先驱                     核心潜网 · 日冕主宰
                              （刷 crisis_core，可重复）                （击败 → 游戏结束）
```

与现有系统的咬合点：

- **危机 → `crisis_core` → T5 配方**：T5 装备原本只能靠掉落，现在有稳定来源。
- **危机 → 终局**：`corona_sovereign` 只在核心潜网危机出现，而核心潜网需要 T5 密钥（9 格）+ 潜网舱 Lv.5。链条自己闭合，不需要新加门控。
- **任务 → 缴款**：`q_seed_2` 用 `TotalPaid` 当进度，把无限债务接进任务系统。

---

## 5. 代码改动清单

### 新增（6 个 Core 文件）

| 文件 | 内容 | 行数估 |
|---|---|---:|
| `Core/Quests/NpcLibrary.cs` | `NpcDefinition` + 4 定义 + `All` / `TryGet` / `TryFindByUplink` | ~140 |
| `Core/Quests/QuestTypes.cs` | `QuestObjectiveKind` / `QuestState` / `QuestDefinition` / `QuestProgress` | ~90 |
| `Core/Quests/QuestLibrary.cs` | 10 个任务 + 按 NPC 查询 + 链式解锁 | ~220 |
| `Core/Quests/QuestLog.cs` | 状态机 + `Accept` / `TryClaim` / `Abandon` / `Tick` / `RefreshStatic`，**无 ActiveLimit** | ~240 |
| `Core/Base/CommsHub.cs` | 设施本体 + **`TryLockUplink` / `UnlockUplink` / `IsLocked(instanceId)` / `LockedQuestId`** | ~200 |
| `Core/Expedition/CrisisRules.cs` | `TryDetect`（同舱）+ `BossForTier` / `IsFinalBoss` + `ShiftTierWeights` + `StagesFor` + `EnemyHpPercent` | ~130 |

### 修改（9 处）

| 文件 | 改什么 | 风险 |
|---|---|---|
| `Core/Items/ItemTypes.cs` | `ItemCategory` 加 `Uplink`（**末尾追加**） | 低 |
| `Core/Items/DefaultItemLibrary.cs` | +4 上行链路、+`cipher_sequencer`、+`crisis_core` | 低（`Parse` 行等长） |
| `Core/Items/ItemText.cs` | `CategoryName(Uplink)` = 「上行链路」 | 低（漏了显示空） |
| `Core/Commerce/Merchant.cs` | 排除列表 + `Uplink` | 低（`Value = 0` 已挡，显式写防以后设价） |
| `Core/Combat/EnemyLibrary.cs` | `Create` 加可选 `int bonusEnergy = 0`；+3 个危机 Boss | 低 |
| `Core/Base/DiveKeys.cs` | `DivePlan` + `IsCrisis` / `CrisisBossId` / `StageCount` / `CrisisUnlocked` | 低 |
| `Core/Expedition/ExpeditionScenario.cs` | `MaxStages` 常量→实例字段；危机不生成 Retreat；`BuildBossStage()`；6 主题 × 2 危机行动 | **中**——`Resolve` / `BuildOptions` / `AdvanceAfterItemUse` 三处都判 `Stage >= MaxStages` |
| `Game/GameSession.cs` | `Comms` / `Quests` 属性；`BeginExpedition(bool crisis)`；两处上报（**先快照 `SiteId`/`IsCrisis`**）；危机敌人加成 | **中** |
| `Game/UI/BasePrototypeBootstrap.cs` | 第 8 标签「通讯」+ 通讯页 + 部署前二选一确认层 | **中**——面板已满 |
| `Game/UI/ExpeditionPrototypeBootstrap.cs` | 危机 HUD：隐藏撤离、Boss 目标、阶段 5 | 低 |
| `Game/UI/CombatPrototypeBootstrap.cs` | `CreateEnemies` 传危机加成 HP / 能量 / 常驻随从 | 低 |

### UI 空间：8 个标签怎么塞

右侧面板 170px，现 7 个标签各 22px（合计 154px），字号 7，两字标签。

| 方案 | 做法 | 评价 |
|---|---|---|
| A | 8 标签 × 20px，字号 7→6 | 最省事，6 号字勉强可读 |
| B | 两行 4+4，每行 22px | 清楚，但吃掉 18px 内容高度（138→120） |
| **C（先试）** | 一行 8 标签 × 21px，字号 7 | 21px 放两个 7 号字约 14px + 留白，可行 |

先按 C 做，**以 Play 模式截图为准**，挤了就退 B。这一项不接受凭算术定稿。

---

## 6. 测试计划（预计 +52 → 约 344）

| 文件 | 数量 | 关键用例 |
|---|---:|---|
| `CommsHubTests.cs` | 14 | 只收 `Uplink`；`Key` 放不进中枢；`Uplink` 放不进潜网舱与密钥盒；四条链路 13 格需 Lv.5；升级保留已放入链路；**锁定后 `TryMove`/`TryTransform`/`Remove` 全部被拒**；解锁后恢复 |
| `QuestTests.cs` | 20 | 状态机四条边；**接任务锁链路、领取才解锁**；一条链路不能同时承载两个任务；`Failed` 自动解锁 + 冷却 2 天；`Abandon` 解锁；`CollectItem` 领奖扣物品；`ItemSearch` 穿透容器；`ClearDive` 只认指定 `SiteId`；`PayDebt` 走 `TotalPaid` |
| `CrisisTests.cs` | 18 | 测序器同舱即解锁；无测序器不解锁；**解锁后仍能选普通下潜**；T3 密钥→`breach_hound`；T4→`void_herald`；T5→`corona_sovereign`；只有 `corona_sovereign` 是终局；危机 5 阶段；危机选项无 `Exit`；档位上移规则（含 T5 封顶）；敌人 HP +120% 与能量 +1 生效 |

回归保护：`TierContentTests.EveryLootPoolAndEnemyPoolIdExists` 这类全库扫描要覆盖新物品与三个 Boss，否则拼错的 id 不会被抓到。

---

## 7. 分两批交付

### C-9：通讯中枢 + 任务（不含危机）

1. `ItemCategory.Uplink` + 4 上行链路 + `ItemText` / `Merchant` 同步
2. `CommsHub`（Lv.1–5）+ **链路锁定机制** + 基地第 8 标签通讯页
3. `NpcLibrary` / `QuestTypes` / `QuestLibrary` / `QuestLog`
4. `GameSession` 接线（三节点上报）+ 开局赠送 `uplink_junk`
5. `CommsHubTests` + `QuestTests`（34 条）

**验收**：联系老陈 → 接任务（链路锁定、拖不走）→ 完成旷野 → 领奖解锁 → 拿到 `uplink_arms` → 联系 K-7 → 买到 T4。

### C-10：密码测序器 + 危机模式 + 终局

1. `cipher_sequencer` + `crisis_core` + 3 个危机 Boss
2. `CrisisRules`（同舱判定 + 档位上移 + Boss 映射 + 强度加成）
3. `ExpeditionScenario` 危机分支（5 阶段 / 无撤离 / Boss 阶段 / 6 主题 × 2 行动）
4. 危机 UI + 部署前二选一确认层 + 终局结算
5. `CrisisTests`（18 条）

**验收**：测序器进舱 → 部署时可选危机 → 打不过就是死 → 打赢拿 `crisis_core` + T5 → 核心潜网危机击败日冕主宰 → **游戏结束**。

---

## 8. 风险

| 风险 | 应对 |
|---|---|
| 链路锁定让玩家卡住 | `Failed` / `Abandon` 强制解锁；`IsLocked` 的拒绝理由是具体文案（「正在执行任务，无法移动」）而不是静默失败 |
| 危机模式太难，一次都不敢进 | 它不是进度必需品（`crisis_core` 只做 T5 配方），第一次可以放弃；且 T1–T3 也能开危机，难度随地点档位走 |
| 同舱判定让危机过于容易触发 | 这是有意的（你要的是兜底路径）。真正的门槛是「5 阶段 + 不能撤离 + HP 翻倍」，不是解锁条件 |
| 8 个标签挤爆面板 | C 方案先试 + 截图；不行退 B |
| 危机 Boss 220 HP 打不动 | 危机档位能刷到 T5 装备，玩家可以先刷两次 `void_herald` 强化再去核心 |
| 终局只有一条路，玩家觉得被卡死 | 破产是另一条出口（失败）；且 `corona_sovereign` 可以反复挑战，只是每次进去都可能死 |

---

## 9. 明确不做

- 任务对话树 / 选项分支 / 好感度。NPC = 一个货架 + 一张任务列表。
- 任务失败惩罚（除冷却 2 天）。
- 危机模式中途存档点 / 撤退。死了就是死了。
- NPC 主动联系玩家 / 推送。
- 通讯中枢的「信号强度」「干扰」等模拟层。格子就是全部规则。
- 逃亡结局。按你的决定，胜利只有击败 `corona_sovereign` 一条路。

---

## 10. 已确认的 6 个决策

| # | 决定 |
|---|---|
| 1 | 通讯密钥 → **通讯上行链路**（`ItemCategory.Uplink`） |
| 2 | **必须击败指定最终 Boss（`corona_sovereign`）才能结束游戏**；逃亡结局废弃 |
| 3 | **同舱即可**解锁；危机模式**极大提升敌人强度**，作为债务兜底路径 |
| 4 | 测序器**永久**，不消耗（因此做成开关而非强制） |
| 5 | 接任务即**锁定上行链路**至完成，并行上限由格子决定，**无 ActiveLimit** |
| 6 | NPC 货架**绕开**周次限制 |

全部已写入本文，无待定项。可直接开工 C-9。

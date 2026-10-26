"""把导出的 JSON 变成可编辑的 xlsx。

输入：docs/data_raw_items.json、docs/data_raw_world.json
输出：docs/game_data.xlsx

表结构的设计原则：
- 一屏能横向看到的关键列放前面（id / 名称 / 档位 / 类别）
- 一对多字段（tags / traits / 敌人配装 / 掉落池）用分隔符 DSL，语法见 Cheatsheet 表
- 枚举列加下拉验证，避免手打出错
"""
import json
import os

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

HERE = os.path.dirname(os.path.abspath(__file__))

# 三种表头底色，代表列的用途分组
GROUP_FILL = {
    "basic": "1B1F26",
    "combat": "4A2A22",
    "economy": "1F3A2A",
    "world": "1B1F26",
    "meta": "2A2438",
}

THIN = Side(style="thin", color="3A4048")
BORDER = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
CENTER = Alignment(horizontal="center", vertical="center")
LEFT = Alignment(horizontal="left", vertical="center", wrap_text=False)


def load():
    with open(os.path.join(HERE, "data_raw_items.json"), encoding="utf-8-sig") as f:
        items = json.load(f)
    with open(os.path.join(HERE, "data_raw_world.json"), encoding="utf-8-sig") as f:
        world = json.load(f)
    return items, world


def write_sheet(wb, title, cols, rows, freeze="B2", note=None, dv_specs=None):
    """cols: [(key, header, width, group)]"""
    ws = wb.create_sheet(title)
    start = 1

    if note:
        ws.cell(row=1, column=1, value=note)
        ws.cell(row=1, column=1).font = Font(color="808A98", size=9, italic=True)
        ws.merge_cells(start_row=1, start_column=1,
                       end_row=1, end_column=max(1, len(cols)))
        start = 2

    for i, (key, header, width, group) in enumerate(cols, start=1):
        c = ws.cell(row=start, column=i, value=header)
        c.fill = PatternFill("solid", fgColor=GROUP_FILL.get(group, "1B1F26"))
        c.font = Font(color="E6ECF4", bold=True, size=10)
        c.alignment = CENTER
        c.border = BORDER
        ws.column_dimensions[get_column_letter(i)].width = width

    ws.row_dimensions[start].height = 22

    for r, row in enumerate(rows, start=start + 1):
        for i, (key, _, _, _) in enumerate(cols, start=1):
            v = row.get(key, "")
            if isinstance(v, str) and v.isdigit():
                v = int(v)
            c = ws.cell(row=r, column=i, value=v)
            c.border = BORDER
            c.alignment = LEFT if isinstance(v, str) and len(str(v)) > 10 else CENTER
            c.font = Font(size=10, color="D8DEE8")

    last_row = start + len(rows)
    ws.freeze_panes = ws.cell(row=start + 1, column=2)
    ws.auto_filter.ref = "A%d:%s%d" % (start, get_column_letter(len(cols)), last_row)

    # 枚举列下拉
    for spec in (dv_specs or []):
        col_letter, options = spec
        dv = DataValidation(type="list", formula1='"%s"' % ",".join(options),
                            allow_blank=True, showDropDown=False)
        ws.add_data_validation(dv)
        dv.add("%s%d:%s%d" % (col_letter, start + 1, col_letter, last_row))

    return ws


def build_readme(wb, items, world):
    ws = wb.create_sheet("README", 0)
    ws.column_dimensions["A"].width = 22
    ws.column_dimensions["B"].width = 96

    rows = [
        ("游戏数据表", ""),
        ("来源", "由 Unity 运行时导出的真实数据生成，不是手写的"),
        ("坐标/尺寸基准", "1280 × 720 px，1 格道具 = 32 × 32 px"),
        ("", ""),
        ("工作表", "内容"),
        ("Items", "%d 件物品（武器/护甲/工具/消耗品/战利品/材料/密钥/弹药/容器/芯片/上行链路/契约）"
                  % len(items["items"])),
        ("Traits", "%d 条词条（物品的特殊效果，rank 不能超过物品档位）" % len(items["traits"])),
        ("Enemies", "%d 种敌人（HP / 能量 / 初始配装）" % len(world["enemies"])),
        ("Sites", "%d 个地点（潜网舱可选的目标，掉落池与敌人池都在这里）" % len(world["sites"])),
        ("Quests", "%d 个任务（NPC 发布，完成条件 + 奖励）" % len(world["quests"])),
        ("NPCs", "%d 位 NPC（通讯上行链路对应的人，卖什么、发什么任务）" % len(world["npcs"])),
        ("Facilities", "%d 行设施升级表（6 种设施 × 各等级的空间/成本/材料）"
                       % len(world["facilities"])),
        ("Buffs", "%d 种状态效果（超载/过热/冷却 等）" % len(world["buffs"])),
        ("Enums", "所有枚举的合法取值，填表前先查这里"),
        ("Cheatsheet", "一对多字段的写法（标签、词条、敌人配装、掉落池）"),
        ("", ""),
        ("改完之后", "改完 Excel 需要导回代码才会生效，见下方「回灌」"),
        ("", ""),
        ("回灌方案 A", "脚本读 xlsx → 生成 ItemData.g.cs 等纯数据文件，编译进游戏。"
                       "类型安全、有编译期校验，但每次改表要重新编译"),
        ("回灌方案 B", "脚本读 xlsx → 生成 JSON 放进 Resources，运行时读取。"
                       "改表不用编译，但会丢掉编译期校验，且要改 Core 的构造逻辑"),
        ("建议", "先走 A。Core 是纯 C# 无引擎依赖、且有 379 条测试锁着数值规则，"
                 "生成代码能继续受测试保护；B 会让测试失去意义"),
    ]

    for r, (a, b) in enumerate(rows, start=1):
        ca = ws.cell(row=r, column=1, value=a)
        cb = ws.cell(row=r, column=2, value=b)
        if a and not b:
            ca.font = Font(bold=True, size=12, color="D8A050")
        else:
            ca.font = Font(bold=True, size=10, color="C8D0DC")
            cb.font = Font(size=10, color="A8B2C0")
        ca.alignment = Alignment(vertical="top")
        cb.alignment = Alignment(vertical="top", wrap_text=True)


def build_enums(wb):
    enums = [
        ("ItemCategory", "Weapon, Armor, Tool, Consumable, Loot, Material, Key, "
                         "Currency, Ammo, Action, Container, Uplink, Quest"),
        ("ItemActivation", "Active, Passive"),
        ("ItemTarget", "None, Self, Enemy"),
        ("DamageType", "Melee, Projectile, Burn, Toxin, Asphyxiation"),
        ("ItemTag", "ItemTag 是位标志，可多选，用逗号分隔。合法值："
                    "Blade, Shield, Armor, Chip, Tool, Consumable, Data, Mechanical, "
                    "Projectile, Energy, Toxin, Cooling, Salvage, Drone, Targeting, "
                    "Container, CpuChip, Assist"),
        ("TraitTrigger", "见 Traits 表 trigger 列的实际取值"),
        ("QuestObjectiveKind", "KillEnemies, KillTagged, ClearDive, CollectItem, "
                               "CollectMine, DefeatCrisisBoss, ClearCrisis, PayDebt"),
        ("QuestState", "Offered, Active, Ready, Claimed, Failed（运行时状态，不用填）"),
        ("StatusType", "None, Overload, ShortCircuit, Cache, Overclock, Coolant, "
                       "Corrosion, Overheat, Nanorepair, Evasion"),
    ]
    ws = wb.create_sheet("Enums")
    ws.column_dimensions["A"].width = 22
    ws.column_dimensions["B"].width = 110
    for r, (name, vals) in enumerate([("枚举", "合法取值")] + enums, start=1):
        a = ws.cell(row=r, column=1, value=name)
        b = ws.cell(row=r, column=2, value=vals)
        if r == 1:
            a.fill = PatternFill("solid", fgColor=GROUP_FILL["meta"])
            b.fill = PatternFill("solid", fgColor=GROUP_FILL["meta"])
            a.font = Font(bold=True, color="E6ECF4")
            b.font = Font(bold=True, color="E6ECF4")
        else:
            a.font = Font(bold=True, size=10, color="C8D0DC")
            b.font = Font(size=10, color="A8B2C0")
            b.alignment = Alignment(wrap_text=True, vertical="top")


def build_cheatsheet(wb):
    rows = [
        ("字段", "写法", "例子", "说明"),
        ("Items.shape", "用 / 分行，# 占格 . 空",
         "##  /  #.", "「##」是横 2 格；「#/#」是竖 2 格；「###/.#.」是 T 形"),
        ("Items.tags", "逗号分隔多个标签", "Blade, Mechanical", "必须是 Enums 表里的合法值"),
        ("Items.tagRanks", "标签:等级|标签:等级", "Sharp:3|Blade:2",
         "不写等于 rank 1；rank 1-5"),
        ("Items.traits", "词条id:rank|词条id:rank", "keen_edge:2|overcharge:1",
         "rank 不能超过物品 level；单件最多 4 条"),
        ("Items.container", "宽x高|允许类别|必需标签|允许嵌套",
         "6x4|Key||0", "空字符串表示不是容器"),
        ("Items.assist", "系列|档位|战利品pp|敌人pp|受伤pp|警戒",
         "light|2|8|0|0|3", "空字符串表示不是辅助标志"),
        ("Enemies.items", "物品id@x,y,旋转,数量", "scrap_blade@0,0,0,1|coin@2,0,0,1",
         "敌人背包是 4×3；坐标从左上角算"),
        ("Sites.tierWeights", "各档份额，用 | 分隔", "0|60|40|0|0",
         "T1..T5 的份额，总和不必是 100"),
        ("Sites.lootPool", "物品 id 用 | 分隔", "scrap|wire|alloy", "按 tierMin 与档份额展开"),
        ("Sites.enemyPool", "敌人 id 用 | 分隔", "scavenger|drone",
         "必须按档拆，别把高阶敌人放进低阶地点"),
        ("Quests.rewardItems", "物品 id 用 | 分隔", "patch|patch", "空表示不给物品"),
        ("NPCs.stockPool", "物品 id 用 | 分隔", "scrap|wire|alloy", "从池里随机抽 stockSize 件"),
    ]
    ws = wb.create_sheet("Cheatsheet")
    widths = [20, 30, 30, 46]
    for i, w in enumerate(widths, start=1):
        ws.column_dimensions[get_column_letter(i)].width = w
        c = ws.cell(row=1, column=i, value=rows[0][i - 1])
        c.fill = PatternFill("solid", fgColor=GROUP_FILL["meta"])
        c.font = Font(bold=True, color="E6ECF4", size=10)
        c.alignment = CENTER
    for r, row in enumerate(rows[1:], start=2):
        for i, v in enumerate(row, start=1):
            c = ws.cell(row=r, column=i, value=v)
            c.font = Font(size=10, color="C8D0DC" if i == 1 else "A8B2C0",
                          bold=(i == 1))
            c.alignment = Alignment(wrap_text=True, vertical="top")


ITEM_COLS = [
    ("id", "ID", 18, "basic"),
    ("name", "名称", 14, "basic"),
    ("level", "档位", 6, "basic"),
    ("category", "类别", 12, "basic"),
    ("activation", "激活", 9, "basic"),
    ("target", "目标", 8, "basic"),
    ("shape", "形状", 12, "basic"),
    ("tags", "标签", 22, "basic"),
    ("tagRanks", "标签等级", 16, "basic"),
    ("traits", "词条", 28, "basic"),
    ("container", "容器规格", 20, "basic"),
    ("assist", "探索辅助", 18, "basic"),
    ("energyCost", "能量", 6, "combat"),
    ("cooldown", "冷却", 6, "combat"),
    ("damage", "伤害", 6, "combat"),
    ("damageType", "伤害类型", 10, "combat"),
    ("shield", "护盾", 6, "combat"),
    ("maxDurability", "耐久", 6, "combat"),
    ("heal", "治疗", 6, "combat"),
    ("armorMelee", "甲·近战", 8, "combat"),
    ("armorProjectile", "甲·弹道", 9, "combat"),
    ("armorBurn", "甲·燃烧", 9, "combat"),
    ("armorToxin", "甲·毒素", 9, "combat"),
    ("armorAsphyxiation", "甲·窒息", 10, "combat"),
    ("consumeOnUse", "用后消耗", 9, "economy"),
    ("value", "价值", 6, "economy"),
    ("maxStack", "堆叠", 6, "economy"),
    ("ammoItemId", "弹药ID", 12, "economy"),
    ("ammoPerUse", "每发消耗", 9, "economy"),
    ("desc", "描述", 40, "basic"),
]

TRAIT_COLS = [
    ("id", "ID", 18, "basic"),
    ("name", "名称", 14, "basic"),
    ("descFormat", "描述模板", 30, "basic"),
    ("trigger", "触发器", 18, "basic"),
    ("condition", "条件", 18, "basic"),
    ("target", "目标", 10, "basic"),
    ("effect", "效果", 22, "basic"),
    ("duration", "持续", 12, "basic"),
    ("stackRule", "叠加规则", 12, "basic"),
    ("status", "状态", 14, "basic"),
    ("statusTurns", "状态回合", 9, "basic"),
    ("shieldDivisor", "护盾除数", 9, "basic"),
    ("rankValues", "五级数值", 22, "combat"),
    ("conditionValues", "条件数值", 22, "combat"),
    ("maxTriggersPerTurn", "每回合上限", 11, "combat"),
]

ENEMY_COLS = [
    ("id", "ID", 16, "world"),
    ("name", "名称", 14, "world"),
    ("maxHp", "HP", 8, "combat"),
    ("baseEnergy", "基础能量", 10, "combat"),
    ("maxItemLevel", "最高物品档", 12, "world"),
    ("items", "初始配装", 60, "world"),
]

SITE_COLS = [
    ("itemId", "密钥物品ID", 16, "world"),
    ("name", "地点名", 16, "world"),
    ("permanent", "永久", 6, "world"),
    ("width", "宽", 6, "world"),
    ("height", "高", 6, "world"),
    ("enemyCount", "敌人数", 8, "world"),
    ("lootCount", "战利品数", 9, "world"),
    ("hazardCount", "危险数", 8, "world"),
    ("stageCount", "阶段数", 8, "world"),
    ("seedOffset", "种子偏移", 9, "world"),
    ("tierMin", "最低档", 8, "world"),
    ("tierWeights", "各档份额", 18, "world"),
    ("lootPool", "掉落池", 60, "world"),
    ("enemyPool", "敌人池", 40, "world"),
]

QUEST_COLS = [
    ("id", "ID", 16, "world"),
    ("npcId", "发布者", 12, "world"),
    ("title", "标题", 18, "world"),
    ("objective", "目标类型", 16, "world"),
    ("targetId", "目标ID", 16, "world"),
    ("targetCount", "目标数量", 9, "world"),
    ("timeLimitDays", "时限天数", 9, "world"),
    ("rewardBit", "赏金", 8, "economy"),
    ("rewardItems", "奖励物品", 26, "economy"),
    ("rewardUplink", "奖励上行链路", 16, "economy"),
    ("requires", "前置任务", 14, "world"),
    ("desc", "描述", 44, "world"),
]

NPC_COLS = [
    ("id", "ID", 14, "world"),
    ("name", "名称", 16, "world"),
    ("uplinkItemId", "对应上行链路", 18, "world"),
    ("tier", "档位", 6, "world"),
    ("faction", "阵营", 20, "world"),
    ("greeting", "问候语", 40, "world"),
    ("stockPool", "货架池", 60, "economy"),
    ("stockSize", "每批件数", 9, "economy"),
    ("refreshDays", "刷新天数", 9, "economy"),
    ("questPool", "任务池", 40, "world"),
]

FACILITY_COLS = [
    ("facility", "设施", 18, "meta"),
    ("level", "等级", 6, "meta"),
    ("maxLevel", "等级上限", 9, "meta"),
    ("width", "宽(格)", 7, "world"),
    ("height", "高(格)", 7, "world"),
    ("upgradeCost", "升级费用", 9, "economy"),
    ("upgradeMats", "升级材料", 40, "economy"),
]

BUFF_COLS = [
    ("type", "状态", 16, "meta"),
    ("name", "名称", 12, "meta"),
    ("desc", "描述", 44, "meta"),
    ("isDebuff", "负面", 6, "meta"),
    ("decays", "会衰减", 8, "meta"),
    ("maxStacks", "最大层数", 9, "meta"),
    ("defaultTurns", "默认回合", 9, "meta"),
    ("stackMode", "叠加方式", 12, "meta"),
    ("tickPhase", "结算时机", 12, "meta"),
]


def main():
    items, world = load()
    wb = Workbook()
    wb.remove(wb.active)

    build_readme(wb, items, world)

    cats = ["Weapon", "Armor", "Tool", "Consumable", "Loot", "Material",
            "Key", "Currency", "Ammo", "Action", "Container", "Uplink", "Quest"]
    write_sheet(wb, "Items", ITEM_COLS, items["items"],
                note="形状用「/」分行、# 占格 . 空；标签多选用逗号；词条写法 id:rank，多条用 | 分隔",
                dv_specs=[("D", cats),
                          ("E", ["Active", "Passive"]),
                          ("F", ["None", "Self", "Enemy"]),
                          ("P", ["Melee", "Projectile", "Burn", "Toxin", "Asphyxiation"])])

    write_sheet(wb, "Traits", TRAIT_COLS, items["traits"],
                note="词条是物品唯一的特殊效果来源，rank 1-5；rankValues 必须是 5 个数，用 | 分隔")

    write_sheet(wb, "Enemies", ENEMY_COLS, world["enemies"],
                note="配装写法 物品id@x,y,旋转,数量，多件用 | 分隔；敌人背包 4×3")

    write_sheet(wb, "Sites", SITE_COLS, world["sites"],
                note="掉落池与敌人池按档拆开，不要把高阶敌人塞进低阶地点",
                dv_specs=[("K", ["1", "2", "3", "4", "5"])])

    write_sheet(wb, "Quests", QUEST_COLS, world["quests"],
                note="requires 为空表示无前置；rewardItemIds 用 | 分隔",
                dv_specs=[("D", ["KillEnemies", "KillTagged", "ClearDive", "CollectItem",
                                 "CollectMine", "DefeatCrisisBoss", "ClearCrisis", "PayDebt"])])

    write_sheet(wb, "NPCs", NPC_COLS, world["npcs"],
                note="stockPool 是货架抽取池；questPool 是能发布的任务")

    write_sheet(wb, "Facilities", FACILITY_COLS, world["facilities"],
                note="每级一行；upgradeCost / upgradeMats 指的是「升到下一级」的代价")

    write_sheet(wb, "Buffs", BUFF_COLS, world["buffs"], note="状态效果定义")

    build_enums(wb)
    build_cheatsheet(wb)

    for ws in wb.worksheets:
        ws.sheet_view.showGridLines = False

    out = os.path.join(HERE, "game_data.xlsx")
    wb.save(out)
    print("saved", out)


if __name__ == "__main__":
    main()

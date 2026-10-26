# -*- coding: utf-8 -*-
"""生成《BitRunner 内容设计审阅》工作簿。

数据来源：从 Unity 运行时导出的 data_raw_items.json / data_raw_world.json。
本脚本不解析 C# 源码，也不推断任何默认值——所有数字都是游戏里实际生效的值。

设计原则：
  * 不出现 id / 字段名等技术细节，只呈现「设计内容」
  * 每张表末尾留空列「✎ 审核意见」供人工批注
  * 空缺档位显式标红，不让它被误认为"没写"
"""
import io, json, os, collections
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter
from openpyxl.chart import LineChart, Reference

BASE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(BASE, 'BitRunner_内容设计审阅.xlsx')

C_TITLE = '1F2933'
C_HEAD = '37475A'

F_TITLE = Font(name='微软雅黑', size=14, bold=True, color='FFFFFF')
F_HEAD = Font(name='微软雅黑', size=10, bold=True, color='FFFFFF')
F_BODY = Font(name='微软雅黑', size=10, color='1F2933')
F_DIM = Font(name='微软雅黑', size=9, color='6B7684')
F_GAP = Font(name='微软雅黑', size=10, bold=True, color='B02A2A')
F_SEC = Font(name='微软雅黑', size=11, bold=True, color='1F2933')

THIN = Side(style='thin', color='CFD6DF')
BOX = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)

f_head = PatternFill('solid', fgColor=C_HEAD)
f_gap = PatternFill('solid', fgColor='F8D7D7')
f_note = PatternFill('solid', fgColor='FFF7E0')
f_edit = PatternFill('solid', fgColor='EAF7EA')
f_band = PatternFill('solid', fgColor='F6F8FA')

# ── 数据 ────────────────────────────────────────────────────────────
items_raw = json.load(io.open(os.path.join(BASE, 'data_raw_items.json'), encoding='utf-8-sig'))
world_raw = json.load(io.open(os.path.join(BASE, 'data_raw_world.json'), encoding='utf-8-sig'))

ITEMS = items_raw['items']
TRAITS = collections.OrderedDict((t['id'], t) for t in items_raw['traits'])
ENEMIES = world_raw['enemies']
SITES = world_raw['sites']
QUESTS = world_raw['quests']
NPCS = world_raw['npcs']
BUFFS = world_raw['buffs']
FACS = world_raw['facilities']

CAT_CN = {
    'Weapon': '武器', 'Armor': '护甲', 'Tool': '工具 / 芯片', 'Consumable': '消耗品',
    'Loot': '战利品', 'Material': '材料', 'Key': '潜网密钥', 'Currency': '货币',
    'Ammo': '弹药', 'Container': '容器', 'Uplink': '上行链路', 'Quest': '契约凭证',
}
DT_CN = {'Melee': '近战', 'Projectile': '投射', 'Burn': '灼烧', 'Toxin': '毒素', 'Asphyxiation': '窒息'}
CAT_ORDER = ['Weapon', 'Armor', 'Tool', 'Consumable', 'Loot', 'Material',
             'Key', 'Uplink', 'Container', 'Currency', 'Ammo', 'Quest']

FAC_CN = {
    'DivePod': '潜网舱', 'CraftingStation': '制作站', 'CpuCore': '中央处理器',
    'InsuranceHub': '保险中枢', 'StorageRack': '仓储', 'CommsHub': '通讯中枢',
}
FAC_ORDER = ['DivePod', 'CraftingStation', 'CpuCore', 'InsuranceHub', 'StorageRack', 'CommsHub']

BUFF_CN = {
    'Overload': '过载', 'ShortCircuit': '短路', 'Cache': '缓存', 'Overclock': '超频',
    'Coolant': '冷却', 'Corrosion': '腐蚀', 'Overheat': '过热', 'Nanorepair': '纳米修复', 'Evasion': '闪避',
}
OBJ_CN = {
    'ClearDive': '通关一次指定潜网', 'CollectItem': '收集指定物品', 'CollectMine': '采集指定的矿',
    'KillEnemies': '累计击杀敌人', 'ClearCrisis': '击败一次危机', 'DefeatCrisisBoss': '击败核心危机 Boss',
    'PayDebt': '向银行还款',
}
NPC_CN = dict((n['id'], n['name']) for n in NPCS)
SITE_CN = dict((s['itemId'], s['name']) for s in SITES)
NAME_OF = dict((q['id'], q['name']) for q in ITEMS)


def shape_size(s):
    rows = [r for r in s.replace('\n', '/').split('/') if r]
    if not rows:
        return 0, 0, 0
    return len(rows[0]), len(rows), sum(r.count('#') for r in rows)


def dur(x):
    """游戏里实际生效的耐久上限。0 = 不会损坏（如实呈现，不做任何回落）。"""
    return int(x.get('durabilityMax', 0))


def dur_text(x):
    d = dur(x)
    return '不损坏' if d <= 0 else str(d)


def trait_text(s):
    if not s.strip():
        return ''
    out = []
    for p in s.split('|'):
        p = p.strip()
        if not p:
            continue
        tid, _, rank = p.partition(':')
        t = TRAITS.get(tid)
        out.append('%s %s' % (t['name'] if t else tid, rank or '1'))
    return ' / '.join(out)


def cell(ws, r, c, v, font=F_BODY, fill=None, wrap=False, align='left'):
    x = ws.cell(row=r, column=c, value=v)
    x.font = font
    x.border = BOX
    if fill is not None:
        x.fill = fill
    x.alignment = Alignment(horizontal=align, vertical='center', wrap_text=wrap)
    return x


def sheet_title(ws, text, ncol, sub=''):
    ws.merge_cells(start_row=1, start_column=1, end_row=1, end_column=ncol)
    c = ws.cell(row=1, column=1, value=text)
    c.font = F_TITLE
    c.fill = PatternFill('solid', fgColor=C_TITLE)
    c.alignment = Alignment(horizontal='left', vertical='center', indent=1)
    ws.row_dimensions[1].height = 30
    if sub:
        ws.merge_cells(start_row=2, start_column=1, end_row=2, end_column=ncol)
        c = ws.cell(row=2, column=1, value=sub)
        c.font = F_DIM
        c.alignment = Alignment(horizontal='left', vertical='center', indent=1, wrap_text=True)
        ws.row_dimensions[2].height = 44
        return 3
    return 2


def headers(ws, row, cols, widths):
    for i, (name, w) in enumerate(zip(cols, widths), start=1):
        c = ws.cell(row=row, column=i, value=name)
        c.font = F_HEAD
        c.fill = f_head
        c.alignment = Alignment(horizontal='center', vertical='center', wrap_text=True)
        c.border = BOX
        ws.column_dimensions[get_column_letter(i)].width = w
    ws.row_dimensions[row].height = 30
    ws.freeze_panes = ws.cell(row=row + 1, column=1)


def detail_sheet(name, title, sub, cols, widths, rows, edit_col='✎ 审核意见', blank_rows=6):
    ws = wb.create_sheet(name)
    ws.sheet_view.showGridLines = False
    r0 = sheet_title(ws, title, len(cols), sub)
    headers(ws, r0, list(cols) + [edit_col], list(widths) + [30])
    r = r0 + 1
    for data in rows:
        for i, v in enumerate(data, start=1):
            fill = None
            font = F_BODY
            al = 'center' if isinstance(v, (int, float)) else 'left'
            if isinstance(v, tuple):
                v, tag = v
                if tag == 'gap':
                    fill, font = f_gap, F_GAP
                elif tag == 'dim':
                    font = F_DIM
                if isinstance(v, (int, float)):
                    al = 'center'
            cell(ws, r, i, v, font, fill, wrap=False, align=al)
        cell(ws, r, len(cols) + 1, None, F_BODY, f_edit, align='left')
        r += 1
    if blank_rows:
        cell(ws, r, 1, '↓ 以下空白行可直接填写新内容', F_DIM, f_edit)
        for i in range(2, len(cols) + 2):
            cell(ws, r, i, None, F_BODY, f_edit)
        r += 1
        for _ in range(blank_rows - 1):
            for i in range(1, len(cols) + 2):
                cell(ws, r, i, None, F_BODY, f_edit)
            r += 1
    ws.auto_filter.ref = '%s%d:%s%d' % ('A', r0, get_column_letter(len(cols) + 1), r - 1)
    return ws


def avg(a):
    return round(sum(a) / len(a), 1) if a else 0


by_cat = collections.defaultdict(list)
for x in ITEMS:
    by_cat[x['category']].append(x)

# ══════════════════════════════════════════════════════════════════
wb = Workbook()

# ── 00 导读 ────────────────────────────────────────────────────────
ws = wb.active
ws.title = '00 导读'
ws.sheet_view.showGridLines = False
r = sheet_title(ws, 'BitRunner · 内容设计审阅', 4,
                '本表只呈现「设计内容」，不含任何内部 ID、字段名或代码路径。所有数字都是从游戏运行时导出的实际生效值，'
                '不是从代码推断的。每张明细表最后一列留空的「✎ 审核意见」是给你批注的，我按批注落地。')
ws.column_dimensions['A'].width = 18
ws.column_dimensions['B'].width = 26
ws.column_dimensions['C'].width = 62
ws.column_dimensions['D'].width = 40

for i, h in enumerate(['表', '内容', '这份表要看什么', '建议先看'], start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1

guide = [
    ('01 内容地图', '12 类 × 5 档的件数矩阵', '一眼看出哪档是空的、哪类堆得太多。红底＝该档没有内容。', '★ 最先看'),
    ('02 标签', '18 个标签，只有 4 个真的影响玩法', '哪些标签会真的改变玩法、哪些只是描述里的分类文字；顺带暴露「标签等级」机制完全空转。', '★ 新增'),
    ('03 武器', '20 件 + 伤害类型覆盖表', '伤害/能量/耐久/词条的递进；末尾附「五种伤害类型 × 五档」覆盖表，能直接看出断供的系。', '★'),
    ('04 护甲', '14 件，按档排列', '五系（近战·投射·灼烧·毒素·窒息）的偏科分布；T1 只有 1 件。', '★'),
    ('05 工具·芯片', '35 件，按档排列', '芯片五档与辅助三系列的重复度；辅助件有耐久、芯片没有。', '★ 件数最多'),
    ('06 消耗·容器·杂项', '消耗 4 / 容器 5 / 战利品 5 / 材料 15', '消耗品只有 4 件且 T3 空缺，是最薄的一环。', '★'),
    ('07 密钥·上行链路', '6 + 4 件', '密钥体积随档递增是物理门控，不要随手改形状。', ''),
    ('08 词条', '22 条', '每条的状态、触发器、五级数值；以及它被几件装备用到。', '★'),
    ('09 敌人', '28 种，按 HP 排序', 'HP 曲线与「可携带最高物品档」的关系；配装件数随档反而变少。', ''),
    ('10 地点·契约·NPC', '6 + 10 + 4', '潜网的尺寸/敌人/掉落密度，契约链的解锁关系。', ''),
    ('11 设施·状态', '6 设施 × 等级 / 9 状态', '升级花费与格子扩张；状态是战斗深度所在。', ''),
    ('12 数值曲线', '按档计算的均值折线', '把上表的数字压缩成走势，看曲线在哪一档断了。', '★'),
    ('13 缺口与待补', '我发现的 16 个问题', '每条都写了现象、影响、可行方向，供你逐条裁决。', '★ 最后看'),
]
for name, content, why, pri in guide:
    cell(ws, r, 1, name, F_SEC)
    cell(ws, r, 2, content)
    cell(ws, r, 3, why, wrap=True)
    c = cell(ws, r, 4, pri)
    if pri.startswith('★'):
        c.fill = f_note
        c.font = Font(name='微软雅黑', size=10, bold=True, color='8A5A00')
    ws.row_dimensions[r].height = 32
    r += 1

r += 1
ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=4)
c = ws.cell(row=r, column=1, value='四条已确认的硬约束（改动前请先看）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
rules = [
    ('增加能量必定有副作用', '任何「基础能量 +N」的芯片都必须同时扣生命上限，这是校验器写死的规则（参考《杀戮尖塔》的能量遗物）。想要纯正收益的能量件，得先改规则。'),
    ('生命上限只由中央处理器的芯片提供', '护甲只提供减伤，不提供生命上限。想让角色更耐打，只能加芯片或加护甲档位。'),
    ('敌人变强靠生命值，不靠护甲', '28 种敌人全部是「高 HP + 无护甲」的形态，唯一允许高 HP 叠高甲的单位是核心危机 Boss。'),
    ('武器 / 护甲必须显式写耐久，0 只表示永不损坏', '没有「默认 8」这回事：新装备忘写耐久会直接启动报错，而不是悄悄变成 8。0 只留给天然武器（目前只有敌人的兽牙），往普通装备上写 0 等于给它免死金牌。'),
]
for k, v in rules:
    cell(ws, r, 1, k, Font(name='微软雅黑', size=10, bold=True, color='B02A2A'))
    ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=4)
    cell(ws, r, 2, v, wrap=True)
    ws.row_dimensions[r].height = 34
    r += 1

# ── 01 内容地图 ────────────────────────────────────────────────────
ws = wb.create_sheet('01 内容地图')
ws.sheet_view.showGridLines = False
r = sheet_title(ws, '内容地图 · 12 类 × 5 档', 8,
                '红底＝该档完全没有内容。这张表是判断「下一批内容该做什么」的起点。')
ws.column_dimensions['A'].width = 16
for col in 'BCDEF':
    ws.column_dimensions[col].width = 9
ws.column_dimensions['G'].width = 10
ws.column_dimensions['H'].width = 58

for i, h in enumerate(['类别', 'T1', 'T2', 'T3', 'T4', 'T5', '合计', '我的判断'], start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1

verdict = {
    'Weapon': '五档各 4 件，最规整的一类。伤害均值 4.0 / 5.5 / 9.0 / 21.8 / 27.2，前四档连续上升，不再是平台期。',
    'Armor': 'T1 只有 1 件，玩家开局几乎没得选。总数 14 件，比武器少 6 件。',
    'Tool': '件数最多但结构最重复：芯片 5 档 ×3 系 = 15 件，辅助 3 系 ×3 档 = 9 件，合计 24 件（69%）。',
    'Consumable': '只有 4 件且 T3 空缺。全游戏唯一一个「中段断档」的实战品类，四件还全是回血。',
    'Loot': 'T5 空缺。T4 的数据雕像价值 290，与 T4 武器同价，捡到它等于捡到一把好武器。',
    'Material': '五档齐全，但 T2→T3 只涨 20%（50→60），而同期成品价值涨了 140%。',
    'Key': '六把钥匙体积 1/1/2/4/6/9 格，T5 是 3×3。体积即门控，不是装饰。',
    'Uplink': 'T5 空缺。四档对应四个 NPC，第五个 NPC 还没设计。',
    'Container': '只有 T2（三件）和 T3（两件），T1 没有、T4/T5 没有。收纳能力到中期就封顶了。',
    'Currency': '只有一种货币，五档只有 T1。这是设计选择而非缺口，列出仅为完整性。',
    'Ammo': '只有弹药一种，五档只有 T1。所有投射武器共用同一种弹药。',
    'Quest': '十张契约凭证在数据上都记在 T1，这是实现细节；真实分级见「10 地点·契约·NPC」。',
}
for cat in CAT_ORDER:
    rows_c = by_cat[cat]
    cell(ws, r, 1, CAT_CN[cat], F_SEC)
    for t in range(1, 6):
        n = len([q for q in rows_c if q['level'] == t])
        if n == 0:
            cell(ws, r, 1 + t, '空', F_GAP, f_gap, align='center')
        else:
            cell(ws, r, 1 + t, n, align='center')
    cell(ws, r, 7, len(rows_c), Font(name='微软雅黑', size=10, bold=True), align='center')
    cell(ws, r, 8, verdict[cat], wrap=True)
    ws.row_dimensions[r].height = 32
    r += 1

r += 1
cell(ws, r, 1, '合计', F_SEC)
for t in range(1, 6):
    n = len([q for q in ITEMS if q['level'] == t and q['category'] != 'Quest'])
    cell(ws, r, 1 + t, n, Font(name='微软雅黑', size=10, bold=True), f_band, align='center')
cell(ws, r, 7, len([q for q in ITEMS if q['category'] != 'Quest']),
     Font(name='微软雅黑', size=10, bold=True), f_band, align='center')
cell(ws, r, 8, '（不含 10 张契约凭证）T3 件数最多是因为工具堆积；实战品类里 T3 反而最空。', F_DIM, f_band, wrap=True)

# ── 02 标签 ────────────────────────────────────────────────────────
TAG_CN = {
    'Blade': '利刃', 'Shield': '护盾', 'Armor': '护甲', 'Chip': '芯片', 'Tool': '工具',
    'Consumable': '消耗品', 'Data': '数据', 'Mechanical': '机械', 'Projectile': '弹道',
    'Energy': '能量', 'Toxin': '毒素', 'Cooling': '冷却', 'Salvage': '回收',
    'Drone': '无人机', 'Targeting': '瞄准', 'Container': '容器', 'CpuChip': '芯片槽',
    'Assist': '探索辅助',
}

TAG_ROLE = {
    'CpuChip': '玩法开关｜中央处理器只认它，没有就插不进 CPU',
    'Chip': '玩法开关｜敌人配装「科技槽」按它筛件',
    'Blade': '玩法开关｜敌人配装「主武器槽」按它筛件',
    'Shield': '玩法开关｜敌人配装「护盾 / 副手槽」按它筛件',
}
TAG_ROLE_PLAIN = '仅分类｜只出现在物品描述末尾，代码不读它'

TAG_NOTE = {
    'CpuChip': '15 件＝3 系（生命 / 底甲 / 能量）×5 档，五档齐全。全游戏唯一「升级＝副作用减轻」的一条线：'
               '能量芯片的代价从生命上限 -18 收到 -6。',
    'Energy': '18 件里只有 5 件是芯片，其余 13 件是带能量味道的材料与武器。当分类用偏模糊——'
              '导线 / 电容组 / 等离子电池 和 弧光撕裂者 / 超驱继电器 混在一起。',
    'Mechanical': '与「回收」「数据」大面积重叠：废料 / 导线 / 电路板 / 合金板 同时带机械与回收。'
                  '三个标签之间没有定义边界，玩家看不出区别。',
    'Tool': '混了三种东西：功能件（密码测序器 / 零场发生器 / 等离子熔炉）、容器（5 件箱子）、'
            '护甲（呼吸器 / 滤毒罐 / 隔离装具 / 虚空服）。护甲带「工具」是历史残留。',
    'Shield': '★ 敌人副手槽按它筛件。15 件＝14 件护甲 + 1 件工具（零场发生器）。',
    'Blade': '★ 敌人主武器槽按它筛件。12 件里 1 件是敌人专属（兽牙）；磁轨长矛是投射武器却也带它，'
             '会被主武器槽当近战选中。',
    'Chip': '★ 敌人科技槽按它筛件。与「芯片槽」是两回事：这 12 件里 6 件是武器、6 件是功能芯片，'
            '一件都插不进中央处理器；能插 CPU 的是「芯片槽」那 15 件。',
    'Assist': '9 件＝3 系 ×3 档，全部停在 T3。辅助是唯一「占背包 + 耗耐久」的工具，'
              '也是探索警戒值的唯一调节手段（光源抬高、消音器压低）。',
    'Salvage': '15 件材料里只有 7 件标了回收，石头与树枝这两件最基础的完全没标签。'
               '哪天「回收」真的影响拆解产出，这两件会被漏掉。',
    'Data': '数据目前没有任何玩法承接（没有「上交数据」这类目标）。密码测序器同时是工具 + 数据，'
            '是全游戏唯一有功能的数据件。',
    'Projectile': '7 件＝全部投射武器。档位 0 / 3 / 1 / 2 / 1，T1 与 T3 的断供在这张表上一眼可见'
                  '（伤害类型覆盖见 03 页末）。',
    'Armor': '同是护甲，只有 7 件带「护甲」标签，另外 7 件只有「护盾」。标法不统一，目前不影响玩法。',
    'Container': '容器收什么由「类别」白名单决定，与这个标签无关——5 件容器都标了它，但「收不收」不读它。',
    'Consumable': '4 件全是回血（8 / 12 / 24 / 40）。标签没问题，问题是品类太薄且 T3 空缺'
                  '（见 13 页第 2 条）。',
    'Toxin': '3 件全是武器，没有毒素材料、毒素护甲或解毒消耗品。想走毒素流，全游戏只有 3 个节点。',
    'Cooling': '2 件（冷却回路 / 冷却罐），都是 T3。冷却没有对应玩法——游戏里没有过热机制。',
    'Targeting': '2 件（靶向阵列 / 瞄准晶格），都是芯片。瞄准同样没有对应玩法——没有命中判定。',
}

_tagged = collections.defaultdict(list)
for _x in ITEMS:
    _ts = set(_t.strip() for _t in _x['tags'].split(',')
              if _t.strip() and _t.strip() != 'None')
    if not _ts:
        _tagged['(none)'].append(_x)
    for _t in _ts:
        _tagged[_t].append(_x)

ws = wb.create_sheet('02 标签')
ws.sheet_view.showGridLines = False
r = sheet_title(ws, '标签 · 18 个标签，只有 4 个真的影响玩法', 11,
                '标签目前只承担两件事：决定中央处理器能不能插这张芯片，以及决定敌人配装槽收不收这件物品'
                '（利刃 / 护盾 / 芯片 三个槽位按标签筛）。除此之外它只是描述末尾的一行分类文字，不产生任何数值。'
                '红底＝这一档没有任何带该标签的物品。')
TAG_COLS = ['标签', '作用', '件数', 'T1', 'T2', 'T3', 'T4', 'T5',
            '带这个标签的物品', '我的判断', '✎ 审核意见']
TAG_W = [12, 34, 7, 5, 5, 5, 5, 5, 46, 56, 28]
headers(ws, r, TAG_COLS, TAG_W)
r += 1

for _t in [k for k in sorted(_tagged, key=lambda z: (-len(_tagged[z]), z)) if k != '(none)']:
    _arr = _tagged[_t]
    _tiers = collections.Counter(_z['level'] for _z in _arr)
    _on = _t in TAG_ROLE
    cell(ws, r, 1, TAG_CN.get(_t, _t),
         Font(name='微软雅黑', size=10, bold=True, color='B02A2A' if _on else '1F2933'))
    cell(ws, r, 2, TAG_ROLE.get(_t, TAG_ROLE_PLAIN),
         F_BODY if _on else F_DIM, f_note if _on else None, wrap=True)
    cell(ws, r, 3, len(_arr), F_BODY, align='center')
    for _i, _lv in enumerate(range(1, 6), start=4):
        _n = _tiers.get(_lv, 0)
        cell(ws, r, _i, _n, F_GAP if _n == 0 else F_BODY,
             f_gap if _n == 0 else None, align='center')
    cell(ws, r, 9, '、'.join(_z['name'] for _z in _arr), wrap=True)
    cell(ws, r, 10, TAG_NOTE.get(_t, ''), wrap=True)
    cell(ws, r, 11, None, F_BODY, f_edit)
    ws.row_dimensions[r].height = 42
    r += 1

# 定义了但没人用
cell(ws, r, 1, '无人机', F_GAP)
cell(ws, r, 2, '预留｜一件物品都没用', F_GAP, f_gap, wrap=True)
cell(ws, r, 3, 0, F_GAP, f_gap, align='center')
for _i in range(4, 9):
    cell(ws, r, _i, 0, F_GAP, f_gap, align='center')
cell(ws, r, 9, '（没有任何物品）', F_GAP, f_gap)
cell(ws, r, 10, '标签表里定义了它，110 件物品一件都没用。要么是给未来留的钩子，要么是漏做。', F_GAP, f_gap, wrap=True)
cell(ws, r, 11, None, F_BODY, f_edit)
ws.row_dimensions[r].height = 42
r += 1

# 无标签
_arr = _tagged['(none)']
_tiers = collections.Counter(_z['level'] for _z in _arr)
cell(ws, r, 1, '（无标签）', F_DIM)
cell(ws, r, 2, '24 件物品没有任何标签', F_DIM, wrap=True)
cell(ws, r, 3, len(_arr), F_DIM, align='center')
for _i, _lv in enumerate(range(1, 6), start=4):
    cell(ws, r, _i, _tiers.get(_lv, 0), F_DIM, align='center')
cell(ws, r, 9, '、'.join(_z['name'] for _z in _arr), F_DIM, wrap=True)
cell(ws, r, 10, '5 把密钥 + 4 条上行链路 + 10 张契约凭证 + 石头 / 树枝 / 垃圾 / 比特。'
                '前三类靠「类别」就能表达、不需要标签；石头与树枝是基础材料漏标。', F_DIM, wrap=True)
cell(ws, r, 11, None, F_BODY, f_edit)
ws.row_dimensions[r].height = 58
r += 1

r += 1
ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=11)
c = ws.cell(row=r, column=1, value='两个机制层面的发现（不是笔误，是缺口）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1

TAG_FINDS = [
    ('标签等级（I–V）完全空转',
     '标签系统支持给单个标签设 I–V 等级，解析与取值也都实现了，但 110 件物品里 0 件设过等级，'
     '而且这个取值方法没有任何调用方——等级对玩家和代码都没有影响。它要么是早先协同判定删掉后留下的残留，'
     '要么就该一并删掉：留着最危险的地方是，你以后在表里填了等级，以为它会生效。'),
    ('同一品类标法不统一',
     '护甲 14 件：7 件是「护甲 + 护盾」两个标签，另 7 件只有「护盾」，其中 4 件'
     '（滤毒罐 / 呼吸器 / 隔离装具 / 虚空服）还带「工具」。武器里磁轨长矛同时带「弹道」和「利刃」，'
     '敌人主武器槽有可能把它当近战刀选走。目前玩法代码只把标签当筛选条件、不产生数值，'
     '所以这些不一致还没造成问题；但凡以后让标签产生效果，必须先统一标法。'),
]
for _k, _v in TAG_FINDS:
    cell(ws, r, 1, _k, Font(name='微软雅黑', size=10, bold=True, color='B02A2A'), wrap=True)
    ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=11)
    cell(ws, r, 2, _v, wrap=True)
    ws.row_dimensions[r].height = 48
    r += 1

# ── 03 武器 ────────────────────────────────────────────────────────
W_NOTE = {
    '废铁刃': 'T1 最高伤害 5，开局就带锋锐 1；耐久 8 也是 T1 上限。',
    '铁棍': '比废铁刃少 1 伤、同耐久，换的是重砸 1（周围每空格 +1 伤害，上限 8 格）。两件都是 2 格，取舍在「稳定」与「靠留空」。',
    '短刺': '1 格 / 3 伤 / 耐久 6，唯一塞得进单格缝的近战，也带锋锐 1。',
    '兽牙': '敌人专属：价值 0、耐久 0（不会损坏）。玩家拿不到，是「野兽的牙不该磨断」这条约定的正确写法。',
    '刀刃': 'T2 伤害峰值 7、耐久 10 也是 T2 最高——对废铁刃（5 伤 / 8 耐）是干净的换代。',
    '脉冲发射器': '投射起点 6 伤，带超频流 1，是 T2 唯一带超频流的武器。',
    '毒素针': '5 伤但耐久只有 4（全游戏最脆），带腐蚀涂层 1——用耐久换持续伤害。',
    '钉枪': '4 伤 + 冷却 1 + 锋锐 1，投射里最弱的一件，定位偏模糊。',
    '电击棍': 'T3 伤害峰值 18，2 能量 / 耐久 12 / 3 格竖条。T3 的强度基本由这一件撑起来。',
    '真空泵': '14 伤 + 短路脉冲 1 + 冷却流体 1，2 能量 / 冷却 2。全游戏唯一的窒息武器。',
    '腐蚀飞针': '只有 4 伤却带腐蚀涂层，耐久 5——纯持续伤害件，付的是腐蚀的钱。',
    '冲压活塞': '伤害 0，全部输出来自护盾活塞（伤害 += 护盾/2）。唯一护盾流武器，不配护盾就是废件。',
    '磁轨长矛': 'T4 峰值 30，而且是投射——投射系最强的点出现在 T4，然后 T5 就断供了。',
    '反应堆重锤': '26 伤 + 重砸 4，理论上限 26+32=58，靠主动留空换伤害，是最讲布局的一件。',
    '等离子切割器': '1 能量 17 伤，每能量 17，T4 效率王。',
    '破片枪': '14 伤 + 锋锐 3，投射里的低档位，和同档磁轨长矛差 16 伤。',
    '奇点锤': '44 伤是天花板，2 能量 / 4 格 / 28 耐久，三项都顶格。',
    '高分子匕首': '1 格 / 1 能量 / 25 伤，全游戏每格伤害最高。它让「背包空间」在 T5 变得不重要，需留意。',
    '弧光撕裂者': '22 伤 + 击穿 5 + 短路脉冲 4，灼烧系终点，唯一带双词条的 T5。',
    '虚空针': '18 伤 + 腐蚀涂层 5 + 信号干扰 3，伤害最低但控制最强；也是 T5 唯一的毒素武器。',
}
rows = []
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Weapon' and x['level'] == t],
               key=lambda z: -z['damage'])
    if not g:
        rows.append([('T%d' % t, 'gap'), ('（本档无武器）', 'gap')] + [('', 'gap')] * 11)
        continue
    for i, x in enumerate(g):
        w, h, f = shape_size(x['shape'])
        eff = round(x['damage'] / x['energyCost'], 1) if x['energyCost'] else 0
        rows.append([
            'T%d' % t, x['name'], x['damage'], DT_CN.get(x['damageType'], x['damageType']),
            x['energyCost'], eff, x['cooldown'] or '—', dur_text(x),
            '%d×%d（实占%d）' % (w, h, f), trait_text(x['traits']) or '—',
            x['value'], '同档第 %d/%d 伤' % (i + 1, len(g)), W_NOTE.get(x['name'], ''),
        ])
ws02 = detail_sheet(
    '03 武器', '武器 · 20 件',
    '「每能量伤害」＝伤害÷能量，用来横向比效率；「同档第 n/m 伤」看同一档内部是否拉得开。'
    '耐久写「不损坏」的是数据里明确声明 0 的装备——游戏里就是永久的，目前只有敌人专属的兽牙属于这一类。',
    ['档', '名称', '伤害', '伤害类型', '能量', '每能量伤害', '冷却', '耐久', '占格', '词条', '价值', '同档位次', '设计备注'],
    [6, 14, 7, 10, 7, 12, 7, 9, 14, 24, 8, 13, 58], rows)

# 伤害类型覆盖小表
r = ws02.max_row + 2
ws02.merge_cells(start_row=r, start_column=1, end_row=r, end_column=13)
c = ws02.cell(row=r, column=1, value='伤害类型 × 档位（这张小表比上面任何一列都更能说明问题）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
for i, h in enumerate(['伤害类型', 'T1', 'T2', 'T3', 'T4', 'T5', '合计', '我的判断'], start=1):
    c = ws02.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1
DT_VERDICT = {
    'Melee': '主力系，五档都有。但 T1 四件全是近战——开局没有任何远程可选。',
    'Projectile': 'T2 与 T4 各有 2 件，T3 与 T5 完全没有。投射系两端断供，而它最强的一件（磁轨长矛 30）恰好停在 T4。',
    'Burn': 'T1 / T2 空缺，从 T3 才出现。想走灼烧流的玩家前两档无从下手。',
    'Toxin': 'T4 空缺，跳跃式分布（T2 / T3 / T5）。',
    'Asphyxiation': '全游戏只有 1 件（真空泵，T3）。这个伤害类型目前等于没有形成体系。',
}
for dt in ['Melee', 'Projectile', 'Burn', 'Toxin', 'Asphyxiation']:
    cnts = [len([q for q in ITEMS if q['category'] == 'Weapon' and q['level'] == lv and q['damageType'] == dt])
            for lv in range(1, 6)]
    cell(ws02, r, 1, DT_CN[dt], F_SEC)
    for k in range(5):
        if cnts[k] == 0:
            cell(ws02, r, 2 + k, '空', F_GAP, f_gap, align='center')
        else:
            cell(ws02, r, 2 + k, cnts[k], align='center')
    cell(ws02, r, 7, sum(cnts), Font(name='微软雅黑', size=10, bold=True), align='center')
    cell(ws02, r, 8, DT_VERDICT[dt], wrap=True)
    ws02.merge_cells(start_row=r, start_column=8, end_row=r, end_column=13)
    ws02.row_dimensions[r].height = 32
    r += 1

# ── 04 护甲 ────────────────────────────────────────────────────────
A_NOTE = {
    '废铁背心': 'T1 唯一护甲，5/4 偏近战，带轻灵 1——背包护甲 ≤1 件才触发闪避，正好奖励「只穿这一件」。',
    '偏导器': '纯投射 10，带偏导 1（使用后获得闪避）。名字与数值一致。',
    '呼吸器': '0 能量、毒 6 / 窒 12，带轻灵 1。与滤毒罐构成「毒 / 窒」双防。',
    '废铁甲': 'T2 通用基石 8/5/2/1，带重甲 1（回合开始周围每件护甲给护盾）。',
    '滤毒罐': '毒 12 / 窒 8，是「滤毒」词条在天梯里的唯一载体（回合开始清腐蚀）。',
    '装甲板': 'T3 通用型 12/8，耐久 12，带重甲 1。',
    '重型装甲': '近战 16 是同档最高，但耐久 10 反而低于装甲板的 12，还要 2 能量。',
    '隔热服': '0 能量、纯灼烧 12，带冷却流体 2（每层使获得的护盾 +1）。',
    '反应装甲': '19/10，重甲 4，T4 的通用终点。',
    '烧蚀外壳': '8/13/10 偏投射与灼烧，重甲 2 + 冷却流体 2——唯一双词条护甲。',
    '隔离装具': '毒 13 / 窒 13，轻灵 4，且 0 能量。',
    '日冕护盾甲': '近战 24 是五系峰值，重甲 5，Boss 掉落。',
    '相位织层': '五系全 10，偏导 5 + 轻灵 5——唯一完全放弃减伤数值、走「不挨打」路线的 T5。',
    '虚空服': '毒 20 / 窒 20 的环境终点，但近 / 射只有 8，进核心潜网打 Boss 会很难受。',
}
rows = []
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Armor' and x['level'] == t],
               key=lambda z: -(z['armorMelee'] + z['armorProjectile']))
    if not g:
        rows.append([('T%d' % t, 'gap'), ('（本档无护甲）', 'gap')] + [('', 'gap')] * 12)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        schools = [x['armorMelee'], x['armorProjectile'], x['armorBurn'], x['armorToxin'], x['armorAsphyxiation']]
        rows.append([
            'T%d' % t, x['name'], schools[0], schools[1], schools[2], schools[3], schools[4], sum(schools),
            '%d×%d（实占%d）' % (w, h, f), trait_text(x['traits']) or '—', x['energyCost'],
            dur_text(x), x['value'], A_NOTE.get(x['name'], ''),
        ])
detail_sheet(
    '04 护甲', '护甲 · 14 件',
    '五系分开列是为了看「偏科」：只在某一系有数字的，是环境专防，不该当通用装备比价。表内按「近战＋投射」排序，'
    '因为这两系是玩家在普通战斗里真正挨的伤害；「五系合计」高不代表更强。14 件全部带词条，没有一件是纯数值白板。',
    ['档', '名称', '近战', '投射', '灼烧', '毒素', '窒息', '五系合计', '占格', '词条', '能量', '耐久', '价值', '设计备注'],
    [6, 14, 7, 7, 7, 7, 7, 10, 14, 26, 7, 9, 8, 54], rows)

# ── 05 工具·芯片 ───────────────────────────────────────────────────
ASSIST_CN = {'light': '照明', 'muffle': '消音', 'ward': '护符'}
T_NOTE = {
    '生命芯片': '五档 +15/+30/+50/+75/+110，是生命上限的唯一来源（护甲不提供 HP）。',
    '底甲芯片': '五档 +1/+2/+3/+4/+6，给五系护甲一个共同基底。',
    '能量芯片': '五档基础能量都是 +1，变的是代价：生命上限 -18/-15/-12/-9/-6。升级方向不是「更强的能量」而是「更轻的副作用」——最反直觉也最聪明的一条曲线。',
    '光源': '只有 I / II / III 三档。设计上是「用警戒换战利品」：战利品 +12pp，但每次行动警戒 +1。',
    '消音器': '同样只有 I / II / III。方向相反：战利品 -6pp、遇敌 -12pp，用收益换安全。',
    '护符': '同样只有 I / II / III。受伤概率 -12pp，纯防御取向，不参与收益取舍。',
    '信号干扰器': 'T2 唯一带信号干扰的工具，也是「干扰」路线起点，但后面没有 T3 / T4 的接力件。',
    '密码测序器': '价值 0、不进商店、只能从契约「带样本回来」拿到。它是危机的开关，不是普通道具。',
    '赃物模块': '价值 120 却没有任何词条——疑似占位件，目前买了就是纯亏。',
    '冷却回路': '过载清除 + 冷却流体，专治「过载流」副作用的解药件。',
    '超频芯片': '只带能量单元 1（能量上限 +1），是 T3 最弱的一件。',
    '超频核心': '能量单元 2 + 过载负载 1：加容量同时背过载。',
    '靶向阵列': '只带超频流 1，与 T4「瞄准晶格」同词条族。',
    '超驱继电器': '能量单元 3 + 过载负载 3，代价与收益同时放大。',
    '瞄准晶格': '超频流 4 + 短路脉冲 3，T4 的「打得更疼」件。',
    '等离子熔炉': '击穿 5 + 超频流 5，双最高档词条叠在一件 4 格工具上。',
    '零场发生器': '偏导 5 + 缓存场 5：完全走防御 / 保盾路线，与等离子熔炉形成 T5 两极。',
}
rows = []
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Tool' and x['level'] == t],
               key=lambda z: (0 if z['assist'] else 1, z['name']))
    if not g:
        rows.append([('T%d' % t, 'gap'), ('（本档无工具）', 'gap')] + [('', 'gap')] * 11)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        if x['assist']:
            p = (x['assist'].split('|') + ['0'] * 6)[:6]
            fam = ASSIST_CN.get(p[0], p[0])

            def sgn(v):
                v = int(v)
                return '—' if v == 0 else ('%+d' % v)

            lt, en, dm, al = sgn(p[2]), sgn(p[3]), sgn(p[4]), sgn(p[5])
        else:
            fam = '芯片' if 'CpuChip' in x['tags'] else '功能件'
            lt = en = dm = al = '—'
        rows.append([
            'T%d' % t, x['name'], fam, trait_text(x['traits']) or '—',
            '%d×%d（实占%d）' % (w, h, f), x['energyCost'], x['value'],
            (dur_text(x) if dur(x) > 0 else '—'),
            lt, en, dm, al, T_NOTE.get(x['name'], ''),
        ])
detail_sheet(
    '05 工具·芯片', '工具 / 芯片 · 35 件',
    '35 件里 24 件是三条重复的升级线（芯片 3 系 ×5 档 = 15，辅助 3 系 ×3 档 = 9），真正「独一份」的功能件只有 11 件。'
    '辅助件的四列是概率修正（百分点，pp）：正数＝更容易发生，负数＝更难。辅助件有耐久（I/II/III = 4/5/6）会磨坏，芯片类没有耐久。',
    ['档', '名称', '系列 / 类型', '词条', '占格', '能量', '价值', '耐久', '战利品 ±pp', '遇敌 ±pp', '受伤 ±pp', '警戒 +', '设计备注'],
    [6, 16, 14, 30, 14, 7, 8, 8, 12, 11, 11, 9, 48], rows)

# ── 06 消耗·容器·杂项 ─────────────────────────────────────────────
CON_NOTE = {
    '修复包': '治疗 8、词条纳米修复 1（每回合回 1 血，3 回合）。T1 的「边打边回」件。',
    '急救包': '治疗 12，2 能量，且没有词条——四件消耗品里唯一的「纯治疗」。',
    '纳米缝合器': '治疗 24 + 纳米修复 4。从 T2 直接跳到 T4，中间少一档。',
    '全修套装': '治疗 40 + 纳米修复 5，占 4 格。',
}
rows = []
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Consumable' and x['level'] == t], key=lambda z: -z['heal'])
    if not g:
        rows.append(['T%d' % t, ('（本档无消耗品）', 'gap'), ('空', 'gap')] + [('', 'gap')] * 8)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        rows.append(['T%d' % t, x['name'], '消耗品', '治疗 %d（用后消失）' % x['heal'],
                     trait_text(x['traits']) or '—', '%d×%d（实占%d）' % (w, h, f),
                     x['energyCost'], x['value'], x['maxStack'], CON_NOTE.get(x['name'], '')])

WHITELIST_CN = {'Consumable': '消耗品', 'Key': '密钥', 'Material': '材料',
                'Tool': '工具', 'Armor': '护甲', 'Weapon': '武器'}
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Container' and x['level'] == t], key=lambda z: z['name'])
    if not g:
        rows.append(['T%d' % t, ('（本档无容器）', 'gap'), ('空', 'gap')] + [('', 'gap')] * 8)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        p = x['container'].split('|')
        wl = ' / '.join(WHITELIST_CN.get(k.strip(), k.strip()) for k in p[1].split(','))
        rows.append(['T%d' % t, x['name'], '容器', '%s 格内容区，只收 %s' % (p[0].replace('x', '×'), wl),
                     '—', '%d×%d（实占%d）' % (w, h, f), x['energyCost'], x['value'], x['maxStack'],
                     '外部只占 %d 格，内部能装 %s 格——箱子是「用小空间换大空间」的唯一手段。' % (f, p[0].replace('x', '×'))])

for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Loot' and x['level'] == t], key=lambda z: z['value'])
    if not g:
        rows.append(['T%d' % t, ('（本档无战利品）', 'gap'), ('空', 'gap')] + [('', 'gap')] * 8)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        rows.append(['T%d' % t, x['name'], '战利品', '无功能，纯卖钱', '—',
                     '%d×%d（实占%d）' % (w, h, f), '—', x['value'], x['maxStack'],
                     '价值 290，与 T4 武器同价——捡到它等于捡到一把好武器。' if x['name'] == '数据雕像' else ''])

MAT_NOTE = {
    '废料': '出现频率最高的基础材料，也是制作与设施升级的通用消耗品。',
    '导线': 'T1 第二档材料，价 12。',
    '石头': '价值 2，几乎只用于极早期的配方。',
    '树枝': '价值 3，与石头同属「旷野捡的」，没和废料形成明显分工。',
    '电路板': 'T2 材料只有两种，都恰好 50。',
    '合金板': '同电路板，两者没有区别。',
    '伺服组件': 'T3 三种材料同价 60，彼此没有区别。',
    '电容组': '同上。',
    '冷却罐': '同上。',
    '超合金板': 'T4 三种同价 140。',
    '光纤束': '同上。',
    '等离子电池': '同上。',
    '奇点核心': 'T5 三种同价 320。',
    '单极导线': '同上。',
    '日冕残片': '名字暗示是 Boss 相关材料，但获取方式与其他两种相同。',
}
for t in range(1, 6):
    g = sorted([x for x in ITEMS if x['category'] == 'Material' and x['level'] == t], key=lambda z: -z['value'])
    if not g:
        rows.append(['T%d' % t, ('（本档无材料）', 'gap')] + [('', 'gap')] * 9)
        continue
    for x in g:
        w, h, f = shape_size(x['shape'])
        rows.append(['T%d' % t, x['name'], '材料', '制作 / 设施升级的消耗', '—',
                     '%d×%d（实占%d）' % (w, h, f), '—', x['value'], x['maxStack'], MAT_NOTE.get(x['name'], '')])

detail_sheet(
    '06 消耗·容器·杂项', '消耗品 / 容器 / 战利品 / 材料',
    '四类合成一张表，因为这四类是「非战斗内容」的全部。红底行代表该档没有内容。',
    ['档', '名称', '类别', '作用', '词条', '占格', '能量', '价值', '堆叠', '设计备注'],
    [6, 16, 10, 34, 24, 14, 7, 7, 7, 58], rows)

# ── 07 密钥·上行链路 ──────────────────────────────────────────────
rows = []
KEY_NOTE = {
    '基础潜网密钥': '1 格。Lv.1 潜网舱（1×2）刚好放得下。',
    '旷野密钥': '1 格，与基础潜网密钥同体积，但通向的是永久可进的旷野。',
    '军用潜网密钥': '2 格横向。Lv.1 潜网舱已经放不下它，逼玩家升到 Lv.2（2×2）。',
    '信号潜网密钥': '4 格 2×2。必须 Lv.2 以上。',
    '深空潜网密钥': '6 格 3×2。必须 Lv.3（3×3）——第二次强制升级。',
    '核心潜网密钥': '9 格 3×3。必须 Lv.3 满格，一个空格都不能浪费。',
    '旧频道上行链路': '1 格。对应回收商 · 老陈。',
    '军械上行链路': '2 格。对应军械贩 · K-7。',
    '情报上行链路': '4 格 2×2。对应掮客 · 白鸦。',
    '核心上行链路': '6 格 3×2。对应核心之声 · SEED。T5 没有第五条链路，因为还没有第五个 NPC。',
}
for x in sorted([q for q in ITEMS if q['category'] == 'Key'], key=lambda z: (z['level'], z['name'])):
    w, h, f = shape_size(x['shape'])
    rows.append(['T%d' % x['level'], x['name'], '潜网密钥', '%d×%d' % (w, h), '—', '否（任务道具）', KEY_NOTE.get(x['name'], '')])
for x in sorted([q for q in ITEMS if q['category'] == 'Uplink'], key=lambda z: (z['level'], z['name'])):
    w, h, f = shape_size(x['shape'])
    rows.append(['T%d' % x['level'], x['name'], '上行链路', '%d×%d' % (w, h), '—', '否（任务道具）', KEY_NOTE.get(x['name'], '')])
for x in sorted([q for q in ITEMS if q['category'] in ('Currency', 'Ammo')], key=lambda z: z['name']):
    w, h, f = shape_size(x['shape'])
    rows.append(['T%d' % x['level'], x['name'], '货币 / 弹药', '%d×%d' % (w, h), x['value'], '是',
                 '全游戏只有一种货币、一种弹药，两者都不占战斗资源。'])
detail_sheet(
    '07 密钥·上行链路', '潜网密钥 / 上行链路 / 货币 / 弹药',
    '密钥的「占格数」不是外观，而是物理门控：形状放不进潜网舱，就进不去那一档潜网。',
    ['档', '名称', '类别', '占格', '价值', '可否交易', '设计备注'],
    [6, 30, 14, 14, 8, 14, 74], rows)

# ── 08 词条 ────────────────────────────────────────────────────────
use_cnt = collections.Counter()
for x in ITEMS:
    for p in x['traits'].split('|'):
        if p.strip():
            use_cnt[p.split(':')[0]] += 1

TRIG_CN = {'Passive': '常驻', 'OnUse': '使用时', 'TurnStart': '回合开始', 'TurnEnd': '回合结束', 'OnHit': '命中时'}
COND_CN = {'None': '无条件', 'Surround8Armor': '周围 8 格内有护甲',
           'Surround8EmptyCells': '周围 8 格有空格', 'BagArmorAtMost': '背包护甲不超过 N 件'}
EFF_CN = {'AddMaxHp': '生命上限', 'AddArmorBase': '五系护甲基底', 'AddBaseEnergy': '基础能量',
          'AddEnergy': '能量上限', 'AddDamage': '伤害', 'AddShield': '护盾', 'GrantStatus': '施加状态',
          'ConsumeStatus': '清除状态', 'AddDamageFromShield': '按护盾折算伤害'}
rows = []
for tid, t in TRAITS.items():
    rows.append([
        t['name'], t['descFormat'].replace('{0}', 'N').replace('{1}', 'M'),
        TRIG_CN.get(t['trigger'], t['trigger']), COND_CN.get(t['condition'], t['condition']),
        EFF_CN.get(t['effect'], t['effect']),
        t['rankValues'].replace('|', ' / '),
        (t['conditionValues'].replace('|', ' / ') if t['conditionValues'] else '—'),
        (BUFF_CN.get(t['status'], t['status']) if t['status'] != 'None' else '—'),
        (t['statusTurns'] if t['status'] != 'None' else '—'),
        t['stackRule'].replace('Max', '取最高').replace('Intensity', '叠强度').replace('Charges', '叠次数').replace('Unique', '唯一'),
        use_cnt[tid],
        '没有被任何装备使用，等于死词条。' if use_cnt[tid] == 0 else '',
    ])
detail_sheet(
    '08 词条', '词条 · %d 条' % len(TRAITS),
    '词条是全游戏唯一的状态 / 能量 / 护盾转伤来源。装备本身不带逻辑，只带词条——所以「加新效果」= 加新词条，不是给定物品加字段。'
    '最后一列是它被几件装备用到：目前 %d 条全部被使用过，没有死词条。' % len(TRAITS),
    ['名称', '说明', '触发时机', '触发条件', '效果', '五级数值（I~V）', '条件值', '施加状态', '状态持续回合', '叠加规则', '被几件装备使用', '备注'],
    [16, 40, 12, 24, 16, 26, 16, 12, 12, 12, 14, 28], rows)

# ── 09 敌人 ────────────────────────────────────────────────────────
E_NOTE = {
    '松鼠': 'HP 5，全游戏最低。旷野专属的练手怪。',
    '野犬': 'HP 8，旷野第二只。',
    '拾荒者': 'HP 20 / 能量 3 / 4 件配装，是基础潜网的标准样本。',
    '磁轨狙击手': '配装 3 件、可携带 T4——唯一在深空档就能拿 T4 装备的敌人。',
    '等离子守卫': 'HP 72 但配装只有 2 件：用「装备少而精」表达强度。',
    '隔离执行官': 'HP 78，配装 2 件。',
    '奇点卫兵': 'HP 110，配装 2 件，与等离子守卫同一手法。',
    '高分子刺客': '基础能量 4，与日冕机甲并列最高——唯一靠「能量多」而不是「血厚」变强的敌人。',
    '日冕战斗机甲': 'HP 140 是当前最高，也是唯一带 Boss 级数值却仍在普通敌人池里的单位。',
}
rows = []
ordered = sorted(ENEMIES, key=lambda e: e['maxHp'])
for i, e in enumerate(ordered):
    kit = [p for p in e['items'].split('|') if p.strip()]
    names = [NAME_OF.get(p.split('@')[0], p.split('@')[0]) for p in kit]
    rows.append([
        e['name'], e['maxHp'], e['baseEnergy'],
        'T1~T%d' % e['maxItemLevel'] if e['maxItemLevel'] > 1 else 'T1',
        len(kit), ' / '.join(names),
        '生命值排序第 %d/%d' % (i + 1, len(ordered)),
        E_NOTE.get(e['name'], ''),
    ])
detail_sheet(
    '09 敌人', '敌人 · %d 种（按生命值排序）' % len(ENEMIES),
    '这一档排序就是难度排序：HP 从 5 到 140。「可携带最高物品档」是敌人的装备上限门控——它决定这场战斗里敌人能不能掏出高阶武器。',
    ['名称', '生命值', '基础能量', '可携带最高物品档', '配装件数', '配装构成', '生命值位次', '设计备注'],
    [16, 8, 10, 18, 10, 48, 14, 52], rows)

# ── 10 地点·契约·NPC ─────────────────────────────────────────────
ws = wb.create_sheet('10 地点·契约·NPC')
ws.sheet_view.showGridLines = False
r = sheet_title(ws, '潜网地点 / 契约 / NPC', 10,
                '地点决定「一次探索能捡到什么」，契约决定「为什么要去」，NPC 决定「去哪买」。三者是同一个循环的三段。')
for i, (h, w) in enumerate(zip(
        ['地点', '地图尺寸', '敌人', '掉落', '危险', '层数', '起始档', '档位权重', '掉落池件数', '我的判断'],
        [16, 11, 7, 7, 7, 7, 9, 16, 12, 62]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
    ws.column_dimensions[get_column_letter(i)].width = w
r += 1
S_NOTE = {
    '旷野': '唯一的永久地点，也是唯一「不用钥匙」的入口。尺寸最小（5×5）、危险最少，新手教学场。',
    '基础潜网': '第一个要用钥匙的潜网。从这里开始掉落池从 7 件扩到 33 件，质变点在「掉落池」而不是尺寸。',
    '军用潜网': '起始档 T2，敌人最多（7 只）。掉落池已在全量边缘。',
    '信号潜网': '起始档 T3，敌人反而只有 6 只——这一档的难点是危险（8）而不是数量。',
    '深空潜网': '最大尺寸（8×8）、掉落最多（15 件）。起始档 T4。',
    '核心潜网': '起始档 T5 且权重 100%，即「只会出 T5」。敌人 11 只、危险 12，终局场。',
}
for s in SITES:
    cell(ws, r, 1, s['name'], F_SEC)
    cell(ws, r, 2, '%d × %d' % (s['width'], s['height']), align='center')
    cell(ws, r, 3, s['enemyCount'], align='center')
    cell(ws, r, 4, s['lootCount'], align='center')
    cell(ws, r, 5, s['hazardCount'], align='center')
    cell(ws, r, 6, s['stageCount'], align='center')
    cell(ws, r, 7, 'T%d' % s['tierMin'], align='center')
    cell(ws, r, 8, s['tierWeights'].replace('|', ' / '), align='center')
    cell(ws, r, 9, len([p for p in s['lootPool'].split('|') if p]), align='center')
    cell(ws, r, 10, S_NOTE.get(s['name'], ''), wrap=True)
    ws.row_dimensions[r].height = 32
    r += 1

r += 1
ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=10)
c = ws.cell(row=r, column=1, value='契约链（前置条件决定解锁顺序）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
for i, (h, w) in enumerate(zip(
        ['契约', '发布者', '要做的事', '目标', '次/个数', '限时', '报酬', '额外奖励', '解锁的链路', '前置契约'],
        [16, 18, 30, 14, 8, 8, 10, 30, 14, 14]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1
for q in QUESTS:
    tgt = SITE_CN.get(q['targetId'], '')
    if not tgt and q['targetId']:
        tgt = NAME_OF.get(q['targetId'], q['targetId'])
    pre = ' / '.join(next((x['title'] for x in QUESTS if x['id'] == p), p)
                     for p in str(q['requires']).split('|') if p)
    ups = ' / '.join(NAME_OF.get(p, p) for p in str(q['rewardUplink']).split('|') if p)
    its = ' / '.join(NAME_OF.get(p, p) for p in str(q['rewardItems']).split('|') if p)
    cell(ws, r, 1, q['title'], F_SEC)
    cell(ws, r, 2, NPC_CN.get(q['npcId'], q['npcId']))
    cell(ws, r, 3, OBJ_CN.get(q['objective'], q['objective']))
    cell(ws, r, 4, tgt or '—')
    cell(ws, r, 5, q['targetCount'], align='center')
    cell(ws, r, 6, ('%d 天' % q['timeLimitDays']) if q['timeLimitDays'] else '不限', align='center')
    cell(ws, r, 7, '%d 比特' % q['rewardBit'], align='center')
    cell(ws, r, 8, its or '—')
    cell(ws, r, 9, ups or '—')
    cell(ws, r, 10, pre or '—')
    r += 1

r += 1
ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=10)
c = ws.cell(row=r, column=1, value='NPC（每个 NPC 对应一条上行链路，库存与契约都挂在链路上）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
for i, (h, w) in enumerate(zip(
        ['NPC', '角色定位', '档', '每日库存', '刷新周期', '招牌台词', '卖什么', '给几条契约'],
        [16, 26, 6, 10, 10, 46, 40, 12]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1
for n in NPCS:
    stock = ' / '.join(NAME_OF.get(p, p) for p in n['stockPool'].split('|') if p)
    cell(ws, r, 1, n['name'], F_SEC)
    cell(ws, r, 2, n['faction'], wrap=True)
    cell(ws, r, 3, 'T%d' % n['tier'], align='center')
    cell(ws, r, 4, '%d 件' % n['stockSize'], align='center')
    cell(ws, r, 5, '%d 天' % n['refreshDays'], align='center')
    cell(ws, r, 6, n['greeting'], wrap=True)
    cell(ws, r, 7, stock, wrap=True)
    cell(ws, r, 8, len([p for p in n['questPool'].split('|') if p]), align='center')
    ws.row_dimensions[r].height = 36
    r += 1

# ── 11 设施·状态 ─────────────────────────────────────────────────
ws = wb.create_sheet('11 设施·状态')
ws.sheet_view.showGridLines = False
r = sheet_title(ws, '基地设施 / 战斗状态', 8,
                '设施的「格子扩张」是玩家升级后能立刻看见的回报；状态是战斗深度的全部来源。'
                '花费列是「从本级升到下一级」的价钱，满级记 0。')
for i, (h, w) in enumerate(zip(['设施', '等级', '占地', '格数', '较上一级', '升到下一级花费', '材料消耗', '这一级给了什么'],
                              [16, 8, 10, 8, 10, 16, 26, 52]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center', wrap_text=True)
    c.border = BOX
    ws.column_dimensions[get_column_letter(i)].width = w
ws.row_dimensions[r].height = 30
r += 1

FAC_GAIN = {
    ('DivePod', 1): '只能进「最低档 ≤ T1」的地点：旷野、基础潜网。',
    ('DivePod', 2): '可进军用潜网（最低档 T2）。',
    ('DivePod', 3): '可进信号潜网（T3）。',
    ('DivePod', 4): '可进深空潜网（T4）。',
    ('DivePod', 5): '可进核心潜网（T5），终局形态。',
    ('CraftingStation', 1): '4 条配方：废铁刃、修复包、脉冲发射器、偏导器。',
    ('CraftingStation', 2): '3 条配方：装甲板、信号潜网密钥、密钥盒。',
    ('CraftingStation', 3): '没有任何专属配方——这一级只是格子从 3×2 变 3×3。',
    ('CraftingStation', 4): '一次性解锁 5 条：等离子切割器、磁轨长矛、反应装甲、纳米缝合器、深空潜网密钥。',
    ('CraftingStation', 5): '4 条 T5 配方：高分子匕首、日冕护盾甲、全修套装、核心潜网密钥。',
    ('CpuCore', 1): '能插的芯片档位 ≤ T1。2×2 = 4 格。',
    ('CpuCore', 2): '芯片档位 ≤ T2。3×3 = 9 格。',
    ('CpuCore', 3): '芯片档位 ≤ T3。3×4 = 12 格。',
    ('CpuCore', 4): '芯片档位 ≤ T4。4×4 = 16 格，放得下 2×2 的大芯片。',
    ('CpuCore', 5): '芯片档位 ≤ T5。5×4 = 20 格。',
    ('InsuranceHub', 1): '保险箱 2×2 = 4 格。保险箱是唯一「阵亡也不丢」的空间，只收 1×1。',
    ('InsuranceHub', 2): '3×2 = 6 格（+2）。',
    ('InsuranceHub', 3): '3×3 = 9 格（+3）。',
    ('StorageRack', 1): '10×6 = 60 格仓库。',
    ('StorageRack', 2): '10×8 = 80 格（+20）。',
    ('StorageRack', 3): '10×10 = 100 格（+20）。只加高，不加宽。',
    ('CommsHub', 1): '2×2 = 4 格，即最多同时挂 4 条 1 格链路（＝并行任务上限）。',
    ('CommsHub', 2): '3×2 = 6 格。',
    ('CommsHub', 3): '3×3 = 9 格。',
    ('CommsHub', 4): '4×3 = 12 格。',
    ('CommsHub', 5): '4×4 = 16 格。',
}
by_fac = collections.defaultdict(list)
for f in FACS:
    by_fac[f['facility']].append(f)
for fac in FAC_ORDER:
    lv_rows = sorted(by_fac[fac], key=lambda z: z['level'])
    prev = None
    for x in lv_rows:
        lv = x['level']
        wd, hh = x['width'], x['height']
        cur = wd * hh
        delta = '—' if prev is None else ('+%d 格' % (cur - prev))
        prev = cur
        mats = ' / '.join(NAME_OF.get(p, p) for p in x['upgradeMats'].split('|') if p)
        cell(ws, r, 1, FAC_CN.get(fac, fac), F_SEC if lv == 1 else F_BODY)
        cell(ws, r, 2, 'Lv.%d' % lv, align='center')
        cell(ws, r, 3, '%d × %d' % (wd, hh), align='center')
        cell(ws, r, 4, '%d 格' % cur, align='center')
        cell(ws, r, 5, delta, align='center')
        cell(ws, r, 6, ('%d 比特' % x['upgradeCost']) if x['upgradeCost'] else '满级', align='center')
        cell(ws, r, 7, mats or '—')
        cell(ws, r, 8, FAC_GAIN.get((fac, lv), ''), wrap=True)
        r += 1
    r += 1

ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=8)
c = ws.cell(row=r, column=1, value='战斗状态（9 种，全战斗深度都在这里）')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
for i, (h, w) in enumerate(zip(['状态', '效果', '增益 / 减益', '层数上限', '默认持续', '每回合衰减', '叠加方式', '结算时机'],
                              [14, 44, 12, 10, 10, 12, 12, 12]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
r += 1
for b in BUFFS:
    debuff = b['isDebuff'] == 1
    cell(ws, r, 1, b['name'], F_SEC)
    cell(ws, r, 2, b['desc'], wrap=True)
    c = cell(ws, r, 3, '减益' if debuff else '增益', align='center')
    if debuff:
        c.font = Font(name='微软雅黑', size=10, bold=True, color='B02A2A')
    cell(ws, r, 4, b['maxStacks'], align='center')
    cell(ws, r, 5, '%d 回合' % b['defaultTurns'], align='center')
    cell(ws, r, 6, b['decays'] if b['decays'] else '不衰减', align='center')
    cell(ws, r, 7, b['stackMode'].replace('Intensity', '叠强度').replace('Charges', '叠次数').replace('Unique', '唯一'), align='center')
    cell(ws, r, 8, b['tickPhase'].replace('TurnStart', '回合开始').replace('TurnEnd', '回合结束'), align='center')
    ws.row_dimensions[r].height = 26
    r += 1

# ── 12 数值曲线 ───────────────────────────────────────────────────
ws = wb.create_sheet('12 数值曲线')
ws.sheet_view.showGridLines = False
r = sheet_title(ws, '数值曲线 · 同一把尺子量五档', 10,
                '把明细表压缩成均值，看曲线在哪一档断了。断点就是下一批内容该补的位置。'
                '「武器件数」不统计敌人专属的兽牙（价值 0、玩家拿不到），以免拉低均值。')
for i, (h, w) in enumerate(zip(['档位', '武器件数（不含敌人专属）', '武器平均伤害', '武器最高伤害', '武器平均每能量伤害', '武器平均耐久',
                               '护甲件数', '护甲单系均值', '材料均价', '全部物品均价'],
                              [8, 16, 14, 14, 18, 14, 10, 14, 12, 14]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center', wrap_text=True)
    c.border = BOX
    ws.column_dimensions[get_column_letter(i)].width = w
ws.row_dimensions[r].height = 34
head_row = r
r += 1

for t in range(1, 6):
    wp = [x for x in ITEMS if x['category'] == 'Weapon' and x['level'] == t and x['value'] > 0]
    ar = [x for x in ITEMS if x['category'] == 'Armor' and x['level'] == t]
    mt = [x for x in ITEMS if x['category'] == 'Material' and x['level'] == t]
    al = [x for x in ITEMS if x['level'] == t and x['category'] not in ('Quest', 'Key', 'Uplink', 'Currency')]
    cell(ws, r, 1, 'T%d' % t, F_SEC, align='center')
    cell(ws, r, 2, len(wp), align='center')
    cell(ws, r, 3, avg([x['damage'] for x in wp]), align='center')
    cell(ws, r, 4, max([x['damage'] for x in wp]) if wp else 0, align='center')
    cell(ws, r, 5, avg([x['damage'] / x['energyCost'] for x in wp if x['energyCost']]), align='center')
    cell(ws, r, 6, avg([dur(x) for x in wp]), align='center')
    cell(ws, r, 7, len(ar), align='center')
    cell(ws, r, 8, avg([max(x['armorMelee'], x['armorProjectile'], x['armorBurn'], x['armorToxin'], x['armorAsphyxiation']) for x in ar]), align='center')
    cell(ws, r, 9, avg([x['value'] for x in mt]), align='center')
    cell(ws, r, 10, avg([x['value'] for x in al]), align='center')
    r += 1
last_row = r - 1

ch = LineChart()
ch.title = '武器伤害与护甲：五档走势'
ch.style = 2
ch.y_axis.title = '数值'
ch.x_axis.title = '档位'
ch.height = 9
ch.width = 16
ch.add_data(Reference(ws, min_col=3, max_col=4, min_row=head_row, max_row=last_row), titles_from_data=True)
ch.add_data(Reference(ws, min_col=8, min_row=head_row, max_row=last_row), titles_from_data=True)
ch.set_categories(Reference(ws, min_col=1, min_row=head_row + 1, max_row=last_row))
ws.add_chart(ch, 'A%d' % (last_row + 2))

ch2 = LineChart()
ch2.title = '价值：材料 / 全部物品'
ch2.style = 2
ch2.y_axis.title = '价值'
ch2.height = 9
ch2.width = 16
ch2.add_data(Reference(ws, min_col=9, max_col=10, min_row=head_row, max_row=last_row), titles_from_data=True)
ch2.set_categories(Reference(ws, min_col=1, min_row=head_row + 1, max_row=last_row))
ws.add_chart(ch2, 'A%d' % (last_row + 21))

r = last_row + 44
ws.merge_cells(start_row=r, start_column=1, end_row=r, end_column=10)
c = ws.cell(row=r, column=1, value='读图结论')
c.font = Font(name='微软雅黑', size=12, bold=True, color='1F2933')
r += 1
notes = [
    ('武器伤害', '4.0 → 5.5 → 9.0 → 21.8 → 27.2。前四档已经连续上升，不再是平台期；但 T3→T4 仍是最大一跳（2.4 倍）。'
                 '最高伤害更陡：5 → 7 → 18 → 30 → 44，T3 靠电击棍一件把上限拉高。'),
    ('武器耐久', '7.3 → 7.0 → 8.8 → 21 → 28。T1→T3 都挤在 7~9 之间，到 T4 才翻倍。这条曲线的问题不是「平」，而是 T3→T4 的 2.4 倍断点。'),
    ('护甲数值', '5 → 10.5 → 13.3 → 15 → 18，平滑上升，没有断点。护甲是四条曲线里最健康的一条。'),
    ('材料价值', '6.2 → 50 → 60 → 140 → 320。T2→T3 只涨 20%，而同期成品价值档从 50 抬到 120（+140%）。两条曲线在 T3 分叉。'),
    ('全部物品均价', '20.9 → 59.5 → 117.6 → 263.5 → 628.8，每档约 ×2.4。总曲线是健康的，问题不在总价而在分类内部的结构。'),
]
for k, v in notes:
    cell(ws, r, 1, k, Font(name='微软雅黑', size=10, bold=True, color='B02A2A'))
    ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=10)
    cell(ws, r, 2, v, wrap=True)
    ws.row_dimensions[r].height = 42
    r += 1

# ── 13 缺口与待补 ─────────────────────────────────────────────────
ws = wb.create_sheet('13 缺口与待补')
ws.sheet_view.showGridLines = False
ISSUES = [
    ('护甲 T1 只有 1 件',
     'T1 护甲仅「废铁背心」一件；T2 有 4 件、T3/T4/T5 各 3 件。护甲总数 14 件，比武器还少 6 件。',
     '开局玩家没有防护选择，第一小时里「装备决策」几乎是空的。',
     '补 2~3 件 T1 护甲，分别偏投射 / 偏毒素，与废铁背心形成三选一。'),
    ('消耗品只有 4 件，T3 空缺',
     '治疗类共 4 件，档位分布 1 / 1 / 空 / 1 / 1，治疗量 8 / 12 / 24 / 40。',
     '中段玩家手上没有任何新消耗品可换，补给体验停在开局；四件还全是回血，没有清状态或临时增益。',
     '补 T3 一件；同时考虑「非治疗型消耗品」（比如清腐蚀 / 临时加能量），让这个品类不只有一根柱子。'),
    ('战利品 T5 空缺',
     '纯卖钱物品五档为 2 / 1 / 1 / 1 / 空。',
     '核心潜网（只会出 T5）里没有「纯财宝」这一档，捡到的东西必须是有用的，少了一种「今晚运气好」的爽点。',
     '补一件 T5 财宝，价值定在 700 以上；或明确决定「终局场不再需要纯财宝」。'),
    ('工具 / 芯片重复度 69%',
     '35 件工具中，芯片 3 系 ×5 档 = 15 件、辅助 3 系 ×3 档 = 9 件，共 24 件是三条直线升级。',
     '看起来内容很多，实际可做的选择很少：同一系永远是「拿更高档那件」。',
     '两条路：① 给每条升级线加「取向分叉」（比如生命芯片分「纯 HP」与「HP 换闪避」）；② 承认它们是基础设施，把精力放到功能件上。'),
    ('辅助三系列卡在 T3',
     '光源 / 消音器 / 护符只有 I~III（耐久 4/5/6），T4 / T5 没有。',
     '到了深空与核心潜网，玩家仍带着 T3 的辅助件，升级感断裂。',
     '要么补 IV / V，要么明确「辅助槽的上限就是 T3」，并让 T3 辅助件成为稀缺品。'),
    ('材料曲线落后成品曲线',
     '材料均价 6.2 / 50 / 60 / 140 / 320；成品价值档约 25 / 50 / 120 / 290 / 700。T2→T3 材料只涨 20%，成品涨 140%。',
     '中期制作会变贵而材料卖不出价，玩家更倾向直接买成品而不是自己造。',
     '把 T3 材料从 60 抬到 80~90，或者让 T3 配方需要的材料数量增加而不是价值增加。'),
    ('伤害类型覆盖有两处断供',
     '投射系只有 T2 与 T4 各有 2 件，T3 和 T5 完全没有；窒息系全游戏只有 1 件武器（真空泵，T3）；'
     '灼烧系 T1 / T2 空缺，毒素系 T4 空缺。',
     '想走投射或窒息流的玩家在特定档位完全没东西可换；投射系最强的一件（磁轨长矛 30）恰好停在 T4，之后没有下文。',
     '优先补一件 T3 投射与一件 T5 投射，让投射系成为贯通的线；窒息系要么补到 3 件以上，要么明确它是「单件特化」。'),
    ('容器只到 T3',
     '容器 5 件：T2 三件 + T3 两件，T1 没有、T4 / T5 没有。',
     '中期之后收纳不再成长，而 T4 / T5 装备占格普遍变大（3~4 格）。',
     '补 T4 容器，或者明确「容器的作用是分流不是扩容」，靠仓储升级解决。'),
    ('能量芯片的升级方向反直觉',
     '五档基础能量都是 +1，变化的是代价：生命上限 -18 → -6。',
     '这一条其实设计得很好（升级＝减少副作用），但玩家看不到「能量 +1」的成长感，容易误以为五档没区别。',
     '保留机制，但在描述里把「代价减轻」写成正面卖点；或到 T5 给一个 +2 的稀有例外。'),
    ('敌人配装件数随档变少',
     '低档敌人配装 4~5 件，高档敌人（等离子守卫 / 隔离执行官 / 奇点卫兵）只有 2 件。',
     '玩家看到的高档敌人「更穷」，打完战利品反而更少。',
     '把高档敌人的「少而精」改成「精而不减量」：件数保持 3~4，但每件都是该档最高。'),
    ('上行链路 T5 空缺',
     '链路 4 条对应 4 个 NPC，全部最高 T4。',
     '核心档没有对应的联系人，终局缺少「向谁交货」的落点。',
     '补第五个 NPC + T5 链路，或者明确「SEED 就是终局联系人」。'),
    ('契约全部记在 T1',
     '十张契约凭证在数据上都属于最低档。',
     '这是实现细节，不影响玩法，但会让「按档筛选」的表看不出契约的难度梯度。',
     '不用改数据，只需在表里按「前置链深度」重新排一次（本表 10 页已按此排序）。'),
    ('制作站 Lv.3 是空档，且全站只有 16 条配方',
     '配方按站等级分布：Lv.1 四条、Lv.2 三条、Lv.3 零条、Lv.4 五条、Lv.5 四条。能制作的东西只占 110 件物品的 15%。',
     '玩家花 600 比特 + 一堆材料升到 Lv.3，唯一变化是格子从 3×2 变 3×3，没有任何新东西能做——最容易让人失望的一次升级。',
     '给 Lv.3 补 2~3 条配方。T3 武器最合适，正好能顺带补上「伤害类型断供」里缺的那件投射。'),
    ('锋锐与重甲让「同类堆叠」成为唯一解',
     '锋锐被 7/20 件武器使用（含三件通用近战与两件投射枪械），重甲被 6/14 件护甲使用；'
     '两条词条的效果都是「周围每件同类 +强度」。',
     '武器带锋锐、护甲带重甲，最优解会收敛到「背包塞满武器 + 塞满护甲」，挤掉工具与消耗品的空间——'
     '而工具和消耗品恰好是内容最薄的两类。',
     '给锋锐 / 重甲设上限（比如每回合只吃 N 件），或让护甲、工具也提供非同类的构筑激励，避免单一堆叠策略。'),
    ('18 个标签里只有 4 个影响玩法，标签等级机制空转',
     '会被代码读取的标签只有 4 个：芯片槽（中央处理器插槽）、芯片 / 利刃 / 护盾（敌人三个配装槽位的筛选）。'
     '其余 14 个只出现在物品描述里。标签等级（I–V）机制完全空转：110 件物品 0 件设过等级，取值方法没有调用方。',
     '你在表里看到「能量 / 机械 / 回收 / 瞄准 / 冷却」会默认它们有机制含义，实际没有——'
     '给新内容打标签时容易低估成本、也容易以为已经做完了一条系统。',
     '二选一：① 把标签做成真机制（回收影响拆解产出、冷却降低过热、瞄准提升命中），每条单独算一个小系统；'
     '② 在文档里明确写「标签只用于分类与敌人配装筛选，不产生数值」，并删掉空转的等级机制。'
     '建议先走 ②，等某条确实要用了再单独升级。'),
    ('无人机标签定义了但零物品',
     '标签枚举里有「无人机」，110 件物品没有一件用它；敌人配装与掉落池里也没有。',
     '玩家侧与敌人侧都没有无人机内容。要么是给未来留的钩子，要么是内容遗漏。',
     '如果要留：T4 放一件无人机工具，正好补上「工具里真正的功能件 T4 只有 2 件」这个薄点'
     '（T4 的 5 件工具里有 3 件是 CPU 芯片）。如果不做，就从标签表里删掉，别留着让人误会。'),
]
r = sheet_title(ws, '我发现的 %d 个问题' % len(ISSUES), 6,
                '每条都按「现象 → 影响 → 可行方向」写。同意的在最后一列写「改」，不同意的写「不改 + 理由」，我据此动手。')
for i, (h, w) in enumerate(zip(['#', '问题', '现象（数据事实）', '影响', '可行方向', '裁决'],
                              [5, 22, 62, 44, 50, 14]), start=1):
    c = ws.cell(row=r, column=i, value=h)
    c.font = F_HEAD
    c.fill = f_head
    c.alignment = Alignment(horizontal='center', vertical='center')
    c.border = BOX
    ws.column_dimensions[get_column_letter(i)].width = w
r += 1
for i, (name, fact, impact, todo) in enumerate(ISSUES, start=1):
    cell(ws, r, 1, i, Font(name='微软雅黑', size=10, bold=True), align='center')
    cell(ws, r, 2, name, Font(name='微软雅黑', size=10, bold=True, color='B02A2A'), wrap=True)
    cell(ws, r, 3, fact, wrap=True)
    cell(ws, r, 4, impact, wrap=True)
    cell(ws, r, 5, todo, wrap=True)
    cell(ws, r, 6, None, F_BODY, f_edit, align='center')
    ws.row_dimensions[r].height = 58
    r += 1
ws.auto_filter.ref = 'A3:F%d' % (r - 1)

wb.save(OUT)
print('已生成:', OUT)
print('issues:', len(ISSUES), '| traits:', len(TRAITS))
print('sheets:', wb.sheetnames)

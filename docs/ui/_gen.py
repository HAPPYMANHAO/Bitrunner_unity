"""从 Unity 导出的 ui_tree.json 生成 UI 套版图与规格表。

用法：python _gen.py
产物：
  ui_layout_1280x720.png   游戏内实际大小（2 倍密度），可直接当描图纸
  ui_layout_annotated.png  放大 2 倍的标注版
  ui_panel_workbench.png   背包 / 仓库 / 工作台 分解图
  ui_panel_right.png       右侧标签页 分解图
  ui_panel_hud.png         左上信息区 分解图
  UI_SPEC.md               规格表
"""
import json
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TREE = os.path.join(HERE, "..", "..", "Assets", "Screenshots", "ui_tree.json")

FONT = "C:/Windows/Fonts/msyh.ttc"
FONT_B = "C:/Windows/Fonts/msyhbd.ttc"

BG = (18, 21, 26, 255)
GRID = (26, 31, 38, 255)
GRID_BOLD = (34, 40, 50, 255)
TOP = (214, 138, 62, 255)
SUB = (74, 150, 168, 255)
MUTED = (130, 140, 152, 255)

SKIP = {"Background", "EndOverlay", "Tooltip", "BaseDragGhost", "NpcBagWindow",
        # 上面这几个是临时/遮罩层；它们的子节点会以 d=2 混进来，一并排除
        "Box", "TitleBar", "Grid"}

# 顶层容器：画师当作一个整体来绘制的块
GROUPS = {"Hud", "LeftStats", "Workbench", "PackPanel", "StashPanel",
          "SecurePanel", "RightPanel", "Deploy", "ArtStage", "MessageBar"}

# 纯文字节点，套版里不画框（尺寸见规格表）
TEXT_ONLY = {"Title", "Subtitle", "Dialogue", "Message", "Stats",
             "Label", "Text", "BenchLabel"}


def load_all():
    """含文字节点，供面板分解图使用。"""
    with open(TREE, encoding="utf-8") as f:
        data = json.load(f)
    return [n for n in data
            if n["d"] in (1, 2)
            and n["n"] not in SKIP
            and n["w"] > 0 and n["h"] > 0]


def load():
    return [n for n in load_all() if n["n"] not in TEXT_ONLY]


def grid(dr, w, h, scale):
    for x in range(0, w, 32 * scale):
        c = GRID_BOLD if (x // (32 * scale)) % 5 == 0 else GRID
        dr.line([(x, 0), (x, h)], fill=c)
    for y in range(0, h, 32 * scale):
        c = GRID_BOLD if (y // (32 * scale)) % 5 == 0 else GRID
        dr.line([(0, y), (w, y)], fill=c)


def draw_overview(scale, annotated):
    w, h = 1280 * scale, 720 * scale
    im = Image.new("RGBA", (w, h), BG)
    dr = ImageDraw.Draw(im)
    grid(dr, w, h, scale)

    f_big = ImageFont.truetype(FONT_B, 22)
    f_mid = ImageFont.truetype(FONT, 17)
    f_small = ImageFont.truetype(FONT, 14)

    tabs = []
    for n in load():
        if n["n"].startswith("Tab_"):
            tabs.append(n)
            continue

        x, y = n["x"] * scale, n["y"] * scale
        ww, hh = n["w"] * scale, n["h"] * scale
        is_top = n["n"] in GROUPS
        color = TOP if is_top else SUB
        dr.rectangle([x, y, x + ww, y + hh], outline=color, width=2 if is_top else 1)

        # 无标注版只画框，当描图纸用
        if not annotated:
            continue

        label = ("%s  %d x %d" % (n["n"], n["w"], n["h"])) if is_top \
            else ("%s  %dx%d" % (n["n"], n["w"], n["h"]))
        font = (f_big if hh > 120 else f_mid) if is_top else f_small

        bbox = dr.textbbox((0, 0), label, font=font)
        tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
        if tw + 8 > ww or th + 6 > hh:
            ty = y - th - 6 if y - th - 6 >= 0 else y + 3
            dr.text((x + 3, ty), label, font=font, fill=color)
        else:
            dr.text((x + 4, y + 3), label, font=font, fill=color)

    # 8 个标签按钮挤在一起，合并标注
    if tabs:
        x0 = min(t["x"] for t in tabs) * scale
        x1 = max(t["x"] + t["w"] for t in tabs) * scale
        y0 = min(t["y"] for t in tabs) * scale
        y1 = max(t["y"] + t["h"] for t in tabs) * scale
        dr.rectangle([x0, y0, x1, y1], outline=SUB, width=1)
        if annotated:
            dr.text((x0, y0 - 19), "Tab_0~7  每个 40 x 36，水平间距 41", font=f_small, fill=SUB)

    if annotated:
        f_note = ImageFont.truetype(FONT, 14)
        dr.text((10, h - 26),
                "1280 x 720 px   |   1 grid = 32 px (= 1 格道具)   |   粗线每 5 格（160 px）",
                font=f_note, fill=MUTED)
    return im


def draw_panel(key, scale=3):
    """把一个顶层面板单独放大，标注它内部的子元素。"""
    nodes = load_all()
    target = next((n for n in nodes if n["n"] == key), None)
    if target is None:
        return None

    pad = 30
    w, h = target["w"] * scale + pad * 2, target["h"] * scale + pad * 2 + 30
    im = Image.new("RGBA", (w, h), BG)
    dr = ImageDraw.Draw(im)
    grid(dr, w, h, scale)

    ox, oy = pad - target["x"] * scale, pad - target["y"] * scale
    dr.rectangle([pad, pad, pad + target["w"] * scale, pad + target["h"] * scale],
                 outline=TOP, width=2)

    f_big = ImageFont.truetype(FONT_B, 20)
    f_small = ImageFont.truetype(FONT, 13)

    dr.text((pad, 8), "%s   %d x %d px   (放大 %dx)" % (key, target["w"], target["h"], scale),
            font=f_big, fill=TOP)

    for n in nodes:
        if n is target or n["d"] not in (1, 2):
            continue
        # 只画落在该面板范围内的元素
        if not (n["x"] >= target["x"] - 2 and n["y"] >= target["y"] - 2
                and n["x"] + n["w"] <= target["x"] + target["w"] + 2
                and n["y"] + n["h"] <= target["y"] + target["h"] + 2):
            continue

        x, y = n["x"] * scale + ox, n["y"] * scale + oy
        ww, hh = n["w"] * scale, n["h"] * scale
        is_text = n["n"] in TEXT_ONLY
        is_top = n["n"] in GROUPS
        if is_text:
            color = MUTED
        elif is_top:
            color = TOP
        else:
            color = SUB
        dr.rectangle([x, y, x + ww, y + hh], outline=color, width=2 if is_top else 1)

        label = "%s %dx%d" % (n["n"], n["w"], n["h"])
        if is_text:
            label += " 文本"
        bbox = dr.textbbox((0, 0), label, font=f_small)
        if bbox[2] - bbox[0] + 8 > ww:
            dr.text((x + 2, y - 15), label, font=f_small, fill=color)
        else:
            dr.text((x + 3, y + 2), label, font=f_small, fill=color)

    f_note = ImageFont.truetype(FONT, 13)
    dr.text((pad, h - 24), "原点 (0,0) 在画布左上角；子元素坐标见 UI_SPEC.md",
            font=f_note, fill=MUTED)
    return im


def spec_md():
    rows = ["# 基地界面 UI 规格（1280 × 720，2 倍像素密度）\n",
            "坐标原点在**左上角**，单位是像素。1 格道具 = 32 × 32 px，"
            "对应 Unity UI 的 16 逻辑单位。\n",
            "| 元素 | 层级 | X | Y | 宽 | 高 | 换算 |",
            "|---|---|---:|---:|---:|---:|---|"]
    for n in load():
        note = ""
        if n["n"] == "Bag" and n["w"] == 192:
            note = "6 格 × 5 格"
        elif n["n"] == "Bag" and n["w"] == 64:
            note = "2 格 × 2 格（保险箱）"
        elif n["n"] == "StashViewport":
            note = "10 格 × 6 格可视区"
        elif n["n"].startswith("Tab_"):
            note = "标签按钮，间距 41"
        lvl = "顶层" if n["n"] in GROUPS else "子元素"
        rows.append("| %s | %s | %d | %d | %d | %d | %s |"
                    % (n["n"], lvl, n["x"], n["y"], n["w"], n["h"], note))
    rows += ["", "## 关键换算\n",
             "- 1 逻辑单位（Unity UI）= 2 px",
             "- 1 格道具 = 16 逻辑单位 = 32 px",
             "- 背包网格 192 × 160 px = 6 格 × 5 格",
             "- 仓库可视区 320 × 192 px = 10 格 × 6 格",
             "- 保险箱 64 × 64 px = 2 格 × 2 格",
             "", "## 不画框的文字节点（尺寸供排版参考）\n",
             "| 元素 | X | Y | 宽 | 高 |", "|---|---:|---:|---:|---:|"]
    with open(TREE, encoding="utf-8") as f:
        data = json.load(f)
    for n in data:
        if n["d"] == 2 and n["n"] in TEXT_ONLY:
            rows.append("| %s | %d | %d | %d | %d |"
                        % (n["n"], n["x"], n["y"], n["w"], n["h"]))
    return "\n".join(rows)


def main():
    draw_overview(1, False).save(os.path.join(HERE, "ui_layout_1280x720.png"))
    draw_overview(2, True).save(os.path.join(HERE, "ui_layout_annotated.png"))

    for key, name in [("Workbench", "ui_panel_workbench"),
                      ("RightPanel", "ui_panel_right"),
                      ("Hud", "ui_panel_hud")]:
        im = draw_panel(key)
        if im:
            im.save(os.path.join(HERE, name + ".png"))

    with open(os.path.join(HERE, "UI_SPEC.md"), "w", encoding="utf-8") as f:
        f.write(spec_md())
    print("done")


if __name__ == "__main__":
    main()

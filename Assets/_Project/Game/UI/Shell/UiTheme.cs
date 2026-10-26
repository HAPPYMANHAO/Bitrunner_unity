using UnityEngine;

namespace BitRunner.Game
{
    /// <summary>
    /// 全项目唯一的配色与字号来源。
    ///
    /// 基准取自基地界面（原 BasePrototypeBootstrap 第 13–24 行）——基地是三种模式的骨架，
    /// 所以基地的值就是标准值。冲突项按"基地优先"统一，见 docs/PLAN_UI_SHELL.md §6。
    ///
    /// 约束：任何 UI 代码都不许再写 new Color32(...) 定义语义色，
    /// 只能从这里取。新增颜色先在这里加，再加调用点。
    /// </summary>
    public static class UiTheme
    {
        // ---- 基础三层 ----
        public static readonly Color BackgroundColor = new Color32(18, 21, 26, 255);
        public static readonly Color PanelColor = new Color32(27, 31, 38, 255);
        public static readonly Color WindowColor = new Color32(16, 20, 26, 255);
        public static readonly Color WindowScreenColor = new Color32(23, 29, 36, 255);

        // ---- 工作台（底带底色与铜色导轨）----
        public static readonly Color BenchColor = new Color32(22, 26, 32, 255);
        public static readonly Color BenchRailColor = new Color32(63, 54, 43, 255);

        // ---- 文字 ----
        public static readonly Color TextColor = new Color32(224, 230, 240, 255);
        public static readonly Color MutedColor = new Color32(138, 146, 162, 255);

        // ---- 语义色 ----
        // 警示用基地的橙：债务逾期、bit 不足这类"钱的问题"。
        public static readonly Color WarningColor = new Color32(208, 128, 80, 255);
        // 危险用探索的红：不可逆的损失提示。
        public static readonly Color DangerColor = new Color32(226, 107, 91, 255);
        public static readonly Color AccentColor = new Color32(58, 110, 165, 255);
        public static readonly Color DisabledColor = new Color32(44, 48, 56, 255);
        public static readonly Color FlashColor = new Color32(255, 236, 184, 255);

        // ---- 容器与选择 ----
        public static readonly Color SlotColor = new Color32(16, 19, 24, 255);
        public static readonly Color SlotHoverColor = new Color32(34, 40, 49, 255);
        public static readonly Color SelectedColor = new Color32(74, 60, 32, 255);
        public static readonly Color PanelSelectedColor = new Color32(42, 51, 64, 255);
        public static readonly Color SellColor = new Color32(58, 46, 34, 255);

        /// <summary>丢弃区恒用红色：它是唯一"删掉东西"的操作，配色必须和卖出（棕）分得开。</summary>
        public static readonly Color DiscardColor = new Color32(120, 46, 42, 255);

        /// <summary>光标停到丢弃区上方时的提醒色。</summary>
        public static readonly Color DiscardHighlightColor = new Color32(178, 66, 58, 255);
        public static readonly Color BagColor = new Color32(91, 160, 210, 255);
        public static readonly Color LootColor = new Color32(220, 200, 70, 255);
        public static readonly Color AlertColor = new Color32(224, 164, 79, 255);

        // ---- 日志 ----
        public static readonly Color LogColor = new Color32(123, 196, 127, 255);
        public static readonly Color SystemColor = new Color32(127, 199, 217, 255);

        // ---- 按钮态 ----
        public static readonly Color ConfirmColor = new Color32(58, 74, 70, 255);
        public static readonly Color ConfirmReadyColor = new Color32(94, 132, 103, 255);
        public static readonly Color RetreatColor = new Color32(122, 74, 44, 255);

        // ---- 生命 ----
        // 玩家生命条统一按比例变色，修掉"探索里红、战斗里绿"的冲突。
        public static readonly Color HpColor = new Color32(111, 191, 115, 255);
        // 敌人血条恒定红：靠"永不变色"与玩家条区分，这样"红"只在玩家真正危险时出现。
        public static readonly Color EnemyHpColor = new Color32(200, 96, 96, 255);

        // ---- 字号阶梯（只有这 5 级，别再出现第 6 级）----
        public const int FontTitle = 11;
        public const int FontPanelTitle = 10;
        public const int FontBody = 9;
        public const int FontSecondary = 8;
        public const int FontCaption = 7;

        /// <summary>画布基准分辨率，四份 CanvasScaler 实测一致。</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(640f, 360f);

        /// <summary>
        /// 玩家生命条颜色：>50% 绿，25–50% 琥珀，<25% 红。
        /// 统一入口，禁止各处自己 if 一遍。
        /// </summary>
        public static Color HpBar(float ratio)
        {
            if (ratio > 0.5f)
            {
                return HpColor;
            }

            if (ratio > 0.25f)
            {
                return WarningColor;
            }

            return DangerColor;
        }
    }
}

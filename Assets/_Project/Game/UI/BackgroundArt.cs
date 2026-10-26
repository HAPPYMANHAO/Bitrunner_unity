using UnityEngine;
using UnityEngine.UI;

namespace BitRunner.Game
{
    /// <summary>
    /// 全屏背景美术的唯一接入口。
    ///
    /// 美术规格：1280×720 px，一格道具 = 32×32 px，对应 UI 的 640×360 参考分辨率（2 倍密度）。
    /// 资源放在 Resources/Art/bt_background.png；即使美术未就位也能正常跑——加载不到就保留调用方给的纯色底，
    /// 所以不要在这里写任何「没有背景图就报错」的逻辑。
    /// </summary>
    public static class BackgroundArt
    {
        public const string ResourcePath = "Art/bt_background";

        /// <summary>
        /// 草稿美术开关。当前阶段背景图还是草稿，先关掉：各界面保持自己设置的纯色底。
        /// 正式图替换后把这里改回 true 即可，调用方不用动。
        /// </summary>
        public const bool UseDraftArt = false;

        private static bool _loaded;
        private static Sprite _sprite;

        public static Sprite Sprite
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _sprite = Resources.Load<Sprite>(ResourcePath);
                }

                return _sprite;
            }
        }

        public static bool HasArt
        {
            get { return Sprite != null; }
        }

        /// <summary>
        /// 把背景图铺到给定的全屏 Image 上。找不到美术时什么都不做，调用方原本的纯色底保留。
        /// 图是按 16:9 画的，非 16:9 目标会被拉伸——要像素完美就保证 Game View 是 16:9。
        /// </summary>
        public static void Apply(RectTransform background)
        {
            if (!UseDraftArt || background == null)
            {
                return;
            }

            Sprite sprite = Sprite;
            if (sprite == null)
            {
                return;
            }

            Image image = background.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }
    }
}

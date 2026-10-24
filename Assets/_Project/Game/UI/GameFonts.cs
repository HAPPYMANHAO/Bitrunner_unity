using UnityEngine;

namespace BitRunner.Game
{
    public static class GameFonts
    {
        private static Font _ui;

        public static Font Ui
        {
            get
            {
                if (_ui == null)
                {
                    _ui = Resources.Load<Font>("Fonts/Ui");
                }

                if (_ui == null)
                {
                    try
                    {
                        _ui = Font.CreateDynamicFontFromOSFont(new[]
                        {
                            "Microsoft YaHei",
                            "微软雅黑",
                            "SimHei",
                            "Noto Sans CJK SC",
                            "PingFang SC",
                            "Arial Unicode MS"
                        }, 24);
                    }
                    catch
                    {
                        _ui = null;
                    }
                }

                if (_ui == null)
                {
                    _ui = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _ui;
            }
        }
    }
}

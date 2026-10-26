using System;
using UnityEngine;

namespace BitRunner.Game
{
    /// <summary>
    /// 玩家设置（动画速度 / 全屏 / 总音量）。
    /// 所有读写都走这里，PlayerPrefs 只作为持久层；战斗界面通过 <see cref="Changed"/> 即时应用。
    /// </summary>
    public static class GameSettings
    {
        private const string AnimationSpeedKey = "bitrunner.settings.animationSpeed";
        private const string LegacyAnimationSpeedKey = "bitrunner.combat.animationSpeed";
        private const string FullscreenKey = "bitrunner.settings.fullscreen";
        private const string VolumeKey = "bitrunner.settings.volume";

        private static bool _loaded;
        private static int _animationSpeed = 1;
        private static float _volume = 1f;

        /// <summary>任意设置变化后触发。</summary>
        public static event Action Changed;

        /// <summary>战斗动画速度：1 / 2 / 4 倍。</summary>
        public static int AnimationSpeed
        {
            get
            {
                EnsureLoaded();
                return _animationSpeed;
            }
            set
            {
                EnsureLoaded();
                int clamped = value >= 4 ? 4 : (value >= 2 ? 2 : 1);
                if (clamped == _animationSpeed)
                {
                    return;
                }

                _animationSpeed = clamped;
                PlayerPrefs.SetInt(AnimationSpeedKey, clamped);
                PlayerPrefs.Save();
                RaiseChanged();
            }
        }

        public static bool Fullscreen
        {
            get
            {
                EnsureLoaded();
                return Screen.fullScreen;
            }
            set
            {
                EnsureLoaded();
                if (Screen.fullScreen == value)
                {
                    PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                    return;
                }

                Screen.fullScreen = value;
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                PlayerPrefs.Save();
                RaiseChanged();
            }
        }

        /// <summary>总音量 0–1，步进 0.1。</summary>
        public static float Volume
        {
            get
            {
                EnsureLoaded();
                return _volume;
            }
            set
            {
                EnsureLoaded();
                float clamped = Mathf.Clamp01(Mathf.Round(value * 10f) / 10f);
                if (Mathf.Abs(clamped - _volume) < 0.001f)
                {
                    return;
                }

                _volume = clamped;
                AudioListener.volume = clamped;
                PlayerPrefs.SetFloat(VolumeKey, clamped);
                PlayerPrefs.Save();
                RaiseChanged();
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;

            int savedSpeed = PlayerPrefs.GetInt(
                AnimationSpeedKey,
                PlayerPrefs.GetInt(LegacyAnimationSpeedKey, 1));
            _animationSpeed = savedSpeed >= 4 ? 4 : (savedSpeed >= 2 ? 2 : 1);

            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            AudioListener.volume = _volume;

            if (PlayerPrefs.HasKey(FullscreenKey))
            {
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            }
        }

        private static void RaiseChanged()
        {
            if (Changed != null)
            {
                Changed();
            }
        }
    }
}

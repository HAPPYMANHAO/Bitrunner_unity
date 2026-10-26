using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class StashScrollView : MonoBehaviour
    {
        public ScrollRect Scroll;
        public RectTransform Viewport;
        public RectTransform Track;
        public RectTransform Handle;
        public float WheelStep = 0.12f;

        private bool _dragging;
        private float _grabOffset;

        private void Start()
        {
            ResetToTop();
        }

        public void ResetToTop()
        {
            if (Scroll != null)
            {
                Scroll.verticalNormalizedPosition = 1f;
            }
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || Scroll == null || Viewport == null || Track == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            bool overViewport = RectTransformUtility.RectangleContainsScreenPoint(Viewport, screen, null);
            bool overTrack = RectTransformUtility.RectangleContainsScreenPoint(Track, screen, null);

            if (!_dragging && overViewport && !mouse.leftButton.isPressed)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    Scroll.verticalNormalizedPosition = Mathf.Clamp01(
                        Scroll.verticalNormalizedPosition + Mathf.Sign(wheel) * WheelStep);
                }
            }

            if (!_dragging && overTrack && mouse.leftButton.wasPressedThisFrame)
            {
                _dragging = true;
                float pointer = TrackValue(screen);
                if (Handle != null && RectTransformUtility.RectangleContainsScreenPoint(Handle, screen, null))
                {
                    _grabOffset = Scroll.verticalNormalizedPosition - pointer;
                }
                else
                {
                    _grabOffset = 0f;
                    SetScroll(pointer);
                }
            }

            if (_dragging && mouse.leftButton.isPressed)
            {
                SetScroll(TrackValue(screen) + _grabOffset);
            }

            if (_dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                _dragging = false;
            }
        }

        private float TrackValue(Vector2 screen)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Track, screen, null, out local))
            {
                return Scroll.verticalNormalizedPosition;
            }

            float height = Track.rect.height;
            if (height <= 0f)
            {
                return Scroll.verticalNormalizedPosition;
            }

            return Mathf.Clamp01((local.y + height * 0.5f) / height);
        }

        private void SetScroll(float value)
        {
            Scroll.verticalNormalizedPosition = Mathf.Clamp01(value);
        }
    }
}

// Cồn Xanh – works with both the new Input System (Unity 6 default) and the legacy Input Manager.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ConXanh
{
    public static class InputCompat
    {
        public struct T { public Vector2 pos, delta; public bool began; }

        /// <summary>Active touches (0, 1 or 2).</summary>
        public static int Touches(T[] buf)
        {
            int n = 0;
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null)
                foreach (var t in ts.touches)
                {
                    if (!t.isInProgress || n >= buf.Length) continue;
                    buf[n++] = new T { pos = t.position.ReadValue(), delta = t.delta.ReadValue(), began = t.press.wasPressedThisFrame };
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < Input.touchCount && n < buf.Length; i++)
            {
                var t = Input.GetTouch(i);
                buf[n++] = new T { pos = t.position, delta = t.deltaPosition, began = t.phase == TouchPhase.Began };
            }
#endif
            return n;
        }

        /// <summary>Primary pointer (mouse left button or first touch).</summary>
        public static void Pointer(out Vector2 pos, out bool down, out bool held, out bool up, out int count)
        {
            pos = default; down = held = up = false; count = 0;
#if ENABLE_INPUT_SYSTEM
            var p = UnityEngine.InputSystem.Pointer.current;
            if (p != null) { pos = p.position.ReadValue(); down = p.press.wasPressedThisFrame; held = p.press.isPressed; up = p.press.wasReleasedThisFrame; }
            var ts = Touchscreen.current;
            if (ts != null) { foreach (var t in ts.touches) if (t.isInProgress) count++; }
            if (count == 0 && held) count = 1;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pos = Input.mousePosition; down = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0); up = Input.GetMouseButtonUp(0);
            count = Mathf.Max(Input.touchCount, held ? 1 : 0);
#endif
        }

        public static void MouseButtons(out bool left, out bool right)
        {
            left = right = false;
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current; if (m != null) { left = m.leftButton.isPressed; right = m.rightButton.isPressed; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            left = Input.GetMouseButton(0); right = Input.GetMouseButton(1);
#endif
        }

        public static bool MouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current; return m != null && (m.leftButton.isPressed || m.rightButton.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(0) || Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        public static Vector2 MouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current; return m != null ? m.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 20f;
#else
            return Vector2.zero;
#endif
        }

        public static float Scroll()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current; return m != null ? Mathf.Sign(m.scroll.ReadValue().y) * (Mathf.Abs(m.scroll.ReadValue().y) > 0.01f ? 1 : 0) : 0;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0;
#endif
        }

        public static bool KeyT()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.tKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.T);
#else
            return false;
#endif
        }
    }
}

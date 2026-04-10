using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Gameplay.Input
{
    public sealed class KeyboardPlayerInputSource : IPlayerInputSource
    {
#if ENABLE_INPUT_SYSTEM
        private static readonly Dictionary<KeyCode, Key> KeyMap = new Dictionary<KeyCode, Key>
        {
            { KeyCode.W, Key.W },
            { KeyCode.A, Key.A },
            { KeyCode.S, Key.S },
            { KeyCode.D, Key.D },
            { KeyCode.I, Key.I },
            { KeyCode.J, Key.J },
            { KeyCode.K, Key.K },
            { KeyCode.L, Key.L },
            { KeyCode.F, Key.F },
            { KeyCode.U, Key.U },
            { KeyCode.O, Key.O },
            { KeyCode.P, Key.P },
            { KeyCode.Q, Key.Q },
            { KeyCode.E, Key.E },
            { KeyCode.Space, Key.Space },
            { KeyCode.Tab, Key.Tab },
            { KeyCode.Escape, Key.Escape },
            { KeyCode.Return, Key.Enter },
            { KeyCode.KeypadEnter, Key.Enter },
            { KeyCode.LeftShift, Key.LeftShift },
            { KeyCode.RightShift, Key.RightShift },
            { KeyCode.LeftControl, Key.LeftCtrl },
            { KeyCode.RightControl, Key.RightCtrl },
            { KeyCode.UpArrow, Key.UpArrow },
            { KeyCode.DownArrow, Key.DownArrow },
            { KeyCode.LeftArrow, Key.LeftArrow },
            { KeyCode.RightArrow, Key.RightArrow }
        };
#endif

        private readonly PlayerKeyboardBindings _bindings;

        public KeyboardPlayerInputSource(PlayerKeyboardBindings bindings)
        {
            _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public PlayerInputFrame ReadFrame()
        {
            Vector2 move = ReadMoveVector();
            Vector2 aim = move;

            bool sprayHeld = IsHeld(_bindings.Spray);
            bool sprayPressed = IsPressed(_bindings.Spray);
            bool dodgePressed = IsPressed(_bindings.Dodge);
            bool pausePressed = IsPressed(_bindings.Pause);

            return new PlayerInputFrame(move, aim, sprayHeld, sprayPressed, dodgePressed, pausePressed);
        }

        private Vector2 ReadMoveVector()
        {
#if ENABLE_INPUT_SYSTEM
            return ReadMoveVectorInputSystem();
#else
            return ReadMoveVectorLegacy();
#endif
        }

        private bool IsHeld(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            return IsHeldInputSystem(keyCode) || UnityEngine.Input.GetKey(keyCode);
#else
            return UnityEngine.Input.GetKey(keyCode);
#endif
        }

        private bool IsPressed(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            return IsPressedInputSystem(keyCode) || UnityEngine.Input.GetKeyDown(keyCode);
#else
            return UnityEngine.Input.GetKeyDown(keyCode);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryMapKey(KeyCode keyCode, out Key key)
        {
            return KeyMap.TryGetValue(keyCode, out key);
        }

        private static bool IsHeldInputSystem(KeyCode keyCode)
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            return TryMapKey(keyCode, out Key key) && Keyboard.current[key].isPressed;
        }

        private static bool IsPressedInputSystem(KeyCode keyCode)
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            return TryMapKey(keyCode, out Key key) && Keyboard.current[key].wasPressedThisFrame;
        }

        private Vector2 ReadMoveVectorInputSystem()
        {
            Vector2 move = Vector2.zero;

            if (IsHeld(_bindings.MoveLeft))
            {
                move.x -= 1f;
            }

            if (IsHeld(_bindings.MoveRight))
            {
                move.x += 1f;
            }

            if (IsHeld(_bindings.MoveUp))
            {
                move.y += 1f;
            }

            if (IsHeld(_bindings.MoveDown))
            {
                move.y -= 1f;
            }

            return Vector2.ClampMagnitude(move, 1f);
        }
#endif

        private Vector2 ReadMoveVectorLegacy()
        {
            Vector2 move = Vector2.zero;

            if (UnityEngine.Input.GetKey(_bindings.MoveLeft))
            {
                move.x -= 1f;
            }

            if (UnityEngine.Input.GetKey(_bindings.MoveRight))
            {
                move.x += 1f;
            }

            if (UnityEngine.Input.GetKey(_bindings.MoveUp))
            {
                move.y += 1f;
            }

            if (UnityEngine.Input.GetKey(_bindings.MoveDown))
            {
                move.y -= 1f;
            }

            return Vector2.ClampMagnitude(move, 1f);
        }
    }
}
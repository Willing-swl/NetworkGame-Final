using System;
using UnityEngine;

namespace Project.Gameplay.Input
{
    [Serializable]
    public sealed class PlayerKeyboardBindings
    {
        public KeyCode MoveUp = KeyCode.W;
        public KeyCode MoveDown = KeyCode.S;
        public KeyCode MoveLeft = KeyCode.A;
        public KeyCode MoveRight = KeyCode.D;
        public KeyCode Spray = KeyCode.F;
        public KeyCode Charge = KeyCode.Q;
        public KeyCode Dodge = KeyCode.LeftShift;
        public KeyCode Jump = KeyCode.Space;
        public KeyCode Pause = KeyCode.Escape;

        public static PlayerKeyboardBindings CreateP1()
        {
            return new PlayerKeyboardBindings
            {
                MoveUp = KeyCode.W,
                MoveDown = KeyCode.S,
                MoveLeft = KeyCode.A,
                MoveRight = KeyCode.D,
                Spray = KeyCode.F,
                Charge = KeyCode.Q,
                Dodge = KeyCode.LeftShift,
                Jump = KeyCode.Space,
                Pause = KeyCode.Escape
            };
        }

        public static PlayerKeyboardBindings CreateP2()
        {
            return new PlayerKeyboardBindings
            {
                MoveUp = KeyCode.UpArrow,
                MoveDown = KeyCode.DownArrow,
                MoveLeft = KeyCode.LeftArrow,
                MoveRight = KeyCode.RightArrow,
                Spray = KeyCode.RightControl,
                Charge = KeyCode.Keypad0,
                Dodge = KeyCode.RightShift,
                Jump = KeyCode.Keypad1,
                Pause = KeyCode.KeypadEnter
            };
        }
    }
}
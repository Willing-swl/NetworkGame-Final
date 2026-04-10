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
        public KeyCode Dodge = KeyCode.Space;
        public KeyCode Pause = KeyCode.Escape;

        public static PlayerKeyboardBindings CreateP1()
        {
            return new PlayerKeyboardBindings();
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
                Dodge = KeyCode.RightShift,
                Pause = KeyCode.KeypadEnter
            };
        }
    }
}
using UnityEngine;

namespace Project.Gameplay.Input
{
    public readonly struct PlayerInputFrame
    {
        public readonly Vector2 Move;
        public readonly Vector2 Aim;
        public readonly bool SprayHeld;
        public readonly bool SprayPressed;
        public readonly bool DodgePressed;
        public readonly bool PausePressed;

        public PlayerInputFrame(Vector2 move, Vector2 aim, bool sprayHeld, bool sprayPressed, bool dodgePressed, bool pausePressed)
        {
            Move = move;
            Aim = aim;
            SprayHeld = sprayHeld;
            SprayPressed = sprayPressed;
            DodgePressed = dodgePressed;
            PausePressed = pausePressed;
        }

        public bool HasMoveInput => Move.sqrMagnitude > 0.01f;

        public bool HasAimInput => Aim.sqrMagnitude > 0.01f;
    }
}
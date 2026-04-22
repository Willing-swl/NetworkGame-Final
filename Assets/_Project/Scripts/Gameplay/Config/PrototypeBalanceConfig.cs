using UnityEngine;

namespace Project.Gameplay.Config
{
    [CreateAssetMenu(fileName = "PrototypeBalanceConfig", menuName = "CQBL/Prototype Balance Config")]
    public class PrototypeBalanceConfig : ScriptableObject
    {
        [Header("Arena")]
        [Min(3)] public int GridWidth = 11;
        [Min(3)] public int GridHeight = 11;
        [Min(0.5f)] public float TileSize = 1f;
        [Min(0f)] public float TileGap = 0.05f;
        [Min(0.05f)] public float TileHeight = 0.2f;
        [Min(0f)] public float PlayerSpawnHeight = 1f;
        [Min(0f)] public float ArenaEliminationMargin = 1.5f;

        [Header("Match")]
        [Min(1f)] public float RoundDuration = 60f;

        [Header("Capture")]
        [Min(0.01f)] public float NeutralCaptureSeconds = 0.15f;
        [Min(0.01f)] public float EnemyCaptureSeconds = 0.25f;
        [Min(0.01f)] public float SprayRange = 3f;

        [Header("Shockwave")]
        [Min(0.01f)] public float ChargeHoldSeconds = 0.18f;
        [Min(0.01f)] public float ShockwaveCooldown = 0.9f;
        [Min(1)] public int ShockwaveAbsorbTileLimit = 80;
        [Min(1)] public int ShockwaveAbsorbSearchRadius = 2;
        [Min(0.1f)] public float ShockwaveRadiusBase = 2.35f;
        [Min(0.1f)] public float ShockwaveRadiusTierStep = 0.9f;
        [Min(0.1f)] public float ShockwaveForceBase = 8f;
        [Min(0.1f)] public float ShockwaveForceTierStep = 3.5f;
        [Min(0.1f)] public float ShockwaveDamageBase = 10f;
        [Min(0.1f)] public float ShockwaveDamageTierStep = 6f;

        [Header("Player")]
        [Min(0.1f)] public float MoveSpeed = 4.5f;
        [Min(1f)] public float RotationSpeed = 720f;
        [Min(0.01f)] public float SprayCooldown = 0.08f;
        [Min(0.01f)] public float DodgeDuration = 0.2f;
        [Min(0.01f)] public float DodgeCooldown = 0.8f;
        [Min(0.1f)] public float DodgeSpeed = 8f;
        [Min(0.1f)] public float KnockbackSpeed = 6f;
        [Range(0.01f, 1f)] public float DeadZone = 0.2f;
        [Min(1)] public int InputBufferSize = 10;

        [Header("Colors")]
        public Color NeutralTileColor = new Color(0.85f, 0.82f, 0.75f, 1f);
        public Color Player1TileColor = new Color(0.18f, 0.55f, 0.75f, 1f);
        public Color Player2TileColor = new Color(0.91f, 0.31f, 0.25f, 1f);
        public Color Player1BodyColor = new Color(0.14f, 0.48f, 0.68f, 1f);
        public Color Player2BodyColor = new Color(0.78f, 0.24f, 0.20f, 1f);
        public Color CapturePulseColor = new Color(1f, 1f, 1f, 1f);

        public float GetCaptureSeconds(int ownerPlayerId)
        {
            return ownerPlayerId == 0 ? NeutralCaptureSeconds : EnemyCaptureSeconds;
        }

        public float GetCaptureProgressPerSecond(int ownerPlayerId)
        {
            return 1f / Mathf.Max(0.01f, GetCaptureSeconds(ownerPlayerId));
        }

        public Color GetTileColor(int playerId)
        {
            if (playerId == 1)
            {
                return Player1TileColor;
            }

            if (playerId == 2)
            {
                return Player2TileColor;
            }

            return NeutralTileColor;
        }

        public Color GetBodyColor(int playerId)
        {
            if (playerId == 1)
            {
                return Player1BodyColor;
            }

            if (playerId == 2)
            {
                return Player2BodyColor;
            }

            return NeutralTileColor;
        }
    }
}
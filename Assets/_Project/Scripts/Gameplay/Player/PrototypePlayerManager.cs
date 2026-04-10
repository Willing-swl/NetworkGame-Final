using System.Collections.Generic;
using NanoFrame.Core;
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Input;
using Project.Gameplay.Match;
using Project.Gameplay.Visuals;
using UnityEngine;

namespace Project.Gameplay.Player
{
    public class PrototypePlayerManager : Singleton<PrototypePlayerManager>, IManager
    {
        private readonly List<PlayerController> _players = new List<PlayerController>();
        private GameObject _playerRoot;
        private PrototypeBalanceConfig _settings;

        public IReadOnlyList<PlayerController> Players => _players;

        public void OnInit()
        {
            _settings = PrototypeMatchManager.Instance.Settings;
            BuildPlayers();
        }

        public void OnUpdate()
        {
            if (!PrototypeMatchManager.Instance.IsRoundActive)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            PrototypeInputManager inputManager = PrototypeInputManager.Instance;

            for (int index = 0; index < _players.Count; index++)
            {
                PlayerController player = _players[index];
                if (player == null || player.IsEliminated)
                {
                    continue;
                }

                player.Tick(inputManager.GetCurrentFrame(player.PlayerId), deltaTime);
            }
        }

        public void OnDestroyManager()
        {
            if (_playerRoot != null)
            {
                Destroy(_playerRoot);
            }

            _playerRoot = null;
            _players.Clear();
        }

        public void ResetPlayers()
        {
            if (_settings == null)
            {
                _settings = PrototypeMatchManager.Instance.Settings;
            }

            for (int index = 0; index < _players.Count; index++)
            {
                PlayerController player = _players[index];
                if (player == null)
                {
                    continue;
                }

                Vector3 spawnPosition = PrototypeGridManager.Instance.GetSpawnPosition(player.PlayerId);
                Vector3 facingDirection = PrototypeGridManager.Instance.GetSpawnFacing(player.PlayerId);
                player.ResetForMatch(spawnPosition, facingDirection);
                UnityEngine.Debug.Log($"[PrototypePlayerManager] 重置 P{player.PlayerId} 出生点={spawnPosition} 朝向={facingDirection}");
            }
        }

        private void BuildPlayers()
        {
            if (_playerRoot != null)
            {
                Destroy(_playerRoot);
            }

            _playerRoot = new GameObject("PrototypePlayers");
            DontDestroyOnLoad(_playerRoot);

            _players.Clear();
            CreatePlayer(1, _settings.GetBodyColor(1));
            CreatePlayer(2, _settings.GetBodyColor(2));
        }

        private void CreatePlayer(int playerId, Color bodyColor)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = $"Player_{playerId}";
            playerObject.transform.SetParent(_playerRoot.transform, false);

            Rigidbody rigidbody = playerObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerObject.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PlayerController controller = playerObject.AddComponent<PlayerController>();
            controller.Initialize(playerId, _settings, bodyColor);
            _players.Add(controller);

            UnityEngine.Debug.Log($"[PrototypePlayerManager] 生成 P{playerId}，初始位置={playerObject.transform.position}");
        }
    }
}
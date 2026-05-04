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

        [SerializeField] private GameObject _playerPrefab;
        private const string PlayerPrefabPath = "Gameplay/Chicken";
        

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

        private void EnsurePlayerPrefabLoaded()
        {
            if (_playerPrefab != null)
            {
                return;
            }

            _playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (_playerPrefab == null)
            {
                UnityEngine.Debug.LogError("[PrototypePlayerManager] 找不到玩家 prefab: Resources/Gameplay/Chicken.prefab");
            }
        }


                private void CreatePlayer(int playerId, Color bodyColor)
        {
            EnsurePlayerPrefabLoaded();
            if (_playerPrefab == null)
            {
                return;
            }

            GameObject playerObject = Object.Instantiate(_playerPrefab);
            playerObject.name = $"Player_{playerId}";
            playerObject.transform.SetParent(_playerRoot.transform, false);
            playerObject.transform.localPosition = Vector3.zero;
            playerObject.transform.localRotation = Quaternion.identity;

            Rigidbody rigidbody = playerObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerObject.AddComponent<Rigidbody>();
            }

            //rigidbody.useGravity = false;
            //rigidbody.isKinematic = true;
            //rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            //rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PlayerRoamingController legacyInput = playerObject.GetComponent<PlayerRoamingController>();
            if (legacyInput != null)
            {
                Object.Destroy(legacyInput);
            }

            Animator animator = playerObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            PlayerController controller = playerObject.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = playerObject.AddComponent<PlayerController>();
            }

            controller.Initialize(playerId, _settings, bodyColor);

            
            if (playerObject.GetComponent<PlayerAnimatorBridge>() == null)
            {
                playerObject.AddComponent<PlayerAnimatorBridge>();
            }
            _players.Add(controller);
        }




        // private void CreatePlayer(int playerId, Color bodyColor)
        // {
            

        //     //GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //     GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //     playerObject.name = $"Player_{playerId}";
        //     playerObject.transform.SetParent(_playerRoot.transform, false);//设置父对象为玩家根对象，并保持局部变换不变
        //     playerObject.transform.localPosition = Vector3.zero;//用于重置时设置初始位置，后续会根据出生点进行调整
        //     playerObject.transform.localRotation = Quaternion.identity;///用于重置时设置初始旋转，后续会根据出生点进行调整

        //     Rigidbody rigidbody = playerObject.GetComponent<Rigidbody>();
        //     if (rigidbody == null)
        //     {
        //         rigidbody = playerObject.AddComponent<Rigidbody>();
        //     }

        //     rigidbody.useGravity = false;
        //     rigidbody.isKinematic = true;
        //     rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        //     rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        //     PlayerController controller = playerObject.AddComponent<PlayerController>();
        //     controller.Initialize(playerId, _settings, bodyColor);
        //     _players.Add(controller);

        //     UnityEngine.Debug.Log($"[PrototypePlayerManager] 生成 P{playerId}，初始位置={playerObject.transform.position}");
        // }
    }
}
// }
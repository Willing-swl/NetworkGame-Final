using System.Collections.Generic;
using Project.Gameplay.Match;
using Project.Gameplay.Player;
using UnityEngine;

namespace Project.Gameplay.CameraRig
{
    [RequireComponent(typeof(Camera))]
    public class PrototypeCameraFollow : MonoBehaviour
    {
        public int TargetPlayerId = 0;

        [Header("3D平台视角配置")]
        [SerializeField] private float _distance = 7.0f;       // 相机距离玩家多远
        [SerializeField] private float _pitch = 38.0f;         // 俯视角度 (向下看38度)
        [SerializeField] private float _heightOffset = 1.0f;   // 焦点在玩家身体偏上

        [Header("平滑/弹簧参数 ")]
        [SerializeField] private float _posSmoothTime = 0.08f; // 相机追随位置的延迟感 
        [SerializeField] private float _yawSmoothTime = 0.7f; // 相机自动转到背后的延迟感 
        [SerializeField] private float _fieldOfView = 60f;

        private Camera _camera;
        private Vector3 _currentPos;
        private Vector3 _posVelocity;
        private float _currentYaw;
        private float _yawVelocity;
        private bool _isInitialized = false;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _camera.orthographic = false;
                _camera.fieldOfView = _fieldOfView;
            }
        }

        private void LateUpdate()
        {
            PrototypeMatchManager matchManager = PrototypeMatchManager.Instance;
            PrototypePlayerManager playerManager = PrototypePlayerManager.Instance;

            if (matchManager == null || playerManager == null) return;

            IReadOnlyList<PlayerController> players = playerManager.Players;
            if (players == null || players.Count == 0) return;

            if (TargetPlayerId != 0)
            {
                PlayerController targetPlayer = null;
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].PlayerId == TargetPlayerId)
                    {
                        targetPlayer = players[i];
                        break;
                    }
                }

                if (targetPlayer != null && !targetPlayer.IsEliminated)
                {
                    Vector3 targetFocusPos = targetPlayer.transform.position + Vector3.up * _heightOffset;

                    if (!_isInitialized)
                    {
                        _currentPos = targetFocusPos;
                        _currentYaw = Quaternion.LookRotation(targetPlayer.FacingDirection).eulerAngles.y;
                        _isInitialized = true;
                    }

                    _currentPos = Vector3.SmoothDamp(_currentPos, targetFocusPos, ref _posVelocity, _posSmoothTime, Mathf.Infinity, Time.deltaTime);

                    if (targetPlayer.HasMoveInput)
                    {
                        float targetYaw = Quaternion.LookRotation(targetPlayer.FacingDirection).eulerAngles.y;
                        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, targetYaw, ref _yawVelocity, _yawSmoothTime, Mathf.Infinity, Time.deltaTime);
                    }

                    Quaternion finalRotation = Quaternion.Euler(_pitch, _currentYaw, 0f);
                    Vector3 finalPosition = _currentPos - (finalRotation * Vector3.forward * _distance);

                    transform.rotation = finalRotation;
                    transform.position = finalPosition;
                }
            }
        }
    }
}
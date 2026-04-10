using System.Collections.Generic;
using Project.Gameplay.Match;
using Project.Gameplay.Player;
using UnityEngine;
using Project.Gameplay.Config;

namespace Project.Gameplay.CameraRig
{
    [RequireComponent(typeof(Camera))]
    public class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private float _pitch = 34f;
        [SerializeField] private float _yaw = 45f;
        [SerializeField] private float _baseDistance = 18f;
        [SerializeField] private float _smoothTime = 0.12f;
        [SerializeField] private float _rotationSmoothSpeed = 12f;
        [SerializeField] private float _distancePadding = 2.5f;
        [SerializeField] private float _spreadDistanceMultiplier = 1.15f;
        [SerializeField] private float _minDistance = 12f;
        [SerializeField] private float _maxDistance = 24f;
        [SerializeField] private float _fieldOfView = 48f;

        private Camera _camera;
        private Vector3 _positionVelocity;
        private float _fieldOfViewVelocity;

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

            if (matchManager == null || matchManager.Settings == null || playerManager == null)
            {
                return;
            }

            IReadOnlyList<PlayerController> players = playerManager.Players;
            if (players == null || players.Count == 0)
            {
                return;
            }

            Vector3 center = CalculateCenter(players);
            float targetDistance = CalculateTargetDistance(matchManager, players, center);

            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = center + orbitRotation * new Vector3(0f, 0f, -targetDistance);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _positionVelocity, _smoothTime, Mathf.Infinity, Time.deltaTime);

            Quaternion desiredRotation = Quaternion.LookRotation(center - transform.position, Vector3.up);
            float rotationBlend = 1f - Mathf.Exp(-_rotationSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);

            if (_camera != null)
            {
                _camera.orthographic = false;
                _camera.fieldOfView = Mathf.SmoothDamp(_camera.fieldOfView, _fieldOfView, ref _fieldOfViewVelocity, _smoothTime);
            }
        }

        private Vector3 CalculateCenter(IReadOnlyList<PlayerController> players)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int index = 0; index < players.Count; index++)
            {
                PlayerController player = players[index];
                if (player == null || player.IsEliminated)
                {
                    continue;
                }

                sum += player.transform.position;
                count++;
            }

            if (count == 0)
            {
                return Vector3.zero;
            }

            return sum / count;
        }

        private float CalculateTargetDistance(PrototypeMatchManager matchManager, IReadOnlyList<PlayerController> players, Vector3 center)
        {
            PrototypeBalanceConfig settings = matchManager.Settings;
            float tileStep = Mathf.Max(0.1f, settings.TileSize + settings.TileGap);
            float boardWidth = Mathf.Max(1f, (settings.GridWidth - 1) * tileStep);
            float boardHeight = Mathf.Max(1f, (settings.GridHeight - 1) * tileStep);

            float boardSpan = Mathf.Max(boardWidth, boardHeight);

            float maxSpread = 0f;
            for (int index = 0; index < players.Count; index++)
            {
                PlayerController player = players[index];
                if (player == null || player.IsEliminated)
                {
                    continue;
                }

                Vector3 offset = player.transform.position - center;
                offset.y = 0f;
                maxSpread = Mathf.Max(maxSpread, offset.magnitude);
            }

            float targetDistance = boardSpan * 0.85f + maxSpread * _spreadDistanceMultiplier + _distancePadding + _baseDistance * 0.2f;
            return Mathf.Clamp(targetDistance, _minDistance, _maxDistance);
        }
    }
}
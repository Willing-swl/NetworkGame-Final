using NanoFrame.Core;
using NanoFrame.Event;
using UnityEngine;

namespace Project.Gameplay.Visuals
{
    public class PrototypeSprayVfxManager : Singleton<PrototypeSprayVfxManager>, IManager
    {
        private const string DefaultResourcePath = "Gameplay/PlayerSprayVfx";

        [SerializeField] private GameObject _sprayPrefab;
        [SerializeField] private string _resourcePath = DefaultResourcePath;
        [SerializeField] private float _spawnForwardOffset = 0.4f;
        [SerializeField] private float _spawnUpOffset = 0.25f;
        [SerializeField] private float _destroyDelayPadding = 0.25f;

        private bool _subscribed;

        public void OnInit()
        {
            EnsurePrefabLoaded();
            Subscribe();
        }

        public void OnUpdate()
        {
        }

        public void OnDestroyManager()
        {
            Unsubscribe();
        }

        private void EnsurePrefabLoaded()
        {
            if (_sprayPrefab != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_resourcePath))
            {
                return;
            }

            _sprayPrefab = Resources.Load<GameObject>(_resourcePath);
            if (_sprayPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[PrototypeSprayVfxManager] Missing spray VFX prefab at Resources/{_resourcePath}.prefab (expected GameObject prefab)");
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            EventManager.Instance.Subscribe<OnPlayerSprayEvent>(HandlePlayerSpray);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<OnPlayerSprayEvent>(HandlePlayerSpray);
            }

            _subscribed = false;
        }

        private void HandlePlayerSpray(OnPlayerSprayEvent eventData)
        {
            EnsurePrefabLoaded();
            if (_sprayPrefab == null)
            {
                return;
            }

            Vector3 direction = eventData.Direction.sqrMagnitude > 0.0001f ? eventData.Direction.normalized : Vector3.forward;
            Vector3 spawnPosition = eventData.Origin + direction * _spawnForwardOffset + Vector3.up * _spawnUpOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject instanceObject = Object.Instantiate(_sprayPrefab, spawnPosition, spawnRotation);
            ParticleSystem instance = instanceObject.GetComponentInChildren<ParticleSystem>();
            if (instance == null)
            {
                UnityEngine.Debug.LogWarning("[PrototypeSprayVfxManager] Spray prefab has no ParticleSystem component.");
                Object.Destroy(instanceObject);
                return;
            }

            instance.Play(true);

            ParticleSystem.MainModule main = instance.main;
            float lifetime = Mathf.Max(0.5f, main.duration + main.startLifetime.constantMax + _destroyDelayPadding);
            Object.Destroy(instanceObject, lifetime);
        }
    }
}
using UnityEngine;
using NanoFrame.Core;
using NanoFrame.Event;

namespace Project.Gameplay.Visuals
{
    public class PrototypeShockwaveVfxManager : Singleton<PrototypeShockwaveVfxManager>, IManager
    {
        [Header("存放特效Prefab")]
        [SerializeField] private GameObject _shockwavePrefab;

        [Header("特效存活时间")]
        [SerializeField] private float _destroyDelay = 2.5f;

        private bool _subscribed;

        public void OnInit()
        {
            Subscribe();
        }

        public void OnUpdate()
        {
        }

        public void OnDestroyManager()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            
            EventManager.Instance.Subscribe<OnPlayerShockwaveEvent>(HandlePlayerShockwave);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<OnPlayerShockwaveEvent>(HandlePlayerShockwave);
            }
            _subscribed = false;
        }

        private void HandlePlayerShockwave(OnPlayerShockwaveEvent eventData)
        {
            if (_shockwavePrefab == null)
            {
                UnityEngine.Debug.LogWarning("[ShockwaveVfxManager] 警告：还没有配置冲击波特效预制体！");
                return;
            }

            GameObject instance = Object.Instantiate(_shockwavePrefab, eventData.Origin, Quaternion.identity);

            instance.transform.localScale = new Vector3(eventData.Radius, eventData.Radius, eventData.Radius);

            Object.Destroy(instance, _destroyDelay);
        }
    }
}
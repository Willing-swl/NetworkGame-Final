using System;
using System.Collections.Generic;
using UnityEngine; 
using NanoFrame.Core;

namespace NanoFrame.Event
{
    public interface IEventMessage { }

    /// <summary>
    /// 全局事件总线 
    /// </summary>
    public class EventManager : Singleton<EventManager>, IManager
    {
        private readonly Dictionary<Type, Delegate> _eventDict = new Dictionary<Type, Delegate>();

        public void OnInit()
        {
            Debug.Log("【成功】EventManager (纯C#单例) 初始化完毕！");
        }

        public void OnUpdate()
        {
        }

        public void OnDestroyManager()
        {
            _eventDict.Clear(); // 对局结束或切换场景时，清空所有事件！
        }

        public void Subscribe<T>(Action<T> listener) where T : IEventMessage
        {
            Type eventType = typeof(T);
            if (_eventDict.TryGetValue(eventType, out Delegate tempDel))
            {
                _eventDict[eventType] = Delegate.Combine(tempDel, listener);
            }
            else
            {
                _eventDict[eventType] = listener;
            }
        }

        public void Unsubscribe<T>(Action<T> listener) where T : IEventMessage
        {
            Type eventType = typeof(T);
            if (_eventDict.TryGetValue(eventType, out Delegate tempDel))
            {
                Delegate currentDel = Delegate.Remove(tempDel, listener);
                if (currentDel == null)
                    _eventDict.Remove(eventType);
                else
                    _eventDict[eventType] = currentDel;
            }
        }

        public void Fire<T>(T message) where T : IEventMessage
        {
            Type eventType = typeof(T);
            if (_eventDict.TryGetValue(eventType, out Delegate tempDel))
            {
                Action<T> callback = tempDel as Action<T>;
                callback?.Invoke(message);
            }
        }
    }
}
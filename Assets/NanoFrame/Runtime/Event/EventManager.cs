using System;
using System.Collections.Generic;
using UnityEngine; // 需要引入UnityEngine来打印日志
using NanoFrame.Core;

namespace NanoFrame.Event
{
    // 事件消息的基类接口
    public interface IEventMessage { }

    /// <summary>
    /// 全局事件总线 (强类型、零GC版本)
    /// </summary>
    // 【修改点1】：在 Singleton<EventManager> 后面加上 , IManager
    public class EventManager : Singleton<EventManager>, IManager
    {
        private readonly Dictionary<Type, Delegate> _eventDict = new Dictionary<Type, Delegate>();

        // 【修改点2】：实现接口要求的初始化方法
        public void OnInit()
        {
            Debug.Log("【成功】EventManager (纯C#单例) 初始化完毕！");
        }

        // 【修改点3】：实现接口要求的每帧更新方法 (暂时不需要就空着)
        public void OnUpdate()
        {
        }

        // 【修改点4】：实现接口要求的清理方法 (非常重要！防内存泄漏)
        public void OnDestroyManager()
        {
            _eventDict.Clear(); // 对局结束或切换场景时，清空所有事件！
        }

        // ================= 下方是你原本的优秀代码，原封不动 =================

        // 订阅事件
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

        // 取消订阅
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

        // 触发/广播事件
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
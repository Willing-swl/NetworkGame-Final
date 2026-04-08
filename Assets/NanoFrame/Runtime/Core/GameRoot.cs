using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Event; // 引入你原有的事件模块

namespace NanoFrame.Core // 完全使用你原有的核心命名空间
{
    /// <summary>
    /// NanoFrame 框架的唯一引擎挂载点 (引擎桥梁)
    /// 负责按策划案顺序启动和驱动所有纯 C# 的 Manager
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        // 核心：维护一个你之前定义的 IManager 接口列表
        private List<IManager> _managers = new List<IManager>();

        private void Start()
        {
            // 防止切换场景时框架被销毁
            DontDestroyOnLoad(this.gameObject);

            Debug.Log("<color=#00FF00>========== NanoFrame 框架启动 ==========</color>");

            // 1. 将你的纯C#单例管理器加入生命周期管控
            // （这里以你已有的 EventManager 为例，以后有 AudioManager 也可以这样加）
            _managers.Add(EventManager.Instance);

            // 2. 严格按顺序初始化
            foreach (var manager in _managers)
            {
                if (manager != null)
                {
                    manager.OnInit();
                }
            }

            Debug.Log("<color=#00FF00>========== NanoFrame 初始化完毕 ==========</color>");
        }

        private void Update()
        {
            // 3. 将 Unity 的 Update 传递给你框架内需要每帧更新的 Manager
            foreach (var manager in _managers)
            {
                manager?.OnUpdate();
            }
        }

        private void OnDestroy()
        {
            // 4. 将 Unity 的退出/销毁事件，传递给你的 Manager 进行内存清理
            foreach (var manager in _managers)
            {
                manager?.OnDestroyManager();
            }
            _managers.Clear();
        }
    }
}
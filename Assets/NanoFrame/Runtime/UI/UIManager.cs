using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Core;

namespace NanoFrame.UI
{
    /// <summary>
    /// 全局 UI 管理器
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        // 缓存字典：记住已经加载过的 UI，避免重复消耗性能
        // Key: UI预制体的名字 (例如 "PauseMenu")
        // Value: 具体的 UI 脚本实例
        private Dictionary<string, BaseUIForm> _uiDict = new Dictionary<string, BaseUIForm>();

        // 全局唯一的画板（所有UI都会变成它的子节点）
        private Transform _canvasRoot;

        /// <summary>
        /// 确保场景中有一个 Canvas
        /// </summary>
        private void EnsureCanvasRoot()
        {
            if (_canvasRoot == null)
            {
                // 尝试在场景中找 Canvas
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    _canvasRoot = canvas.transform;
                }
                else
                {
                    Debug.LogError("场景中没有找到 Canvas！UI 管家无法工作。请在场景里右键新建一个 UI -> Canvas。");
                }
            }
        }

        /// <summary>
        /// 打开一个 UI 面板
        /// </summary>
        /// <typeparam name="T">要打开的 UI 脚本类型</typeparam>
        /// <param name="uiName">UI 预制体的名字</param>
        public T OpenUI<T>(string uiName) where T : BaseUIForm
        {
            EnsureCanvasRoot();

            // 1. 如果字典里已经有这个 UI 了（说明之前打开过），直接调用 OnOpen()
            if (_uiDict.TryGetValue(uiName, out BaseUIForm form))
            {
                form.OnOpen();
                form.transform.SetAsLastSibling(); // 把它移到层级最下面，保证它显示在最前面，不被遮挡！
                return form as T;
            }

            // 2. 如果是第一次打开，需要从 Resources 文件夹里动态加载出来
            // 注意：路径相对于 Resources 文件夹
            string path = "UI/" + uiName;
            GameObject prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"找不到 UI 预制体！请检查是否将 {uiName} 放到了 Resources/UI 目录下！");
                return null;
            }

            // 3. 实例化（生成出来），并设置为 Canvas 的子物体
            GameObject uiObj = Instantiate(prefab, _canvasRoot);
            uiObj.name = uiName; // 去掉讨厌的 "(Clone)" 后缀

            // 4. 获取它身上挂载的 T (比如 PauseMenu 脚本)
            T uiScript = uiObj.GetComponent<T>();
            if (uiScript == null)
            {
                Debug.LogError($"UI 预制体 {uiName} 身上没有挂载 {typeof(T).Name} 脚本！");
                return null;
            }

            // 5. 存入字典，以后不用再加载了
            _uiDict.Add(uiName, uiScript);

            // 6. 执行打开逻辑
            uiScript.OnOpen();

            return uiScript;
        }

        /// <summary>
        /// 关闭一个 UI 面板
        /// </summary>
        public void CloseUI(string uiName)
        {
            if (_uiDict.TryGetValue(uiName, out BaseUIForm form))
            {
                form.OnClose();
            }
            else
            {
                Debug.LogWarning($"你试图关闭一个从未打开过的 UI: {uiName}");
            }
        }
    }
}

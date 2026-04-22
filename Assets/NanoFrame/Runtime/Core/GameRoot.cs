using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Event; // 引入你原有的事件模块
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Input;
using Project.Gameplay.Match;
using Project.Gameplay.Player;
using Project.Gameplay.CameraRig;
using Project.Gameplay.Visuals;

namespace NanoFrame.Core // 完全使用你原有的核心命名空间
{
    /// <summary>
    /// NanoFrame 框架的唯一引擎挂载点 (引擎桥梁)
    /// 负责按策划案顺序启动和驱动所有纯 C# 的 Manager
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<GameRoot>() != null)
            {
                return;
            }

            GameObject rootObject = new GameObject(nameof(GameRoot));
            rootObject.AddComponent<GameRoot>();
        }

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
            _managers.Add(PrototypeInputManager.Instance);
            _managers.Add(PrototypeMatchManager.Instance);
            _managers.Add(PrototypeGridManager.Instance);
            _managers.Add(PrototypePlayerManager.Instance);
            _managers.Add(PrototypeSprayVfxManager.Instance);

            // 2. 严格按顺序初始化
            foreach (var manager in _managers)
            {
                if (manager != null)
                {
                    manager.OnInit();
                }
            }

            PrototypeMatchManager.Instance.RestartMatch();

            EnsurePrototypePresentation();

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

            // 5. 关闭时顺手销毁这些自动创建出来的单例对象，避免 Unity 报未清理的场景对象
            foreach (var manager in _managers)
            {
                if (manager is MonoBehaviour behaviour && behaviour != null && behaviour.gameObject != null && behaviour.gameObject != this.gameObject)
                {
                    Destroy(behaviour.gameObject);
                }
            }

            _managers.Clear();
        }

        private void EnsurePrototypePresentation()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("PrototypeCamera");
                cameraObject.tag = "MainCamera";

                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                DontDestroyOnLoad(cameraObject);
            }

            ConfigurePrototypeCamera(camera);

            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightObject = new GameObject("PrototypeDirectionalLight");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.95f, 0.88f, 1f);
                light.intensity = 1.2f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                DontDestroyOnLoad(lightObject);
            }
        }

        private static void ConfigurePrototypeCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            GameObject cameraObject = camera.gameObject;
            if (!cameraObject.CompareTag("MainCamera"))
            {
                cameraObject.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;

            cameraObject.transform.position = new Vector3(-10f, 12f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(34f, 45f, 0f);

            if (cameraObject.GetComponent<PrototypeCameraFollow>() == null)
            {
                cameraObject.AddComponent<PrototypeCameraFollow>();
            }

            if (cameraObject.GetComponent<AudioListener>() == null && FindObjectOfType<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            DontDestroyOnLoad(cameraObject);
        }
    }
}
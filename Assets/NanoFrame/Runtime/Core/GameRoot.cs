using System.Collections.Generic;
using UnityEngine;
using NanoFrame.Event; 
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Input;
using Project.Gameplay.Match;
using Project.Gameplay.Player;
using Project.Gameplay.CameraRig;
using Project.Gameplay.Visuals;

namespace NanoFrame.Core 
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

        private List<IManager> _managers = new List<IManager>();

        private void Start()
        {
            DontDestroyOnLoad(this.gameObject);

            _managers.Add(EventManager.Instance);
            _managers.Add(PrototypeInputManager.Instance);
            _managers.Add(PrototypeMatchManager.Instance);
            _managers.Add(PrototypeGridManager.Instance);
            _managers.Add(PrototypePlayerManager.Instance);
            _managers.Add(PrototypeSprayVfxManager.Instance);
            _managers.Add(PrototypeShockwaveVfxManager.Instance);

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
            foreach (var manager in _managers)
            {
                manager?.OnUpdate();
            }
        }

        private void OnDestroy()
        {
            foreach (var manager in _managers)
            {
                manager?.OnDestroyManager();
            }

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
            bool hasScenePresentationRoot = FindObjectOfType<PrototypeScenePresentationRoot>() != null;

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
                ConfigurePrototypeCamera(camera);
            }

            else if (hasScenePresentationRoot)
            {
                if (!camera.gameObject.CompareTag("MainCamera"))
                {
                    camera.gameObject.tag = "MainCamera";
                }

                PrototypeCameraFollow follow = camera.GetComponent<PrototypeCameraFollow>();
                if (follow != null)
                {
                    follow.enabled = false;
                }

                if (camera.GetComponent<AudioListener>() == null && FindObjectOfType<AudioListener>() == null)
                {
                    camera.gameObject.AddComponent<AudioListener>();
                }

                DontDestroyOnLoad(camera.gameObject);
            }

            else
            {
                ConfigurePrototypeCamera(camera);
            }

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
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

        //    private void EnsurePrototypePresentation()
        //    {
        //        bool hasScenePresentationRoot = FindObjectOfType<PrototypeScenePresentationRoot>() != null;

        //        Camera camera = Camera.main;
        //        if (camera == null)
        //        {
        //            camera = FindObjectOfType<Camera>();
        //        }

        //        if (camera == null)
        //        {
        //            GameObject cameraObject = new GameObject("PrototypeCamera");
        //            cameraObject.tag = "MainCamera";

        //            camera = cameraObject.AddComponent<Camera>();
        //            cameraObject.AddComponent<AudioListener>();
        //            DontDestroyOnLoad(cameraObject);
        //            ConfigurePrototypeCamera(camera);
        //        }

        //        else if (hasScenePresentationRoot)
        //        {
        //            if (!camera.gameObject.CompareTag("MainCamera"))
        //            {
        //                camera.gameObject.tag = "MainCamera";
        //            }

        //            PrototypeCameraFollow follow = camera.GetComponent<PrototypeCameraFollow>();
        //            if (follow != null)
        //            {
        //                follow.enabled = false;
        //            }

        //            if (camera.GetComponent<AudioListener>() == null && FindObjectOfType<AudioListener>() == null)
        //            {
        //                camera.gameObject.AddComponent<AudioListener>();
        //            }

        //            DontDestroyOnLoad(camera.gameObject);
        //        }

        //        else
        //        {
        //            ConfigurePrototypeCamera(camera);
        //        }

        //        if (FindObjectOfType<Light>() == null)
        //        {
        //            GameObject lightObject = new GameObject("PrototypeDirectionalLight");
        //            Light light = lightObject.AddComponent<Light>();
        //            light.type = LightType.Directional;
        //            light.color = new Color(1f, 0.95f, 0.88f, 1f);
        //            light.intensity = 1.2f;
        //            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        //            DontDestroyOnLoad(lightObject);
        //        }
        //    }

        //    private static void ConfigurePrototypeCamera(Camera camera)
        //    {
        //        if (camera == null)
        //        {
        //            return;
        //        }

        //        GameObject cameraObject = camera.gameObject;
        //        if (!cameraObject.CompareTag("MainCamera"))
        //        {
        //            cameraObject.tag = "MainCamera";
        //        }

        //        camera.clearFlags = CameraClearFlags.Skybox;
        //        camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        //        camera.orthographic = false;
        //        camera.fieldOfView = 48f;
        //        camera.nearClipPlane = 0.3f;
        //        camera.farClipPlane = 150f;

        //        cameraObject.transform.position = new Vector3(-10f, 12f, -10f);
        //        cameraObject.transform.rotation = Quaternion.Euler(34f, 45f, 0f);

        //        if (cameraObject.GetComponent<PrototypeCameraFollow>() == null)
        //        {
        //            cameraObject.AddComponent<PrototypeCameraFollow>();
        //        }

        //        if (cameraObject.GetComponent<AudioListener>() == null && FindObjectOfType<AudioListener>() == null)
        //        {
        //            cameraObject.AddComponent<AudioListener>();
        //        }

        //        DontDestroyOnLoad(cameraObject);
        //    }

        private void EnsurePrototypePresentation()
        {
            // 【已删除原来导致罢工的 return 检查，现在强制执行！】

            // 1. 无脑强杀场景里原有的所有摄像机（给分屏让路）
            Camera[] existingCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in existingCameras)
            {
                if (cam.gameObject.name != "PrototypeCamera_P1" && cam.gameObject.name != "PrototypeCamera_P2")
                {
                    Destroy(cam.gameObject);
                }
            }

            // 2. 强制生成两个分屏相机！
            if (GameObject.Find("PrototypeCamera_P1") == null)
            {
                // 创建 P1 相机 (左半屏: X=0, Width=0.5)
                CreateSplitCamera(1, new Rect(0f, 0f, 0.5f, 1f));

                // 创建 P2 相机 (右半屏: X=0.5, Width=0.5)
                CreateSplitCamera(2, new Rect(0.5f, 0f, 0.5f, 1f));
            }

            // 3. 确保环境光存在
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

        private static void CreateSplitCamera(int playerId, Rect viewportRect)
        {
            GameObject cameraObject = new GameObject($"PrototypeCamera_P{playerId}");

            if (playerId == 1) cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.rect = viewportRect;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;

            cameraObject.transform.position = new Vector3(-10f, 12f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(34f, 45f, 0f);

            PrototypeCameraFollow follow = cameraObject.AddComponent<PrototypeCameraFollow>();
            follow.TargetPlayerId = playerId;

            var cameraData = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask |= 1 << 0;

            if (playerId == 1)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            DontDestroyOnLoad(cameraObject);
        }
    }
}
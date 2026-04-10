using UnityEngine;
using RenderMoodSnapshot = NanoFrame.Rendering.RenderMoodManager.RenderMoodSnapshot;

namespace NanoFrame.Rendering
{
    /// <summary>
    /// 运行时渲染调试面板
    /// 用来在 Play Mode 里直接把“太黑 / 过曝 / 没有童核味”的参数拖回来。
    /// </summary>
    public class RenderMoodDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private bool liveApply = true;
        [SerializeField] private bool lockAutoTimelineWhenApplying = true;
        [SerializeField] private bool instantPhaseSwitch = true;
        [SerializeField] private Rect windowRect = new Rect(18f, 18f, 540f, 860f);

        private const int WindowId = 482917;

        private RenderMoodManager _manager;
        private RenderMoodPhase _selectedPhase = RenderMoodPhase.Intro;
        private RenderMoodSnapshot _snapshot;
        private Vector2 _scrollPosition;
        private bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindFirstObjectByType<RenderMoodDebugPanel>() != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("RenderMoodDebugPanel");
            panelObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(panelObject);
            panelObject.AddComponent<RenderMoodDebugPanel>();
        }

        private void Awake()
        {
            SyncFromManager();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                visible = !visible;
            }

            if (!_initialized)
            {
                SyncFromManager();
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !visible)
            {
                return;
            }

            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "渲染气氛面板");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            GUILayout.Space(4f);
            GUILayout.Label($"阶段：{GetPhaseLabel(_manager.ActivePhase)} | 自动：{(_manager.IsAutoTimelineEnabled ? "开" : "关")} | 手动：{(_manager.IsManualOverrideActive ? "是" : "否")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("同步", GUILayout.Height(28f)))
            {
                SyncFromManager();
            }

            if (GUILayout.Button("应用", GUILayout.Height(28f)))
            {
                PushAllToManager();
            }

            if (GUILayout.Button("自动开", GUILayout.Height(28f)))
            {
                _manager.SetAutoTimelineEnabled(true);
                SyncFromManager();
            }

            if (GUILayout.Button("自动关", GUILayout.Height(28f)))
            {
                _manager.SetAutoTimelineEnabled(false);
                SyncFromManager();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("童核提亮", GUILayout.Height(28f)))
            {
                ApplyWarmPreset();
            }

            if (GUILayout.Button("读取已保存模板", GUILayout.Height(28f)))
            {
                LoadSelectedPhasePreset();
            }

            if (GUILayout.Button("梦核压暗", GUILayout.Height(28f)))
            {
                ApplyDarkPreset();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            liveApply = GUILayout.Toggle(liveApply, "实时生效", GUILayout.Width(92f));
            lockAutoTimelineWhenApplying = GUILayout.Toggle(lockAutoTimelineWhenApplying, "应用时锁定自动", GUILayout.Width(148f));
            instantPhaseSwitch = GUILayout.Toggle(instantPhaseSwitch, "阶段瞬切", GUILayout.Width(92f));
            GUILayout.EndHorizontal();

            GUILayout.Label("流程：先选阶段 -> 调参数 -> 点“写入并保存”写入项目配置；“应用”只影响当前画面。");

            GUILayout.Space(6f);
            GUILayout.Label("阶段");
            GUILayout.BeginHorizontal();
            DrawPhaseButton(RenderMoodPhase.Intro);
            DrawPhaseButton(RenderMoodPhase.Combat);
            DrawPhaseButton(RenderMoodPhase.Climax);
            DrawPhaseButton(RenderMoodPhase.Elimination);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("写入并保存", GUILayout.Height(26f)))
            {
                SaveCurrentToSelectedPhase();
            }

            if (GUILayout.Button("同步当前画面", GUILayout.Height(26f)))
            {
                CaptureCurrentSnapshot();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            DrawTimelineSection();
            DrawPostFxSection();
            DrawEnvironmentSection();
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));

            if (GUI.changed && liveApply)
            {
                PushAllToManager();
            }
        }

        private void DrawPhaseButton(RenderMoodPhase phase)
        {
            bool isSelected = _selectedPhase == phase;
            if (GUILayout.Toggle(isSelected, GetPhaseLabel(phase), "Button", GUILayout.Height(24f)))
            {
                if (!isSelected)
                {
                    SelectPhase(phase);
                }
            }
        }

        private void DrawTimelineSection()
        {
            GUILayout.Label("时间轴");

            float introEndTime = DrawSlider("试探期结束", _manager.IntroEndTime, 0f, 60f);
            float combatEndTime = DrawSlider("对战期结束", _manager.CombatEndTime, introEndTime, 60f);
            float climaxEndTime = DrawSlider("高潮期结束", _manager.ClimaxEndTime, combatEndTime, 60f);
            float matchDuration = DrawSlider("对局时长", _manager.MatchDuration, climaxEndTime, 120f);
            float phaseBlendSpeed = DrawSlider("过渡速度", _manager.PhaseBlendSpeed, 0f, 10f);

            _timelineDirty = !Mathf.Approximately(_manager.IntroEndTime, introEndTime)
                || !Mathf.Approximately(_manager.CombatEndTime, combatEndTime)
                || !Mathf.Approximately(_manager.ClimaxEndTime, climaxEndTime)
                || !Mathf.Approximately(_manager.MatchDuration, matchDuration)
                || !Mathf.Approximately(_manager.PhaseBlendSpeed, phaseBlendSpeed);

            _pendingIntroEnd = introEndTime;
            _pendingCombatEnd = combatEndTime;
            _pendingClimaxEnd = climaxEndTime;
            _pendingMatchDuration = matchDuration;
            _pendingPhaseBlendSpeed = phaseBlendSpeed;
        }

        private void DrawPostFxSection()
        {
            GUILayout.Space(6f);
            GUILayout.Label("后处理");

            _snapshot.PostExposure = DrawSlider("整体曝光", _snapshot.PostExposure, -1.5f, 1.5f);
            _snapshot.Contrast = DrawSlider("对比度", _snapshot.Contrast, -40f, 40f);
            _snapshot.Saturation = DrawSlider("饱和度", _snapshot.Saturation, -100f, 100f);
            _snapshot.BloomThreshold = DrawSlider("发光阈值", _snapshot.BloomThreshold, 0f, 4f);
            _snapshot.BloomIntensity = DrawSlider("自发光感", _snapshot.BloomIntensity, 0f, 2f);
            _snapshot.BloomScatter = DrawSlider("光晕扩散", _snapshot.BloomScatter, 0f, 1f);
            _snapshot.VignetteIntensity = DrawSlider("暗角", _snapshot.VignetteIntensity, 0f, 1f);
            _snapshot.ChromaticAberrationIntensity = DrawSlider("色散", _snapshot.ChromaticAberrationIntensity, 0f, 1f);
        }

        private void DrawEnvironmentSection()
        {
            GUILayout.Space(6f);
            GUILayout.Label("环境");

            _snapshot.FogDensity = DrawSlider("雾密度", _snapshot.FogDensity, 0f, 0.08f);
            _snapshot.FogStartDistance = DrawSlider("雾起始", _snapshot.FogStartDistance, 0f, 50f);
            _snapshot.FogEndDistance = DrawSlider("雾终止", _snapshot.FogEndDistance, 0f, 120f);
            _snapshot.AmbientIntensity = DrawSlider("环境光强度", _snapshot.AmbientIntensity, 0f, 2f);
            _snapshot.ReflectionIntensity = DrawSlider("反射强度", _snapshot.ReflectionIntensity, 0f, 2f);
            _snapshot.SkyboxExposure = DrawSlider("天空盒曝光", _snapshot.SkyboxExposure, 0f, 2f);

            _snapshot.FogColor = DrawColorSliders("雾颜色", _snapshot.FogColor, 0f, 2f);
            _snapshot.AmbientSkyColor = DrawColorSliders("环境天空", _snapshot.AmbientSkyColor, 0f, 2f);
            _snapshot.AmbientEquatorColor = DrawColorSliders("环境中间", _snapshot.AmbientEquatorColor, 0f, 2f);
            _snapshot.AmbientGroundColor = DrawColorSliders("环境地面", _snapshot.AmbientGroundColor, 0f, 2f);
        }

        private void SelectPhase(RenderMoodPhase phase)
        {
            _selectedPhase = phase;
            _manager.SetPhase(phase, instantPhaseSwitch);

            if (instantPhaseSwitch)
            {
                SyncFromManager();
            }
        }

        private void LoadSelectedPhasePreset()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            _snapshot = _manager.GetPhaseSnapshot(_selectedPhase);
            PushAllToManager();
        }

        private void SaveCurrentToSelectedPhase()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            if (_timelineDirty)
            {
                _manager.SetTimelineSettings(_pendingIntroEnd, _pendingCombatEnd, _pendingClimaxEnd, _pendingMatchDuration, _pendingPhaseBlendSpeed);
            }

            _manager.SetPhaseSnapshot(_selectedPhase, _snapshot);
            _manager.ResumeAutoTimeline();
            SyncFromManager();
        }

        private void CaptureCurrentSnapshot()
        {
            SyncFromManager();
        }

        private void ApplyWarmPreset()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            _selectedPhase = RenderMoodPhase.Intro;
            _snapshot = _manager.GetPhaseSnapshot(RenderMoodPhase.Intro);
            _snapshot.PostExposure = 0.12f;
            _snapshot.Contrast = 4.5f;
            _snapshot.Saturation = 17f;
            _snapshot.BloomThreshold = 1.22f;
            _snapshot.BloomIntensity = 0.62f;
            _snapshot.BloomScatter = 0.80f;
            _snapshot.VignetteIntensity = 0.05f;
            _snapshot.ChromaticAberrationIntensity = 0f;
            _snapshot.FogDensity = 0.0045f;
            _snapshot.FogStartDistance = 30f;
            _snapshot.FogEndDistance = 86f;
            _snapshot.FogColor = new Color(0.34f, 0.31f, 0.28f, 1f);
            _snapshot.AmbientIntensity = 1.12f;
            _snapshot.AmbientSkyColor = new Color(0.16f, 0.20f, 0.28f, 1f);
            _snapshot.AmbientEquatorColor = new Color(0.22f, 0.19f, 0.16f, 1f);
            _snapshot.AmbientGroundColor = new Color(0.12f, 0.11f, 0.10f, 1f);
            _snapshot.ReflectionIntensity = 1.05f;
            _snapshot.SkyboxExposure = 1.18f;

            PushAllToManager();
        }

        private void ApplyDarkPreset()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            _selectedPhase = RenderMoodPhase.Elimination;
            _snapshot = _manager.GetPhaseSnapshot(RenderMoodPhase.Elimination);
            _snapshot.PostExposure = -0.44f;
            _snapshot.Contrast = 18f;
            _snapshot.Saturation = 6f;
            _snapshot.BloomThreshold = 2.6f;
            _snapshot.BloomIntensity = 0.14f;
            _snapshot.BloomScatter = 0.42f;
            _snapshot.VignetteIntensity = 0.28f;
            _snapshot.ChromaticAberrationIntensity = 0.05f;
            _snapshot.FogDensity = 0.03f;
            _snapshot.FogStartDistance = 8f;
            _snapshot.FogEndDistance = 18f;
            _snapshot.FogColor = new Color(0.18f, 0.16f, 0.14f, 1f);
            _snapshot.AmbientIntensity = 0.42f;
            _snapshot.AmbientSkyColor = new Color(0.03f, 0.04f, 0.06f, 1f);
            _snapshot.AmbientEquatorColor = new Color(0.08f, 0.07f, 0.06f, 1f);
            _snapshot.AmbientGroundColor = new Color(0.04f, 0.03f, 0.03f, 1f);
            _snapshot.ReflectionIntensity = 0.55f;
            _snapshot.SkyboxExposure = 0.52f;

            PushAllToManager();
        }

        private void PushAllToManager()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            if (_timelineDirty)
            {
                _manager.SetTimelineSettings(_pendingIntroEnd, _pendingCombatEnd, _pendingClimaxEnd, _pendingMatchDuration, _pendingPhaseBlendSpeed);
                _timelineDirty = false;
            }

            _manager.ApplyDebugSnapshot(_snapshot, keepAutoTimeline: !lockAutoTimelineWhenApplying);
            SyncFromManager();
        }

        private void SyncFromManager()
        {
            EnsureManager();
            if (_manager == null)
            {
                return;
            }

            _selectedPhase = _manager.ActivePhase;
            _snapshot = _manager.GetCurrentSnapshot();
            _pendingIntroEnd = _manager.IntroEndTime;
            _pendingCombatEnd = _manager.CombatEndTime;
            _pendingClimaxEnd = _manager.ClimaxEndTime;
            _pendingMatchDuration = _manager.MatchDuration;
            _pendingPhaseBlendSpeed = _manager.PhaseBlendSpeed;
            _timelineDirty = false;
            _initialized = true;
        }

        private void EnsureManager()
        {
            if (_manager == null)
            {
                _manager = RenderMoodManager.Instance;
            }
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150f));
            float newValue = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(newValue.ToString("0.00"), GUILayout.Width(72f));
            GUILayout.EndHorizontal();
            return newValue;
        }

        private Color DrawColorSliders(string label, Color color, float min, float max)
        {
            GUILayout.Space(4f);
            GUILayout.Label($"{label} ({color.r:0.00}, {color.g:0.00}, {color.b:0.00})");
            color.r = DrawSlider($"{label} 红", color.r, min, max);
            color.g = DrawSlider($"{label} 绿", color.g, min, max);
            color.b = DrawSlider($"{label} 蓝", color.b, min, max);
            return color;
        }

        private static string GetPhaseLabel(RenderMoodPhase phase)
        {
            switch (phase)
            {
                case RenderMoodPhase.Intro:
                    return "试探期";
                case RenderMoodPhase.Combat:
                    return "对战期";
                case RenderMoodPhase.Climax:
                    return "高潮期";
                case RenderMoodPhase.Elimination:
                    return "终局期";
                default:
                    return phase.ToString();
            }
        }

        private bool _timelineDirty;
        private float _pendingIntroEnd;
        private float _pendingCombatEnd;
        private float _pendingClimaxEnd;
        private float _pendingMatchDuration;
        private float _pendingPhaseBlendSpeed;
    }
}
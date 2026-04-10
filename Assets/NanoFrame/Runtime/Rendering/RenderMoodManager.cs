using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NanoFrame.Core;
using NanoFrame.Event;

namespace NanoFrame.Rendering
{
    /// <summary>
    /// 运行时渲染气氛控制器
    /// 负责统一压亮度、切雾、控景深，让战斗画面保留童核/梦核的清晰感。
    /// </summary>
    public class RenderMoodManager : Singleton<RenderMoodManager>
    {
        [Header("Timeline")]
        [SerializeField] private bool useAutoTimeline = true;
        [SerializeField] private float introEndTime = 20f;
        [SerializeField] private float combatEndTime = 35f;
        [SerializeField] private float climaxEndTime = 50f;
        [SerializeField] private float matchDuration = 60f;

        [Header("Transition")]
        [SerializeField] private float phaseBlendSpeed = 2.5f;

        private Volume _runtimeVolume;
        private VolumeProfile _runtimeProfile;
        private Camera _targetCamera;
        private UniversalAdditionalCameraData _cameraData;

        private Material _runtimeSkybox;
        private bool _capturedOriginalSettings;
        private RenderSettingsSnapshot _originalSettings;

        private RenderMoodPhase _activePhase = RenderMoodPhase.Intro;
        private RenderMoodSnapshot _currentSnapshot;
        private RenderMoodSnapshot _targetSnapshot;
        private RenderMoodSnapshot _introPhaseSnapshot;
        private RenderMoodSnapshot _combatPhaseSnapshot;
        private RenderMoodSnapshot _climaxPhaseSnapshot;
        private RenderMoodSnapshot _eliminationPhaseSnapshot;
        private bool _manualOverride;
        private bool _isInitialized;
        private bool _subscribedToEvents;
        private bool _phaseSnapshotsInitialized;
        private float _giRefreshTimer;

        private static readonly int SkyboxExposureId = Shader.PropertyToID("_Exposure");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (_isInitialized)
            {
                return;
            }

            CaptureOriginalSettings();
            CreateRuntimeVolume();
            ResolveCamera();
            InitializePhaseSnapshots();
            LoadPersistedProfile();

            _activePhase = ResolveAutoPhase(Time.timeSinceLevelLoad);
            _currentSnapshot = GetSnapshot(_activePhase);
            _targetSnapshot = _currentSnapshot;
            ApplySnapshot(_currentSnapshot, true);

            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (_subscribedToEvents)
            {
                return;
            }

            EventManager.Instance.Subscribe<OnPlayerDieEvent>(HandlePlayerDieEvent);
            _subscribedToEvents = true;
        }

        private void OnDisable()
        {
            if (!_subscribedToEvents)
            {
                return;
            }

            EventManager.Instance.Unsubscribe<OnPlayerDieEvent>(HandlePlayerDieEvent);
            _subscribedToEvents = false;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            ResolveCamera();

            if (useAutoTimeline && !_manualOverride)
            {
                RenderMoodPhase autoPhase = ResolveAutoPhase(Time.timeSinceLevelLoad);
                if (autoPhase != _activePhase)
                {
                    SetPhaseInternal(autoPhase, false, true);
                }
            }

            BlendTowardsTarget();
            TickEnvironmentRefresh();
        }

        private void OnDestroy()
        {
            RestoreOriginalSettings();

            if (_runtimeProfile != null)
            {
                Destroy(_runtimeProfile);
                _runtimeProfile = null;
            }
        }

        public void SetPhase(RenderMoodPhase phase, bool instant = false)
        {
            _manualOverride = true;
            SetPhaseInternal(phase, instant, true);
        }

        public void ResumeAutoTimeline()
        {
            useAutoTimeline = true;
            _manualOverride = false;
            SetPhaseInternal(ResolveAutoPhase(Time.timeSinceLevelLoad), false, true);
        }

        public void SetAutoTimelineEnabled(bool enabled)
        {
            if (enabled)
            {
                ResumeAutoTimeline();
                return;
            }

            useAutoTimeline = false;
            _manualOverride = true;
        }

        public void SetTimelineSettings(float introEnd, float combatEnd, float climaxEnd, float totalDuration, float blendSpeed)
        {
            introEndTime = Mathf.Max(0f, introEnd);
            combatEndTime = Mathf.Max(introEndTime, combatEnd);
            climaxEndTime = Mathf.Max(combatEndTime, climaxEnd);
            matchDuration = Mathf.Max(climaxEndTime, totalDuration);
            phaseBlendSpeed = Mathf.Max(0f, blendSpeed);

            if (useAutoTimeline && !_manualOverride)
            {
                SetPhaseInternal(ResolveAutoPhase(Time.timeSinceLevelLoad), false, true);
            }
        }

        public RenderMoodPhase ActivePhase => _activePhase;

        public bool IsAutoTimelineEnabled => useAutoTimeline;

        public bool IsManualOverrideActive => _manualOverride;

        public float IntroEndTime => introEndTime;

        public float CombatEndTime => combatEndTime;

        public float ClimaxEndTime => climaxEndTime;

        public float MatchDuration => matchDuration;

        public float PhaseBlendSpeed => phaseBlendSpeed;

        public RenderMoodSnapshot CurrentSnapshot => _currentSnapshot;

        public RenderMoodSnapshot GetCurrentSnapshot()
        {
            return _currentSnapshot;
        }

        public RenderMoodSnapshot GetPhaseSnapshot(RenderMoodPhase phase)
        {
            return GetSnapshot(phase);
        }

        public void SetPhaseSnapshot(RenderMoodPhase phase, RenderMoodSnapshot snapshot)
        {
            InitializePhaseSnapshots();

            switch (phase)
            {
                case RenderMoodPhase.Intro:
                    _introPhaseSnapshot = snapshot;
                    break;
                case RenderMoodPhase.Combat:
                    _combatPhaseSnapshot = snapshot;
                    break;
                case RenderMoodPhase.Climax:
                    _climaxPhaseSnapshot = snapshot;
                    break;
                default:
                    _eliminationPhaseSnapshot = snapshot;
                    break;
            }

            if (_activePhase == phase)
            {
                _currentSnapshot = snapshot;
                _targetSnapshot = snapshot;
                ApplySnapshot(snapshot, true);
                DynamicGI.UpdateEnvironment();
                _giRefreshTimer = 0.5f;
            }

            SavePersistedProfile();
        }

        public void ApplyDebugSnapshot(RenderMoodSnapshot snapshot, bool keepAutoTimeline = false)
        {
            if (!keepAutoTimeline)
            {
                useAutoTimeline = false;
                _manualOverride = true;
            }

            _currentSnapshot = snapshot;
            _targetSnapshot = snapshot;
            ApplySnapshot(snapshot, true);
            DynamicGI.UpdateEnvironment();
            _giRefreshTimer = 0.5f;
        }

        public void ResetTimeline()
        {
            _manualOverride = false;
            SetPhaseInternal(RenderMoodPhase.Intro, true, true);
        }

        private void HandlePlayerDieEvent(OnPlayerDieEvent eventData)
        {
            // 死亡瞬间切到终局感，后续由回合系统决定是否恢复自动节奏。
            SetPhase(RenderMoodPhase.Elimination, false);
        }

        private void InitializePhaseSnapshots()
        {
            if (_phaseSnapshotsInitialized)
            {
                return;
            }

            _introPhaseSnapshot = CreateDefaultSnapshot(RenderMoodPhase.Intro);
            _combatPhaseSnapshot = CreateDefaultSnapshot(RenderMoodPhase.Combat);
            _climaxPhaseSnapshot = CreateDefaultSnapshot(RenderMoodPhase.Climax);
            _eliminationPhaseSnapshot = CreateDefaultSnapshot(RenderMoodPhase.Elimination);
            _phaseSnapshotsInitialized = true;
        }

        private void LoadPersistedProfile()
        {
#if UNITY_EDITOR
            string profilePath = GetProfileFilePath();
            if (!File.Exists(profilePath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(profilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                RenderMoodProfileData data = JsonUtility.FromJson<RenderMoodProfileData>(json);
                if (data == null)
                {
                    return;
                }

                introEndTime = data.IntroEndTime;
                combatEndTime = data.CombatEndTime;
                climaxEndTime = data.ClimaxEndTime;
                matchDuration = data.MatchDuration;
                phaseBlendSpeed = data.PhaseBlendSpeed;
                _introPhaseSnapshot = data.IntroSnapshot;
                _combatPhaseSnapshot = data.CombatSnapshot;
                _climaxPhaseSnapshot = data.ClimaxSnapshot;
                _eliminationPhaseSnapshot = data.EliminationSnapshot;
                _phaseSnapshotsInitialized = true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Failed to load RenderMood profile: {exception.Message}");
            }
#endif
        }

        private void SavePersistedProfile()
        {
#if UNITY_EDITOR
            try
            {
                string profilePath = GetProfileFilePath();
                string directory = Path.GetDirectoryName(profilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RenderMoodProfileData data = new RenderMoodProfileData
                {
                    IntroEndTime = introEndTime,
                    CombatEndTime = combatEndTime,
                    ClimaxEndTime = climaxEndTime,
                    MatchDuration = matchDuration,
                    PhaseBlendSpeed = phaseBlendSpeed,
                    IntroSnapshot = _introPhaseSnapshot,
                    CombatSnapshot = _combatPhaseSnapshot,
                    ClimaxSnapshot = _climaxPhaseSnapshot,
                    EliminationSnapshot = _eliminationPhaseSnapshot
                };

                File.WriteAllText(profilePath, JsonUtility.ToJson(data, true));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Failed to save RenderMood profile: {exception.Message}");
            }
#endif
        }

        private static string GetProfileFilePath()
        {
#if UNITY_EDITOR
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings", "RenderMoodProfile.json"));
#else
            return string.Empty;
#endif
        }

        private void CreateRuntimeVolume()
        {
            if (_runtimeVolume != null)
            {
                return;
            }

            _runtimeVolume = gameObject.GetComponent<Volume>();
            if (_runtimeVolume == null)
            {
                _runtimeVolume = gameObject.AddComponent<Volume>();
            }

            _runtimeVolume.isGlobal = true;
            _runtimeVolume.priority = 100f;

            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _runtimeProfile.name = "RuntimeRenderMoodProfile";
            _runtimeProfile.hideFlags = HideFlags.HideAndDontSave;
            _runtimeVolume.sharedProfile = _runtimeProfile;

            Bloom bloom = RequireComponent<Bloom>(_runtimeProfile);
            Tonemapping tonemapping = RequireComponent<Tonemapping>(_runtimeProfile);
            Vignette vignette = RequireComponent<Vignette>(_runtimeProfile);
            ColorAdjustments colorAdjustments = RequireComponent<ColorAdjustments>(_runtimeProfile);
            ChromaticAberration chromaticAberration = RequireComponent<ChromaticAberration>(_runtimeProfile);

            bloom.active = true;
            bloom.highQualityFiltering.value = false;

            tonemapping.active = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            vignette.active = true;
            colorAdjustments.active = true;
            chromaticAberration.active = true;
        }

        private void ResolveCamera()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera == null)
            {
                _targetCamera = Object.FindFirstObjectByType<Camera>();
            }

            if (_targetCamera == null)
            {
                return;
            }

            _cameraData = _targetCamera.GetComponent<UniversalAdditionalCameraData>();
            if (_cameraData == null)
            {
                _cameraData = _targetCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            _cameraData.renderPostProcessing = true;

            LayerMask volumeMask = _cameraData.volumeLayerMask;
            volumeMask.value |= 1 << 0;
            _cameraData.volumeLayerMask = volumeMask;
        }

        private void CaptureOriginalSettings()
        {
            if (_capturedOriginalSettings)
            {
                return;
            }

            _originalSettings = new RenderSettingsSnapshot
            {
                FogEnabled = RenderSettings.fog,
                FogMode = RenderSettings.fogMode,
                FogColor = RenderSettings.fogColor,
                FogDensity = RenderSettings.fogDensity,
                FogStartDistance = RenderSettings.fogStartDistance,
                FogEndDistance = RenderSettings.fogEndDistance,
                AmbientMode = RenderSettings.ambientMode,
                AmbientIntensity = RenderSettings.ambientIntensity,
                AmbientSkyColor = RenderSettings.ambientSkyColor,
                AmbientEquatorColor = RenderSettings.ambientEquatorColor,
                AmbientGroundColor = RenderSettings.ambientGroundColor,
                ReflectionIntensity = RenderSettings.reflectionIntensity,
                Skybox = RenderSettings.skybox,
                SkyboxExposure = GetSkyboxExposure(RenderSettings.skybox),
                HasSkyboxExposure = HasSkyboxExposure(RenderSettings.skybox)
            };

            if (_originalSettings.Skybox != null)
            {
                _runtimeSkybox = Instantiate(_originalSettings.Skybox);
                _runtimeSkybox.hideFlags = HideFlags.HideAndDontSave;
                RenderSettings.skybox = _runtimeSkybox;
            }

            _capturedOriginalSettings = true;
        }

        private void RestoreOriginalSettings()
        {
            if (!_capturedOriginalSettings)
            {
                return;
            }

            RenderSettings.fog = _originalSettings.FogEnabled;
            RenderSettings.fogMode = _originalSettings.FogMode;
            RenderSettings.fogColor = _originalSettings.FogColor;
            RenderSettings.fogDensity = _originalSettings.FogDensity;
            RenderSettings.fogStartDistance = _originalSettings.FogStartDistance;
            RenderSettings.fogEndDistance = _originalSettings.FogEndDistance;
            RenderSettings.ambientMode = _originalSettings.AmbientMode;
            RenderSettings.ambientIntensity = _originalSettings.AmbientIntensity;
            RenderSettings.ambientSkyColor = _originalSettings.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = _originalSettings.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = _originalSettings.AmbientGroundColor;
            RenderSettings.reflectionIntensity = _originalSettings.ReflectionIntensity;
            RenderSettings.skybox = _originalSettings.Skybox;

            if (_runtimeSkybox != null)
            {
                Destroy(_runtimeSkybox);
                _runtimeSkybox = null;
            }
        }

        private void SetPhaseInternal(RenderMoodPhase phase, bool instant, bool fireEvent)
        {
            _activePhase = phase;
            _targetSnapshot = GetSnapshot(phase);

            if (instant)
            {
                _currentSnapshot = _targetSnapshot;
                ApplySnapshot(_currentSnapshot, true);
            }

            if (fireEvent)
            {
                EventManager.Instance.Fire(new OnRenderMoodPhaseChangedEvent
                {
                    Phase = phase,
                    IsManual = _manualOverride
                });
            }

            DynamicGI.UpdateEnvironment();
            _giRefreshTimer = 0.5f;
        }

        private void BlendTowardsTarget()
        {
            if (phaseBlendSpeed <= 0f)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-phaseBlendSpeed * Time.deltaTime);
            if (blend <= 0f)
            {
                return;
            }

            _currentSnapshot = RenderMoodSnapshot.Lerp(_currentSnapshot, _targetSnapshot, blend);
            ApplySnapshot(_currentSnapshot, false);
        }

        private void ApplySnapshot(RenderMoodSnapshot snapshot, bool applySkybox)
        {
            if (_runtimeProfile == null)
            {
                return;
            }

            if (_runtimeProfile.TryGet(out Bloom bloom))
            {
                bloom.threshold.value = snapshot.BloomThreshold;
                bloom.intensity.value = snapshot.BloomIntensity;
                bloom.scatter.value = snapshot.BloomScatter;
            }

            if (_runtimeProfile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping.mode.value = TonemappingMode.ACES;
            }

            if (_runtimeProfile.TryGet(out Vignette vignette))
            {
                vignette.intensity.value = snapshot.VignetteIntensity;
                vignette.smoothness.value = 0.4f;
            }

            if (_runtimeProfile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.postExposure.value = snapshot.PostExposure;
                colorAdjustments.contrast.value = snapshot.Contrast;
                colorAdjustments.saturation.value = snapshot.Saturation;
            }

            if (_runtimeProfile.TryGet(out ChromaticAberration chromaticAberration))
            {
                chromaticAberration.intensity.value = snapshot.ChromaticAberrationIntensity;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = snapshot.FogColor;
            RenderSettings.fogDensity = snapshot.FogDensity;
            RenderSettings.fogStartDistance = snapshot.FogStartDistance;
            RenderSettings.fogEndDistance = snapshot.FogEndDistance;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = snapshot.AmbientIntensity;
            RenderSettings.ambientSkyColor = snapshot.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = snapshot.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = snapshot.AmbientGroundColor;
            RenderSettings.reflectionIntensity = snapshot.ReflectionIntensity;

            ApplySkyboxExposure(snapshot.SkyboxExposure);
        }

        private void ApplySkyboxExposure(float exposure)
        {
            Material skybox = RenderSettings.skybox;
            if (skybox == null)
            {
                return;
            }

            if (skybox.HasProperty(SkyboxExposureId))
            {
                skybox.SetFloat(SkyboxExposureId, exposure);
            }
        }

        private void TickEnvironmentRefresh()
        {
            if (_giRefreshTimer > 0f)
            {
                _giRefreshTimer -= Time.deltaTime;
                if (_giRefreshTimer <= 0f)
                {
                    DynamicGI.UpdateEnvironment();
                }
            }
        }

        private RenderMoodPhase ResolveAutoPhase(float elapsed)
        {
            if (elapsed < introEndTime)
            {
                return RenderMoodPhase.Intro;
            }

            if (elapsed < combatEndTime)
            {
                return RenderMoodPhase.Combat;
            }

            if (elapsed < climaxEndTime)
            {
                return RenderMoodPhase.Climax;
            }

            if (elapsed < matchDuration)
            {
                return RenderMoodPhase.Elimination;
            }

            return RenderMoodPhase.Elimination;
        }

        private static bool HasSkyboxExposure(Material skybox)
        {
            return skybox != null && skybox.HasProperty(SkyboxExposureId);
        }

        private static float GetSkyboxExposure(Material skybox)
        {
            if (!HasSkyboxExposure(skybox))
            {
                return 1f;
            }

            return skybox.GetFloat(SkyboxExposureId);
        }

        private static T RequireComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
            {
                return component;
            }

            return profile.Add<T>(true);
        }

        private RenderMoodSnapshot GetSnapshot(RenderMoodPhase phase)
        {
            InitializePhaseSnapshots();

            switch (phase)
            {
                case RenderMoodPhase.Intro:
                    return _introPhaseSnapshot;

                case RenderMoodPhase.Combat:
                    return _combatPhaseSnapshot;

                case RenderMoodPhase.Climax:
                    return _climaxPhaseSnapshot;

                default:
                    return _eliminationPhaseSnapshot;
            }
        }

        internal static RenderMoodSnapshot CreateDefaultSnapshot(RenderMoodPhase phase)
        {
            switch (phase)
            {
                case RenderMoodPhase.Intro:
                    return new RenderMoodSnapshot
                    {
                        PostExposure = -0.18f,
                        Contrast = 8f,
                        Saturation = 10f,
                        BloomThreshold = 2.2f,
                        BloomIntensity = 0.32f,
                        BloomScatter = 0.58f,
                        VignetteIntensity = 0.12f,
                        ChromaticAberrationIntensity = 0f,
                        FogDensity = 0.010f,
                        FogStartDistance = 22f,
                        FogEndDistance = 60f,
                        FogColor = new Color(0.30f, 0.27f, 0.24f, 1f),
                        AmbientIntensity = 0.72f,
                        AmbientSkyColor = new Color(0.07f, 0.10f, 0.15f, 1f),
                        AmbientEquatorColor = new Color(0.16f, 0.14f, 0.12f, 1f),
                        AmbientGroundColor = new Color(0.08f, 0.07f, 0.06f, 1f),
                        ReflectionIntensity = 0.85f,
                        SkyboxExposure = 0.90f
                    };

                case RenderMoodPhase.Combat:
                    return new RenderMoodSnapshot
                    {
                        PostExposure = -0.32f,
                        Contrast = 12f,
                        Saturation = 12f,
                        BloomThreshold = 2.35f,
                        BloomIntensity = 0.26f,
                        BloomScatter = 0.56f,
                        VignetteIntensity = 0.18f,
                        ChromaticAberrationIntensity = 0.01f,
                        FogDensity = 0.015f,
                        FogStartDistance = 16f,
                        FogEndDistance = 42f,
                        FogColor = new Color(0.27f, 0.24f, 0.21f, 1f),
                        AmbientIntensity = 0.64f,
                        AmbientSkyColor = new Color(0.06f, 0.08f, 0.12f, 1f),
                        AmbientEquatorColor = new Color(0.14f, 0.12f, 0.10f, 1f),
                        AmbientGroundColor = new Color(0.07f, 0.06f, 0.05f, 1f),
                        ReflectionIntensity = 0.78f,
                        SkyboxExposure = 0.78f
                    };

                case RenderMoodPhase.Climax:
                    return new RenderMoodSnapshot
                    {
                        PostExposure = -0.50f,
                        Contrast = 16f,
                        Saturation = 8f,
                        BloomThreshold = 2.55f,
                        BloomIntensity = 0.18f,
                        BloomScatter = 0.50f,
                        VignetteIntensity = 0.24f,
                        ChromaticAberrationIntensity = 0.04f,
                        FogDensity = 0.022f,
                        FogStartDistance = 12f,
                        FogEndDistance = 28f,
                        FogColor = new Color(0.23f, 0.20f, 0.18f, 1f),
                        AmbientIntensity = 0.54f,
                        AmbientSkyColor = new Color(0.05f, 0.06f, 0.09f, 1f),
                        AmbientEquatorColor = new Color(0.11f, 0.10f, 0.08f, 1f),
                        AmbientGroundColor = new Color(0.05f, 0.04f, 0.04f, 1f),
                        ReflectionIntensity = 0.68f,
                        SkyboxExposure = 0.65f
                    };

                default:
                    return new RenderMoodSnapshot
                    {
                        PostExposure = -0.68f,
                        Contrast = 20f,
                        Saturation = 4f,
                        BloomThreshold = 2.75f,
                        BloomIntensity = 0.12f,
                        BloomScatter = 0.45f,
                        VignetteIntensity = 0.30f,
                        ChromaticAberrationIntensity = 0.06f,
                        FogDensity = 0.030f,
                        FogStartDistance = 8f,
                        FogEndDistance = 18f,
                        FogColor = new Color(0.18f, 0.16f, 0.14f, 1f),
                        AmbientIntensity = 0.42f,
                        AmbientSkyColor = new Color(0.03f, 0.04f, 0.06f, 1f),
                        AmbientEquatorColor = new Color(0.08f, 0.07f, 0.06f, 1f),
                        AmbientGroundColor = new Color(0.04f, 0.03f, 0.03f, 1f),
                        ReflectionIntensity = 0.55f,
                        SkyboxExposure = 0.52f
                    };
            }
        }

        private struct RenderSettingsSnapshot
        {
            public bool FogEnabled;
            public FogMode FogMode;
            public Color FogColor;
            public float FogDensity;
            public float FogStartDistance;
            public float FogEndDistance;
            public AmbientMode AmbientMode;
            public float AmbientIntensity;
            public Color AmbientSkyColor;
            public Color AmbientEquatorColor;
            public Color AmbientGroundColor;
            public float ReflectionIntensity;
            public Material Skybox;
            public float SkyboxExposure;
            public bool HasSkyboxExposure;
        }

        [System.Serializable]
        public struct RenderMoodSnapshot
        {
            public float PostExposure;
            public float Contrast;
            public float Saturation;
            public float BloomThreshold;
            public float BloomIntensity;
            public float BloomScatter;
            public float VignetteIntensity;
            public float ChromaticAberrationIntensity;
            public float FogDensity;
            public float FogStartDistance;
            public float FogEndDistance;
            public Color FogColor;
            public float AmbientIntensity;
            public Color AmbientSkyColor;
            public Color AmbientEquatorColor;
            public Color AmbientGroundColor;
            public float ReflectionIntensity;
            public float SkyboxExposure;

            public static RenderMoodSnapshot Lerp(RenderMoodSnapshot from, RenderMoodSnapshot to, float t)
            {
                t = Mathf.Clamp01(t);

                return new RenderMoodSnapshot
                {
                    PostExposure = Mathf.Lerp(from.PostExposure, to.PostExposure, t),
                    Contrast = Mathf.Lerp(from.Contrast, to.Contrast, t),
                    Saturation = Mathf.Lerp(from.Saturation, to.Saturation, t),
                    BloomThreshold = Mathf.Lerp(from.BloomThreshold, to.BloomThreshold, t),
                    BloomIntensity = Mathf.Lerp(from.BloomIntensity, to.BloomIntensity, t),
                    BloomScatter = Mathf.Lerp(from.BloomScatter, to.BloomScatter, t),
                    VignetteIntensity = Mathf.Lerp(from.VignetteIntensity, to.VignetteIntensity, t),
                    ChromaticAberrationIntensity = Mathf.Lerp(from.ChromaticAberrationIntensity, to.ChromaticAberrationIntensity, t),
                    FogDensity = Mathf.Lerp(from.FogDensity, to.FogDensity, t),
                    FogStartDistance = Mathf.Lerp(from.FogStartDistance, to.FogStartDistance, t),
                    FogEndDistance = Mathf.Lerp(from.FogEndDistance, to.FogEndDistance, t),
                    FogColor = Color.Lerp(from.FogColor, to.FogColor, t),
                    AmbientIntensity = Mathf.Lerp(from.AmbientIntensity, to.AmbientIntensity, t),
                    AmbientSkyColor = Color.Lerp(from.AmbientSkyColor, to.AmbientSkyColor, t),
                    AmbientEquatorColor = Color.Lerp(from.AmbientEquatorColor, to.AmbientEquatorColor, t),
                    AmbientGroundColor = Color.Lerp(from.AmbientGroundColor, to.AmbientGroundColor, t),
                    ReflectionIntensity = Mathf.Lerp(from.ReflectionIntensity, to.ReflectionIntensity, t),
                    SkyboxExposure = Mathf.Lerp(from.SkyboxExposure, to.SkyboxExposure, t)
                };
            }
        }
    }
}
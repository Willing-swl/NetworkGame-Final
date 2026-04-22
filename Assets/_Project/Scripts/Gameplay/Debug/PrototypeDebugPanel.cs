using NanoFrame.Core;
using NanoFrame.Event;
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Match;
using Project.Gameplay.Player;
using UnityEngine;

namespace Project.Gameplay.Debug
{
    public class PrototypeDebugPanel : MonoBehaviour
    {
        private Rect _windowRect = new Rect(20f, 20f, 420f, 620f);
        private bool _visible = true;
        private string _latestAbsorbMessage = "暂无吸收记录";
        private string _latestChargeMessage = "暂无取色记录";
        private string _latestShockwaveMessage = "暂无冲击波记录";
        private string _latestHitMessage = "暂无受击记录";
        private float _latestAbsorbMessageTime;
        private float _latestChargeMessageTime;
        private float _latestShockwaveMessageTime;
        private float _latestHitMessageTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<PrototypeDebugPanel>() != null)
            {
                return;
            }

            GameObject panelObject = new GameObject(nameof(PrototypeDebugPanel));
            panelObject.AddComponent<PrototypeDebugPanel>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                if (GUI.Button(new Rect(20f, 20f, 140f, 30f), "显示调试面板"))
                {
                    _visible = true;
                }

                DrawPlayerWorldMarkers();
                return;
            }

            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "原型调试面板");
            DrawPlayerWorldMarkers();
        }

        private void DrawWindow(int windowId)
        {
            PrototypeMatchManager matchManager = PrototypeMatchManager.Instance;
            PrototypeBalanceConfig settings = matchManager.Settings;
            PrototypeGridManager gridManager = PrototypeGridManager.Instance;

            if (settings == null)
            {
                GUILayout.Label("原型系统初始化中...");
                GUILayout.Label("等待回合配置完成。");
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 30f));
                return;
            }

            GUILayout.Label($"回合状态：{(matchManager.IsRoundActive ? "进行中" : matchManager.IsPaused ? "暂停中" : "已结束")}");
            GUILayout.Label($"剩余时间：{matchManager.RemainingTime:0.0} / {settings.RoundDuration:0.0}");
            GUILayout.Label($"P1 领地：{gridManager.Player1TerritoryCount}");
            GUILayout.Label($"P2 领地：{gridManager.Player2TerritoryCount}");
            GUILayout.Label($"最近吸收：{FormatRecentMessage(_latestAbsorbMessage, _latestAbsorbMessageTime)}");
            GUILayout.Label($"最近取色：{FormatRecentMessage(_latestChargeMessage, _latestChargeMessageTime)}");
            GUILayout.Label($"最近冲击波：{FormatRecentMessage(_latestShockwaveMessage, _latestShockwaveMessageTime)}");
            GUILayout.Label($"最近受击：{FormatRecentMessage(_latestHitMessage, _latestHitMessageTime)}");

            PrototypePlayerManager playerManager = PrototypePlayerManager.Instance;
            if (playerManager != null && playerManager.Players.Count >= 2)
            {
                GUILayout.Label($"P1 坐标：{playerManager.Players[0].transform.position}");
                GUILayout.Label($"P2 坐标：{playerManager.Players[1].transform.position}");
            }

            GUILayout.Space(8f);
            GUILayout.Label("按键说明");
            GUILayout.Label("P1：W/A/S/D 移动，F 喷射，Q 取色/冲击波，Space 闪避，Esc 暂停");
            GUILayout.Label("P2：方向键移动，RightCtrl 喷射，Keypad0 取色/冲击波，RightShift 闪避，KeypadEnter 暂停");

            GUILayout.Space(8f);
            GUILayout.Label("回合调参");

            DrawFloatSlider("回合时长", ref settings.RoundDuration, 20f, 120f);
            DrawFloatSlider("中立占领秒数", ref settings.NeutralCaptureSeconds, 0.05f, 1.0f);
            DrawFloatSlider("敌方占领秒数", ref settings.EnemyCaptureSeconds, 0.05f, 1.0f);
            DrawFloatSlider("移动速度", ref settings.MoveSpeed, 1f, 12f);
            DrawFloatSlider("喷射冷却", ref settings.SprayCooldown, 0.01f, 0.5f);
            DrawFloatSlider("蓄力时间", ref settings.ChargeHoldSeconds, 0.05f, 0.5f);
            DrawFloatSlider("冲击波冷却", ref settings.ShockwaveCooldown, 0.1f, 2f);
            DrawIntSlider("取色格数上限", ref settings.ShockwaveAbsorbTileLimit, 1, 8);
            DrawIntSlider("取色搜索半径", ref settings.ShockwaveAbsorbSearchRadius, 1, 5);
            DrawFloatSlider("冲击波半径", ref settings.ShockwaveRadiusBase, 0.5f, 3f);
            DrawFloatSlider("冲击波力道", ref settings.ShockwaveForceBase, 1f, 12f);
            DrawFloatSlider("闪避冷却", ref settings.DodgeCooldown, 0.05f, 2f);
            DrawFloatSlider("闪避速度", ref settings.DodgeSpeed, 1f, 20f);
            DrawFloatSlider("击退速度", ref settings.KnockbackSpeed, 1f, 20f);

            GUILayout.Space(8f);
            GUILayout.Label("输入 / 场地");
            DrawFloatSlider("死区", ref settings.DeadZone, 0.05f, 0.5f);
            DrawFloatSlider("场地边界", ref settings.ArenaEliminationMargin, 0.5f, 4f);
            DrawIntSlider("输入缓存帧数", ref settings.InputBufferSize, 1, 20);

            GUILayout.Space(8f);
            GUILayout.Label("操作");
            if (GUILayout.Button("重新开始"))
            {
                matchManager.RestartMatch();
            }

            if (GUILayout.Button(matchManager.IsPaused ? "继续" : "暂停"))
            {
                matchManager.TogglePause();
            }

            if (GUILayout.Button("刷新格子显示"))
            {
                gridManager.RefreshAllCellsVisuals();
            }

            if (GUILayout.Button("隐藏调试面板"))
            {
                _visible = false;
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 30f));
        }

        private void DrawFloatSlider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"{label}：{value:0.00}");
            float newValue = GUILayout.HorizontalSlider(value, min, max);

            if (!Mathf.Approximately(newValue, value))
            {
                value = newValue;
                EventManager.Instance.Fire(new OnDebugBalanceChangedEvent
                {
                    FieldName = label,
                    Value = value
                });
            }

            GUILayout.EndVertical();
        }

        private void DrawIntSlider(string label, ref int value, int min, int max)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"{label}：{value}");
            int newValue = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));

            if (newValue != value)
            {
                value = newValue;
                EventManager.Instance.Fire(new OnDebugBalanceChangedEvent
                {
                    FieldName = label,
                    Value = value
                });
            }

            GUILayout.EndVertical();
        }

        private void SubscribeEvents()
        {
            if (EventManager.Instance == null)
            {
                return;
            }

            EventManager.Instance.Subscribe<OnTileAbsorbedEvent>(HandleTileAbsorbed);
            EventManager.Instance.Subscribe<OnPlayerChargeStartedEvent>(HandlePlayerChargeStarted);
            EventManager.Instance.Subscribe<OnPlayerShockwaveEvent>(HandlePlayerShockwave);
            EventManager.Instance.Subscribe<OnPlayerHitEvent>(HandlePlayerHit);
        }

        private void UnsubscribeEvents()
        {
            if (EventManager.Instance == null)
            {
                return;
            }

            EventManager.Instance.Unsubscribe<OnTileAbsorbedEvent>(HandleTileAbsorbed);
            EventManager.Instance.Unsubscribe<OnPlayerChargeStartedEvent>(HandlePlayerChargeStarted);
            EventManager.Instance.Unsubscribe<OnPlayerShockwaveEvent>(HandlePlayerShockwave);
            EventManager.Instance.Unsubscribe<OnPlayerHitEvent>(HandlePlayerHit);
        }

        private void HandleTileAbsorbed(OnTileAbsorbedEvent eventData)
        {
            _latestAbsorbMessage = $"P{eventData.PlayerID} 吸收 Tile {eventData.TileID} @ {eventData.GridPosition}";
            _latestAbsorbMessageTime = Time.unscaledTime;
        }

        private void HandlePlayerChargeStarted(OnPlayerChargeStartedEvent eventData)
        {
            _latestChargeMessage = eventData.AbsorbedTileCount > 0
                ? $"P{eventData.PlayerID} 启动取色，吸收{eventData.AbsorbedTileCount}格，搜索半径{eventData.SearchRadius}"
                : $"P{eventData.PlayerID} 启动取色，但附近没有可吸收的己方格子（搜索半径{eventData.SearchRadius}）";
            _latestChargeMessageTime = Time.unscaledTime;
        }

        private void HandlePlayerShockwave(OnPlayerShockwaveEvent eventData)
        {
            _latestShockwaveMessage = $"P{eventData.PlayerID} 等级{eventData.ShockwaveTier} 吸收{eventData.AbsorbedTileCount} 半径{eventData.Radius:0.0} 力{eventData.Force:0.0}";
            _latestShockwaveMessageTime = Time.unscaledTime;
        }

        private void HandlePlayerHit(OnPlayerHitEvent eventData)
        {
            _latestHitMessage = $"P{eventData.AttackerPlayerID} -> P{eventData.DefenderPlayerID} 伤害{eventData.DamagePercent:0.0}% 力{eventData.Force.magnitude:0.0}";
            _latestHitMessageTime = Time.unscaledTime;
        }

        private static string FormatRecentMessage(string message, float messageTime)
        {
            if (Time.unscaledTime - messageTime > 4f)
            {
                return $"{message}（旧）";
            }

            return message;
        }

        private void DrawPlayerWorldMarkers()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            PrototypePlayerManager playerManager = PrototypePlayerManager.Instance;
            if (playerManager == null || playerManager.Players.Count == 0)
            {
                return;
            }

            GUIStyle labelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal =
                {
                    textColor = Color.white
                }
            };

            for (int index = 0; index < playerManager.Players.Count; index++)
            {
                PlayerController player = playerManager.Players[index];
                if (player == null)
                {
                    continue;
                }

                Vector3 worldPosition = player.transform.position + Vector3.up * 2f;
                Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
                if (screenPosition.z <= 0f)
                {
                    continue;
                }

                float guiX = screenPosition.x - 60f;
                float guiY = Screen.height - screenPosition.y - 30f;
                Rect markerRect = new Rect(guiX, guiY, 120f, 28f);

                Color previousColor = GUI.color;
                GUI.color = player.PlayerId == 1 ? new Color(0.2f, 0.8f, 1f, 0.95f) : new Color(1f, 0.4f, 0.3f, 0.95f);
                GUI.Box(markerRect, $"P{player.PlayerId}", labelStyle);
                GUI.color = previousColor;

                Rect detailRect = new Rect(guiX - 30f, guiY + 28f, 180f, 20f);
                GUI.Label(detailRect, $"{player.transform.position:0.00}");
            }
        }
    }
}
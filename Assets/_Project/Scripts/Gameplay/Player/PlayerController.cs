using System.Collections.Generic;
using NanoFrame.Event;
using NanoFrame.FSM;
using NanoFrame.Utility;
using Project.Gameplay.Config;
using Project.Gameplay.Grid;
using Project.Gameplay.Input;
using Project.Gameplay.Visuals;
using UnityEngine;

namespace Project.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        private enum PlayerStateId
        {
            Idle,
            Move,
            Jump,
            Spray,
            Dodge,
            Hurt,
            Knockback,
            Eliminated
        }

        private StateMachine<PlayerController> _stateMachine;
        private PrototypeBalanceConfig _settings;
        private Rigidbody _rigidbody;
        private Renderer _renderer;

        [Header("Inflatable Settings (充气悖论)")]
        [SerializeField] private float _baseScale = 1.0f;       
        [SerializeField] private float _maxScale = 1.5f;       
        [SerializeField] private float _inflationSpeed = 5.0f;  // 充气/放气的动画平滑速度
        private float _targetScale = 1.0f;

        [Header("Ammo System (弹药系统)")]
        [SerializeField] private float _maxAmmo = 100f;          
        [SerializeField] private float _sprayConsumePerShot = 20f;  
        [SerializeField] private float _ammoRecoverRate = 10f;   
        private float _currentAmmo = 100f; 

        private PlayerInputFrame _currentInput;
        private Vector3 _facingDirection = Vector3.forward;
        private Vector3 _knockbackDirection = Vector3.zero;
        private float _currentDeltaTime;
        private float _sprayCooldownRemaining;
        private float _shockwaveCooldownRemaining;
        private float _dodgeCooldownRemaining;
        private float _stateTimer;
        private float _damagePercent;
        private int _chargeAbsorbedTileCount;
        private bool _isEliminated;
        private bool _isInvulnerable;
        private int _playerId;
        private string _currentStateName = "Idle";
        private float _nextMovementDebugLogTime;

        [SerializeField] private bool _enableMovementDebugLog = true;
        [SerializeField] private float _movementDebugLogInterval = 0.35f;

        public int PlayerId => _playerId;
        public bool IsEliminated => _isEliminated;
        public bool HasMoveInput => _currentInput.HasMoveInput && _currentInput.Move.sqrMagnitude >= _settings.DeadZone * _settings.DeadZone;

        public bool CanSpray => _sprayCooldownRemaining <= 0f && _currentAmmo > 20f ;  //添加冷却逻辑
        public bool CanCharge => _shockwaveCooldownRemaining <= 0f;
        public bool CanDodge => _dodgeCooldownRemaining <= 0f;

        public float CurrentAmmo => _currentAmmo;
        public float MaxAmmo => _maxAmmo;

        public Vector2 CurrentMoveInput => _currentInput.Move;
        public Vector3 FacingDirection => _facingDirection;
        public string CurrentStateName => _currentStateName;

        public void Initialize(int playerId, PrototypeBalanceConfig settings, Color bodyColor)
        {
            _playerId = playerId;
            _settings = settings != null ? settings : ScriptableObject.CreateInstance<PrototypeBalanceConfig>();
            _rigidbody = GetComponent<Rigidbody>();
            _renderer = GetComponentInChildren<Renderer>();

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            //Material sharedMaterial = PrototypeMaterialFactory.GetSharedLitMaterial();
            // if (_renderer != null && sharedMaterial != null)
            // {
            //     _renderer.sharedMaterial = sharedMaterial;
            //     VisualUtility.SetInstancedColor(_renderer, bodyColor);
            // }

            _stateMachine = new StateMachine<PlayerController>(this);
            _stateMachine.AddState(new IdleState());
            _stateMachine.AddState(new MoveState());
            _stateMachine.AddState(new JumpState());
            _stateMachine.AddState(new SprayState());
            _stateMachine.AddState(new ChargeState());
            _stateMachine.AddState(new DodgeState());
            _stateMachine.AddState(new HurtState());
            _stateMachine.AddState(new KnockbackState());
            _stateMachine.AddState(new EliminatedState());
            _stateMachine.ChangeState<IdleState>();

            EventManager.Instance.Subscribe<TerritoryCountChangedEvent>(OnTerritoryCountChanged);
        }

        private void OnDestroy()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<TerritoryCountChangedEvent>(OnTerritoryCountChanged);
            }
        }

        public void ResetForMatch(Vector3 spawnPosition, Vector3 facingDirection)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            gameObject.SetActive(true);
            _isEliminated = false;
            _isInvulnerable = false;
            _damagePercent = 0f;
            _sprayCooldownRemaining = 0f;
            _shockwaveCooldownRemaining = 0f;
            _dodgeCooldownRemaining = 0f;
            _stateTimer = 0f;

            _currentAmmo = _maxAmmo; 

            _knockbackDirection = Vector3.zero;
            _chargeAbsorbedTileCount = 0;
            _currentInput = default;
            _facingDirection = NormalizeFacingDirection(facingDirection);
            _currentStateName = nameof(IdleState);
            _nextMovementDebugLogTime = 0f;

            _targetScale = _baseScale;
            transform.localScale = new Vector3(_baseScale, _baseScale, _baseScale);

            _rigidbody.position = spawnPosition;
            _rigidbody.rotation = Quaternion.LookRotation(_facingDirection, Vector3.up);

            _stateMachine?.ChangeState<IdleState>();
            LogDebug($"重置完成，出生点={FormatVector3(_rigidbody.position)} 朝向={FormatVector3(_facingDirection)}");
        }

        public void Tick(PlayerInputFrame input, float deltaTime)
        {
            if (_isEliminated)
            {
                return;
            }

            _currentInput = input;
            _currentDeltaTime = deltaTime;

            if (_enableMovementDebugLog && HasMoveInput && Time.unscaledTime >= _nextMovementDebugLogTime)
            {
                LogDebug($"输入 move={FormatVector2(_currentInput.Move)} 位置={FormatVector3(_rigidbody.position)} 状态={_currentStateName}");
                _nextMovementDebugLogTime = Time.unscaledTime + _movementDebugLogInterval;
            }

            if (_sprayCooldownRemaining > 0f)
            {
                _sprayCooldownRemaining = Mathf.Max(0f, _sprayCooldownRemaining - deltaTime);
            }

            if (_shockwaveCooldownRemaining > 0f)
            {
                _shockwaveCooldownRemaining = Mathf.Max(0f, _shockwaveCooldownRemaining - deltaTime);
            }

            if (_dodgeCooldownRemaining > 0f)
            {
                _dodgeCooldownRemaining = Mathf.Max(0f, _dodgeCooldownRemaining - deltaTime);
            }

            _stateMachine?.Update();

            if (Mathf.Abs(transform.localScale.x - _targetScale) > 0.001f)
            {
                float currentScale = Mathf.Lerp(transform.localScale.x, _targetScale, _currentDeltaTime * _inflationSpeed);
                transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            }

            if (_currentStateName != nameof(SprayState))
            {
                if (_currentAmmo < _maxAmmo)
                {
                    _currentAmmo = Mathf.Min(_maxAmmo, _currentAmmo + _ammoRecoverRate * _currentDeltaTime);

                    // 【测试用代码，测试完后可删】
                    if (Time.frameCount % 10 == 0)
                    {
                        Debug.Log($"[P{_playerId}] 停止喷漆，快速回蓝中... {_currentAmmo:0.0} / {_maxAmmo}");
                    }
                }
            }

            if (!_isEliminated && !PrototypeGridManager.Instance.IsInsideArena(_rigidbody.position))
            {
                Eliminate();
            }
        }

        public void ApplyHit(Vector3 force, int attackerPlayerId, float damagePercent)
        {
            if (_isEliminated || _isInvulnerable)
            {
                return;
            }

            _damagePercent += Mathf.Max(0f, damagePercent);
            _knockbackDirection = force.sqrMagnitude > 0.0001f ? force.normalized : -_facingDirection;

            EventManager.Instance.Fire(new OnPlayerHitEvent
            {
                AttackerPlayerID = attackerPlayerId,
                DefenderPlayerID = _playerId,
                DamagePercent = _damagePercent,
                Force = force
            });

            LogDebug($"受击，攻击者=P{attackerPlayerId} 伤害={_damagePercent:0.0}% 当前位置={FormatVector3(_rigidbody.position)} 力={FormatVector3(force)}");

            _stateMachine?.ChangeState<HurtState>();
        }

        public void Eliminate()
        {
            if (_isEliminated)
            {
                return;
            }

            LogDebug($"淘汰，位置={FormatVector3(_rigidbody.position)}");

            _stateMachine?.ChangeState<EliminatedState>();
        }

        private void TriggerStateChanged(string stateName)
        {
            _currentStateName = stateName;
            EventManager.Instance.Fire(new OnPlayerStateChangedEvent
            {
                PlayerID = _playerId,
                StateName = stateName
            });

            LogDebug($"状态切换 -> {stateName} 位置={FormatVector3(_rigidbody.position)}");
        }

        private void OnTerritoryCountChanged(TerritoryCountChangedEvent evt)
        {
            if (evt.PlayerId != this._playerId) return;
            if (evt.TotalTileCount <= 0) return;

            float territoryPercentage = (float)evt.MyTileCount / evt.TotalTileCount;
            float scaleIncrease = territoryPercentage * 0.5f;
            _targetScale = Mathf.Clamp(_baseScale + scaleIncrease, _baseScale, _maxScale);
        }

        private Vector3 GetMoveDirection()
        {
            Vector3 direction = new Vector3(_currentInput.Move.x, 0f, _currentInput.Move.y);
            if (direction.sqrMagnitude < _settings.DeadZone * _settings.DeadZone)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private Vector3 GetPreferredDodgeDirection()
        {
            Vector3 moveDirection = GetMoveDirection();
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                return moveDirection;
            }

            return _facingDirection.sqrMagnitude > 0.0001f ? _facingDirection : (_playerId == 1 ? Vector3.right : Vector3.left);
        }

        private void ApplyMovement(Vector3 direction, float speed)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flattened.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 previousPosition = _rigidbody.position;
            flattened.Normalize();
            _facingDirection = flattened;

            Quaternion targetRotation = Quaternion.LookRotation(_facingDirection, Vector3.up);
            _rigidbody.MoveRotation(Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, _settings.RotationSpeed * _currentDeltaTime));
            _rigidbody.MovePosition(_rigidbody.position + flattened * speed * _currentDeltaTime);

            if (_enableMovementDebugLog && Time.unscaledTime >= _nextMovementDebugLogTime)
            {
                Vector3 currentPosition = _rigidbody.position;
                if ((currentPosition - previousPosition).sqrMagnitude > 0.000001f)
                {
                    LogDebug($"移动 {FormatVector3(previousPosition)} -> {FormatVector3(currentPosition)} 方向={FormatVector3(_facingDirection)} 速度={speed:0.00}");
                    _nextMovementDebugLogTime = Time.unscaledTime + _movementDebugLogInterval;
                }
            }
        }

        private void ExecuteSpray()
        {
            if (!CanSpray)
            {
                return;
            }

            _currentAmmo = Mathf.Max(0f, _currentAmmo - _sprayConsumePerShot);
            Debug.Log($"[P{_playerId}] 喷射了一次！消耗20%，剩余弹药: {_currentAmmo}");

            _sprayCooldownRemaining = _settings.SprayCooldown;
            Vector3 sprayDirection = _facingDirection;
            Vector3 sprayOrigin = _rigidbody.position;

            PrototypeGridManager.Instance.TryApplySpray(_playerId, sprayOrigin, sprayDirection, 10f);

            EventManager.Instance.Fire(new OnPlayerSprayEvent
            {
                PlayerID = _playerId,
                Origin = sprayOrigin,
                Direction = sprayDirection
            });
        }

        private void BeginDodge()
        {
            if (!CanDodge)
            {
                return;
            }

            _dodgeCooldownRemaining = _settings.DodgeCooldown;
            _stateTimer = _settings.DodgeDuration;
            _isInvulnerable = true;
            _facingDirection = GetPreferredDodgeDirection();
            _rigidbody.rotation = Quaternion.LookRotation(_facingDirection, Vector3.up);
            LogDebug($"闪避开始，方向={FormatVector3(_facingDirection)} 位置={FormatVector3(_rigidbody.position)}");
        }

        private void ReleaseShockwave()
        {
            if (_chargeAbsorbedTileCount <= 0)
            {
                LogDebug("未吸收到己方格子，无法释放冲击波！");
                _chargeAbsorbedTileCount = 0;
                FinishActionState();
                return;
            }

            // 直接通过公式计算威力，而不是按段位！
            // 每吸取1格，威力就平滑增加，无缝过渡！
            float tileStep = Mathf.Max(0.1f, _settings.TileSize + _settings.TileGap);
            float powerRatio = Mathf.Clamp01((float)_chargeAbsorbedTileCount / _settings.ShockwaveAbsorbTileLimit);
            
            float radius = tileStep * Mathf.Lerp(_settings.ShockwaveRadiusBase, _settings.ShockwaveRadiusBase + 2f * _settings.ShockwaveRadiusTierStep, powerRatio);
            float force = Mathf.Lerp(_settings.ShockwaveForceBase, _settings.ShockwaveForceBase + 2f * _settings.ShockwaveForceTierStep, powerRatio);
            float damagePercent = Mathf.Lerp(_settings.ShockwaveDamageBase, _settings.ShockwaveDamageBase + 2f * _settings.ShockwaveDamageTierStep, powerRatio);

            // 保持一个事件格式，将Tier强行设为一个动态浮点等级供特效判断
            int shockwaveTier = 1 + Mathf.FloorToInt(powerRatio * 2f);

            PrototypePlayerManager playerManager = PrototypePlayerManager.Instance;
            if (playerManager != null)
            {
                IReadOnlyList<PlayerController> players = playerManager.Players;
                if (players != null)
                {
                    for (int index = 0; index < players.Count; index++)
                    {
                        PlayerController target = players[index];
                        if (target == null || target == this || target.IsEliminated)
                        {
                            continue;
                        }

                        Vector3 offset = target.transform.position - _rigidbody.position;
                        offset.y = 0f;
                        float distance = offset.magnitude;
                        if (distance > radius)
                        {
                            continue;
                        }

                        Vector3 forceDirection = distance > 0.0001f ? offset.normalized : _facingDirection;
                        float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, radius));
                        float appliedForce = force * Mathf.Lerp(0.65f, 1f, falloff);
                        float appliedDamage = damagePercent * Mathf.Lerp(0.75f, 1f, falloff);
                        target.ApplyHit(forceDirection * appliedForce, _playerId, appliedDamage);
                    }
                }
            }

            EventManager.Instance.Fire(new OnPlayerShockwaveEvent
            {
                PlayerID = _playerId,
                Origin = _rigidbody.position,
                AbsorbedTileCount = _chargeAbsorbedTileCount,
                ShockwaveTier = shockwaveTier,
                Radius = radius,
                Force = force,
                DamagePercent = damagePercent
            });

            LogDebug($"冲击波释放，平滑等级={shockwaveTier} 吸收格子={_chargeAbsorbedTileCount} 半径={radius:0.00} 力={force:0.00}");
            _chargeAbsorbedTileCount = 0;
            FinishActionState();
        }

        private void ApplyDodgeMovement()
        {
            ApplyMovement(_facingDirection, _settings.DodgeSpeed);
        }

        private void ApplyKnockbackMovement()
        {
            float strength = _settings.KnockbackSpeed * (1f + Mathf.Clamp01(_damagePercent / 100f));
            ApplyMovement(_knockbackDirection, strength);
        }

        private void LogDebug(string message)
        {
            if (!_enableMovementDebugLog)
            {
                return;
            }

            UnityEngine.Debug.Log($"[P{_playerId}] {message}", this);
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:0.00}, {value.y:0.00})";
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
        }

        private void FinishActionState()
        {
            // 跳跃优先级最高（防止取色释放后因 ChargeHeld 仍为 true 而错过跳跃）
            if (_currentInput.JumpPressed)
            {
                _stateMachine.ChangeState<JumpState>();
                return;
            }

            if (_currentInput.ChargeHeld && CanCharge)
            {
                _stateMachine.ChangeState<ChargeState>();
                return;
            }

            if (_currentInput.DodgePressed && CanDodge)
            {
                _stateMachine.ChangeState<DodgeState>();
                return;
            }

            if (HasMoveInput)
            {
                _stateMachine.ChangeState<MoveState>();
                return;
            }

            _stateMachine.ChangeState<IdleState>();
        }

        private Vector3 NormalizeFacingDirection(Vector3 direction)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flattened.sqrMagnitude < 0.0001f)
            {
                return _playerId == 1 ? Vector3.right : Vector3.left;
            }

            return flattened.normalized;
        }

        private abstract class PlayerStateBase : IState<PlayerController>
        {
            public virtual void OnEnter(PlayerController owner)
            {
            }

            public virtual void OnUpdate(PlayerController owner)
            {
            }

            public virtual void OnExit(PlayerController owner)
            {
            }
        }

        private sealed class IdleState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = false;
                owner.TriggerStateChanged(nameof(IdleState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                if (owner._currentInput.JumpPressed)
                {
                    owner._stateMachine.ChangeState<JumpState>();
                    return;
                }

                if (owner._currentInput.HasChargeInput && owner.CanCharge)
                {
                    owner._stateMachine.ChangeState<ChargeState>();
                    return;
                }

                if (owner._currentInput.DodgePressed && owner.CanDodge)
                {
                    owner._stateMachine.ChangeState<DodgeState>();
                    return;
                }

                if (owner._currentInput.SprayHeld && owner.CanSpray)
                {
                    owner._stateMachine.ChangeState<SprayState>();
                    return;
                }

                if (owner.HasMoveInput)
                {
                    owner._stateMachine.ChangeState<MoveState>();
                }
            }
        }

        private sealed class MoveState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = false;
                owner.TriggerStateChanged(nameof(MoveState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                if (owner._currentInput.JumpPressed)
                {
                    owner._stateMachine.ChangeState<JumpState>();
                    return;
                }

                if (owner._currentInput.HasChargeInput && owner.CanCharge)
                {
                    owner._stateMachine.ChangeState<ChargeState>();
                    return;
                }

                if (owner._currentInput.DodgePressed && owner.CanDodge)
                {
                    owner._stateMachine.ChangeState<DodgeState>();
                    return;
                }

                if (owner._currentInput.SprayHeld && owner.CanSpray)
                {
                    owner._stateMachine.ChangeState<SprayState>();
                    return;
                }

                Vector3 moveDirection = owner.GetMoveDirection();
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    owner.ApplyMovement(moveDirection, owner._settings.MoveSpeed);
                    return;
                }

                owner._stateMachine.ChangeState<IdleState>();
            }
        }

        private sealed class JumpState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = false;
                owner._stateTimer = 0.18f;
                owner.TriggerStateChanged(nameof(JumpState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);

                if (owner._stateTimer > 0f)
                {
                    return;
                }

                owner.FinishActionState();
            }

            public override void OnExit(PlayerController owner)
            {
                owner._stateTimer = 0f;
            }
        }

        private sealed class SprayState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = false;
                owner._stateTimer = 0.05f;
                owner.TriggerStateChanged(nameof(SprayState));
                owner.ExecuteSpray();
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);

                if (owner._currentInput.HasChargeInput && owner.CanCharge)
                {
                    owner._stateMachine.ChangeState<ChargeState>();
                    return;
                }

                if (owner._currentInput.DodgePressed && owner.CanDodge)
                {
                    owner._stateMachine.ChangeState<DodgeState>();
                    return;
                }

                if (owner._currentAmmo <= 20f)
                {
                    owner.FinishActionState();
                    return;
                }

                if (owner._currentInput.SprayHeld && owner.CanSpray)
                {
                    owner.ExecuteSpray();
                }

                if (owner._currentInput.SprayHeld)
                {
                    return;
                }

                if (owner._stateTimer > 0f)
                {
                    return;
                }

                owner.FinishActionState();
            }
        }

        private sealed class ChargeState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = false;
                owner._stateTimer = owner._settings.ChargeHoldSeconds;
                owner._chargeAbsorbedTileCount = PrototypeGridManager.Instance != null
                    ? PrototypeGridManager.Instance.AbsorbTilesForShockwave(owner._playerId, owner._rigidbody.position, owner._settings.ShockwaveAbsorbTileLimit)
                    : 0;
                owner._shockwaveCooldownRemaining = owner._settings.ShockwaveCooldown;
                owner.TriggerStateChanged(nameof(ChargeState));
                owner.LogDebug($"取色蓄力，吸收格子={owner._chargeAbsorbedTileCount} 搜索半径={owner._settings.ShockwaveAbsorbSearchRadius}");
                EventManager.Instance.Fire(new OnPlayerChargeStartedEvent
                {
                    PlayerID = owner._playerId,
                    AbsorbedTileCount = owner._chargeAbsorbedTileCount,
                    SearchRadius = owner._settings.ShockwaveAbsorbSearchRadius
                });
                if (owner._chargeAbsorbedTileCount <= 0)
                {
                    owner.LogDebug($"取色失败：附近没有可吸收的己方格子（搜索半径={owner._settings.ShockwaveAbsorbSearchRadius}）");
                }
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);

                if (owner._currentInput.ChargeHeld && owner._stateTimer > 0f)
                {
                    return;
                }

                owner.ReleaseShockwave();
            }

            public override void OnExit(PlayerController owner)
            {
                owner._chargeAbsorbedTileCount = 0;
                owner._stateTimer = 0f;
            }
        }

        private sealed class DodgeState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner.BeginDodge();
                owner.TriggerStateChanged(nameof(DodgeState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);
                owner.ApplyDodgeMovement();

                if (owner._stateTimer > 0f)
                {
                    return;
                }

                owner._isInvulnerable = false;
                owner.FinishActionState();
            }

            public override void OnExit(PlayerController owner)
            {
                owner._isInvulnerable = false;
            }
        }

        private sealed class HurtState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = true;
                owner._stateTimer = 0.08f;
                owner.TriggerStateChanged(nameof(HurtState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);
                if (owner._stateTimer > 0f)
                {
                    return;
                }

                owner._stateMachine.ChangeState<KnockbackState>();
            }
        }

        private sealed class KnockbackState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isInvulnerable = true;
                owner._stateTimer = Mathf.Lerp(0.12f, 0.3f, Mathf.Clamp01(owner._damagePercent / 100f));
                owner.TriggerStateChanged(nameof(KnockbackState));
            }

            public override void OnUpdate(PlayerController owner)
            {
                owner._stateTimer = Mathf.Max(0f, owner._stateTimer - owner._currentDeltaTime);
                owner.ApplyKnockbackMovement();

                if (owner._stateTimer > 0f)
                {
                    return;
                }

                owner._damagePercent = Mathf.Clamp(owner._damagePercent * 0.85f, 0f, 100f);
                owner._isInvulnerable = false;
                owner.FinishActionState();
            }

            public override void OnExit(PlayerController owner)
            {
                owner._isInvulnerable = false;
            }
        }

        private sealed class EliminatedState : PlayerStateBase
        {
            public override void OnEnter(PlayerController owner)
            {
                owner._isEliminated = true;
                owner._isInvulnerable = true;
                owner.TriggerStateChanged(nameof(EliminatedState));
                EventManager.Instance.Fire(new OnPlayerDieEvent
                {
                    PlayerID = owner._playerId
                });
            }
        }
    }
}
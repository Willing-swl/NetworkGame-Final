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

        private PlayerInputFrame _currentInput;
        private Vector3 _facingDirection = Vector3.forward;
        private Vector3 _knockbackDirection = Vector3.zero;
        private float _currentDeltaTime;
        private float _sprayCooldownRemaining;
        private float _dodgeCooldownRemaining;
        private float _stateTimer;
        private float _damagePercent;
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
        public bool CanSpray => _sprayCooldownRemaining <= 0f;
        public bool CanDodge => _dodgeCooldownRemaining <= 0f;

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

            Material sharedMaterial = PrototypeMaterialFactory.GetSharedLitMaterial();
            if (_renderer != null && sharedMaterial != null)
            {
                _renderer.sharedMaterial = sharedMaterial;
                VisualUtility.SetInstancedColor(_renderer, bodyColor);
            }

            _stateMachine = new StateMachine<PlayerController>(this);
            _stateMachine.AddState(new IdleState());
            _stateMachine.AddState(new MoveState());
            _stateMachine.AddState(new SprayState());
            _stateMachine.AddState(new DodgeState());
            _stateMachine.AddState(new HurtState());
            _stateMachine.AddState(new KnockbackState());
            _stateMachine.AddState(new EliminatedState());
            _stateMachine.ChangeState<IdleState>();
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
            _dodgeCooldownRemaining = 0f;
            _stateTimer = 0f;
            _knockbackDirection = Vector3.zero;
            _currentInput = default;
            _facingDirection = NormalizeFacingDirection(facingDirection);
            _currentStateName = nameof(IdleState);
            _nextMovementDebugLogTime = 0f;

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

            if (_dodgeCooldownRemaining > 0f)
            {
                _dodgeCooldownRemaining = Mathf.Max(0f, _dodgeCooldownRemaining - deltaTime);
            }

            _stateMachine?.Update();

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

            _sprayCooldownRemaining = _settings.SprayCooldown;
            Vector3 sprayDirection = _facingDirection;
            Vector3 sprayOrigin = _rigidbody.position;

            PrototypeGridManager.Instance.TryApplySpray(_playerId, sprayOrigin, sprayDirection, _currentDeltaTime);

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

                if (owner._currentInput.DodgePressed && owner.CanDodge)
                {
                    owner._stateMachine.ChangeState<DodgeState>();
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
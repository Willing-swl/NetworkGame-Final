using System.Collections;
using UnityEngine;

namespace Project.Gameplay.Player
{
    /// <summary>
    /// 动画桥接器：每帧直接轮询 PlayerController 的状态名，
    /// 不依赖 EventManager 订阅，彻底规避初始化时序问题。
    /// 由 PrototypePlayerManager 动态 AddComponent 到角色 Prefab 根节点。
    /// 注意：故意不写 [RequireComponent(typeof(Animator))]，
    ///       因为 Animator 在子节点（骨骼 Mesh）上，
    ///       [RequireComponent] 会在根节点生成幽灵 Animator 导致断路。
    /// </summary>
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        
        [SerializeField] private PlayerController controller;

        private Animator _animator;

        // 上一帧的状态名，用于检测状态变化
        private string _previousStateName = string.Empty;

        // Animator 参数 Hash（一次性计算，避免每帧字符串查找）
        private static readonly int HashInputX     = Animator.StringToHash("InputX");
        private static readonly int HashInputY     = Animator.StringToHash("InputY");
        private static readonly int HashShoot      = Animator.StringToHash("Shoot");
        private static readonly int HashRoll        = Animator.StringToHash("Roll");
        private static readonly int HashJump        = Animator.StringToHash("Jump");
        private static readonly int HashHit         = Animator.StringToHash("Hit");
        private static readonly int HashKnockdown   = Animator.StringToHash("Knockdown");
        private static readonly int HashDie         = Animator.StringToHash("Die");
        private static readonly int HashPowerUp     = Animator.StringToHash("PowerUp");
        private static readonly int HashIsGrounded  = Animator.StringToHash("IsGrounded");
        private static readonly int HashIsBattle    = Animator.StringToHash("IsBattle");

        // ── 生命周期 ─────────────────────────────────────────────────

        private void Start()
        {
            // Start() 在 Awake()/OnEnable() 都结束后才执行，
            // 此时 PrototypePlayerManager.CreatePlayer() 一定已经跑完，
            // PlayerController.Initialize() 也已经调用过，可以安全查找。
            FindReferences();
        }

        private void LateUpdate()
        {
            // 懒初始化兜底（极少数情况下 Start 还没跑到）
            if (_animator == null || controller == null)
            {
                FindReferences();
                return;
            }

            // 1. 更新移动混合树（Locomotion Blend Tree）
            UpdateMovementBlend();

            // 2. 检测状态变化并驱动 Trigger
            string currentStateName = controller.CurrentStateName;
            if (currentStateName != _previousStateName)
            {
                OnStateChanged(_previousStateName, currentStateName);
                _previousStateName = currentStateName;
            }
        }

        // ── 内部方法 ─────────────────────────────────────────────────

        private void FindReferences()
        {
            // —— 找 PlayerController ——
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
                if (controller == null)
                {
                    // 不打日志：可能还在初始化中，下帧再试
                    return;
                }
            }

            // —— 找 Animator（跳过无 Controller 的幽灵 Animator）——
            if (_animator == null)
            {
                Animator[] candidates = GetComponentsInChildren<Animator>(true);
                foreach (Animator candidate in candidates)
                {
                    if (candidate.runtimeAnimatorController != null)
                    {
                        _animator = candidate;
                        break;
                    }
                }

                if (_animator == null)
                {
                    Debug.LogError(
                        $"[PlayerAnimatorBridge] P{controller.PlayerId}：" +
                        $"在 {gameObject.name} 及其子节点中找不到带 AnimatorController 的 Animator！" +
                        $"请检查 Prefab 子节点是否挂载了 Animator 并赋了 Controller。");
                    return;
                }

                _animator.applyRootMotion = false;
                _animator.SetBool(HashIsGrounded, true);
                _animator.SetBool(HashIsBattle, true);   // 对战模式始终为 true

                // 输出所有可用参数，方便对照 Animator Controller 错误
                var parameters = _animator.parameters;
                var paramNames = new System.Text.StringBuilder();
                foreach (var p in parameters) paramNames.Append($"{p.name}({p.type}) ");
                Debug.Log($"[PlayerAnimatorBridge] P{controller.PlayerId} Animator 参数列表：{paramNames}");

                _previousStateName = controller.CurrentStateName;
                Debug.Log($"[PlayerAnimatorBridge] P{controller.PlayerId} 初始状态={_previousStateName}");
            }
        }

        private void UpdateMovementBlend()
        {
            Vector2 move = controller.CurrentMoveInput;
            Vector3 worldMove = new Vector3(move.x, 0f, move.y);
            // 世界坐标 → 角色局部坐标，防止滑步
            Vector3 localMove = transform.InverseTransformDirection(worldMove);

            _animator.SetFloat(HashInputX, localMove.x, 0.1f, Time.deltaTime);
            _animator.SetFloat(HashInputY, localMove.z, 0.1f, Time.deltaTime);
        }

        /// <summary>
        /// 每当检测到 FSM 状态名变化时调用，
        /// 将新状态名翻译成 Animator Trigger / Bool。
        /// </summary>
        private void OnStateChanged(string from, string to)
        {
            // 输出状态变化日志，方便对照 FSM 和 Animator
            Debug.Log($"[PlayerAnimatorBridge] P{controller?.PlayerId} 状态变化: [{from}] → [{to}]");

            switch (to)
            {
                case "IdleState":
                case "MoveState":
                    // 移动和待机由 InputX/InputY 的混合树驱动，不需要额外 Trigger
                    break;

                case "JumpState":
                    _animator.SetTrigger(HashJump);
                    _animator.SetBool(HashIsGrounded, false);
                    StopAllCoroutines();
                    StartCoroutine(SimulateLanding(0.18f));
                    break;

                case "SprayState":
                    _animator.SetTrigger(HashShoot);
                    break;

                case "ChargeState":
                    _animator.SetTrigger(HashPowerUp);
                    break;

                case "DodgeState":
                    _animator.SetTrigger(HashRoll);
                    break;

                case "HurtState":
                    _animator.SetTrigger(HashHit);
                    break;

                case "KnockbackState":
                    _animator.SetTrigger(HashKnockdown);
                    break;

                case "EliminatedState":
                    _animator.SetTrigger(HashDie);
                    break;

                default:
                    Debug.LogWarning($"[PlayerAnimatorBridge] P{controller.PlayerId}：未处理的状态 [{to}]");
                    break;
            }
        }

        private IEnumerator SimulateLanding(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_animator != null)
            {
                _animator.SetBool(HashIsGrounded, true);
            }
        }
    }
}
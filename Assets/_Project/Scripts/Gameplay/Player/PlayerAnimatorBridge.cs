using NanoFrame.Event;
using UnityEngine;

namespace Project.Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;

        private Animator animator;
        private EventManager eventManager;

        private readonly int hashInputX = Animator.StringToHash("InputX");
        private readonly int hashInputY = Animator.StringToHash("InputY");
        private readonly int hashShoot = Animator.StringToHash("Shoot");
        private readonly int hashReload = Animator.StringToHash("Reload");
        private readonly int hashRoll = Animator.StringToHash("Roll");
        private readonly int hashJump = Animator.StringToHash("Jump");
        private readonly int hashHit = Animator.StringToHash("Hit");
        private readonly int hashKnockdown = Animator.StringToHash("Knockdown");
        private readonly int hashDie = Animator.StringToHash("Die");
        private readonly int hashPowerUp = Animator.StringToHash("PowerUp");
        private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            eventManager = EventManager.Instance;
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.SetBool(hashIsGrounded, true);
            }
        }

        private void OnEnable()
        {
            if (eventManager != null)
            {
                eventManager.Subscribe<OnPlayerStateChangedEvent>(HandlePlayerStateChanged);
            }
        }

        private void OnDisable()
        {
            if (eventManager != null)
            {
                eventManager.Unsubscribe<OnPlayerStateChangedEvent>(HandlePlayerStateChanged);
            }

            StopCoroutine(nameof(SimulateLanding));
        }

        private void LateUpdate()
        {
            if (controller == null || animator == null)
            {
                return;
            }

            Vector2 move = controller.CurrentMoveInput;
            animator.SetFloat(hashInputX, move.x, 0.1f, Time.deltaTime);
            animator.SetFloat(hashInputY, move.y, 0.1f, Time.deltaTime);
        }

        private void HandlePlayerStateChanged(OnPlayerStateChangedEvent eventData)
        {
            if (controller == null || eventData.PlayerID != controller.PlayerId)
            {
                return;
            }

            switch (eventData.StateName)
            {
                case "IdleState":
                    break;

                case "MoveState":
                    break;

                case "JumpState":
                    animator.SetTrigger(hashJump);
                    animator.SetBool(hashIsGrounded, false);
                    StopCoroutine(nameof(SimulateLanding));
                    StartCoroutine(SimulateLanding(0.18f));
                    break;

                case "SprayState":
                    animator.SetTrigger(hashShoot);
                    break;

                case "ChargeState":
                    animator.SetTrigger(hashPowerUp);
                    break;

                case "DodgeState":
                    animator.SetTrigger(hashRoll);
                    break;

                case "HurtState":
                    animator.SetTrigger(hashHit);
                    break;

                case "KnockbackState":
                    animator.SetTrigger(hashKnockdown);
                    break;

                case "EliminatedState":
                    animator.SetTrigger(hashDie);
                    break;
            }
        }

        private System.Collections.IEnumerator SimulateLanding(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (animator != null)
            {
                animator.SetBool(hashIsGrounded, true);
            }
        }
    }
}
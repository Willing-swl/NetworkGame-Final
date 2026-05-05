using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerRoamingController : MonoBehaviour
{
    [Header("移動控制")]
    public InputActionReference moveAction;
    public InputActionReference toggleWeaponAction;
    public InputActionReference reloadAction;
    public InputActionReference jumpAction;
    public InputActionReference dodgeAction;
    public InputActionReference fireAction; 

    [Header("�ƶ�����")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("״̬")]
    public bool isBattleMode = true;

    private Animator animator;
    private Vector2 currentMoveInput;
    private Vector3 moveDirection;

    // Animator Hash
    private readonly int hashInputX = Animator.StringToHash("InputX");
    private readonly int hashInputY = Animator.StringToHash("InputY");
    private readonly int hashIsBattle = Animator.StringToHash("IsBattle");
    private readonly int hashShoot = Animator.StringToHash("Shoot");
    private readonly int hashReload = Animator.StringToHash("Reload");
    private readonly int hashRoll = Animator.StringToHash("Roll");
    private readonly int hashJump = Animator.StringToHash("Jump");
    private readonly int hashIsGrounded = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        animator = GetComponent<Animator>();
        // ע�ᰴ���ص�
        toggleWeaponAction.action.performed += _ => OnToggleWeapon();
        reloadAction.action.performed += _ => OnReload();// ����װ�
        jumpAction.action.performed += _ => OnJump();// ��Ծ
        dodgeAction.action.performed += _ => OnDodge();// ����
        fireAction.action.performed += _ => OnFire(); //����
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        toggleWeaponAction.action.Enable();
        reloadAction.action.Enable();
        jumpAction.action.Enable();
        dodgeAction.action.Enable();
        fireAction.action.Enable(); 
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        toggleWeaponAction.action.Disable();
        reloadAction.action.Disable();
        jumpAction.action.Disable();
        dodgeAction.action.Disable();
        fireAction.action.Disable(); 
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        currentMoveInput = moveAction.action.ReadValue<Vector2>();
        
        animator.SetFloat(hashInputX, currentMoveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat(hashInputY, currentMoveInput.y, 0.1f, Time.deltaTime);

        moveDirection = new Vector3(currentMoveInput.x, 0, currentMoveInput.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

 
    private void OnToggleWeapon()
    {
        isBattleMode = !isBattleMode;
        animator.SetBool(hashIsBattle, isBattleMode);
    }

    private void OnReload()
    {
        if (isBattleMode) animator.SetTrigger(hashReload);
    }

    private void OnJump()
    {
        animator.SetTrigger(hashJump);
        // Ϊ�˷�ֹ��Ծ����������ǰ���ᵽ�����ñ����߼�
        animator.SetBool(hashIsGrounded, false);
        StopCoroutine(nameof(SimulateLanding));
        StartCoroutine(SimulateLanding(0.1f));
    }

    private void OnDodge()
    {
        animator.SetTrigger(hashRoll);
    }

    private void OnFire()
    {
        if (isBattleMode)
        {
            animator.SetTrigger(hashShoot);
        }
    }

    // ����������Э�̣������Ծʹ�ã�
    private System.Collections.IEnumerator SimulateLanding(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool(hashIsGrounded, true);
    }
}
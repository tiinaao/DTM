using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;
    [SerializeField] private float sneakMultiplier = 0.4f;
    [SerializeField] private float gravityMultiplayer = 2.0f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Health health;
    [SerializeField] private AnimReach animReach;

    [SerializeField] private bool flipSpriteForLeft = true;
    [SerializeField] private float directionDeadzone = 0.1f;

    [Header("Conversation Pose")]
    [SerializeField] private string sideState = "walk_r";
    [SerializeField] private string towardCameraState = "walk_f";
    [SerializeField] private string awayFromCameraState = "walk_b";
    [SerializeField] private string idleState = "idle";

    private Vector3 currentMovement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool conversing;

    private bool IsSneaking => playerInputHandler != null && playerInputHandler.CrouchTriggered;
    private bool SprintInput => playerInputHandler != null && playerInputHandler.SprintTriggered;
    private bool IsMoving => playerInputHandler != null && playerInputHandler.MovementInput.sqrMagnitude > 0.01f;
    public bool IsSprinting => stamina != null && stamina.IsSprinting;
    private bool MovementBlocked => GameplayBlocker.IsBlocked(BlockFlags.Movement);

    private float CurrentSpeed
    {
        get
        {
            if (IsSneaking) return walkSpeed * sneakMultiplier;
            if (IsSprinting) return walkSpeed * sprintMultiplier;
            return walkSpeed;
        }
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        GameManager.Instance.Player = this;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleSprinting();
        HandleMovement();
        HandleAnimation();
    }

    private void HandleSprinting()
    {
        if (stamina == null) return;
        bool wantsToSprint = !MovementBlocked && SprintInput && IsMoving && !IsSneaking;
        stamina.SetSprinting(wantsToSprint);
    }

    private Vector3 CalculateWorldDirection()
    {
        if (MovementBlocked) return Vector3.zero;

        Vector2 input = playerInputHandler.MovementInput;

        Camera cam = Camera.main;
        Vector3 camForward;
        Vector3 camRight;

        if (cam != null)
        {
            camForward = cam.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            camRight = cam.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
        }
        else
        {
            camForward = Vector3.forward;
            camRight = Vector3.right;
        }

        Vector3 dir = camForward * input.y + camRight * input.x;
        return dir.normalized;
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;
        }
        else
        {
            currentMovement.y = Mathf.Clamp(
                currentMovement.y + Physics.gravity.y * gravityMultiplayer * Time.deltaTime, -20f, 10f);
        }
    }

    private void HandleMovement()
    {
        Vector3 dir = CalculateWorldDirection();

        currentMovement.x = dir.x * CurrentSpeed;
        currentMovement.z = dir.z * CurrentSpeed;

        HandleGravity();

        characterController.Move(currentMovement * Time.deltaTime);

        if (health != null)
            health.CheckFallDamage(characterController.isGrounded, transform.position.y);
    }

    private void HandleAnimation()
    {
        if (animator == null || conversing) return;

        Vector2 input = (playerInputHandler != null && !MovementBlocked) ? playerInputHandler.MovementInput : Vector2.zero;
        bool moving = input.sqrMagnitude > 0.01f;

        float sendMoveX = 0f;
        float sendMoveY = 0f;

        if (moving)
        {
            if (Mathf.Abs(input.y) >= Mathf.Abs(input.x))
            {
                sendMoveY = input.y;
            }
            else
            {
                sendMoveX = Mathf.Abs(input.x);
            }
        }

        animator.SetFloat("moveX", sendMoveX);
        animator.SetFloat("moveY", sendMoveY);
        animator.SetBool("isMoving", moving);

        if (spriteRenderer != null && flipSpriteForLeft)
        {
            if (input.x > directionDeadzone)
                spriteRenderer.flipX = true;
            else if (input.x < -directionDeadzone)
                spriteRenderer.flipX = false;
        }
    }

    public void EnterConversation(Vector3 npcPosition)
    {
        conversing = true;

        if (animator == null) return;

        Vector3 toNpc = npcPosition - transform.position;
        toNpc.y = 0f;

        Camera cam = Camera.main;
        Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        camRight.y = 0f;
        camForward.y = 0f;
        camRight.Normalize();
        camForward.Normalize();

        float x = Vector3.Dot(toNpc, camRight);
        float y = Vector3.Dot(toNpc, camForward);

        string state;
        if (Mathf.Abs(x) >= Mathf.Abs(y))
        {
            state = sideState;
            if (spriteRenderer != null) spriteRenderer.flipX = x > 0f;
        }
        else
        {
            state = y > 0f ? awayFromCameraState : towardCameraState;
        }

        animator.enabled = true;
        animator.SetFloat("moveX", 0f);
        animator.SetFloat("moveY", 0f);
        animator.SetBool("isMoving", false);
        animator.Play(state, 0, 0f);
        animator.Update(0f);
        animator.enabled = false;
    }

    public void ExitConversation()
    {
        if (!conversing) return;
        conversing = false;

        if (animator == null) return;

        animator.enabled = true;
        animator.Play(idleState, 0, 0f);
        animator.Update(0f);
    }

    public void FaceToward(float worldX)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = worldX > transform.position.x;
    }

    public void Save(ref PlayerSaveData data)
    {
        data.position = transform.position;
    }

    public void Load(PlayerSaveData data)
    {
        if (characterController != null)
            characterController.enabled = false;

        transform.position = data.position;

        if (characterController != null)
            characterController.enabled = true;
    }
}

[System.Serializable]
public struct PlayerSaveData
{
    public Vector3 position;
}
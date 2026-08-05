using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;
    [SerializeField] private float sneakMultiplier = 0.4f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 8.5f;
    [SerializeField] private float gravityMultiplayer = 2.0f;
    [SerializeField] private float jumpStaminaCost = 0.2f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float upDownLookRange = 80.0f;

    [Header("Climbing")]
    [SerializeField] private float climbCheckDistance = 0.45f;
    [SerializeField] private float climbStepForce = 8.0f;
    [SerializeField] private float climbHoldGravityScale = 0.05f;
    [SerializeField] private LayerMask climbableLayers;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Stamina stamina;
    [SerializeField] private Health health;
    [SerializeField] private AnimReach animReach;

    private Vector3 climbWallNormal;
    private Vector3 currentMovement;
    private float verticalRotation;
    private bool isClimbing;
    private bool wasJumpHeld;
    private bool vaulting;
    private bool vaultUpApplied;
    private bool jumpConsumed;
    private float climbWallLostTimer = 0f;
    private const float climbWallGrace = 0.12f;

    private float halfHeight => characterController.height * 0.5f;
    private bool IsSneaking => playerInputHandler != null && playerInputHandler.CrouchTriggered;
    private bool SprintInput => playerInputHandler != null && playerInputHandler.SprintTriggered;
    private bool IsMoving => playerInputHandler != null && playerInputHandler.MovementInput.sqrMagnitude > 0.01f;
    public bool IsSprinting => stamina != null && stamina.IsSprinting;

    private float CurrentSpeed
    {
        get
        {
            if (IsSneaking) return walkSpeed * sneakMultiplier;
            if (IsSprinting) return walkSpeed * sprintMultiplier;
            return walkSpeed;
        }
    }

    void Start()
    {
        GameManager.Instance.Player = this;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleSprinting();
        HandleMovement();
        HandleRotation();
    }

    private void HandleSprinting()
    {
        if (stamina == null) return;
        bool wantsToSprint = SprintInput && IsMoving && !IsSneaking;
        stamina.SetSprinting(wantsToSprint);
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 input = new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y);
        return transform.TransformDirection(input).normalized;
    }

    private Vector3 HorizontalForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    private bool CheckWallAhead(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * halfHeight * 0.5f;
        return Physics.Raycast(origin, HorizontalForward(), out hit, climbCheckDistance, climbableLayers);
    }

    private bool CheckClimbWall(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * halfHeight * 0.5f;
        return Physics.Raycast(origin, -climbWallNormal, out hit, climbCheckDistance, climbableLayers);
    }

    private bool WallIsClimbHeight(RaycastHit hit)
    {
        float wallTop = hit.collider.bounds.max.y;
        float playerTop = transform.position.y + characterController.height;
        return wallTop > transform.position.y + 0.1f && wallTop < playerTop + characterController.height;
    }

    private bool CheckVaultable()
    {
        Vector3 fwd = -climbWallNormal;
        fwd.y = 0f;
        fwd.Normalize();
        Vector3 overWallPos = transform.position + fwd * climbCheckDistance * 1.5f + Vector3.up * characterController.height;
        return Physics.Raycast(overWallPos, Vector3.down, characterController.height * 1.5f, climbableLayers);
    }

    private void HandleClimbing()
    {
        bool jumpHeld = playerInputHandler != null && playerInputHandler.JumpTriggered;
        bool jumpJustPressed = jumpHeld && !wasJumpHeld;
        wasJumpHeld = jumpHeld;

        if (!isClimbing)
        {
            if (!characterController.isGrounded && jumpHeld &&
                CheckWallAhead(out RaycastHit hit) &&
                WallIsClimbHeight(hit))
            {
                isClimbing = true;
                climbWallNormal = hit.normal;
                vaultUpApplied = false;
                jumpConsumed = true;
            }
            return;
        }

        if (IsSneaking)
        {
            isClimbing = false;
            vaulting = false;
            vaultUpApplied = false;
            climbWallLostTimer = 0f;
            return;
        }

        if (stamina != null && (stamina.IsExhausted || stamina.CurrentStamina <= 0f))
        {
            isClimbing = false;
            vaulting = false;
            vaultUpApplied = false;
            climbWallLostTimer = 0f;
            return;
        }

        bool wallPresent = CheckClimbWall(out _);

        if (!wallPresent)
        {
            Vector3 right = Vector3.Cross(climbWallNormal, Vector3.up).normalized;
            Vector3 origin = transform.position + Vector3.up * halfHeight * 0.5f;
            bool leftHit = Physics.Raycast(origin - right * 0.2f, -climbWallNormal, climbCheckDistance, climbableLayers);
            bool rightHit = Physics.Raycast(origin + right * 0.2f, -climbWallNormal, climbCheckDistance, climbableLayers);
            if (leftHit || rightHit)
                wallPresent = true;
        }

        if (!wallPresent && !vaulting)
        {
            climbWallLostTimer += Time.deltaTime;
            if (climbWallLostTimer >= climbWallGrace)
            {
                climbWallLostTimer = 0f;
                if (CheckVaultable())
                {
                    vaulting = true;
                    vaultUpApplied = false;
                }
                else
                {
                    isClimbing = false;
                    vaulting = false;
                    vaultUpApplied = false;
                }
            }
        }
        else if (wallPresent)
        {
            climbWallLostTimer = 0f;
        }

        if (vaulting)
        {
            if (!vaultUpApplied)
            {
                currentMovement.y = climbStepForce * 1f;
                vaultUpApplied = true;
                if (stamina != null)
                    stamina.UseStamina(jumpStaminaCost);
            }

            Vector3 fwd = -climbWallNormal;
            fwd.y = 0f;
            fwd.Normalize();
            currentMovement.x = fwd.x * walkSpeed;
            currentMovement.z = fwd.z * walkSpeed;

            currentMovement.y += Physics.gravity.y * gravityMultiplayer * Time.deltaTime;

            if (characterController.isGrounded && currentMovement.y <= 0f)
            {
                isClimbing = false;
                vaulting = false;
                vaultUpApplied = false;
                climbWallLostTimer = 0f;
            }
            return;
        }

        currentMovement.y = jumpJustPressed ? climbStepForce : Mathf.Lerp(currentMovement.y, 0f, 10f * Time.deltaTime);
        if (jumpJustPressed && stamina != null)
            stamina.UseStamina(jumpStaminaCost);

        currentMovement.y += Physics.gravity.y * climbHoldGravityScale * Time.deltaTime;

        if (characterController.isGrounded && currentMovement.y < 0f)
        {
            isClimbing = false;
            vaulting = false;
            vaultUpApplied = false;
            climbWallLostTimer = 0f;
        }
    }

    private void HandleJumping()
    {
        if (isClimbing) return;

        bool jumpHeld = playerInputHandler != null && playerInputHandler.JumpTriggered;

        if (!jumpHeld)
            jumpConsumed = false;

        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;

            if (jumpHeld && !jumpConsumed && (stamina == null || stamina.HasStaminaForJump))
            {
                currentMovement.y = jumpForce;
                jumpConsumed = true;
                if (stamina != null)
                {
                    stamina.UseStamina(jumpStaminaCost);
                    stamina.SetSprinting(false);
                }
            }
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplayer * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 dir = CalculateWorldDirection();

        currentMovement.x = dir.x * CurrentSpeed;
        currentMovement.z = dir.z * CurrentSpeed;

        HandleClimbing();
        HandleJumping();

        characterController.Move(currentMovement * Time.deltaTime);

        if (health != null)
            health.CheckFallDamage(characterController.isGrounded, transform.position.y);
    }

    private void ApplyHorizontalRotation(float rot)
    {
        transform.Rotate(0, rot, 0);
    }

    private void ApplyVerticalRotation(float rot)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rot, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = playerInputHandler.RotationInput.x * mouseSensitivity;
        float my = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mx);
        ApplyVerticalRotation(my);
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
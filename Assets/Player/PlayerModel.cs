using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 4.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 7.0f;
    [SerializeField] private float gravityMultiplayer = 2.0f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float upDownLookRange = 80.0f;

    [Header("Referances")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Vector3 currentMovement;
    private float verticalRotation;
    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y);
        return transform.TransformDirection(inputDirection).normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;

            if (playerInputHandler.JumpTriggered)
                currentMovement.y = jumpForce;
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplayer * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseXRotation = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }
}
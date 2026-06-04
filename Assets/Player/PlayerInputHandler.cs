using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string rotation = "Rotation";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string interact = "Interact";
    [SerializeField] private string esc = "Esc";

    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction escAction;

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
    public bool InteractTriggered => interactAction.WasPressedThisFrame();
    public bool EscTriggered => escAction.WasPressedThisFrame();

    private void Awake()
    {
        InputActionMap actionMap = playerControls.FindActionMap(actionMapName);
        movementAction = actionMap.FindAction(movement);
        rotationAction = actionMap.FindAction(rotation);
        jumpAction = actionMap.FindAction(jump);
        sprintAction = actionMap.FindAction(sprint);
        interactAction = actionMap.FindAction(interact);
        escAction = actionMap.FindAction(esc);

        SubscribeActionValueToInputEvent();
    }

    private void SubscribeActionValueToInputEvent()
    {
        movementAction.performed += InputInfo => MovementInput = movementAction.ReadValue<Vector2>();
        movementAction.canceled += InputInfo => MovementInput = Vector2.zero;

        rotationAction.performed += InputInfo => RotationInput = rotationAction.ReadValue<Vector2>();
        rotationAction.canceled += InputInfo => RotationInput = Vector2.zero;

        jumpAction.performed += InputInfo => JumpTriggered = true;
        jumpAction.canceled += InputInfo => JumpTriggered = false;

        sprintAction.performed += InputInfo => SprintTriggered = true;
        sprintAction.canceled += InputInfo => SprintTriggered = false;
    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
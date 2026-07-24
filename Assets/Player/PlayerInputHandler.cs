using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerControls;

    private string actionMapName = "Player";
    private string movement = "Movement";
    private string rotation = "Rotation";
    private string jump = "Jump";
    private string sprint = "Sprint";
    private string interact = "Interact";
    private string esc = "Esc";
    private string crouch = "Crouch";
    private string inventory = "Inventory";
    private string useL = "Use_L";
    private string useR = "Use_R";
    private string scroll = "Scroll";

    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction escAction;
    private InputAction crouchAction;
    private InputAction inventoryAction;
    private InputAction useLAction;
    private InputAction useRAction;
    private InputAction scrollAction;

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    private bool inputManuallyDisabled = false;
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
    public bool CrouchTriggered { get; private set; }
    public float ScrollInput { get; private set; }

    public bool InteractTriggered => interactAction.WasPressedThisFrame();
    public bool EscTriggered => escAction.WasPressedThisFrame();
    public bool InventoryTriggered => inventoryAction.WasPressedThisFrame();
    public bool UseLTriggered => useLAction.WasPressedThisFrame();
    public bool UseRTriggered => useRAction.WasPressedThisFrame();

    private void Awake()
    {
        InputActionMap actionMap = playerControls.FindActionMap(actionMapName);

        movementAction = actionMap.FindAction(movement);
        rotationAction = actionMap.FindAction(rotation);
        jumpAction = actionMap.FindAction(jump);
        sprintAction = actionMap.FindAction(sprint);
        interactAction = actionMap.FindAction(interact);
        escAction = actionMap.FindAction(esc);
        crouchAction = actionMap.FindAction(crouch);
        inventoryAction = actionMap.FindAction(inventory);
        useLAction = actionMap.FindAction(useL);
        useRAction = actionMap.FindAction(useR);
        scrollAction = actionMap.FindAction(scroll);

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

        crouchAction.performed += InputInfo => CrouchTriggered = true;
        crouchAction.canceled += InputInfo => CrouchTriggered = false;
    }

    private void LateUpdate()
    {
        ScrollInput = scrollAction.ReadValue<Vector2>().y;
    }

    public void DisableInput()
    {
        inputManuallyDisabled = true;
        playerControls.FindActionMap(actionMapName).Disable();
        MovementInput = Vector2.zero;
        RotationInput = Vector2.zero;
        JumpTriggered = false;
        SprintTriggered = false;
        CrouchTriggered = false;
        ScrollInput = 0f;
    }

    public void EnableInput()
    {
        inputManuallyDisabled = false;
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnEnable()
    {
        if (!inputManuallyDisabled)
            playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
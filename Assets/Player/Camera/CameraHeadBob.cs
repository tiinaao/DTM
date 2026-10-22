using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visual; 
    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Bob")]
    [SerializeField] private float frequency = 2.2f;    
    [SerializeField] private float amplitude = 0.06f;    
    [SerializeField] private float tiltAmount = 3f;       
    [SerializeField] private float squashAmount = 0.04f;  
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.4f;
    [SerializeField] private float sneakMultiplier = 0.6f;

    private Vector3 restPosition;
    private Vector3 restScale;
    private float timer;
    private float currentRoll;

    void Start()
    {
        if (visual == null) visual = transform;
        restPosition = visual.localPosition;
        restScale = visual.localScale;
    }

    void LateUpdate()
    {
        if (visual == null) return;

        bool blocked = GameplayBlocker.IsBlocked(BlockFlags.Camera); 
        bool isMoving = !blocked && inputHandler != null && inputHandler.MovementInput.sqrMagnitude > 0.01f;
        bool isSprinting = playerModel != null && playerModel.IsSprinting;
        bool isSneaking = inputHandler != null && inputHandler.CrouchTriggered;

        Vector3 targetPos = restPosition;
        Vector3 targetScale = restScale;
        float targetRoll = 0f;

        if (isMoving)
        {
            float freq = frequency;
            if (isSprinting) freq *= sprintMultiplier;
            else if (isSneaking) freq *= sneakMultiplier;

            timer += Time.deltaTime * freq;

            float hop = Mathf.Abs(Mathf.Sin(timer * Mathf.PI));
            float side = Mathf.Sin(timer * Mathf.PI); 

            targetPos = restPosition + new Vector3(0f, hop * amplitude, 0f);
            targetRoll = -side * tiltAmount;

            float squash = (hop - 0.5f) * squashAmount;
            targetScale = new Vector3(restScale.x * (1f - squash), restScale.y * (1f + squash), restScale.z);
        }
        else
        {
            timer = 0f;
        }

        float t = (blocked ? Time.unscaledDeltaTime : Time.deltaTime) * smoothSpeed;
        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, t);
        visual.localScale = Vector3.Lerp(visual.localScale, targetScale, t);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, t);
        visual.localRotation = Quaternion.Euler(0f, 0f, currentRoll);
    }
}
using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    private float frequency = 2.2f;
    private float amplitude = 0.06f;
    private float swayAmplitude = 0.025f;
    private float smoothSpeed = 10f;
    private float sprintMultiplier = 1.4f;
    private float sneakMultiplier = 0.6f;

    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private PlayerInputHandler inputHandler;

    private Vector3 restPosition;
    private float Timer;
    private float currentRoll;

    void Start()
    {
        restPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        float pitch = playerModel != null ? playerModel.VerticalRotation : 0f;

        if (GameplayBlocker.IsBlocked(BlockFlags.Camera))
        {
            Timer = 0f;
            float blockedT = Time.unscaledDeltaTime * smoothSpeed;
            transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, blockedT);
            currentRoll = Mathf.Lerp(currentRoll, 0f, blockedT);
            transform.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
            return;
        }

        bool isMoving = inputHandler != null && inputHandler.MovementInput.sqrMagnitude > 0.01f;
        bool isSprinting = playerModel != null && playerModel.IsSprinting;
        bool isSneaking = inputHandler != null && inputHandler.CrouchTriggered;

        float targetRoll = 0f;
        Vector3 targetPosition = restPosition;

        if (isMoving)
        {
            float freq = frequency;
            if (isSprinting) freq *= sprintMultiplier;
            else if (isSneaking) freq *= sneakMultiplier;

            Timer += Time.deltaTime * freq;

            float Y = Mathf.Sin(Timer * Mathf.PI * 2f) * amplitude;
            float X = Mathf.Sin(Timer * Mathf.PI) * swayAmplitude;
            float Z = Mathf.Sin(Timer * Mathf.PI * 2f) * swayAmplitude * 8f;

            targetPosition = restPosition + new Vector3(X, Y, 0f);
            targetRoll = -Z;
        }
        else
        {
            Timer = 0f;
        }

        float t = Time.deltaTime * smoothSpeed;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, t);
        transform.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
    }
}
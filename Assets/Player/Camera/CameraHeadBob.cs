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

    void Start()
    {
        restPosition = transform.localPosition;
    }

    void Update()
    {
        bool isMoving = inputHandler != null && inputHandler.MovementInput.sqrMagnitude > 0.01f;
        bool isSprinting = playerModel != null && playerModel.IsSprinting;
        bool isSneaking = inputHandler != null && inputHandler.CrouchTriggered;

        if (isMoving)
        {
            float freq = frequency;
            if (isSprinting) freq *= sprintMultiplier;
            else if (isSneaking) freq *= sneakMultiplier;

            Timer += Time.deltaTime * freq;

            float Y = Mathf.Sin(Timer * Mathf.PI * 2f) * amplitude;
            float X = Mathf.Cos(Timer * Mathf.PI) * swayAmplitude;
            float Z = Mathf.Sin(Timer * Mathf.PI * 2f) * swayAmplitude * 8f;

            Vector3 target = restPosition + new Vector3(X, Y, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * smoothSpeed);

            Quaternion targetRot = Quaternion.Euler(0f, 0f, -Z);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
        }
        else
        {
            Timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, Time.deltaTime * smoothSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * smoothSpeed);
        }
    }
}
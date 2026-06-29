using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private float bobFrequency = 2.2f;
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float swayAmplitude = 0.025f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float sprintMultiplier = 1.4f;
    [SerializeField] private float sneakMultiplier = 0.6f;

    [Header("References")]
    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private PlayerInputHandler inputHandler;

    private Vector3 restPosition;
    private float bobTimer;

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
            float freq = bobFrequency;
            if (isSprinting) freq *= sprintMultiplier;
            else if (isSneaking) freq *= sneakMultiplier;

            bobTimer += Time.deltaTime * freq;

            float bobY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            float bobX = Mathf.Cos(bobTimer * Mathf.PI) * swayAmplitude;
            float rollZ = Mathf.Sin(bobTimer * Mathf.PI * 2f) * swayAmplitude * 8f;

            Vector3 target = restPosition + new Vector3(bobX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * smoothSpeed);

            Quaternion targetRot = Quaternion.Euler(0f, 0f, -rollZ);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothSpeed);
        }
        else
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition, Time.deltaTime * smoothSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * smoothSpeed);
        }
    }
}
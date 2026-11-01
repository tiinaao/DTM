using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -3f);
    [SerializeField] private Vector3 fixedAngles = new Vector3(20f, 0f, 0f);
    [SerializeField] private float followSpeed = 5f;

    [SerializeField] private float focusZoom = 1.45f;
    [SerializeField] private float focusInTime = 0.75f;
    [SerializeField] private float focusOutTime = 0.55f;

    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float cameraRadius = 0.4f;
    [SerializeField] private float collisionBuffer = 0.1f;
    [SerializeField] private float minCameraDistance = 1.0f;

    [SerializeField] private float maxYaw = 1f;
    [SerializeField] private float maxPitch = 0.6f;
    [SerializeField] private float reachRadius = 0.3f;
    [SerializeField] private float lookSmoothing = 5f;
    [SerializeField] private float lookDeadzone = 0.03f;
    [SerializeField] private float lookCurvePower = 1.5f;
    [SerializeField] private bool hideCursor = true;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private float spriteYawFollow = 0.5f;

    [SerializeField] private float collisionPushInSpeed = 6f;
    [SerializeField] private float collisionPullOutSpeed = 2.5f;

    private float currentFactor = -1f;
    private bool lockRotation = true;
    private Camera cam;
    private float baseFov;
    private float baseOrthoSize;
    private Vector3 followPos;
    private Transform focusSubject;
    private bool focusing;
    private float blend;
    private Vector2 look;
    public bool IsFocused => focusing;
    public Quaternion BaseRotation => Quaternion.Euler(fixedAngles);

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        baseFov = cam.fieldOfView;
        baseOrthoSize = cam.orthographicSize;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        followPos = transform.position;

        if (lockRotation)
            transform.rotation = Quaternion.Euler(fixedAngles);
    }

    public void FocusOn(Transform subject)
    {
        if (subject == null) return;
        focusSubject = subject;
        focusing = true;
    }

    public void ClearFocus()
    {
        focusing = false;
    }

    private Vector2 ReadLookTarget()
    {
        if (focusing || GameplayBlocker.IsBlocked(BlockFlags.Camera)) return Vector2.zero;

        Mouse mouse = Mouse.current;
        if (mouse == null) return Vector2.zero;

        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        float radius = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) * reachRadius);

        Vector2 offset = (mouse.position.ReadValue() - center) / radius;
        offset = Vector2.ClampMagnitude(offset, 1f);

        float mag = offset.magnitude;
        if (mag <= lookDeadzone) return Vector2.zero;

        float scaled = (mag - lookDeadzone) / (1f - lookDeadzone);
        return offset.normalized * Mathf.Pow(scaled, lookCurvePower);
    }

    private void UpdateCursor()
    {
        Cursor.lockState = CursorLockMode.Confined;

        if (hideCursor)
            Cursor.visible = GameplayBlocker.IsBlocked(BlockFlags.FreeCursor);
    }

    private Vector3 GetCollisionAdjustedPosition(Vector3 pivot, Vector3 desiredPos)
    {
        Vector3 dir = desiredPos - pivot;
        float distance = dir.magnitude;

        if (distance < 0.001f) return desiredPos;

        Vector3 unit = dir / distance;
        float allowedDistance = distance;

        if (Physics.SphereCast(pivot, cameraRadius, unit, out RaycastHit hit, distance, obstructionMask))
            allowedDistance = Mathf.Max(0f, hit.distance - collisionBuffer);

        allowedDistance = Mathf.Max(allowedDistance, Mathf.Min(minCameraDistance, distance));
        float targetFactor = allowedDistance / distance;

        if (currentFactor < 0f) currentFactor = targetFactor;

        float speed = targetFactor < currentFactor ? collisionPushInSpeed : collisionPullOutSpeed;
        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        currentFactor = Mathf.Lerp(currentFactor, targetFactor, t);

        if (Mathf.Abs(currentFactor - targetFactor) < 0.001f)
            currentFactor = targetFactor;

        return pivot + dir * currentFactor;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            followPos = Vector3.Lerp(followPos, desiredPosition, t);
        }

        UpdateCursor();

        float lookT = 1f - Mathf.Exp(-lookSmoothing * Time.unscaledDeltaTime);
        look = Vector2.Lerp(look, ReadLookTarget(), lookT);

        float yaw = look.x * maxYaw;
        float pitch = -look.y * maxPitch;
        Quaternion followRot = Quaternion.Euler(fixedAngles.x + pitch, fixedAngles.y + yaw, fixedAngles.z);

        if (playerSprite != null)
        {
            Vector3 currentEuler = playerSprite.localEulerAngles;
            playerSprite.localRotation = Quaternion.Euler(currentEuler.x, yaw * spriteYawFollow, currentEuler.z);
        }

        float goal = focusing ? 1f : 0f;
        float duration = focusing ? focusInTime : focusOutTime;
        blend = Mathf.MoveTowards(blend, goal, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));

        if (blend <= 0f || focusSubject == null)
        {
            if (!focusing) focusSubject = null;
            if (target == null) return;

            transform.position = GetCollisionAdjustedPosition(target.position, followPos);
            if (lockRotation) transform.rotation = followRot;
            SetZoom(0f);
            return;
        }

        float e = blend * blend * blend * (blend * (blend * 6f - 15f) + 10f);

        transform.position = GetCollisionAdjustedPosition(target.position, followPos);
        if (lockRotation) transform.rotation = followRot;

        SetZoom(e);
    }

    private void SetZoom(float e)
    {
        float zoom = Mathf.Lerp(1f, Mathf.Max(1f, focusZoom), e);

        if (cam.orthographic)
        {
            cam.orthographicSize = baseOrthoSize / zoom;
        }
        else
        {
            float halfRad = baseFov * 0.5f * Mathf.Deg2Rad;
            cam.fieldOfView = 2f * Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(halfRad) / zoom);
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Camera))]
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

    private bool lockRotation = true;

    private Camera cam;
    private float baseFov;
    private float baseOrthoSize;
    private Vector3 followPos;
    private Transform focusSubject;
    private bool focusing;
    private float blend;

    public bool IsFocused => focusing;

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

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            followPos = Vector3.Lerp(followPos, desiredPosition, t);
        }

        Quaternion followRot = Quaternion.Euler(fixedAngles);

        float goal = focusing ? 1f : 0f;
        float duration = focusing ? focusInTime : focusOutTime;
        blend = Mathf.MoveTowards(blend, goal, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));

        if (blend <= 0f || focusSubject == null)
        {
            if (!focusing) focusSubject = null;
            if (target == null) return;

            transform.position = followPos;
            if (lockRotation) transform.rotation = followRot;
            SetZoom(0f);
            return;
        }

        float e = blend * blend * blend * (blend * (blend * 6f - 15f) + 10f);

        Vector3 pos = followPos;

        transform.position = pos;
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
using System;
using UnityEngine;

public enum TimeState
{
    Morning,
    Day,
    Evening,
    Night
}

public class SunCycle : MonoBehaviour
{
    public Light sun;
    public Camera cam;
    [Range(0, 24)] public float hour = 8f;
    public float dayLengthSeconds = 120f;
    public bool pauseTime = false;
    [Range(0f, 1f)] public float zTilt = 0.4f;
    public float rimStrengthMultiplier = 1f;

    public Gradient lightColor;
    public Gradient ambientColor;
    public Gradient backgroundColor;
    public AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private float morningStart = 6f;
    [SerializeField] private float dayStart = 11f;
    [SerializeField] private float eveningStart = 17f;
    [SerializeField] private float nightStart = 21f;
    [SerializeField] private float defaultTransitionSeconds = 3f;

    [SerializeField] private bool controlFog = false;
    public Gradient fogColor;

    public event Action<TimeState> OnTimeStateChanged;

    static readonly int SunRimColorID = Shader.PropertyToID("_SunRimColor");

    private bool transitioning;
    private float transStart;
    private float transTarget; 
    private float transElapsed;
    private float transDuration;
    private TimeState lastState;

    public float Hour => hour;
    public bool IsTransitioning => transitioning;
    public TimeState CurrentState => StateAt(hour);

    void Reset()
    {
        lightColor = MakeGradient(
            (0.00f, new Color(0.35f, 0.45f, 0.85f)),
            (0.25f, new Color(1.00f, 0.55f, 0.25f)),
            (0.50f, new Color(1.00f, 0.96f, 0.88f)),
            (0.75f, new Color(1.00f, 0.45f, 0.20f)),
            (1.00f, new Color(0.35f, 0.45f, 0.85f)));

        ambientColor = MakeGradient(
            (0.00f, new Color(0.10f, 0.12f, 0.22f)),
            (0.25f, new Color(0.55f, 0.45f, 0.45f)),
            (0.50f, new Color(0.80f, 0.85f, 0.90f)),
            (0.75f, new Color(0.55f, 0.40f, 0.40f)),
            (1.00f, new Color(0.10f, 0.12f, 0.22f)));

        backgroundColor = MakeGradient(
            (0.00f, new Color(0.02f, 0.03f, 0.08f)),
            (0.25f, new Color(0.95f, 0.60f, 0.40f)),
            (0.50f, new Color(0.45f, 0.70f, 0.95f)),
            (0.75f, new Color(0.90f, 0.45f, 0.35f)),
            (1.00f, new Color(0.02f, 0.03f, 0.08f)));

        fogColor = MakeGradient(
            (0.00f, new Color(0.04f, 0.05f, 0.12f)),
            (0.25f, new Color(0.75f, 0.55f, 0.45f)),
            (0.50f, new Color(0.60f, 0.75f, 0.90f)),
            (0.75f, new Color(0.70f, 0.45f, 0.40f)),
            (1.00f, new Color(0.04f, 0.05f, 0.12f)));
    }

    static Gradient MakeGradient(params (float t, Color c)[] stops)
    {
        var g = new Gradient();
        var keys = new GradientColorKey[stops.Length];
        for (int i = 0; i < stops.Length; i++) keys[i] = new GradientColorKey(stops[i].c, stops[i].t);
        g.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return g;
    }

    void Awake()
    {
        GameManager.Instance.Sun = this;
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!sun) sun = FindAnyObjectByType<Light>();
        if (sun) RenderSettings.sun = sun;
        lastState = CurrentState;
        Apply();
    }

    public float StartHourOf(TimeState state)
    {
        switch (state)
        {
            case TimeState.Morning: return morningStart;
            case TimeState.Day: return dayStart;
            case TimeState.Evening: return eveningStart;
            default: return nightStart;
        }
    }

    public TimeState StateAt(float h)
    {
        h = Mathf.Repeat(h, 24f);
        if (h >= nightStart || h < morningStart) return TimeState.Night;
        if (h >= eveningStart) return TimeState.Evening;
        if (h >= dayStart) return TimeState.Day;
        return TimeState.Morning;
    }

    public void SetTimeState(TimeState state, float seconds = -1f)
    {
        if (seconds < 0f) seconds = defaultTransitionSeconds;

        float target = Mathf.Repeat(StartHourOf(state), 24f);
        float delta = Mathf.Repeat(target - hour, 24f);

        if (seconds <= 0f || delta < 0.001f)
        {
            transitioning = false;
            hour = target;
            RaiseIfStateChanged();
            Apply();
            return;
        }

        transitioning = true;
        transStart = hour;
        transTarget = hour + delta;
        transElapsed = 0f;
        transDuration = seconds;
    }

    public void AdvanceToNextState(float seconds = -1f)
    {
        TimeState next = (TimeState)(((int)CurrentState + 1) % 4);
        SetTimeState(next, seconds);
    }

    public void SetHour(float h)
    {
        transitioning = false;
        hour = Mathf.Repeat(h, 24f);
        RaiseIfStateChanged();
        Apply();
    }

    void Update()
    {
        if (transitioning)
        {
            transElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transElapsed / transDuration);
            hour = Mathf.Repeat(Mathf.Lerp(transStart, transTarget, Mathf.SmoothStep(0f, 1f, t)), 24f);
            if (t >= 1f)
            {
                hour = Mathf.Repeat(transTarget, 24f);
                transitioning = false;
            }
        }
        else if (!pauseTime)
        {
            hour = (hour + Time.deltaTime / dayLengthSeconds * 24f) % 24f;
        }

        RaiseIfStateChanged();
        Apply();
    }

    void RaiseIfStateChanged()
    {
        TimeState now = CurrentState;
        if (now == lastState) return;
        lastState = now;
        OnTimeStateChanged?.Invoke(now);
    }

    void Apply()
    {
        if (!sun) return;

        float t = hour / 24f;
        float a = (t - 0.25f) * Mathf.PI * 2f;

        Vector3 toSun = new Vector3(-Mathf.Cos(a), Mathf.Sin(a), -zTilt).normalized;
        Vector3 lightDir = toSun.y >= 0f ? toSun : -toSun;

        sun.transform.rotation = Quaternion.LookRotation(-lightDir);
        Color col = lightColor.Evaluate(t);
        sun.color = col;
        sun.intensity = Mathf.Lerp(0.15f, 1.2f, sunIntensity.Evaluate(Mathf.Abs(toSun.y)));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor.Evaluate(t);
        if (cam) cam.backgroundColor = backgroundColor.Evaluate(t);

        if (controlFog && fogColor != null)
            RenderSettings.fogColor = fogColor.Evaluate(t);

        float rimStrength = Mathf.SmoothStep(0f, 1f, Mathf.Abs(toSun.y) * 4f) * rimStrengthMultiplier;
        Shader.SetGlobalColor(SunRimColorID, new Color(col.r, col.g, col.b, rimStrength));
    }

    public void Save(ref TimeSaveData data)
    {
        data.valid = true;
        data.hour = hour;
    }

    public void Load(TimeSaveData data)
    {
        if (!data.valid) return;

        transitioning = false;
        hour = Mathf.Repeat(data.hour, 24f);
        lastState = CurrentState;
        Apply();
    }
}

[Serializable]
public struct TimeSaveData
{
    public bool valid;
    public float hour;
}
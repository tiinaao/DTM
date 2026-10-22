using UnityEngine;

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

    static readonly int SunRimColorID = Shader.PropertyToID("_SunRimColor");

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
    }

    static Gradient MakeGradient(params (float t, Color c)[] stops)
    {
        var g = new Gradient();
        var keys = new GradientColorKey[stops.Length];
        for (int i = 0; i < stops.Length; i++) keys[i] = new GradientColorKey(stops[i].c, stops[i].t);
        g.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return g;
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!sun) sun = FindAnyObjectByType<Light>();
        if (sun) RenderSettings.sun = sun;  
        Apply();
    }

    void Update()
    {
        if (!pauseTime)
            hour = (hour + Time.deltaTime / dayLengthSeconds * 24f) % 24f;
        Apply();
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

        float rimStrength = Mathf.SmoothStep(0f, 1f, Mathf.Abs(toSun.y) * 4f) * rimStrengthMultiplier;
        Shader.SetGlobalColor(SunRimColorID, new Color(col.r, col.g, col.b, rimStrength));
    }
}
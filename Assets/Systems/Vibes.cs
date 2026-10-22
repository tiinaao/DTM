using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public class Vibes : MonoBehaviour
{
    [Header("References (auto-found if empty)")]
    public Camera targetCamera;
    public Light sun;
    public Transform focusTarget;

    [Header("Camera")]
    public bool hdr = true;
    public bool antiAliasing = true;

    [Header("Tonemapping & Color")]
    public bool tonemapping = true;
    [Range(-3f, 3f)] public float exposure = 0.2f;
    [Range(-100f, 100f)] public float contrast = 15f;
    [Range(-100f, 100f)] public float saturation = 12f;
    [Range(-100f, 100f)] public float temperature = 8f;
    [Range(-100f, 100f)] public float tint = 0f;
    public Color colorFilter = Color.white;

    [Header("Bloom")]
    public bool bloom = true;
    [Range(0f, 5f)] public float bloomThreshold = 1.1f;
    [Range(0f, 3f)] public float bloomIntensity = 0.5f;
    [Range(0f, 1f)] public float bloomScatter = 0.6f;
    public Color bloomTint = Color.white;

    [Header("Depth of Field")]
    public bool depthOfField = true;
    public bool autoFocus = true;
    [Min(0.1f)] public float focusDistance = 10f;
    [Range(1f, 300f)] public float focalLength = 50f;
    [Range(1f, 32f)] public float aperture = 5.6f;
    [Range(0f, 20f)] public float focusSmoothing = 6f;

    [Header("Vignette")]
    public bool vignette = true;
    [Range(0f, 1f)] public float vignetteIntensity = 0.25f;
    [Range(0.01f, 1f)] public float vignetteSmoothness = 0.4f;

    [Header("Film Grain & Lens")]
    public bool filmGrain = true;
    [Range(0f, 1f)] public float grainIntensity = 0.15f;
    public bool chromaticAberration = true;
    [Range(0f, 1f)] public float chromaticIntensity = 0.08f;

    [Header("Motion Blur")]
    public bool motionBlur = false;
    [Range(0f, 1f)] public float motionBlurIntensity = 0.3f;

    [Header("Sun & Shadows")]
    public bool controlSun = true;
    public Color sunColor = new Color(1f, 0.95f, 0.85f);
    [Range(0f, 5f)] public float sunIntensity = 1.2f;
    [Range(0f, 1f)] public float shadowStrength = 0.85f;
    public LightShadows shadowType = LightShadows.Soft;
    [Range(5f, 300f)] public float shadowDistance = 60f;

    [Header("Ambient & Fog")]
    public bool controlAmbient = true;
    public Color ambientColor = new Color(0.42f, 0.46f, 0.55f);
    public bool fog = false;
    public Color fogColor = new Color(0.6f, 0.7f, 0.8f);
    [Range(0f, 0.2f)] public float fogDensity = 0.01f;

    [Header("Sprites")]
    public bool setupAllSpriteShadows = true;
    public ShadowCastingMode spriteCastShadows = ShadowCastingMode.On;
    public bool spriteReceiveShadows = true;

    Volume volume;
    VolumeProfile profile;
    GameObject volumeGO;
    float currentFocus;

    void OnEnable()
    {
        BuildVolume();
        if (setupAllSpriteShadows) ApplySpriteShadows();
        Apply(true);
    }

    void OnDisable()
    {
        if (profile != null) DestroyImmediate(profile);
        if (volumeGO != null) DestroyImmediate(volumeGO);
        volume = null;
        profile = null;
        volumeGO = null;
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    if (volume == null) BuildVolume();
                    Apply(true);
                };
        #endif
    }

    void LateUpdate()
    {
        if (volume == null) BuildVolume();
        Apply(false);
    }

    void BuildVolume()
    {
        if (volumeGO != null) return;
        volumeGO = new GameObject("AAA Look Volume");
        volumeGO.hideFlags = HideFlags.HideAndDontSave;
        volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100;
        volume.weight = 1f;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        volume.sharedProfile = profile;
    }

    T Get<T>() where T : VolumeComponent
    {
        if (!profile.TryGet(out T c)) c = profile.Add<T>(true);
        return c;
    }

    void Apply(bool snapFocus)
    {
        if (profile == null) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (sun == null) sun = RenderSettings.sun != null ? RenderSettings.sun : FindAnyObjectByType<Light>();

        if (cam != null)
        {
            cam.allowHDR = hdr;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = antiAliasing ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
        }

        var tm = Get<Tonemapping>();
        tm.active = tonemapping;
        tm.mode.Override(TonemappingMode.ACES);

        var ca = Get<ColorAdjustments>();
        ca.postExposure.Override(exposure);
        ca.contrast.Override(contrast);
        ca.saturation.Override(saturation);
        ca.colorFilter.Override(colorFilter);

        var wb = Get<WhiteBalance>();
        wb.temperature.Override(temperature);
        wb.tint.Override(tint);

        var bl = Get<Bloom>();
        bl.active = bloom;
        bl.threshold.Override(bloomThreshold);
        bl.intensity.Override(bloomIntensity);
        bl.scatter.Override(bloomScatter);
        bl.tint.Override(bloomTint);

        var vg = Get<Vignette>();
        vg.active = vignette;
        vg.intensity.Override(vignetteIntensity);
        vg.smoothness.Override(vignetteSmoothness);

        var fg = Get<FilmGrain>();
        fg.active = filmGrain;
        fg.type.Override(FilmGrainLookup.Thin1);
        fg.intensity.Override(grainIntensity);

        var ch = Get<ChromaticAberration>();
        ch.active = chromaticAberration;
        ch.intensity.Override(chromaticIntensity);

        var mb = Get<MotionBlur>();
        mb.active = motionBlur;
        mb.intensity.Override(motionBlurIntensity);

        var dof = Get<DepthOfField>();
        dof.active = depthOfField;
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        float targetFocus = focusDistance;
        if (autoFocus && cam != null && focusTarget != null)
            targetFocus = Mathf.Max(0.1f, Vector3.Distance(cam.transform.position, focusTarget.position));
        if (snapFocus || !Application.isPlaying || focusSmoothing <= 0f) currentFocus = targetFocus;
        else currentFocus = Mathf.Lerp(currentFocus, targetFocus, 1f - Mathf.Exp(-focusSmoothing * Time.deltaTime));
        dof.focusDistance.Override(currentFocus);
        dof.focalLength.Override(focalLength);
        dof.aperture.Override(aperture);

        if (controlSun && sun != null)
        {
            sun.color = sunColor;
            sun.intensity = sunIntensity;
            sun.shadows = shadowType;
            sun.shadowStrength = shadowStrength;
            RenderSettings.sun = sun;
        }

        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null) urp.shadowDistance = shadowDistance;

        if (controlAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
        }

        RenderSettings.fog = fog;
        if (fog)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
        }
    }

    [ContextMenu("Apply Sprite Shadows Now")]
    public void ApplySpriteShadows()
    {
        foreach (var r in FindObjectsByType<SpriteRenderer>())
        {
            r.shadowCastingMode = spriteCastShadows;
            r.receiveShadows = spriteReceiveShadows;
        }
    }

    [ContextMenu("Preset/Cinematic")]
    void PresetCinematic()
    {
        exposure = 0.1f; contrast = 25f; saturation = 5f; temperature = 5f;
        bloomThreshold = 1.1f; bloomIntensity = 0.6f; vignetteIntensity = 0.35f;
        grainIntensity = 0.2f; chromaticIntensity = 0.1f; aperture = 4f;
        sunColor = new Color(1f, 0.92f, 0.8f); sunIntensity = 1.1f;
        ambientColor = new Color(0.32f, 0.36f, 0.45f);
        Apply(true);
    }

    [ContextMenu("Preset/Bright Stylized")]
    void PresetBright()
    {
        exposure = 0.4f; contrast = 10f; saturation = 30f; temperature = 10f;
        bloomThreshold = 1.2f; bloomIntensity = 0.4f; vignetteIntensity = 0.15f;
        grainIntensity = 0.05f; chromaticIntensity = 0.03f; aperture = 8f;
        sunColor = new Color(1f, 0.97f, 0.9f); sunIntensity = 1.4f;
        ambientColor = new Color(0.55f, 0.58f, 0.65f);
        Apply(true);
    }

    [ContextMenu("Preset/Moody Dark")]
    void PresetMoody()
    {
        exposure = -0.4f; contrast = 35f; saturation = -10f; temperature = -12f;
        bloomThreshold = 0.9f; bloomIntensity = 0.8f; vignetteIntensity = 0.5f;
        grainIntensity = 0.3f; chromaticIntensity = 0.15f; aperture = 2.8f;
        sunColor = new Color(0.8f, 0.85f, 1f); sunIntensity = 0.8f;
        ambientColor = new Color(0.2f, 0.22f, 0.3f);
        fog = true; fogColor = new Color(0.1f, 0.12f, 0.18f); fogDensity = 0.03f;
        Apply(true);
    }
}
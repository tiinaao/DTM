using UnityEngine;
using UnityEngine.UI;

public class VirusOverlay : MonoBehaviour
{
    [Header("References")]
    public AnimReach animReach;
    public Shader virusShader;
    public Shader veinsShader;
    public RawImage veinsImage;

    [Header("Intensity")]
    public float maxIntensity = 0.6f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 3f;

    [Header("Colors")]
    public Color vignColorA = new Color(0.45f, 0.00f, 0.00f, 1f);
    public Color vignColorB = new Color(0.12f, 0.00f, 0.00f, 1f);
    public Color vignColorDark = new Color(0.0f, 0.0f, 0.0f, 1f);

    [Header("Grain")]
    public float grainStrength = 0.365f;
    public float grainScale = 16.5f;
    public float grainSpeed = 2.5f;

    [Header("Shape")]
    public float centerX = 0.478f;
    public float centerY = 0.478f;
    public float radiusX = 0.45f;
    public float radiusY = 0.45f;

    [Header("Veins Properties")]
    public Texture2D veinsColour;
    public Texture2D veinsMaskMap1;
    public Texture2D veinsMaskMap2;

    public float veinsprimaryContourSmoothness = 0.595f;
    public float veinssecondaryContourSmoothness = 0.8f;
    public float smoothnessOfMaterial = 0f;
    public float emissionBrightness = 0.65f;
    public Color emissionColour = Color.red;
    public float wiggleSpeed = 1f;
    public float wiggleIntensity = 0.25f;

    RawImage rawImage;
    Material mat;
    Material veinsMat;
    float currentIntensity = 0f;
    float currentVeinsIntensity = 0f;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        mat = new Material(virusShader);
        rawImage.material = mat;
        rawImage.color = Color.white;
        veinsImage.color = Color.white;

        if (veinsShader != null && veinsImage != null)
        {
            veinsMat = new Material(veinsShader);
            veinsImage.material = veinsMat;
            veinsImage.color = Color.white;
            PushVeinsProps(0f);
        }

        PushAllProps();
    }

    void Update()
    {
        if (animReach == null) return;

        float target = Mathf.Min(1f - animReach.CurrentSlowAmount, maxIntensity);
        bool fadingIn = target > currentIntensity;
        float duration = fadingIn ? fadeInDuration : fadeOutDuration;
        float step = Time.deltaTime / duration;
        float veinsStep = Time.deltaTime / (fadingIn ? fadeInDuration : fadeOutDuration * 0.3f);

        currentIntensity = Mathf.MoveTowards(currentIntensity, target, step);
        mat.SetFloat("_Intensity", currentIntensity);
        PushAllProps();

        if (veinsMat != null)
        {
            float veinsTarget = maxIntensity > 0f ? currentIntensity / maxIntensity : 0f;
            currentVeinsIntensity = Mathf.MoveTowards(currentVeinsIntensity, veinsTarget, veinsStep);
            veinsImage.color = new Color(1f, 1f, 1f, currentVeinsIntensity);
            PushVeinsProps(currentVeinsIntensity);
        }
    }

    void PushAllProps()
    {
        if (mat == null) return;
        mat.SetColor("_VignColor0", vignColorA);
        mat.SetColor("_VignColor1", vignColorB);
        mat.SetColor("_VignColorDark", vignColorDark);
        mat.SetFloat("_GrainStrength", grainStrength);
        mat.SetFloat("_GrainScale", grainScale);
        mat.SetFloat("_GrainSpeed", grainSpeed);
        mat.SetFloat("_CenterX", centerX);
        mat.SetFloat("_CenterY", centerY);
        mat.SetFloat("_RadiusX", radiusX);
        mat.SetFloat("_RadiusY", radiusY);
    }

    void PushVeinsProps(float intensity)
    {
        if (veinsMat == null) return;
        if (veinsColour != null) veinsMat.SetTexture("_colour", veinsColour);
        if (veinsMaskMap1 != null) veinsMat.SetTexture("_mask_map1", veinsMaskMap1);
        if (veinsMaskMap2 != null) veinsMat.SetTexture("_mask_map2", veinsMaskMap2);
        veinsMat.SetFloat("_driver_veins_primary", intensity);
        veinsMat.SetFloat("_driver_veins_secondary", intensity * 0.7f);
        veinsMat.SetFloat("_veins_primary_contour_smoothness", veinsprimaryContourSmoothness);
        veinsMat.SetFloat("_veins_secondary_contour_smoothness", veinssecondaryContourSmoothness);
        veinsMat.SetFloat("_smoothness_of_material", smoothnessOfMaterial * intensity);
        veinsMat.SetFloat("_emission_brightness", emissionBrightness * intensity);
        veinsMat.SetColor("_emission_colour", emissionColour);
        veinsMat.SetFloat("_wiggle_speed", wiggleSpeed);
        veinsMat.SetFloat("_wiggle_intensity", wiggleIntensity * intensity);
        veinsMat.SetFloat("_Alpha", intensity);
    }

#if UNITY_EDITOR
    void OnValidate() { if (mat != null) PushAllProps(); }
#endif
}
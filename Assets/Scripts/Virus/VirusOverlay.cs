using UnityEngine;
using UnityEngine.UI;

public class VirusOverlay : MonoBehaviour
{
    [Header("References")]
    public AnimReach animReach;

    [Header("Intensity")]
    public float maxIntensity = 1f;
    public float fadeInSpeed = 0.18f;
    public float fadeOutSpeed = 0.45f;

    [Header("Colors")]
    public Color vignColorA = new Color(0.45f, 0.00f, 0.00f, 1f);
    public Color vignColorB = new Color(0.12f, 0.00f, 0.00f, 1f);
    public Color vignColorDark = new Color(0.0f, 0.0f, 0.0f, 1f);

    [Header("Grain")]
    [Range(0f, 1f)] public float grainStrength = 0.72f;
    [Range(1f, 40f)] public float grainScale = 18f;
    [Range(0f, 10f)] public float grainSpeed = 3.8f;

    [Header("Shape")]
    [Range(0f, 1f)] public float centerX = 0.5f;
    [Range(0f, 1f)] public float centerY = 0.5f;
    [Range(0.1f, 2f)] public float radiusX = 0.7f;
    [Range(0.1f, 2f)] public float radiusY = 0.5f;
    [Range(0f, 0.5f)] public float cornerRadius = 0.15f;

    RawImage rawImage;
    Material mat;
    float currentIntensity = 0f;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        mat = new Material(Shader.Find("UI/VirusOverlay"));
        rawImage.material = mat;
        rawImage.color = Color.white;
        PushAllProps();
    }

    void Update()
    {
        if (animReach == null) return;

        float target = Mathf.Min(1f - animReach.CurrentSlowAmount, maxIntensity);
        float speed = target > currentIntensity ? fadeInSpeed : fadeOutSpeed;
        currentIntensity = Mathf.MoveTowards(currentIntensity, target, speed * Time.deltaTime);

        mat.SetFloat("_Intensity", currentIntensity);
        PushAllProps();
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
        mat.SetFloat("_CornerRadius", cornerRadius);
    }

#if UNITY_EDITOR
    void OnValidate() { if (mat != null) PushAllProps(); }
#endif
}
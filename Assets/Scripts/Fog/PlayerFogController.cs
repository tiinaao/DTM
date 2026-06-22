using UnityEngine;

[ExecuteAlways]
public class PlayerFogController : MonoBehaviour
{
    public Transform player;

    public float innerRadius = 60f;
    public float outerRadius = 90f;

    public Color fogColor = new Color(0.72f, 0.76f, 0.82f, 1f);
    public float maxDensity = 0.8f;
    public float heightFalloff = 0.05f;
    public float noiseScale = 0.02f;
    public float noiseSpeed = 0.05f;

    static readonly int PlayerPosID = Shader.PropertyToID("_FogPlayerPos");
    static readonly int InnerRadiusID = Shader.PropertyToID("_FogInnerRadius");
    static readonly int OuterRadiusID = Shader.PropertyToID("_FogOuterRadius");
    static readonly int FogColorID = Shader.PropertyToID("_FogColor");
    static readonly int MaxDensityID = Shader.PropertyToID("_FogMaxDensity");
    static readonly int HeightFalloffID = Shader.PropertyToID("_FogHeightFalloff");
    static readonly int NoiseParamsID = Shader.PropertyToID("_FogNoiseParams");

    void LateUpdate()
    {
        if (player == null) return;

        Shader.SetGlobalVector(PlayerPosID, player.position);
        Shader.SetGlobalFloat(InnerRadiusID, innerRadius);
        Shader.SetGlobalFloat(OuterRadiusID, outerRadius);
        Shader.SetGlobalColor(FogColorID, fogColor);
        Shader.SetGlobalFloat(MaxDensityID, maxDensity);
        Shader.SetGlobalFloat(HeightFalloffID, heightFalloff);
        Shader.SetGlobalVector(NoiseParamsID, new Vector4(noiseScale, noiseSpeed, Time.time, 0f));
    }
}

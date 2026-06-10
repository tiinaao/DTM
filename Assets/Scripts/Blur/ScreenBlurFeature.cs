using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenBlurFeature : ScriptableRendererFeature
{
    public static float CurrentBlur = 0f;

    ScreenBlurPass pass;
    Material mat;

    public override void Create()
    {
        mat = new Material(Shader.Find("Hidden/ScreenBlur"));
        pass = new ScreenBlurPass(mat);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        pass.blurAmount = CurrentBlur;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        if (mat != null) CoreUtils.Destroy(mat);
    }
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ScreenBlurPass : ScriptableRenderPass
{
    Material mat;
    RTHandle tempRT;
    static readonly int BlurAmountID = Shader.PropertyToID("_BlurAmount");

    public float blurAmount = 0f;

    public ScreenBlurPass(Material material)
    {
        mat = material;
        renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (mat == null || blurAmount <= 0.001f) return;

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;

        TextureHandle src = resourceData.activeColorTexture;
        TextureHandle tmp = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurTemp", false);

        mat.SetFloat(BlurAmountID, blurAmount);

        using (var builder = renderGraph.AddUnsafePass<PassData>("ScreenBlur", out var passData))
        {
            passData.mat = mat;
            passData.src = src;
            passData.tmp = tmp;
            passData.blurAmount = blurAmount;

            builder.UseTexture(src, AccessFlags.ReadWrite);
            builder.UseTexture(tmp, AccessFlags.ReadWrite);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                data.mat.SetFloat(BlurAmountID, data.blurAmount);
                Blitter.BlitCameraTexture(cmd, data.src, data.tmp, data.mat, 0);
                Blitter.BlitCameraTexture(cmd, data.tmp, data.src);
            });
        }
    }

    class PassData
    {
        public Material mat;
        public TextureHandle src;
        public TextureHandle tmp;
        public float blurAmount;
    }

    public void Dispose()
    {
        tempRT?.Release();
    }
}
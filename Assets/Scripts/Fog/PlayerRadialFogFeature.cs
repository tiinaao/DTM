using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class PlayerRadialFogFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material fogMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();
    PlayerRadialFogPass pass;

    public override void Create()
    {
        pass = new PlayerRadialFogPass
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.fogMaterial == null) return;
        pass.Setup(settings.fogMaterial);
        renderer.EnqueuePass(pass);
    }

    class PlayerRadialFogPass : ScriptableRenderPass
    {
        Material material;
        const string ProfilerTag = "Player Radial Fog";

        class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        public void Setup(Material mat)
        {
            material = mat;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;

            var descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "_PlayerFogTemp";
            descriptor.clearBuffer = false;
            descriptor.depthBufferBits = 0;
            TextureHandle tempTexture = renderGraph.CreateTexture(descriptor);

            renderGraph.AddBlitPass(source, tempTexture, Vector2.one, Vector2.zero, passName: "Player Radial Fog Copy");

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(ProfilerTag, out var passData))
            {
                passData.material = material;
                passData.source = tempTexture;

                builder.UseTexture(tempTexture);
                builder.SetRenderAttachment(source, 0);
                builder.UseTexture(resourceData.activeDepthTexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }
    }
}
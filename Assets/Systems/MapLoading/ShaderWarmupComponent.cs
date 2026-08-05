using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class ShaderWarmupComponent : MonoBehaviour, IWarmable
{
    public Shader[] shaders;
    public Material[] materials;

    public bool IsComplete { get; private set; } = false;

    private void OnEnable()
    {
        WarmupManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        WarmupManager.Instance?.Unregister(this);
    }

    public IEnumerator Warmup()
    {
        if (materials != null)
        {
            foreach (var mat in materials)
            {
                if (mat == null) continue;

                var urpAsset = GraphicsSettings.currentRenderPipeline
                               as UniversalRenderPipelineAsset;
                if (urpAsset != null)
                {
                    ShaderWarmup.WarmupShader(mat.shader, new ShaderWarmupSetup());
                }

                yield return null;
            }
        }

        IsComplete = true;
    }

    public void WarmupBlocking()
    {
        Debug.LogWarning("[ShaderWarmupComponent] WarmupBlocking() freezes the main thread. Use WarmupManager queue instead.");
    }
}
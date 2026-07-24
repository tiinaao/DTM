using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreenBlurEffect : MonoBehaviour
{
    public AnimReach animReach;

    public float interval = 8f;
    public float duration = 0.6f;
    public float maxBlur = 0.8f;

    Material mat;
    float blurAmount = 0f;

    void Awake()
    {
        mat = new Material(Shader.Find("Hidden/ScreenBlur"));
    }

    void Start()
    {
        StartCoroutine(BlurLoop());
    }

    IEnumerator BlurLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (animReach != null && animReach.CurrentSlowAmount >= 1f)
                continue;

            float intensity = animReach != null ? (1f - animReach.CurrentSlowAmount) : 1f;
            float peak = maxBlur * intensity;

            float half = duration * 0.5f;

            for (float e = 0f; e < half; e += Time.deltaTime)
            {
                blurAmount = Mathf.Lerp(0f, peak, e / half);
                yield return null;
            }

            for (float e = 0f; e < half; e += Time.deltaTime)
            {
                blurAmount = Mathf.Lerp(peak, 0f, e / half);
                yield return null;
            }

            blurAmount = 0f;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (mat == null || blurAmount <= 0.001f)
        {
            Graphics.Blit(src, dst);
            return;
        }

        mat.SetFloat("_BlurAmount", blurAmount);
        Graphics.Blit(src, dst, mat);
    }
}

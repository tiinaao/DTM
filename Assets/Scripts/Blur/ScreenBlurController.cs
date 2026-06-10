using System.Collections;
using UnityEngine;

public class ScreenBlurController : MonoBehaviour
{
    public AnimReach animReach;

    [Range(2f, 30f)] public float interval = 8f;
    [Range(0.1f, 3f)] public float duration = 0.6f;
    [Range(0f, 1f)] public float maxBlur = 0.8f;

    void Start()
    {
        StartCoroutine(BlurLoop());
    }

    IEnumerator BlurLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            float intensity = animReach != null ? (1f - animReach.CurrentSlowAmount) : 1f;

            float peak = maxBlur * intensity;
            float half = duration * 0.5f;

            for (float e = 0f; e < half; e += Time.deltaTime)
            {
                ScreenBlurFeature.CurrentBlur = Mathf.Lerp(0f, peak, e / half);
                yield return null;
            }

            for (float e = 0f; e < half; e += Time.deltaTime)
            {
                ScreenBlurFeature.CurrentBlur = Mathf.Lerp(peak, 0f, e / half);
                yield return null;
            }

            ScreenBlurFeature.CurrentBlur = 0f;
        }
    }

    void OnDestroy()
    {
        ScreenBlurFeature.CurrentBlur = 0f;
    }
}
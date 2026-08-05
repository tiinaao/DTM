using System.Collections;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class GlobalWarmupComponent : MonoBehaviour
{
    public TextMeshProUGUI prewarmText;
    public Material[] materialsToTouch;
    public Texture[] texturesToTouch;
    public int warmFrames = 5;

    private IEnumerator Start()
    {
        if (prewarmText != null)
        {
            string warm = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,";
            int frames = Mathf.Max(1, warmFrames);
            int perFrame = Mathf.Max(1, warm.Length / frames);
            for (int i = 0; i < frames; i++)
            {
                int start = i * perFrame;
                int len = Mathf.Min(perFrame, warm.Length - start);
                if (len <= 0) break;
                prewarmText.text = warm.Substring(start, len);
                prewarmText.ForceMeshUpdate();
                yield return null;
            }
            prewarmText.text = string.Empty;
        }

        if (materialsToTouch != null)
        {
            foreach (var m in materialsToTouch)
            {
                if (m == null) continue;
                var t = m.mainTexture;
                yield return null;
            }
        }

        if (texturesToTouch != null)
        {
            foreach (var tx in texturesToTouch)
            {
                if (tx == null) continue;
                var w = tx.width;
                yield return null;
            }
        }

        yield break;
    }
}

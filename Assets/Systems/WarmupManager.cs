using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWarmable
{
    IEnumerator Warmup();
}

[DisallowMultipleComponent]
public class WarmupManager : MonoBehaviour
{
    public static WarmupManager Instance { get; private set; }

    private readonly LinkedList<IEnumerator> queue = new LinkedList<IEnumerator>();
    private readonly Dictionary<IWarmable, LinkedListNode<IEnumerator>> registry = new Dictionary<IWarmable, LinkedListNode<IEnumerator>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private void Start()
    {
        StartCoroutine(ProcessQueue());
    }

    public void Register(IWarmable w)
    {
        if (w == null) return;
        if (registry.ContainsKey(w)) return;
        var e = w.Warmup();
        var node = queue.AddLast(e);
        registry[w] = node;
    }

    public void Unregister(IWarmable w)
    {
        if (w == null) return;
        if (!registry.TryGetValue(w, out var node)) return;
        queue.Remove(node);
        registry.Remove(w);
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (queue.First == null)
            {
                yield return null;
                continue;
            }

            var node = queue.First;
            var enumerator = node.Value;
            bool hasNext = false;
            try { hasNext = enumerator.MoveNext(); } catch { hasNext = false; }

            if (!hasNext)
            {
                queue.RemoveFirst();
                IWarmable found = null;
                foreach (var kv in registry)
                {
                    if (kv.Value == node) { found = kv.Key; break; }
                }
                if (found != null) registry.Remove(found);
            }
            else
            {
                queue.RemoveFirst();
                queue.AddLast(enumerator);
            }

            yield return null; 
        }
    }
}

[DisallowMultipleComponent]
public class ChunkWarmupComponent : MonoBehaviour, IWarmable
{
    public Material[] materialsToTouch;
    public GameObject[] prefabsToInstantiate;
    public Texture[] texturesToTouch;
    public AudioClip[] audioClipsToLoad;
    public int warmFrames = 3;

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
        if (materialsToTouch != null)
        {
            foreach (var m in materialsToTouch)
            {
                if (m == null) continue;
                var t = m.mainTexture;
                yield return null;
            }
        }
        if (prefabsToInstantiate != null)
        {
            foreach (var p in prefabsToInstantiate)
            {
                if (p == null) continue;
                GameObject inst = null;
                try
                {
                    inst = Object.Instantiate(p);
                    inst.SetActive(false);
                    inst.hideFlags = HideFlags.DontSave;
                }
                catch { inst = null; }
                yield return null;

                if (inst != null)
                {
                    try { Object.DestroyImmediate(inst); } catch { }
                }
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

        if (audioClipsToLoad != null)
        {
            foreach (var ac in audioClipsToLoad)
            {
                if (ac == null) continue;
                try { ac.LoadAudioData(); } catch { }
                yield return null;
            }
        }
        yield break;
    }
}

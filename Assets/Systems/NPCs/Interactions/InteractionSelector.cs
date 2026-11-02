using System.Collections.Generic;
using UnityEngine;

public static class InteractionSelector
{
    private class Entry
    {
        public IInteractable target;
        public Transform transform;
    }

    private static readonly List<Entry> candidates = new List<Entry>();
    private static int cachedFrame = -1;
    private static IInteractable cachedActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        candidates.Clear();
        cachedFrame = -1;
        cachedActive = null;
    }

    public static void Register(IInteractable target, Transform transform)
    {
        if (target == null || transform == null) return;
        for (int i = 0; i < candidates.Count; i++)
            if (candidates[i].target == target) return;

        candidates.Add(new Entry { target = target, transform = transform });
        cachedFrame = -1;
    }

    public static void Unregister(IInteractable target)
    {
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (candidates[i].target == target) candidates.RemoveAt(i);
        }
        cachedFrame = -1;
    }

    public static IInteractable Active
    {
        get
        {
            Resolve();
            return cachedActive;
        }
    }

    public static bool IsActive(IInteractable target)
    {
        Resolve();
        return cachedActive == target;
    }

    private static void Resolve()
    {
        if (cachedFrame == Time.frameCount) return;
        cachedFrame = Time.frameCount;
        cachedActive = null;

        Camera cam = Camera.main;
        PlayerModel player = GameManager.Instance.Player;
        Vector3 origin = player != null ? player.transform.position
                       : cam != null ? cam.transform.position
                       : Vector3.zero;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;

        float best = float.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            Entry e = candidates[i];
            if (e.transform == null)
            {
                candidates.RemoveAt(i);
                continue;
            }

            if (string.IsNullOrEmpty(e.target.InteractionText)) continue;

            Vector3 to = e.transform.position - origin;
            float dist = to.magnitude;
            float dot = dist > 0.001f ? Vector3.Dot(forward, to / dist) : 1f;

            float score = dist * (1.5f - 0.5f * dot);
            if (score < best)
            {
                best = score;
                cachedActive = e.target;
            }
        }
    }
}
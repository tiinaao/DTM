using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AudioOcclusionManager : MonoBehaviour
{
    public AudioClip song;
    public LayerMask occluderMask;
    public int maxAudibleSpeakers = 2;
    public int checksPerFrame = 4;
    public int wallsForFullMuffle = 2;
    public float listenerSpread = 0.4f;
    public float listenerHeight = 0.6f;
    public float startDelay = 0.3f;
    public float driftCheckInterval = 2f;
    public bool driftCorrection = false;
    public float speakerClearance = 0.3f;
    public bool useDiffraction = true;
    public float diffractionRadius = 3f;
    [Range(0f, 1f)] public float diffractedOcclusion = 0.35f;

    static readonly List<OccludedSpeaker> speakers = new List<OccludedSpeaker>();
    static readonly RaycastHit[] hitBuffer = new RaycastHit[8];
    static AudioOcclusionManager instance;
    static double startDspTime;
    static bool started;

    Transform listener;
    int cursor;
    float driftTimer;

    void Awake()
    {
        instance = this;
        started = false;
        Physics.queriesHitBackfaces = true;
    }

    void Start()
    {
        var al = FindAnyObjectByType<AudioListener>();
        if (al != null) listener = al.transform;

        startDspTime = AudioSettings.dspTime + startDelay;
        started = true;

        for (int i = 0; i < speakers.Count; i++)
            speakers[i].StartSynced(song, startDspTime);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        started = false;
        speakers.Clear();
    }

    public static void Register(OccludedSpeaker s)
    {
        if (!speakers.Contains(s)) speakers.Add(s);
        if (started && instance != null)
            s.StartSynced(instance.song, startDspTime);
    }

    public static void Unregister(OccludedSpeaker s)
    {
        speakers.Remove(s);
    }

    void Update()
    {
        int count = speakers.Count;
        if (count == 0 || listener == null) return;

        Vector3 pos = listener.position;

        OccludedSpeaker.GlobalGain = 1f / Mathf.Sqrt(Mathf.Max(1, Mathf.Min(maxAudibleSpeakers, count)));

        UpdateGates(pos, count);

        int budget = Mathf.Min(checksPerFrame, count);
        for (int i = 0; i < budget; i++)
        {
            cursor = (cursor + 1) % count;
            var s = speakers[cursor];

            float max = s.Source.maxDistance;
            if ((s.transform.position - pos).sqrMagnitude > max * max)
            {
                s.targetOcclusion = 0f;
                continue;
            }

            s.targetOcclusion = CalculateOcclusion(s.transform.position);
        }

        driftTimer += Time.deltaTime;
        if (driftCorrection && driftTimer >= driftCheckInterval)
        {
            driftTimer = 0f;
            for (int i = 0; i < count; i++)
                speakers[i].CorrectDrift(startDspTime);
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < count; i++)
            speakers[i].Tick(dt, pos);
    }

    void UpdateGates(Vector3 pos, int count)
    {
        int limit = Mathf.Max(1, maxAudibleSpeakers);

        for (int i = 0; i < count; i++)
        {
            float di = (speakers[i].transform.position - pos).sqrMagnitude;
            int rank = 0;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;
                float dj = (speakers[j].transform.position - pos).sqrMagnitude;
                if (dj < di || (dj == di && j < i)) rank++;
            }

            speakers[i].targetGate = rank < limit ? 1f : 0f;
        }
    }

    float CalculateOcclusion(Vector3 from)
    {
        Vector3 p = listener.position;
        Vector3 right = listener.right * listenerSpread;
        Vector3 up = Vector3.up * listenerHeight;

        float total = 0f;
        total += Cast(from, p);
        total += Cast(from, p + right);
        total += Cast(from, p - right);
        total += Cast(from, p + up);
        float direct = total * 0.25f;

        if (useDiffraction && direct > diffractedOcclusion)
        {
            if (HasDiffractedPath(from, p))
                direct = Mathf.Min(direct, diffractedOcclusion);
        }

        return direct;
    }

    float Cast(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return 0f;

        Vector3 n = dir / dist;

        float skip = Mathf.Min(speakerClearance, dist * 0.5f);
        Vector3 origin = from + n * skip;
        float rayLen = dist - skip;

        int hits = Physics.RaycastNonAlloc(origin, n, hitBuffer, rayLen, occluderMask, QueryTriggerInteraction.Ignore);
        return Mathf.Clamp01((float)hits / Mathf.Max(1, wallsForFullMuffle));
    }

    bool IsClear(Vector3 a, Vector3 b)
    {
        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        Vector3 n = dir / dist;
        float skip = Mathf.Min(speakerClearance, dist * 0.5f);
        return !Physics.Raycast(a + n * skip, n, dist - skip, occluderMask, QueryTriggerInteraction.Ignore);
    }

    bool HasDiffractedPath(Vector3 speaker, Vector3 listenerPos)
    {
        const int dirs = 8;
        for (int i = 0; i < dirs; i++)
        {
            float a = i * Mathf.PI * 2f / dirs;
            Vector3 offset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * diffractionRadius;

            Vector3 w1 = speaker + offset;
            if (IsClear(speaker, w1) && IsClear(w1, listenerPos)) return true;

            Vector3 w2 = listenerPos + offset;
            if (IsClear(speaker, w2) && IsClear(w2, listenerPos)) return true;
        }

        Vector3 over = Vector3.Lerp(speaker, listenerPos, 0.5f) + Vector3.up * diffractionRadius;
        return IsClear(speaker, over) && IsClear(over, listenerPos);
    }
}
using UnityEngine;

public class OccludedSpeaker : MonoBehaviour
{
    public static float GlobalGain = 1f;
    public float minDistance = 2000f;
    public float maxDistance = 30000f;
    [Range(0.3f, 0.98f)] public float fadeStart = 0.8f;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 360f)] public float stereoSpread = 180f;
    public float clearCutoff = 20000f;
    public float farCutoff = 5000f;
    public float mutedCutoff = 500f;
    [Range(0f, 1f)] public float volumeLossWhenBlocked = 0.5f;
    public float occludeSpeed = 4f;
    public float clearSpeed = 3f;
    public float gateSmoothing = 5f;

    [HideInInspector] public float targetOcclusion;
    [HideInInspector] public float targetGate = 1f;

    public AudioSource Source { get; private set; }

    AudioLowPassFilter lpf;
    float occlusion;
    float gate = 1f;

    void Awake()
    {
        Source = GetComponent<AudioSource>();
        lpf = GetComponent<AudioLowPassFilter>();

        Source.Stop();
        Source.clip = null;
        Source.playOnAwake = false;
        Source.loop = true;
        Source.spatialBlend = 1f;
        Source.dopplerLevel = 0f;
        Source.spread = stereoSpread;
        Source.volume = volume;

        ApplyRolloff();

        lpf.lowpassResonanceQ = 0.707f;
        lpf.cutoffFrequency = Mathf.Min(clearCutoff, 20000f);
        lpf.enabled = false;
    }

    void OnValidate()
    {
        if (minDistance < 0.5f) minDistance = 0.5f;
        if (maxDistance < minDistance + 1f) maxDistance = minDistance + 1f;
        if (Application.isPlaying && Source != null) ApplyRolloff();
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    float RolloffValue(float x)
    {
        float d = Mathf.Max(x * maxDistance, 0.0001f);
        float inverse = Mathf.Min(1f, minDistance / d);
        float window = 1f - Smooth((x - fadeStart) / (1f - fadeStart));
        return inverse * window;
    }

    void ApplyRolloff()
    {
        Source.minDistance = minDistance;
        Source.maxDistance = maxDistance;
        Source.spread = stereoSpread;
        Source.rolloffMode = AudioRolloffMode.Custom;

        const int steps = 160;
        float[] xs = new float[steps + 1];
        float[] ys = new float[steps + 1];

        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            xs[i] = u * u;
            ys[i] = RolloffValue(xs[i]);
        }

        var curve = new AnimationCurve();
        for (int i = 0; i <= steps; i++)
        {
            int a = Mathf.Max(0, i - 1);
            int b = Mathf.Min(steps, i + 1);
            float tangent = (ys[b] - ys[a]) / Mathf.Max(xs[b] - xs[a], 1e-6f);
            curve.AddKey(new Keyframe(xs[i], ys[i], tangent, tangent));
        }

        Source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
    }

    void OnEnable()
    {
        AudioOcclusionManager.Register(this);
    }

    void OnDisable()
    {
        AudioOcclusionManager.Unregister(this);
    }

    public void StartSynced(AudioClip clip, double startDspTime)
    {
        if (clip == null) return;

        Source.Stop();
        Source.clip = clip;
        Source.loop = true;

        double now = AudioSettings.dspTime;

        if (now < startDspTime)
        {
            Source.PlayScheduled(startDspTime);
        }
        else
        {
            double elapsed = now - startDspTime;
            Source.timeSamples = (int)((elapsed * clip.frequency) % clip.samples);
            Source.Play();
        }
    }

    public void CorrectDrift(double startDspTime)
    {
        if (Source.clip == null || !Source.isPlaying) return;

        double now = AudioSettings.dspTime;
        if (now < startDspTime) return;

        int length = Source.clip.samples;
        int expected = (int)(((now - startDspTime) * Source.clip.frequency) % length);
        int actual = Source.timeSamples;

        int diff = Mathf.Abs(expected - actual);
        diff = Mathf.Min(diff, length - diff);

        if (diff > Source.clip.frequency * 0.05f)
            Source.timeSamples = expected;
    }

    public void Tick(float dt, Vector3 listenerPos)
    {
        float speed = targetOcclusion > occlusion ? occludeSpeed : clearSpeed;
        occlusion = Mathf.Lerp(occlusion, targetOcclusion, 1f - Mathf.Exp(-speed * dt));
        if (Mathf.Abs(occlusion - targetOcclusion) < 0.0005f) occlusion = targetOcclusion;

        gate = Mathf.Lerp(gate, targetGate, 1f - Mathf.Exp(-gateSmoothing * dt));
        if (Mathf.Abs(gate - targetGate) < 0.0005f) gate = targetGate;

        float dist = Vector3.Distance(transform.position, listenerPos);
        float far = Smooth((dist - minDistance) / (maxDistance - minDistance));

        float top = Mathf.Min(clearCutoff, 20000f);
        float logClear = Mathf.Log(top);
        float logCut = logClear
            + (Mathf.Log(farCutoff) - logClear) * far
            + (Mathf.Log(mutedCutoff) - logClear) * occlusion;

        float cutoff = Mathf.Clamp(Mathf.Exp(logCut), 200f, top);

        lpf.enabled = cutoff < top * 0.98f;
        lpf.cutoffFrequency = cutoff;
        Source.volume = volume * GlobalGain * (1f - volumeLossWhenBlocked * occlusion) * gate;
    }
}
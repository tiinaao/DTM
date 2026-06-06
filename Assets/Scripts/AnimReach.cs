using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimReach : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform bonesRoot;

    [Header("Detection")]
    public float detectDistance = 3f;
    public float loseDistance = 4f;
    public float reactionDelay = 0.2f;

    [Header("Tentacle Feel")]
    public float maxBendAngle = 70f;
    public float boneSpeed = 4f;
    public float tipBias = 2.5f;

    [Header("Reach & Return")]
    public float reachSpeed = 4f;   
    public float returnSpeed = 1f;  
    public float orbitRadius = 0.5f;
    public float orbitSpeed = 1.5f;
    public float stopDistance = 0.6f;   

    [Header("Idle Sway")]
    public float swayAmount = 0.09f;
    public float swaySpeed = 0.9f;

    class Tentacle
    {
        public Transform[] bones;
        public Quaternion[] restLocalRot;
        public Quaternion[] currentLocalRot;
        public Vector3 ikTarget;
        public float phase;
        public int boneCount;    
        public float perBoneAngle;  
    }

    List<Tentacle> tentacles = new List<Tentacle>();
    bool active = false;
    bool cooling = false;
    bool ready = false;

    void Start()
    {
        if (bonesRoot == null) { Debug.LogError("Assign bonesRoot!"); return; }
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null; 

        foreach (Transform child in bonesRoot)
        {
            Tentacle t = BuildTentacle(child);
            if (t != null) tentacles.Add(t);
        }

        ready = true;
    }

    Tentacle BuildTentacle(Transform root)
    {
        var chain = new List<Transform>();
        Transform cur = root;
        while (cur != null)
        {
            chain.Add(cur);
            cur = cur.childCount > 0 ? cur.GetChild(0) : null;
        }

        if (chain.Count < 2) return null;

        var t = new Tentacle();
        t.bones = chain.ToArray();
        t.restLocalRot = new Quaternion[chain.Count];
        t.currentLocalRot = new Quaternion[chain.Count];
        t.phase = Random.Range(0f, Mathf.PI * 2f);
        t.boneCount = chain.Count;
        t.perBoneAngle = Mathf.Clamp(maxBendAngle * (4f / Mathf.Max(chain.Count, 1)), 20f, 90f);

        for (int i = 0; i < chain.Count; i++)
        {
            t.restLocalRot[i] = chain[i].localRotation;
            t.currentLocalRot[i] = chain[i].localRotation;
        }

        t.ikTarget = chain[chain.Count - 1].position;
        return t;
    }

    void Update()
    {
        if (!ready || player == null) return;

        float dist = Vector3.Distance(bonesRoot.position, player.position);

        if (!active && !cooling && dist <= detectDistance)
            StartCoroutine(Activate());

        if (active && dist >= loseDistance)
        {
            active = false;
            cooling = true;
            StartCoroutine(Cooldown());
        }

        foreach (var t in tentacles)
            UpdateTentacle(t);
    }

    void UpdateTentacle(Tentacle t)
    {
        int n = t.bones.Length;

        if (active)
        {
            float time = Time.time + t.phase;
            Vector3 orbit = new Vector3(
                Mathf.Cos(time * orbitSpeed),
                Mathf.Sin(time * orbitSpeed * 0.5f),
                Mathf.Sin(time * orbitSpeed * 0.8f)
            ) * orbitRadius;

            Vector3 goal = player.position + orbit;
            goal.y = Mathf.Max(goal.y, t.bones[0].position.y);

            float dist = Vector3.Distance(bonesRoot.position, player.position);
            float proximityT = Mathf.Clamp01(dist / detectDistance);
            float adjustedSpeed = Mathf.Lerp(reachSpeed * 0.2f, reachSpeed, proximityT);

            t.ikTarget = Vector3.Lerp(t.ikTarget, goal, adjustedSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 restTip = GetRestTipWorldPos(t);
            restTip.y = Mathf.Max(restTip.y, t.bones[0].position.y);
            t.ikTarget = Vector3.Lerp(t.ikTarget, restTip, returnSpeed * Time.deltaTime);
        }

        for (int i = 0; i < n; i++)
            t.bones[i].localRotation = t.currentLocalRot[i];
        Vector3 rootToTarget = (t.ikTarget - t.bones[0].position);
        float totalDist = rootToTarget.magnitude;

        for (int i = 0; i < n - 1; i++)
        {
            Transform bone = t.bones[i];

            Vector3 toTarget = t.ikTarget - bone.position;
            if (toTarget.sqrMagnitude < 0.0001f) continue;

            Vector3 toNext = t.bones[i + 1].position - bone.position;
            if (toNext.sqrMagnitude < 0.0001f) continue;

            toTarget = toTarget.normalized;
            toNext = toNext.normalized;

            if (Vector3.Dot(toTarget, toNext) > 0.9999f) continue;

            Quaternion aimRot = Quaternion.FromToRotation(toNext, toTarget);
            if (float.IsNaN(aimRot.x) || float.IsNaN(aimRot.y) ||
                float.IsNaN(aimRot.z) || float.IsNaN(aimRot.w)) continue;

            float angle;
            Vector3 axis;
            aimRot.ToAngleAxis(out angle, out axis);

            if (float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z)) continue;
            if (axis.sqrMagnitude < 0.0001f) continue;

            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -t.perBoneAngle, t.perBoneAngle);
            aimRot = Quaternion.AngleAxis(angle, axis);

            float frac = (float)i / (n - 2 < 1 ? 1 : n - 2);  
            float speed = boneSpeed * Mathf.Lerp(0.5f, tipBias, frac);
            float alignment = 1f - Mathf.Clamp01(Vector3.Dot(toNext, toTarget));
            float urgency = Mathf.Lerp(0.5f, 1.5f, alignment);
            speed *= urgency;

            Quaternion goalWorld = aimRot * bone.rotation;
            if (float.IsNaN(goalWorld.x) || float.IsNaN(goalWorld.y) ||
                float.IsNaN(goalWorld.z) || float.IsNaN(goalWorld.w)) continue;

            Quaternion newWorld = Quaternion.Slerp(bone.rotation, goalWorld, speed * Time.deltaTime);
            if (float.IsNaN(newWorld.x) || float.IsNaN(newWorld.y) ||
                float.IsNaN(newWorld.z) || float.IsNaN(newWorld.w)) continue;

            Quaternion parentRot = (bone.parent != null) ? bone.parent.rotation : Quaternion.identity;
            if (float.IsNaN(parentRot.x) || float.IsNaN(parentRot.y) ||
                float.IsNaN(parentRot.z) || float.IsNaN(parentRot.w)) continue;

            t.currentLocalRot[i] = Quaternion.Inverse(parentRot) * newWorld;

            if (float.IsNaN(t.currentLocalRot[i].x) || float.IsNaN(t.currentLocalRot[i].y) ||
                float.IsNaN(t.currentLocalRot[i].z) || float.IsNaN(t.currentLocalRot[i].w)) continue;

            bone.localRotation = t.currentLocalRot[i];
        }

        float st = Time.time * swaySpeed + t.phase;
        for (int i = 0; i < n; i++)
        {
            float frac = (float)i / (n - 1);   
            float ang = Mathf.Sin(st * 1.1f + i * 0.5f) * swayAmount * frac * 30f;
            Quaternion swayRot = Quaternion.AngleAxis(ang, t.bones[i].right);

            t.currentLocalRot[i] = swayRot * t.currentLocalRot[i];
            t.bones[i].localRotation = t.currentLocalRot[i];
        }
    }

    Vector3 GetRestTipWorldPos(Tentacle t)
    {
        Quaternion[] saved = new Quaternion[t.bones.Length];
        for (int i = 0; i < t.bones.Length; i++)
            saved[i] = t.bones[i].localRotation;

        for (int i = 0; i < t.bones.Length; i++)
            t.bones[i].localRotation = t.restLocalRot[i];

        Vector3 tipPos = t.bones[t.bones.Length - 1].position;

        for (int i = 0; i < t.bones.Length; i++)
            t.bones[i].localRotation = saved[i];

        return tipPos;
    }

    IEnumerator Activate()
    {
        cooling = true;
        yield return new WaitForSeconds(reactionDelay);
        active = true;
        cooling = false;
    }

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(1f);
        cooling = false;
    }
}
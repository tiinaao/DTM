using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimReach : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform bonesRoot;

    [Header("Detection")]
    public float detectDistance = 3f;
    public float loseDistance = 4f;

    [Header("Tentacle Feel")]
    public float maxBendAngle = 70f;
    public float boneSpeed = 6f;
    public float tipBias = 3f;

    [Header("Reach & Return")]
    public float reachSpeed = 14f;
    public float returnSpeed = 2f;
    public float orbitRadius = 0.5f;

    [Header("Idle Motion")]
    public float swayAmount = 0.06f;
    public float swaySpeed = 0.7f;

    [Header("Collision")]
    public float skinThickness = 0.05f;
    public float boneSphereRadius = 0.08f;

    [Header("Slow Effect")]
    public float slowMultiplierSetting = 0.3f;
    public float slowLerpSpeed = 5f;
    public float slowProximityDistance = 1.2f;

    BoxCollider playerBox;
    PlayerModel playerModel;

    int touchCount = 0;
    float currentSlow = 1f;
    float baseWalkSpeed;
    public bool IsInSlowZone { get; private set; }
    public bool IsSlowed => currentSlow < 1f;
    public float CurrentSlowAmount => currentSlow;

    enum AttackPhase { Idle, Attack }
    AttackPhase phase = AttackPhase.Idle;
    bool cooling = false;
    bool ready = false;

    class Tentacle
    {
        public Transform[] bones;
        public Quaternion[] restLocalRot;
        public Quaternion[] ikLocalRot;
        public Vector3 restTipLocal;
        public Vector3 ikTarget;
        public float tentaclePhase;
        public float orbitAngle;
        public float perBoneAngle;
        public float chainBoneSpeed;
        public SphereCollider[] boneSpheres;
        public bool touchingPlayer;
        public bool isStubChain;
        public float orbitSpeed;
        public float spreadOffset;
        public bool settled;
    }

    List<Tentacle> tentacles = new List<Tentacle>();

    void Start() => StartCoroutine(Init());

    IEnumerator Init()
    {
        yield return null;
        yield return null;

        playerBox = player.GetComponent<BoxCollider>()
                 ?? player.GetComponentInChildren<BoxCollider>();
        playerModel = player.GetComponent<PlayerModel>()
                   ?? player.GetComponentInChildren<PlayerModel>();

        if (playerModel != null)
            baseWalkSpeed = playerModel.walkSpeed;

        if (playerBox != null)
            Physics.IgnoreLayerCollision(playerBox.gameObject.layer, 2, true);

        CollectAllChains(bonesRoot);

        for (int i = 0; i < tentacles.Count; i++)
        {
            var t = tentacles[i];
            for (int j = 0; j < t.bones.Length; j++)
            {
                t.ikLocalRot[j] = t.restLocalRot[j];
                t.bones[j].localRotation = t.restLocalRot[j];
            }
            t.ikTarget = t.bones[t.bones.Length - 1].position;
            t.settled = true;
        }

        currentSlow = 1f;
        IsInSlowZone = false;

        ready = true;

        void CollectAllChains(Transform root)
        {
            var allChains = new List<List<Transform>>();
            foreach (Transform child in root)
                WalkChain(child, new List<Transform>(), allChains);

            int cap = Mathf.Min(allChains.Count, 30);
            for (int i = 0; i < cap; i++)
            {
                Tentacle t = BuildTentacle(allChains[i]);
                if (t != null) tentacles.Add(t);
            }
        }

        void WalkChain(Transform bone, List<Transform> chain, List<List<Transform>> results)
        {
            chain.Add(bone);
            if (bone.childCount == 0)
                results.Add(new List<Transform>(chain));
            else if (bone.childCount == 1)
                WalkChain(bone.GetChild(0), chain, results);
            else
                foreach (Transform child in bone)
                    WalkChain(child, new List<Transform>(chain), results);
        }
    }

    Tentacle BuildTentacle(List<Transform> chain)
    {
        if (chain.Count < 2) return null;

        var t = new Tentacle();
        t.bones = chain.ToArray();
        t.restLocalRot = new Quaternion[chain.Count];
        t.ikLocalRot = new Quaternion[chain.Count];
        t.boneSpheres = new SphereCollider[chain.Count];
        t.tentaclePhase = Random.Range(0f, Mathf.PI * 2f);
        t.orbitAngle = Random.Range(0f, 360f);
        t.isStubChain = chain.Count <= 3;
        t.perBoneAngle = t.isStubChain ? 90f : Mathf.Clamp(maxBendAngle * (5f / Mathf.Max(chain.Count, 1)), 15f, 90f);
        t.chainBoneSpeed = boneSpeed * Mathf.Lerp(1.5f, 1f, Mathf.Clamp01((float)chain.Count / 8f));
        t.orbitSpeed = Random.Range(0.5f, 4f);
        t.spreadOffset = Random.Range(0.4f, 2.2f);

        for (int i = 0; i < chain.Count; i++)
        {
            t.restLocalRot[i] = chain[i].localRotation;
            t.ikLocalRot[i] = chain[i].localRotation;
        }

        t.restTipLocal = bonesRoot.InverseTransformPoint(chain[chain.Count - 1].position);
        t.ikTarget = chain[chain.Count - 1].position;
        t.settled = true;

        for (int i = 1; i < chain.Count; i++)
        {
            var existing = chain[i].GetComponentInChildren<SphereCollider>();
            if (existing != null) { t.boneSpheres[i] = existing; continue; }

            var go = new GameObject("BS_" + i);
            go.layer = 2;
            go.transform.SetParent(chain[i]);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var sc = go.AddComponent<SphereCollider>();
            sc.radius = boneSphereRadius;
            sc.isTrigger = true;
            t.boneSpheres[i] = sc;
        }

        return t;
    }

    void Update()
    {
        if (!ready || player == null) return;

        float dist = Vector3.Distance(bonesRoot.position, player.position);

        UpdatePhase(dist);

        touchCount = 0;
        foreach (var t in tentacles)
            UpdateTentacle(t);

        bool inAttack = phase == AttackPhase.Attack;
        bool proximityTouch = inAttack && dist <= slowProximityDistance;
        bool inSlow = (inAttack && (touchCount > 0 || proximityTouch));
        float targetSlow = inSlow ? slowMultiplierSetting : 1f;

        IsInSlowZone = inSlow;

        currentSlow = Mathf.Lerp(currentSlow, targetSlow, slowLerpSpeed * Time.deltaTime);

        if (playerModel != null)
            playerModel.walkSpeed = baseWalkSpeed * currentSlow;
    }

    void UpdatePhase(float dist)
    {
        switch (phase)
        {
            case AttackPhase.Idle:
                if (!cooling && dist <= detectDistance)
                    phase = AttackPhase.Attack;
                break;

            case AttackPhase.Attack:
                if (dist >= loseDistance) { ResetToIdle(); cooling = true; StartCoroutine(Cooldown()); }
                break;
        }
    }

    void ResetToIdle()
    {
        phase = AttackPhase.Idle;

        foreach (var t in tentacles)
            t.settled = false;
    }

    void UpdateTentacle(Tentacle t)
    {
        int n = t.bones.Length;

        if (t.isStubChain && phase == AttackPhase.Idle)
        {
            for (int i = 0; i < n; i++)
            {
                t.ikLocalRot[i] = Quaternion.Slerp(t.ikLocalRot[i], t.restLocalRot[i], returnSpeed * Time.deltaTime);
                t.bones[i].localRotation = t.ikLocalRot[i];
            }
            return;
        }

        bool inReach = phase == AttackPhase.Attack;

        if (inReach)
        {
            t.settled = false;

            Vector3 origin = t.bones[0].position;

            Vector3 approachDir = (player.position - origin).normalized;
            t.orbitAngle = (t.orbitAngle + t.orbitSpeed * Time.deltaTime) % 360f;
            float rad = t.orbitAngle * Mathf.Deg2Rad;
            Vector3 spreadAxis = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            Vector3 curlDir = Vector3.Cross(approachDir, spreadAxis).normalized;
            if (curlDir.sqrMagnitude < 0.001f)
                curlDir = Vector3.Cross(approachDir, Vector3.up).normalized;

            Vector3 goal = player.position
                + curlDir * orbitRadius * t.spreadOffset
                + spreadAxis * (orbitRadius * t.spreadOffset * 0.5f)
                - approachDir * orbitRadius * t.spreadOffset * 0.3f;

            t.ikTarget = Vector3.Lerp(t.ikTarget, goal, reachSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 restWorld = bonesRoot.TransformPoint(t.restTipLocal);
            restWorld.y = Mathf.Max(restWorld.y, t.bones[0].position.y);

            t.ikTarget = Vector3.Lerp(t.ikTarget, restWorld, returnSpeed * Time.deltaTime);

            if (!t.settled)
            {
                for (int i = 0; i < n; i++)
                    t.ikLocalRot[i] = Quaternion.Slerp(t.ikLocalRot[i], t.restLocalRot[i], returnSpeed * Time.deltaTime);

                float tipDist = Vector3.Distance(t.bones[n - 1].position, restWorld);
                if (tipDist < 0.5f) t.settled = true;
            }

            if (t.settled)
            {
                ApplyIdleMotion(t, n);
                return;
            }
        }

        for (int i = 0; i < n; i++)
            t.bones[i].localRotation = t.ikLocalRot[i];

        Vector3 root = t.bones[0].position;
        Vector3 totalDelta = t.ikTarget - root;
        float totalDist = totalDelta.magnitude;
        Vector3 totalDirNorm = totalDist > 0.0001f ? totalDelta / totalDist : Vector3.forward;

        Vector3[] subTargets = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float frac = (float)(i + 1) / n;
            float curved = frac * frac;
            Vector3 sub = root + totalDirNorm * (totalDist * curved);
            subTargets[i] = sub;
            subTargets[i].y = Mathf.Max(subTargets[i].y, root.y);
        }

        for (int i = 0; i < n - 1; i++)
        {
            Transform bone = t.bones[i];
            Vector3 toNext = t.bones[i + 1].position - bone.position;
            Vector3 toSub = subTargets[i] - bone.position;

            if (toNext.sqrMagnitude < 0.0001f || toSub.sqrMagnitude < 0.0001f) continue;

            toNext = toNext.normalized;
            toSub = toSub.normalized;

            float dot = Vector3.Dot(toNext, toSub);
            if (dot > 0.9999f) continue;

            Quaternion aimRot = Quaternion.FromToRotation(toNext, toSub);
            if (IsNaNQ(aimRot)) continue;

            aimRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (IsNaNV(axis) || axis.sqrMagnitude < 0.0001f) continue;

            if (angle > 180f) angle -= 360f;
            float rootBias = 1f - (0.6f * (1f - (float)i / Mathf.Max(n - 2, 1)));
            angle = Mathf.Clamp(angle, -t.perBoneAngle * rootBias, t.perBoneAngle * rootBias);
            aimRot = Quaternion.AngleAxis(angle, axis);

            float frac = (float)i / Mathf.Max(n - 2, 1);
            float speed = t.chainBoneSpeed * Mathf.Lerp(0.3f, tipBias, frac);
            float alignment = 1f - Mathf.Clamp01(dot);
            speed *= Mathf.Lerp(0.5f, 1.5f, alignment);

            Quaternion goalWorld = aimRot * bone.rotation;
            if (IsNaNQ(goalWorld)) continue;

            Quaternion newWorld = Quaternion.Slerp(bone.rotation, goalWorld, speed * Time.deltaTime);
            if (IsNaNQ(newWorld)) continue;

            Quaternion parentRot = bone.parent != null ? bone.parent.rotation : Quaternion.identity;
            if (IsNaNQ(parentRot)) continue;

            Quaternion local = Quaternion.Inverse(parentRot) * newWorld;
            if (IsNaNQ(local)) continue;

            t.ikLocalRot[i] = local;
            bone.localRotation = local;
        }

        if (inReach)
        {
            t.touchingPlayer = false;
            CheckTouchOnly(t);
            if (t.touchingPlayer) touchCount++;
        }
    }

    void ApplyIdleMotion(Tentacle t, int n)
    {
        float st = Time.time * swaySpeed + t.tentaclePhase;

        Quaternion worldRestRot = t.bones[0].parent != null
            ? t.bones[0].parent.rotation
            : Quaternion.identity;

        for (int i = 0; i < n; i++)
        {
            float frac = (float)i / Mathf.Max(n - 1, 1);

            Quaternion boneRestWorld = worldRestRot * t.restLocalRot[i];
            Vector3 stableRight = boneRestWorld * Vector3.right;

            float swayAng = Mathf.Sin(st + i * 0.5f) * swayAmount * frac * 18f;
            Quaternion sway = Quaternion.AngleAxis(swayAng, stableRight);

            Quaternion target = t.restLocalRot[i] * sway;

            Quaternion result = Quaternion.Slerp(t.ikLocalRot[i], target, returnSpeed * Time.deltaTime);

            if (IsNaNQ(result)) continue;

            t.bones[i].localRotation = result;
            t.ikLocalRot[i] = result;

            worldRestRot = worldRestRot * t.restLocalRot[i];
        }
    }

    void CheckTouchOnly(Tentacle t)
    {
        if (playerBox == null) return;

        for (int i = 1; i < t.bones.Length; i++)
        {
            if (t.boneSpheres[i] == null) continue;

            Vector3 closest = playerBox.ClosestPoint(t.bones[i].position);
            if ((closest - t.bones[i].position).sqrMagnitude < skinThickness * skinThickness)
                t.touchingPlayer = true;
        }
    }

    bool IsNaNQ(Quaternion q) =>
        float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w);
    bool IsNaNV(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);

    IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(1f);
        cooling = false;
    }

    void OnDestroy()
    {
        if (playerModel != null)
            playerModel.walkSpeed = baseWalkSpeed;
    }
}
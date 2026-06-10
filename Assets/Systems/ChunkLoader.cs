using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] GameObject[] chunkParents;

    [SerializeField] float activationDistance = 50f;
    [SerializeField] float deactivationDistance = 50f;
    [SerializeField] float checkInterval = 0.50f;

    private Vector3 playerPos;
    private Dictionary<GameObject, Bounds> chunkBounds = new Dictionary<GameObject, Bounds>();

    void Start()
    {
        foreach (GameObject chunk in chunkParents)
        {
            Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                chunkBounds[chunk] = b;
            }
        }

        StartCoroutine(CheckChunkDistances());
    }

    IEnumerator CheckChunkDistances()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        float activationDistanceSqr = activationDistance * activationDistance;
        float deactivationDistanceSqr = deactivationDistance * deactivationDistance;

        while (true)
        {
            playerPos = player.transform.position;

            foreach (GameObject chunk in chunkParents)
            {
                float distSqr;
                if (chunkBounds.TryGetValue(chunk, out Bounds bounds))
                {
                    Vector3 closest = bounds.ClosestPoint(playerPos);
                    distSqr = (playerPos - closest).sqrMagnitude;
                }
                else
                {
                    distSqr = (playerPos - chunk.transform.position).sqrMagnitude;
                }

                float thresholdSqr = chunk.activeSelf ? deactivationDistanceSqr : activationDistanceSqr;
                bool shouldBeVisible = distSqr <= thresholdSqr;

                if (chunk.activeSelf != shouldBeVisible)
                    chunk.SetActive(shouldBeVisible);
            }

            yield return wait;
        }
    }
}
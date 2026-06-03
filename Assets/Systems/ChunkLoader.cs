using System.Collections;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] GameObject[] chunkParents;

    [SerializeField] float activationDistance = 50f;
    [SerializeField] float checkInterval = 0.50f;
    [SerializeField] float deactivationDistance = 50f;

    private Vector3 playerPos;

    void Start()
    {
        StartCoroutine(CheckChunkDistances());
    }

    IEnumerator CheckChunkDistances()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        float activationDistanceSqr = activationDistance * activationDistance;

        while (true)
        {
            playerPos = player.transform.position;

            float deactivationDistanceSqr = deactivationDistance * deactivationDistance;

            foreach (GameObject chunk in chunkParents)
            {
                float distSqr;
                Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    Vector3 closest = bounds.ClosestPoint(playerPos);
                    distSqr = (playerPos - closest).sqrMagnitude;
                }
                else
                {
                    Vector3 diff = playerPos - chunk.transform.position;
                    distSqr = diff.sqrMagnitude;
                }

                float thresholdSqr = chunk.activeSelf ? deactivationDistanceSqr : activationDistanceSqr;
                bool shouldBeVisible = distSqr <= thresholdSqr;
                SetChunkVisuals(chunk, shouldBeVisible);
            }
            yield return wait;
        }
    }

    void SetChunkVisuals(GameObject chunk, bool visible)
    {
        if (chunk.activeSelf != visible)
            chunk.SetActive(visible);
    }
}
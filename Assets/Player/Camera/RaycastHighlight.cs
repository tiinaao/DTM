using UnityEngine;

public class RaycastHighlight : MonoBehaviour
{
    public Material outlineMaterial;
    public float range = 4f;

    private GameObject outlineMeshObj;
    private GameObject lastHit;

    void Start()
    {
        outlineMeshObj = new GameObject("OutlineMesh");
        outlineMeshObj.AddComponent<MeshFilter>();
        outlineMeshObj.AddComponent<MeshRenderer>();
        outlineMeshObj.GetComponent<MeshRenderer>().material = outlineMaterial;
        outlineMeshObj.SetActive(false);
    }

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            GameObject obj = hit.collider.gameObject;
            MeshFilter mf = obj.GetComponent<MeshFilter>();

            if (mf != null && obj != lastHit && obj.layer == LayerMask.NameToLayer("Outline"))
            {
                lastHit = obj;

                outlineMeshObj.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                outlineMeshObj.transform.position = obj.transform.position;
                outlineMeshObj.transform.rotation = obj.transform.rotation;
                outlineMeshObj.transform.localScale = obj.transform.localScale;
                outlineMeshObj.SetActive(true);
            }
        }
        else
        {
            lastHit = null;
            outlineMeshObj.SetActive(false);
        }
    }
}
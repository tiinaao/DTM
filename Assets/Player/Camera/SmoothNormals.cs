using UnityEngine;
using System.Collections.Generic;

public class SmoothNormals : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        Mesh mesh = mf.mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3[] smoothNormals = new Vector3[vertices.Length];

        Dictionary<Vector3, Vector3> normalMap = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            if (!normalMap.ContainsKey(vertices[i]))
                normalMap[vertices[i]] = normals[i];
            else
                normalMap[vertices[i]] += normals[i];
        }

        for (int i = 0; i < vertices.Length; i++)
            smoothNormals[i] = normalMap[vertices[i]].normalized;

        mesh.SetUVs(3, new System.Collections.Generic.List<Vector3>(smoothNormals));
    }
}
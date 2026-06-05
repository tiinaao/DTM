using UnityEditor;
using UnityEngine;

public static class ChunkWarmupEditor
{
    [MenuItem("Tools/Warmup/Attach ChunkWarmup to Selected")]
    public static void AttachToSelected()
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;

            var comp = go.GetComponent<ChunkWarmupComponent>();
            if (comp == null)
            {
                comp = Undo.AddComponent<ChunkWarmupComponent>(go);
            }
            EditorUtility.SetDirty(go);
            if (comp != null) EditorUtility.SetDirty(comp);
        }
    }

    [MenuItem("Tools/Warmup/Remove ChunkWarmup from Selected")] 
    public static void RemoveFromSelected()
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go == null) continue;

            var comp = go.GetComponent<ChunkWarmupComponent>();

                Undo.DestroyObjectImmediate(comp);
                EditorUtility.SetDirty(go);
            }
        }
    }
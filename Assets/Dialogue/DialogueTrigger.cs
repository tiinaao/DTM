using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private string startNode = "Start";
    [SerializeField] private MonoBehaviour dialogueRunnerHelper;
    [SerializeField] private GameObject[] objectsToHideDuringDialogue;
    [SerializeField] private float fadeDuration = 0.25f;

    private CursorManager cursorManager;
    private Dictionary<GameObject, CanvasGroup> canvasGroups = new Dictionary<GameObject, CanvasGroup>();

    void Awake()
    {
        cursorManager = FindAnyObjectByType<CursorManager>();

        foreach (var obj in objectsToHideDuringDialogue)
        {
            if (obj == null) continue;
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();
            canvasGroups[obj] = cg;
        }
    }

    public void TriggerDialogue()
    {
        cursorManager?.OnDialogueStart();
        StartCoroutine(FadeObjects(false));

        if (dialogueRunnerHelper != null)
        {
            var helperType = dialogueRunnerHelper.GetType();
            var method = helperType.GetMethod("StartDialogue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(dialogueRunnerHelper, new object[] { startNode });
                return;
            }
        }

        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (var mb in all)
        {
            var t = mb.GetType();
            if (t.Name == "DialogueRunner" || (t.FullName != null && t.FullName.EndsWith(".DialogueRunner")))
            {
                var method = t.GetMethod("StartDialogue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(mb, new object[] { startNode });
                    return;
                }
            }
        }
    }

    public void OnDialogueEnd()
    {
        StartCoroutine(FadeObjects(true));
        cursorManager?.OnDialogueEnd();
    }

    private IEnumerator FadeObjects(bool visible)
    {
        float start = visible ? 0f : 1f;
        float end = visible ? 1f : 0f;
        float elapsed = 0f;

        foreach (var kvp in canvasGroups)
        {
            if (visible) kvp.Value.gameObject.SetActive(true);
            kvp.Value.blocksRaycasts = visible;
            kvp.Value.interactable = visible;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            foreach (var kvp in canvasGroups)
                kvp.Value.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        foreach (var kvp in canvasGroups)
        {
            kvp.Value.alpha = end;
            if (!visible) kvp.Value.gameObject.SetActive(false);
        }
    }
}
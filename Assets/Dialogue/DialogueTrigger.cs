using System.Reflection;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private string startNode = "Start";
    [SerializeField] private MonoBehaviour dialogueRunnerHelper;

    private CursorManager cursorManager;

    void Awake()
    {
        cursorManager = FindAnyObjectByType<CursorManager>();
    }

    public void TriggerDialogue()
    {
        cursorManager?.OnDialogueStart();

        if (dialogueRunnerHelper != null)
        {
            var helperType = dialogueRunnerHelper.GetType();
            var method = helperType.GetMethod("StartDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                var method = t.GetMethod("StartDialogue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(mb, new object[] { startNode });
                    return;
                }
            }
        }
    }
}
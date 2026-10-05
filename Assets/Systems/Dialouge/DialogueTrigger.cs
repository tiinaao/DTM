using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public RuntimeDialogueGraph Graph;
    public string PromptText = "Press F to interact";

    public void Interact()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.StartDialogue(Graph);
    }
}
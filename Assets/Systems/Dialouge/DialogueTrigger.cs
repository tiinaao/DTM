using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    public RuntimeDialogueGraph Graph;
    public string PromptText = "Press F to interact";

    public string InteractionText => PromptText;

    public void Interact()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.StartDialogue(Graph);
    }
}

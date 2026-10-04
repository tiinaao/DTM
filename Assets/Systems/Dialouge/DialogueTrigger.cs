using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public RuntimeDialogueGraph Graph;
    public string PromptText = "Press F to interact";
    private bool _used;

    public void Interact()
    {
        if (DialogueManager.Instance == null) return;

        if (DialogueManager.Instance.StartDialogue(Graph))
        {
            _used = true;
        }
    }

}
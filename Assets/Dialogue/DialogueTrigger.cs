using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private string jsonFileName = "example";
    [SerializeField] private Dialogue dialogue;

    public void TriggerDialogue()
    {
        dialogue.Open(jsonFileName);
    }
}
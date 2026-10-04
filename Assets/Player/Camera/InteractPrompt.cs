using UnityEngine;
using TMPro;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Camera mainCamera;
    private ItemPickup currentPickup;
    private DialogueTrigger currentDialogue;
    private string defaultPromptText;

    private int outlineLayer;
    private int interactableLayer;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        promptUI.SetActive(false);

        if (promptText != null) defaultPromptText = promptText.text;

        outlineLayer = LayerMask.NameToLayer("Outline");
        interactableLayer = LayerMask.NameToLayer("Interactable");
    }

    void Update()
    {
        if (DialogueManager.IsDialogueActive)
        {
            ResetPrompt();
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            ResetPrompt();
            return;
        }

        int hitLayer = hit.collider.gameObject.layer;

        if (hitLayer != outlineLayer && hitLayer != interactableLayer)
        {
            ResetPrompt();
            return;
        }

        hit.collider.TryGetComponent(out currentDialogue);
        hit.collider.TryGetComponent(out currentPickup);

        if (currentDialogue == null && currentPickup == null)
        {
            ResetPrompt();
            return;
        }

        if (promptText != null)
        {
            promptText.SetText(currentDialogue != null ? currentDialogue.PromptText : defaultPromptText);
        }

        promptUI.SetActive(true);
        promptUI.transform.position = hit.collider.transform.position + offset;
        promptUI.transform.LookAt(mainCamera.transform);
        promptUI.transform.Rotate(0, 180, 0);

        if (playerInputHandler.InteractTriggered)
        {
            if (currentDialogue != null)
            {
                currentDialogue.Interact();
            }
            else
            {
                currentPickup.GiveItem();
            }
        }
    }

    void ResetPrompt()
    {
        promptUI.SetActive(false);
        currentPickup = null;
        currentDialogue = null;
    }
}
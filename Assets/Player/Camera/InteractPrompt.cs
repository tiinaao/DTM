using UnityEngine;
using TMPro;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private float interactRange = 3f;

    private Camera mainCamera;
    private IInteractable currentInteractable;
    private Transform currentTarget;
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

        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
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

        hit.collider.TryGetComponent(out IInteractable interactable);

        if (interactable == null || interactable is NPCBase)
        {
            ResetPrompt();
            return;
        }

        currentInteractable = interactable;
        currentTarget = hit.collider.transform;

        if (promptText != null)
        {
            string text = currentInteractable.InteractionText;
            promptText.SetText(string.IsNullOrEmpty(text) ? defaultPromptText : text);
        }

        promptUI.SetActive(true);
        promptUI.transform.position = currentTarget.position + offset;
        promptUI.transform.LookAt(mainCamera.transform);
        promptUI.transform.Rotate(0, 180, 0);

        if (playerInputHandler.InteractTriggered)
        {
            currentInteractable.Interact();
        }
    }

    void ResetPrompt()
    {
        promptUI.SetActive(false);
        currentInteractable = null;
        currentTarget = null;
    }
}

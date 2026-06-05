using UnityEngine;
using TMPro;

public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);
    [SerializeField] private PlayerInputHandler playerInputHandler;

    private Camera mainCamera;
    private DialogueTrigger currentTrigger;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        promptUI.SetActive(false);
    }

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 3f))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Outline"))
            {
                promptUI.SetActive(true);
                promptUI.transform.position = hit.collider.transform.position + offset;
                promptUI.transform.LookAt(mainCamera.transform);
                promptUI.transform.Rotate(0, 180, 0);

                currentTrigger = hit.collider.GetComponent<DialogueTrigger>();

                if (playerInputHandler.InteractTriggered && currentTrigger != null)
                    currentTrigger.TriggerDialogue();
            }
            else
            {
                ResetPrompt();
            }
        }
        else
        {
            ResetPrompt();
        }
    }

    void ResetPrompt()
    {
        promptUI.SetActive(false);
        currentTrigger = null;
    }
}
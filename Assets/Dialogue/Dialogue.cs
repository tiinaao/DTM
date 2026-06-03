using System.Collections;
using TMPro;
using UnityEngine;

public class Dialogue : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private PlayerModel playerModel;
    [SerializeField] private float textSpeed = 0.02f;

    private DialogueLine[] lines;
    private int currentLine;

    private bool isTyping;
    private bool isOpen;

    private float lastCloseTime = -10f;

    private Coroutine typingCoroutine;
    private bool ignoreNextInteract;

    void Awake()
    {
        dialoguePanel.SetActive(false);
        textComponent.text = "";
        textComponent.maxVisibleCharacters = 0;
    }

    void Update()
    {
        if (!isOpen) return;
        if (ignoreNextInteract && playerInputHandler.InteractTriggered)
        {
            ignoreNextInteract = false;
            return;
        }

        if (!playerInputHandler.InteractTriggered) return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        Advance();
    }

    public void Open(string jsonFileName)
    {
        if (isOpen) return;
        if (Time.time - lastCloseTime < 0.2f) return;

        TextAsset file = Resources.Load<TextAsset>($"Dialogue/Text/{jsonFileName}");
        if (file == null) return;

        DialogueData data = JsonUtility.FromJson<DialogueData>(file.text);
        if (data == null || data.lines == null || data.lines.Length == 0) return;

        lines = data.lines;
        currentLine = 0;
        isOpen = true;

        playerModel.enabled = false;
        dialoguePanel.SetActive(true);
        ignoreNextInteract = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        textComponent.text = "";
        textComponent.maxVisibleCharacters = 0;
        typingCoroutine = StartCoroutine(TypeLine());
    }

    void Advance()
    {
        currentLine++;

        if (currentLine >= lines.Length)
        {
            Close();
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine());
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        string full = lines[currentLine].text ?? "";

        textComponent.text = full;
        textComponent.maxVisibleCharacters = full.Length;

        isTyping = false;
    }

    IEnumerator TypeLine()
    {
        isTyping = true;

        string full = lines[currentLine].text ?? "";
        textComponent.text = "";
        textComponent.maxVisibleCharacters = 0;

        yield return null;
        textComponent.text = full;
        textComponent.ForceMeshUpdate();

        int length = full.Length;

        for (int i = 1; i <= length; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(textSpeed);
        }

        textComponent.maxVisibleCharacters = length;
        isTyping = false;
        typingCoroutine = null;
    }

    void Close()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isOpen = false;
        isTyping = false;
        currentLine = 0;
        textComponent.text = "";
        textComponent.maxVisibleCharacters = 0;
        playerModel.enabled = true;
        dialoguePanel.SetActive(false);
        lastCloseTime = Time.time;
    }
}
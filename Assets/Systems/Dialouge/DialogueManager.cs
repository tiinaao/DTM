using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsDialogueActive { get; private set; }

    public static event Action DialogueStarted;
    public static event Action DialogueEnded;

    public RuntimeDialogueGraph RuntimeGraph;
    public GameObject DialoguePanel;
    public TextMeshProUGUI SpeakerNameText;
    public TextMeshProUGUI DialogueText;

    public Button ChoiceButtonPrefab;
    public Transform ChoiceButtonContainer;

    public float SlideDuration = 0.35f;
    public float HiddenPadding = 40f;

    private readonly Dictionary<string, RuntimeDialogueNode> _nodeLookup = new Dictionary<string, RuntimeDialogueNode>();
    private RuntimeDialogueNode _currentNode;

    private RectTransform _panelRect;
    private Vector2 _shownPosition;
    private Coroutine _slideRoutine;
    private Coroutine _displayLineCoroutine;
    private bool _canContinue = false;

    private int _openedFrame = -1;

    private const BlockFlags PauseBlocks =
        BlockFlags.Movement | BlockFlags.Camera | BlockFlags.Actions |
        BlockFlags.Interaction | BlockFlags.Inventory |
        BlockFlags.FreeCursor | BlockFlags.FreezeTime;

    private void Awake()
    {
        Instance = this;

        _panelRect = DialoguePanel.GetComponent<RectTransform>();
        _shownPosition = _panelRect.anchoredPosition;
        EnsureUIInput();
        DialoguePanel.SetActive(false);
    }

    private void EnsureUIInput()
    {
        var canvas = DialoguePanel.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        var eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null) return;

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null) Destroy(oldModule);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void OnDestroy()
    {
        GameplayBlocker.Release(this);

        if (Instance != this) return;

        IsDialogueActive = false;
        Instance = null;
    }

    private void Update()
    {
        if (!IsDialogueActive || _currentNode == null || _currentNode.Choices.Count > 0) return;
        if (Time.frameCount == _openedFrame) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (!_canContinue)
        {
            SkipTyping();
            return;
        }

        ShowNode(_currentNode.NextNodeID);
    }

    public bool StartDialogue(RuntimeDialogueGraph graph)
    {
        if (graph == null || IsDialogueActive) return false;

        RuntimeGraph = graph;

        _nodeLookup.Clear();
        foreach (var node in graph.AllNodes)
        {
            _nodeLookup[node.NodeID] = node;
        }

        var first = Resolve(graph.EntryNodeID);
        if (first == null) return false;

        GameplayBlocker.Block(this, PauseBlocks);

        IsDialogueActive = true;
        _openedFrame = Time.frameCount;

        DialogueStarted?.Invoke();

        BeginSlide(true);
        Display(first);
        return true;
    }

    public void EndDialogue()
    {
        if (!IsDialogueActive) return;

        _currentNode = null;
        ClearChoices();

        if (_displayLineCoroutine != null)
        {
            StopCoroutine(_displayLineCoroutine);
            _displayLineCoroutine = null;
        }

        IsDialogueActive = false;

        DialogueEnded?.Invoke();

        BeginSlide(false);
        GameplayBlocker.Release(this);
    }

    private RuntimeDialogueNode Resolve(string nodeID)
    {
        int guard = 0;

        while (!string.IsNullOrEmpty(nodeID) && guard++ < 1000)
        {
            if (!_nodeLookup.TryGetValue(nodeID, out var node)) return null;

            if (node.NodeType != DialogueNodeType.Item) return node;

            ApplyItem(node);
            nodeID = node.NextNodeID;
        }

        return null;
    }

    private InventorySystem GetInventory()
    {
        if (GameManager.Instance == null) return null;
        return GameManager.Instance.Inventory;
    }

    private void ApplyItem(RuntimeDialogueNode node)
    {
        var inventory = GetInventory();
        if (inventory == null) return;

        if (node.ItemOperation == ItemAction.Add)
        {
            if (!inventory.AddItem(node.ItemName, node.Amount))
            {
                Debug.LogWarning($"DialogueManager: could not add {node.Amount}x {node.ItemName}.");
            }
        }
        else
        {
            if (!inventory.RemoveItem(node.ItemName, node.Amount))
            {
                Debug.LogWarning($"DialogueManager: could not remove {node.Amount}x {node.ItemName}.");
            }
        }
    }

    private void ShowNode(string nodeID)
    {
        var node = Resolve(nodeID);

        if (node == null)
        {
            EndDialogue();
            return;
        }

        Display(node);
    }

    private void Display(RuntimeDialogueNode node)
    {
        _currentNode = node;

        SpeakerNameText.SetText(node.SpeakerName);

        if (_displayLineCoroutine != null)
        {
            StopCoroutine(_displayLineCoroutine);
        }

        _displayLineCoroutine = StartCoroutine(DisplayLine(node.DialogueText));

        ClearChoices();

        foreach (var choice in node.Choices)
        {
            Button button = Instantiate(ChoiceButtonPrefab, ChoiceButtonContainer);

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.ChoiceText;
            }

            string destination = choice.DestinationNodeID;
            button.onClick.AddListener(() => ShowNode(destination));
        }
    }

    private IEnumerator DisplayLine(string line)
    {
        _canContinue = false;
        DialogueText.text = "";

        bool isAddingRichTextTag = false;
        var wait = new WaitForSecondsRealtime(0.05f);

        foreach (char letter in line)
        {
            if (letter == '<' || isAddingRichTextTag)
            {
                isAddingRichTextTag = true;
                DialogueText.text += letter;

                if (letter == '>')
                {
                    isAddingRichTextTag = false;
                }

                continue;
            }

            DialogueText.text += letter;
            yield return wait;
        }

        _canContinue = true;
        _displayLineCoroutine = null;
    }

    private void SkipTyping()
    {
        if (_displayLineCoroutine != null)
        {
            StopCoroutine(_displayLineCoroutine);
            _displayLineCoroutine = null;
        }

        DialogueText.text = _currentNode.DialogueText;
        _canContinue = true;
    }

    private void ClearChoices()
    {
        foreach (Transform child in ChoiceButtonContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void BeginSlide(bool show)
    {
        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
        _slideRoutine = StartCoroutine(Slide(show));
    }

    private IEnumerator Slide(bool show)
    {
        bool wasActive = DialoguePanel.activeSelf;

        if (show)
        {
            DialoguePanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
        }

        Vector2 hiddenPosition = _shownPosition + Vector2.down * (_panelRect.rect.height + HiddenPadding);
        Vector2 from = show && !wasActive ? hiddenPosition : _panelRect.anchoredPosition;
        Vector2 to = show ? _shownPosition : hiddenPosition;

        _panelRect.anchoredPosition = from;

        float elapsed = 0f;
        while (elapsed < SlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, SlideDuration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            _panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        _panelRect.anchoredPosition = to;

        if (!show)
        {
            DialoguePanel.SetActive(false);
        }

        _slideRoutine = null;
    }
}
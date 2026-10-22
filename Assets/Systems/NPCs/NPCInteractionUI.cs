using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[DefaultExecutionOrder(200)]

public class NPCInteractionUI : MonoBehaviour
{
    private const float PanelGapFromNpc = 50f;
    private const float ScreenMargin = 24f;
    private const float ButtonHeight = 48f;
    private const float ButtonGap = 8f;
    private const float HeaderHeight = 92f;

    private static NPCInteractionUI instance;

    public static bool MenuIsOpen
    {
        get { return instance != null && instance.sessionOpen && instance.menuVisible; }
    }

    public static NPCInteractionUI Get()
    {
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform sessionRoot;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private NPCMenuButton[] buttons;

    private CanvasGroup panelGroup;
    private CanvasGroup promptGroup;
    private int activeCount;
    private PlayerInputHandler inputHandler;

    private bool sessionOpen;
    private bool menuVisible;
    private float sessionBlend;
    private float menuBlend;
    private int selected;
    private int openedFrame = -10;

    private Action<int> onChoose;
    private Action onCancel;
    private Func<Vector3> menuAnchor;
    private Vector3 lastAnchor;
    private bool hasAnchor;

    private UnityEngine.Object promptOwner;
    private Vector3 promptPosition;
    private Vector3 lastPromptPosition;
    private string promptKey;
    private string promptAction;
    private string shownPromptText;
    private float promptDistance;
    private int promptFrame = -10;
    private float promptBlend;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        EnsureEventSystem();

        panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        promptGroup = GetOrAdd<CanvasGroup>(promptRoot.gameObject);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Index = i;
            buttons[i].Hovered = OnButtonHovered;
            buttons[i].Clicked = OnButtonClicked;
        }

        sessionRoot.gameObject.SetActive(false);
        promptRoot.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Open(string title, string subtitle, IList<string> labels, Action<int> choose, Action cancel, Func<Vector3> anchor)
    {
        onChoose = choose;
        onCancel = cancel;
        menuAnchor = anchor;
        hasAnchor = false;

        titleText.text = title;
        subtitleText.text = subtitle;
        subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

        FillButtons(labels);

        sessionOpen = true;
        menuVisible = true;
        openedFrame = Time.frameCount;
        sessionRoot.gameObject.SetActive(true);

        Sfx("npc_open");
    }

    public void HideMenu()
    {
        menuVisible = false;
    }

    public void ShowMenu()
    {
        if (!sessionOpen) return;

        menuVisible = true;
        openedFrame = Time.frameCount;

        for (int i = 0; i < activeCount; i++)
        {
            buttons[i].PlayIntro(0.15f + i * 0.06f);
        }
    }

    public void Close()
    {
        if (sessionOpen) Sfx("npc_close");

        sessionOpen = false;
        menuVisible = false;
        onChoose = null;
        onCancel = null;
        menuAnchor = null;
    }

    public void RequestPrompt(UnityEngine.Object owner, Vector3 worldPosition, string key, string action, float distance)
    {
        if (promptFrame == Time.frameCount && promptOwner != null && distance >= promptDistance) return;

        promptOwner = owner;
        promptPosition = worldPosition;
        promptKey = key;
        promptAction = action;
        promptDistance = distance;
        promptFrame = Time.frameCount;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        sessionBlend = Mathf.MoveTowards(sessionBlend, sessionOpen ? 1f : 0f, dt / (sessionOpen ? 0.3f : 0.25f));
        menuBlend = Mathf.MoveTowards(menuBlend, (sessionOpen && menuVisible) ? 1f : 0f, dt / 0.25f);

        if (sessionBlend > 0f && !sessionRoot.gameObject.activeSelf) sessionRoot.gameObject.SetActive(true);
        if (!sessionOpen && sessionBlend <= 0f && sessionRoot.gameObject.activeSelf) sessionRoot.gameObject.SetActive(false);

        if (sessionOpen && menuVisible) HandleInput();
    }

    private void LateUpdate()
    {
        PositionPanel();
        UpdatePrompt();
    }

    private void PositionPanel()
    {
        if (!sessionRoot.gameObject.activeSelf) return;

        float me = Smoother(menuBlend);
        panelGroup.alpha = me;
        panelGroup.blocksRaycasts = menuBlend > 0.6f;
        panelGroup.interactable = menuBlend > 0.6f;

        Camera cam = Camera.main;
        if (menuAnchor != null && cam != null)
        {
            lastAnchor = menuAnchor();
            hasAnchor = true;
        }

        Vector2 local = new Vector2(canvasRect.rect.width * 0.25f, 0f);

        if (hasAnchor && cam != null)
        {
            Vector3 screen = cam.WorldToScreenPoint(lastAnchor);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
                local.x += PanelGapFromNpc;
            }
        }

        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        float panelH = panel.sizeDelta.y;

        float maxX = halfW - panel.sizeDelta.x - ScreenMargin;
        float maxY = Mathf.Max(0f, halfH - panelH * 0.5f - ScreenMargin);
        local.x = Mathf.Min(local.x, maxX);
        local.y = Mathf.Clamp(local.y, -maxY, maxY);

        panel.anchoredPosition = local + new Vector2((1f - me) * 30f, 0f);
    }

    private void UpdatePrompt()
    {
        bool want = !sessionOpen && promptFrame == Time.frameCount && promptOwner != null;

        promptBlend = Mathf.MoveTowards(promptBlend, want ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);

        if (promptBlend <= 0f)
        {
            if (promptRoot.gameObject.activeSelf) promptRoot.gameObject.SetActive(false);
            return;
        }

        if (!promptRoot.gameObject.activeSelf) promptRoot.gameObject.SetActive(true);

        if (want)
        {
            lastPromptPosition = promptPosition;

            string text = "<color=#FFD161><b>" + promptKey + "</b></color>   " + promptAction;
            if (text != shownPromptText)
            {
                shownPromptText = text;
                promptLabel.text = text;
                promptLabel.ForceMeshUpdate();
                promptRoot.sizeDelta = new Vector2(promptLabel.preferredWidth + 60f, 54f);
            }
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(lastPromptPosition);
        if (screen.z <= 0f)
        {
            promptGroup.alpha = 0f;
            return;
        }

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);

        float bob = Mathf.Sin(Time.unscaledTime * 3f) * 5f;
        promptRoot.anchoredPosition = local + new Vector2(0f, bob);

        float e = promptBlend * promptBlend * (3f - 2f * promptBlend);
        promptGroup.alpha = e;
        promptRoot.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, EaseOutBack(promptBlend));
    }

    private void HandleInput()
    {
        if (Time.frameCount <= openedFrame + 1 || menuBlend < 0.75f) return;
        if (activeCount == 0) return;

        Keyboard kb = Keyboard.current;
        Gamepad gp = Gamepad.current;

        if (kb != null)
        {
            int count = Mathf.Min(activeCount - 1, 9);
            for (int i = 0; i < count; i++)
            {
                if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                {
                    Choose(i);
                    return;
                }
            }

            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Move(1);
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Move(-1);

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            {
                Choose(selected);
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Choose(activeCount - 1);
                return;
            }
        }

        if (gp != null)
        {
            if (gp.dpad.down.wasPressedThisFrame) Move(1);
            if (gp.dpad.up.wasPressedThisFrame) Move(-1);

            if (gp.buttonSouth.wasPressedThisFrame)
            {
                Choose(selected);
                return;
            }

            if (gp.buttonEast.wasPressedThisFrame)
            {
                Choose(activeCount - 1);
                return;
            }
        }

        if (inputHandler == null) inputHandler = FindAnyObjectByType<PlayerInputHandler>();
        if (inputHandler != null && inputHandler.InteractTriggered)
        {
            Choose(selected);
        }
    }

    private void Move(int direction)
    {
        if (activeCount == 0) return;
        SetSelected((selected + direction + activeCount) % activeCount, true);
    }

    private void SetSelected(int index, bool playSound)
    {
        selected = index;
        for (int i = 0; i < activeCount; i++)
        {
            buttons[i].SetSelected(i == selected);
        }

        if (playSound) Sfx("npc_hover");
    }

    private void Choose(int index)
    {
        if (!sessionOpen || !menuVisible) return;
        if (index < 0 || index >= activeCount) return;

        Sfx("npc_select");

        if (index == activeCount - 1)
        {
            if (onCancel != null) onCancel();
        }
        else
        {
            if (onChoose != null) onChoose(index);
        }
    }

    private void FillButtons(IList<string> labels)
    {
        activeCount = Mathf.Min(labels.Count + 1, buttons.Length);

        for (int i = 0; i < buttons.Length; i++)
        {
            NPCMenuButton b = buttons[i];
            bool on = i < activeCount;
            b.gameObject.SetActive(on);
            if (!on) continue;

            bool isLeave = i == activeCount - 1;
            b.Label.text = isLeave ? "Leave" : labels[i];
            b.Hint.text = isLeave ? "Esc" : (i < 9 ? (i + 1).ToString() : "");
            b.PlayIntro(0.25f + i * 0.06f);
        }

        panel.sizeDelta = new Vector2(panel.sizeDelta.x, HeaderHeight + activeCount * (ButtonHeight + ButtonGap));

        SetSelected(0, false);
    }

    private void OnButtonHovered(NPCMenuButton button)
    {
        if (!menuVisible) return;
        if (button.Index != selected) SetSelected(button.Index, true);
    }

    private void OnButtonClicked(NPCMenuButton button)
    {
        Choose(button.Index);
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return;
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null) Destroy(oldModule);

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static float Smoother(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private static void Sfx(string soundName)
    {
        if (UISoundManager.instance != null && UISoundManager.instance.uiSource != null)
        {
            UISoundManager.instance.Play(soundName);
        }
    }
}
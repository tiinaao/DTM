using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public enum NPCOptionType
{
    Talk,
    Custom
}

[Serializable]
public class NPCOption
{
    public string label = "Option";
    public NPCOptionType type = NPCOptionType.Custom;
    public bool hideMenuWhileBusy = false;
    public bool endInteraction = false;
    public UnityEvent onSelected;
}

[DisallowMultipleComponent]
public class NPCInteraction : NPCBase
{
    private const BlockFlags InteractBlocks =
        BlockFlags.Movement | BlockFlags.Camera | BlockFlags.Actions |
        BlockFlags.Interaction | BlockFlags.Inventory | BlockFlags.FreeCursor;

    public static NPCInteraction Current { get; private set; }

    public static bool BlocksPause
    {
        get { return NPCInteractionUI.MenuIsOpen || Time.frameCount == lastEndFrame; }
    }

    private static readonly List<NPCInteraction> nearby = new List<NPCInteraction>();
    private static int lastEndFrame = -1;
    private static PlayerInputHandler cachedInput;
    private static PlayerModel cachedPlayer;

    [SerializeField] private string menuTitle;
    [SerializeField] private string subtitle;
    [SerializeField] private RuntimeDialogueGraph talkGraph;
    [SerializeField] private bool returnToMenuAfterTalk = true;
    [SerializeField] private List<NPCOption> options = new List<NPCOption>();

    [SerializeField] private string promptAction = "Talk";
    [SerializeField] private string promptKey = "F";
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.6f, 0f);

    [SerializeField] private bool flipToFacePlayer = true;
    [SerializeField] private bool artFacesLeftByDefault = true;
    [SerializeField] private bool restoreFacing = true;

    private bool active;
    private bool waitingForDialogue;
    private SpriteRenderer npcSprite;
    private bool savedFlip;
    private readonly List<string> labels = new List<string>();

    private string Title
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(menuTitle)) return menuTitle;
            if (!string.IsNullOrEmpty(Id)) return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Id.Replace('_', ' '));
            return gameObject.name;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        lastEndFrame = -1;
        cachedInput = null;
        cachedPlayer = null;
        nearby.Clear();
    }

    private void Reset()
    {
        options = new List<NPCOption>
        {
            new NPCOption { label = "Talk", type = NPCOptionType.Talk },
            new NPCOption { label = "Gift", type = NPCOptionType.Custom },
            new NPCOption { label = "Trade", type = NPCOptionType.Custom }
        };

        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger) return;
        }

        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2f;
    }

    private void Awake()
    {
        npcSprite = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void OnDisable()
    {
        if (active) EndInteraction();
        nearby.Remove(this);
        base.OnDisable();
    }

    protected override void OnPlayerEnter()
    {
        if (!nearby.Contains(this)) nearby.Add(this);
    }

    protected override void OnPlayerExit()
    {
        nearby.Remove(this);
    }

    protected override void Update()
    {
        if (active || !PlayerIsNear) return;
        if (!CanStart() || !IsClosestNearby()) return;

        PlayerInputHandler input = GetInput();
        if (input != null && input.InteractTriggered)
        {
            Interact();
        }
    }

    private void LateUpdate()
    {
        if (active || !PlayerIsNear) return;
        if (!CanStart() || !IsClosestNearby()) return;

        PlayerModel player = GetPlayer();
        float distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : 0f;

        NPCInteractionUI.Get().RequestPrompt(this, transform.position + promptOffset, promptKey, promptAction, distance);
    }

    protected override void OnInteract()
    {
        if (active) return;
        Begin();
    }

    public void ReturnToMenu()
    {
        if (!active) return;
        waitingForDialogue = false;
        NPCInteractionUI.Get().ShowMenu();
    }

    public void EndInteraction()
    {
        if (!active) return;

        active = false;
        waitingForDialogue = false;
        DialogueManager.DialogueEnded -= OnDialogueEnded;

        if (Current == this) Current = null;
        lastEndFrame = Time.frameCount;

        GameplayBlocker.Release(this);

        PlayerModel player = GetPlayer();
        if (player != null) player.ExitConversation();

        if (CameraFollow.Instance != null) CameraFollow.Instance.ClearFocus();
        NPCInteractionUI.Get().Close();

        if (restoreFacing && flipToFacePlayer && npcSprite != null) npcSprite.flipX = savedFlip;
    }

    private bool CanStart()
    {
        if (Current != null) return false;
        if (DialogueManager.IsDialogueActive) return false;
        if (GameplayBlocker.IsBlocked(BlockFlags.Interaction)) return false;
        return true;
    }

    private bool IsClosestNearby()
    {
        PlayerModel player = GetPlayer();
        if (player == null) return nearby.Count == 0 || nearby[0] == this;

        float mine = (player.transform.position - transform.position).sqrMagnitude;
        for (int i = 0; i < nearby.Count; i++)
        {
            NPCInteraction other = nearby[i];
            if (other == this || other == null) continue;

            float theirs = (player.transform.position - other.transform.position).sqrMagnitude;
            if (theirs < mine) return false;
        }

        return true;
    }

    private void Begin()
    {
        if (active || Current != null) return;

        active = true;
        Current = this;

        GameplayBlocker.Block(this, InteractBlocks);

        FaceEachOther();

        PlayerModel player = GetPlayer();
        if (player != null) player.EnterConversation(transform.position);

        if (CameraFollow.Instance != null) CameraFollow.Instance.FocusOn(transform);

        labels.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            labels.Add(options[i].label);
        }

        NPCInteractionUI.Get().Open(Title, subtitle, labels, OnOptionChosen, EndInteraction, GetMenuAnchor);
    }

    private Vector3 GetMenuAnchor()
    {
        Transform t = transform;
        if (npcSprite != null)
        {
            Bounds b = npcSprite.bounds;
            return new Vector3(b.max.x, t.position.y, t.position.z);
        }
        return t.position + Vector3.right * 0.6f;
    }

    private void OnOptionChosen(int index)
    {
        if (!active || index < 0 || index >= options.Count) return;

        NPCOption option = options[index];

        if (option.type == NPCOptionType.Talk)
        {
            StartTalk();
            return;
        }

        if (option.hideMenuWhileBusy) NPCInteractionUI.Get().HideMenu();
        if (option.onSelected != null) option.onSelected.Invoke();
        if (option.endInteraction) EndInteraction();
    }

    private void StartTalk()
    {
        NPCInteractionUI.Get().HideMenu();
        waitingForDialogue = true;
        DialogueManager.DialogueEnded += OnDialogueEnded;

        if (!DialogueManager.Instance.StartDialogue(talkGraph))
        {
            DialogueManager.DialogueEnded -= OnDialogueEnded;
            waitingForDialogue = false;
            NPCInteractionUI.Get().ShowMenu();
        }
    }

    private void OnDialogueEnded()
    {
        DialogueManager.DialogueEnded -= OnDialogueEnded;

        if (!active || !waitingForDialogue) return;
        waitingForDialogue = false;

        if (returnToMenuAfterTalk) NPCInteractionUI.Get().ShowMenu();
        else EndInteraction();
    }

    private void FaceEachOther()
    {
        PlayerModel player = GetPlayer();
        if (player == null) return;

        bool playerOnRight = player.transform.position.x > transform.position.x;

        if (flipToFacePlayer && npcSprite != null)
        {
            savedFlip = npcSprite.flipX;
            npcSprite.flipX = playerOnRight == artFacesLeftByDefault;
        }

        player.FaceToward(transform.position.x);
    }

    private static PlayerInputHandler GetInput()
    {
        if (cachedInput == null) cachedInput = FindAnyObjectByType<PlayerInputHandler>();
        return cachedInput;
    }

    private static PlayerModel GetPlayer()
    {
        if (cachedPlayer == null) cachedPlayer = FindAnyObjectByType<PlayerModel>();
        return cachedPlayer;
    }
}
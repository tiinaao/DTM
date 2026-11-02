using UnityEngine;

public class QuestPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private QuestInfoSO questInfoForPoint;
    [SerializeField] private bool startPoint = true;
    [SerializeField] private bool finishPoint = true;

    private bool playerIsNear;
    private string questId;
    private QuestState currentQuestState = QuestState.REQUIREMENTS_NOT_MET;
    private QuestIcon questIcon;
    private PlayerInputHandler input;

    private void Awake()
    {
        questId = questInfoForPoint.id;
        questIcon = GetComponentInChildren<QuestIcon>();
    }

    private void OnEnable()
    {
        GameEventsManager.instance.questEvents.onQuestStateChange += QuestStateChange;
    }

    private void OnDisable()
    {
        InteractionSelector.Unregister(this);
        playerIsNear = false;

        if (GameEventsManager.instance == null) return;
        GameEventsManager.instance.questEvents.onQuestStateChange -= QuestStateChange;
    }

    private void Update()
    {
        if (!playerIsNear) return;
        if (GameplayBlocker.IsBlocked(BlockFlags.Interaction)) return;
        if (!InteractionSelector.IsActive(this)) return;

        if (input == null) input = FindAnyObjectByType<PlayerInputHandler>();
        if (input != null && input.InteractTriggered) Interact();
    }

    public string InteractionText
    {
        get
        {
            if (currentQuestState == QuestState.CAN_START && startPoint) return "Start Quest";
            if (currentQuestState == QuestState.CAN_FINISH && finishPoint) return "Finish Quest";
            return string.Empty;
        }
    }

    public void Interact()
    {
        if (!playerIsNear) return;
        if (!InteractionSelector.IsActive(this)) return; 

        if (currentQuestState == QuestState.CAN_START && startPoint)
        {
            GameEventsManager.instance.questEvents.StartQuest(questId);
        }
        else if (currentQuestState == QuestState.CAN_FINISH && finishPoint)
        {
            GameEventsManager.instance.questEvents.FinishQuest(questId);
        }
    }

    private void QuestStateChange(Quest quest)
    {
        if (quest.info.id != questId) return;

        currentQuestState = quest.state;
        if (questIcon != null)
            questIcon.SetState(currentQuestState, startPoint, finishPoint);
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
        if (!otherCollider.CompareTag("Player")) return;
        playerIsNear = true;
        InteractionSelector.Register(this, transform);
    }

    private void OnTriggerExit(Collider otherCollider)
    {
        if (!otherCollider.CompareTag("Player")) return;
        playerIsNear = false;
        InteractionSelector.Unregister(this);
    }
}
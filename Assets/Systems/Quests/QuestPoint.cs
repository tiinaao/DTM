using UnityEngine;

public class QuestPoint : MonoBehaviour
{
    [SerializeField] private QuestInfoSO questInfoForPoint;
    [SerializeField] private bool startPoint = true;
    [SerializeField] private bool finishPoint = true;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private bool playerIsNear;
    private string questId;
    private QuestState currentQuestState = QuestState.REQUIREMENTS_NOT_MET;
    private QuestIcon questIcon;

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
        if (GameEventsManager.instance == null)
        {
            return;
        }

        GameEventsManager.instance.questEvents.onQuestStateChange -= QuestStateChange;
    }

    private void Update()
    {
        if (playerIsNear && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (!playerIsNear)
        {
            return;
        }

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
        if (quest.info.id != questId)
        {
            return;
        }

        currentQuestState = quest.state;
        if (questIcon != null)
        {
            questIcon.SetState(currentQuestState, startPoint, finishPoint);
        }
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            playerIsNear = true;
        }
    }

    private void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            playerIsNear = false;
        }
    }
}
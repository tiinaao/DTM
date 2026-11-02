using UnityEngine;

public enum QuestTriggerType
{
    EnterArea,      
    HasItem,       
    TimeState,      
    MinReputation,  
    Automatic,      
    Manual        
}

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private QuestInfoSO quest;
    [SerializeField] private QuestTriggerType type = QuestTriggerType.EnterArea;

    [SerializeField] private ItemData requiredItem;
    [SerializeField] private int requiredAmount = 1;

    [SerializeField] private TimeState requiredTimeState = TimeState.Morning;

    [SerializeField] private int minReputation = 1;

    [SerializeField] private float checkInterval = 0.25f;

    private QuestState state = QuestState.REQUIREMENTS_NOT_MET;
    private bool playerInside;
    private float timer;

    private void OnEnable()
    {
        GameEventsManager.instance.questEvents.onQuestStateChange += OnQuestStateChange;
    }

    private void Start()
    {
        if (quest != null && GameManager.Instance.Quests != null &&
            GameManager.Instance.Quests.TryGetState(quest.id, out QuestState current))
        {
            state = current;
        }
    }

    private void OnDisable()
    {
        if (GameEventsManager.instance == null) return;
        GameEventsManager.instance.questEvents.onQuestStateChange -= OnQuestStateChange;
    }

    private void OnQuestStateChange(Quest q)
    {
        if (quest != null && q.info.id == quest.id) state = q.state;
    }

    private void Update()
    {
        if (quest == null || type == QuestTriggerType.Manual) return;
        if (state != QuestState.CAN_START) return;

        timer += Time.unscaledDeltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        if (ConditionMet()) TriggerStart();
    }

    private bool ConditionMet()
    {
        switch (type)
        {
            case QuestTriggerType.EnterArea:
                return playerInside;

            case QuestTriggerType.HasItem:
                return requiredItem != null &&
                       GameManager.Instance.Inventory != null &&
                       GameManager.Instance.Inventory.HasItem(requiredItem.itemId, requiredAmount);

            case QuestTriggerType.TimeState:
                return GameManager.Instance.Sun != null &&
                       GameManager.Instance.Sun.CurrentState == requiredTimeState;

            case QuestTriggerType.MinReputation:
                return GameManager.Instance.Reputation != null &&
                       GameManager.Instance.Reputation.reputation >= minReputation;

            case QuestTriggerType.Automatic:
                return true;
        }
        return false;
    }

    public void TriggerStart()
    {
        if (quest == null || state != QuestState.CAN_START) return;
        GameEventsManager.instance.questEvents.StartQuest(quest.id);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
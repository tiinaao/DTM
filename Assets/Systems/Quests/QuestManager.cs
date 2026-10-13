using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    private Dictionary<string, Quest> questMap;

    private void Awake()
    {
        questMap = CreateQuestMap();
        GameManager.Instance.Quests = this;
    }

    private void OnEnable()
    {
        GameEventsManager.instance.questEvents.onStartQuest += StartQuest;
        GameEventsManager.instance.questEvents.onAdvanceQuest += AdvanceQuest;
        GameEventsManager.instance.questEvents.onFinishQuest += FinishQuest;
        GameEventsManager.instance.questEvents.onQuestStepStateChange += QuestStepStateChange;
    }

    private void OnDisable()
    {
        if (GameEventsManager.instance == null)
        {
            return;
        }

        GameEventsManager.instance.questEvents.onStartQuest -= StartQuest;
        GameEventsManager.instance.questEvents.onAdvanceQuest -= AdvanceQuest;
        GameEventsManager.instance.questEvents.onFinishQuest -= FinishQuest;
        GameEventsManager.instance.questEvents.onQuestStepStateChange -= QuestStepStateChange;
    }

    private void Start()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.IN_PROGRESS)
            {
                quest.InstantiateCurrentQuestStep(transform);
            }
            GameEventsManager.instance.questEvents.QuestStateChange(quest);
        }
        RefreshRequirements();
    }

    public void Save(ref QuestSaveData data)
    {
        data.ids = new string[questMap.Count];
        data.quests = new QuestData[questMap.Count];

        int index = 0;
        foreach (KeyValuePair<string, Quest> pair in questMap)
        {
            data.ids[index] = pair.Key;
            data.quests[index] = pair.Value.GetQuestData();
            index++;
        }
    }

    public void Load(QuestSaveData data)
    {
        if (data.ids == null || data.quests == null)
        {
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        int count = Mathf.Min(data.ids.Length, data.quests.Length);
        for (int i = 0; i < count; i++)
        {
            if (data.quests[i] == null || !questMap.TryGetValue(data.ids[i], out Quest existing))
            {
                continue;
            }

            QuestData questData = data.quests[i];
            questMap[data.ids[i]] = new Quest(existing.info, questData.state, questData.questStepIndex, questData.questStepStates);
        }

        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.IN_PROGRESS)
            {
                quest.InstantiateCurrentQuestStep(transform);
            }
            GameEventsManager.instance.questEvents.QuestStateChange(quest);
        }
        RefreshRequirements();
    }

    private void ChangeQuestState(string id, QuestState state)
    {
        Quest quest = GetQuestById(id);
        if (quest == null)
        {
            return;
        }

        quest.state = state;
        GameEventsManager.instance.questEvents.QuestStateChange(quest);
        RefreshRequirements();
    }

    private bool CheckRequirementsMet(Quest quest)
    {
        foreach (QuestInfoSO prerequisite in quest.info.questPrerequisites)
        {
            if (prerequisite == null)
            {
                continue;
            }

            Quest prerequisiteQuest = GetQuestById(prerequisite.id);
            if (prerequisiteQuest == null || prerequisiteQuest.state != QuestState.FINISHED)
            {
                return false;
            }
        }
        return true;
    }

    private void RefreshRequirements()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.REQUIREMENTS_NOT_MET && CheckRequirementsMet(quest))
            {
                ChangeQuestState(quest.info.id, QuestState.CAN_START);
            }
        }
    }

    private void StartQuest(string id)
    {
        Quest quest = GetQuestById(id);
        if (quest == null || quest.state != QuestState.CAN_START)
        {
            return;
        }

        quest.InstantiateCurrentQuestStep(transform);
        ChangeQuestState(id, QuestState.IN_PROGRESS);
    }

    private void AdvanceQuest(string id)
    {
        Quest quest = GetQuestById(id);
        if (quest == null)
        {
            return;
        }

        quest.MoveToNextStep();

        if (quest.CurrentStepExists())
        {
            quest.InstantiateCurrentQuestStep(transform);
        }
        else
        {
            ChangeQuestState(id, QuestState.CAN_FINISH);
        }
    }

    private void FinishQuest(string id)
    {
        Quest quest = GetQuestById(id);
        if (quest == null || quest.state != QuestState.CAN_FINISH)
        {
            return;
        }

        ChangeQuestState(id, QuestState.FINISHED);
    }

    private void QuestStepStateChange(string id, int stepIndex, QuestStepState questStepState)
    {
        Quest quest = GetQuestById(id);
        if (quest == null)
        {
            return;
        }

        quest.StoreQuestStepState(questStepState, stepIndex);
        GameEventsManager.instance.questEvents.QuestStateChange(quest);
    }

    private Dictionary<string, Quest> CreateQuestMap()
    {
        QuestInfoSO[] allQuests = Resources.LoadAll<QuestInfoSO>("Quests");
        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();
        foreach (QuestInfoSO questInfo in allQuests)
        {
            idToQuestMap[questInfo.id] = new Quest(questInfo);
        }
        return idToQuestMap;
    }

    private Quest GetQuestById(string id)
    {
        questMap.TryGetValue(id, out Quest quest);
        return quest;
    }
}
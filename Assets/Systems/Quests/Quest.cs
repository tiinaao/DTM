using UnityEngine;

public class Quest
{
    public QuestInfoSO info;
    public QuestState state;
    private int currentQuestStepIndex;
    private QuestStepState[] questStepStates;

    public Quest(QuestInfoSO questInfo)
    {
        info = questInfo;
        state = QuestState.REQUIREMENTS_NOT_MET;
        currentQuestStepIndex = 0;
        questStepStates = CreateEmptyStates(info.questStepPrefabs.Length);
    }

    public Quest(QuestInfoSO questInfo, QuestState questState, int currentQuestStepIndex, QuestStepState[] questStepStates)
    {
        info = questInfo;
        state = questState;
        this.currentQuestStepIndex = Mathf.Clamp(currentQuestStepIndex, 0, info.questStepPrefabs.Length);

        int length = info.questStepPrefabs.Length;
        this.questStepStates = CreateEmptyStates(length);
        if (questStepStates != null)
        {
            for (int i = 0; i < length && i < questStepStates.Length; i++)
            {
                if (questStepStates[i] != null)
                {
                    this.questStepStates[i] = questStepStates[i];
                }
            }
        }
    }

    private static QuestStepState[] CreateEmptyStates(int length)
    {
        QuestStepState[] states = new QuestStepState[length];
        for (int i = 0; i < length; i++)
        {
            states[i] = new QuestStepState();
        }
        return states;
    }

    public void MoveToNextStep()
    {
        currentQuestStepIndex++;
    }

    public bool CurrentStepExists()
    {
        return currentQuestStepIndex < info.questStepPrefabs.Length;
    }

    public void InstantiateCurrentQuestStep(Transform parentTransform)
    {
        if (!CurrentStepExists())
        {
            return;
        }

        GameObject questStepPrefab = info.questStepPrefabs[currentQuestStepIndex];
        if (questStepPrefab == null)
        {
            return;
        }

        QuestStep questStep = Object.Instantiate(questStepPrefab, parentTransform).GetComponent<QuestStep>();
        if (questStep != null)
        {
            questStep.InitializeQuestStep(info.id, currentQuestStepIndex, questStepStates[currentQuestStepIndex].state);
        }
    }

    public void StoreQuestStepState(QuestStepState questStepState, int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= questStepStates.Length)
        {
            return;
        }
        questStepStates[stepIndex].state = questStepState.state;
    }

    public QuestData GetQuestData()
    {
        return new QuestData(state, currentQuestStepIndex, questStepStates);
    }
}
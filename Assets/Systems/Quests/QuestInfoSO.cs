using UnityEngine;

[System.Serializable]
public class QuestItemReward
{
    public ItemData item;
    public int amount = 1;
}

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "Quests/QuestInfoSO", order = 1)]
public class QuestInfoSO : ScriptableObject
{
    public string id => displayName.ToLower().Replace(" ", "_");
    public string displayName;
    public QuestInfoSO[] questPrerequisites;
    public GameObject[] questStepPrefabs;

    public int moraReward;
    public int reputationReward;
    public QuestItemReward[] itemRewards;
}
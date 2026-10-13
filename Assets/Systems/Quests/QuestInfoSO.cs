using UnityEngine;

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "ScriptableObjects/QuestInfoSO", order = 1)]
public class QuestInfoSO : ScriptableObject
{
    public string id => displayName.ToLower().Replace(" ", "_");
    public string displayName;
    public QuestInfoSO[] questPrerequisites;
    public GameObject[] questStepPrefabs;
}
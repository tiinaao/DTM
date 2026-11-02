using UnityEngine;

public class Reputation : MonoBehaviour
{
    public int reputation = 0;
    void Start()
    {
        GameManager.Instance.Reputation = this;
    }

    public void ChangeReputation(int delta)
    {
        reputation += delta;
    }

    public void Save(ref ReputationSaveData data)
    {
        data.reputation = reputation;
    }

    public void Load(ReputationSaveData data)
    {
        reputation = data.reputation;
    }
}

[System.Serializable]
public struct ReputationSaveData
{
    public int reputation;
}
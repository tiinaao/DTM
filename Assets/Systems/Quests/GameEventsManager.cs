using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameEventsManager : MonoBehaviour
{
    public static GameEventsManager instance { get; private set; }

    public QuestEvents questEvents { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        questEvents = new QuestEvents();
    }
}
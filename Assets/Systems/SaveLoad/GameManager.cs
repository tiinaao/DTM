using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                var prefab = Resources.Load<GameManager>("GameManager");
                Instantiate(prefab);
            }
            return instance;
        }
    }

    public PlayerModel Player { get; set; }
    public Health Health { get; set; }
    public Stamina Stamina { get; set; }
    public InventorySystem Inventory { get; set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
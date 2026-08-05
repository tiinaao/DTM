using System.IO;
using UnityEngine;
using static InventorySystem;

public class SaveSystem
{
    private static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData PlayerData;
        public HealthSaveData HealthData;
        public StaminaSaveData StaminaData;
        public InventorySaveData InventoryData;
    }

    public static string SaveFileName() => Application.persistentDataPath + "/save.save";

    public static void Save()
    {
        HandleSaveData();
        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    private static void HandleSaveData()
    {
        if (GameManager.Instance.Player != null)
            GameManager.Instance.Player.Save(ref _saveData.PlayerData);

        if (GameManager.Instance.Health != null)
            GameManager.Instance.Health.Save(ref _saveData.HealthData);

        if (GameManager.Instance.Stamina != null)
            GameManager.Instance.Stamina.Save(ref _saveData.StaminaData);

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.Save(ref _saveData.InventoryData);
    }

    public static void Load()
    {
        if (!File.Exists(SaveFileName())) return;

        string saveContent = File.ReadAllText(SaveFileName());
        _saveData = JsonUtility.FromJson<SaveData>(saveContent);
        HandleLoadData();
    }

    private static void HandleLoadData()
    {
        if (GameManager.Instance.Player != null)
            GameManager.Instance.Player.Load(_saveData.PlayerData);

        if (GameManager.Instance.Health != null)
            GameManager.Instance.Health.Load(_saveData.HealthData);

        if (GameManager.Instance.Stamina != null)
            GameManager.Instance.Stamina.Load(_saveData.StaminaData);

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.Load(_saveData.InventoryData);
    }
}
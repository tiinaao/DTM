using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using static InventorySystem;

public enum LoadResult
{
    Loaded,
    LoadedFromRecovery,
    Failed
}

[Serializable]
public struct SaveMetadata
{
    public int version;
    public string savedAtUtc;
    public float playTimeSeconds;
    public string areaName;
    public int sourceSlot;
}

public struct SlotInfo
{
    public int slot;
    public bool exists;
    public bool valid;
    public SaveMetadata meta;

    public string DateText
    {
        get
        {
            if (!valid) return "";
            if (DateTime.TryParse(meta.savedAtUtc, null, DateTimeStyles.RoundtripKind, out DateTime date))
                return date.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return "";
        }
    }

    public string PlayTimeText
    {
        get
        {
            if (!valid) return "";
            TimeSpan t = TimeSpan.FromSeconds(meta.playTimeSeconds);
            return string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
        }
    }
}

public static class SaveSystem
{
    public const int CurrentVersion = 1;
    public const int MaxSlots = 3;

    private const string FolderName = "SaveData";
    private const string RecoveryFileName = "recovery.save";

    public static int CurrentSlot { get; private set; } = -1;
    public static string CurrentAreaName { get; set; }

    private static float loadedPlayTime;
    private static float sessionStart;

    [Serializable]
    public struct SaveData
    {
        public PlayerSaveData PlayerData;
        public HealthSaveData HealthData;
        public StaminaSaveData StaminaData;
        public InventorySaveData InventoryData;
        public QuestSaveData QuestsData;
        public NPCShopSaveData ShopsData;
        public ReputationSaveData ReputationData;
    }

    [Serializable]
    public class SaveFile
    {
        public SaveMetadata meta;
        public SaveData data;
    }

    public static float PlayTime => loadedPlayTime + (Time.realtimeSinceStartup - sessionStart);

    private static string FolderPath => Path.Combine(Application.persistentDataPath, FolderName);
    private static string RecoveryPath => Path.Combine(FolderPath, RecoveryFileName);

    public static string SlotPath(int slot) => Path.Combine(FolderPath, "slot_" + slot + ".save");

    public static bool IsValidSlot(int slot) => slot >= 1 && slot <= MaxSlots;

    private static void SetPlayTime(float seconds)
    {
        loadedPlayTime = seconds;
        sessionStart = Time.realtimeSinceStartup;
    }

    public static void NewGame(int slot)
    {
        if (!IsValidSlot(slot)) return;
        CurrentSlot = slot;
        SetPlayTime(0f);
    }

    public static bool Save() => Save(CurrentSlot);

    public static bool Save(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning("Save failed: invalid slot " + slot);
            return false;
        }

        try
        {
            WriteFile(SlotPath(slot), BuildSaveFile(slot));
            CurrentSlot = slot;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
            return false;
        }
    }

    public static bool AutoSave()
    {
        if (!IsValidSlot(CurrentSlot)) return false;

        try
        {
            WriteFile(RecoveryPath, BuildSaveFile(CurrentSlot));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Auto-save failed: " + e.Message);
            return false;
        }
    }

    public static LoadResult Load(int slot)
    {
        if (IsValidSlot(slot) && TryReadFile(SlotPath(slot), out SaveFile file))
        {
            if (Apply(file, slot)) return LoadResult.Loaded;
        }

        Debug.LogWarning("Slot " + slot + " could not be loaded, trying recovery file");

        if (TryReadFile(RecoveryPath, out SaveFile recovery))
        {
            int target = IsValidSlot(slot) ? slot : recovery.meta.sourceSlot;
            if (Apply(recovery, target)) return LoadResult.LoadedFromRecovery;
        }

        return LoadResult.Failed;
    }

    public static LoadResult LoadRecovery()
    {
        if (TryReadFile(RecoveryPath, out SaveFile recovery))
        {
            if (Apply(recovery, recovery.meta.sourceSlot)) return LoadResult.LoadedFromRecovery;
        }

        return LoadResult.Failed;
    }

    public static SlotInfo GetSlotInfo(int slot)
    {
        SlotInfo info = new SlotInfo { slot = slot };
        if (!IsValidSlot(slot)) return info;

        info.exists = File.Exists(SlotPath(slot));
        if (info.exists && TryReadFile(SlotPath(slot), out SaveFile file))
        {
            info.valid = true;
            info.meta = file.meta;
        }

        return info;
    }

    public static SlotInfo GetRecoveryInfo()
    {
        SlotInfo info = new SlotInfo { slot = 0 };
        info.exists = File.Exists(RecoveryPath);
        if (info.exists && TryReadFile(RecoveryPath, out SaveFile file))
        {
            info.valid = true;
            info.meta = file.meta;
        }

        return info;
    }

    public static void DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;

        try
        {
            if (File.Exists(SlotPath(slot))) File.Delete(SlotPath(slot));
            if (CurrentSlot == slot) CurrentSlot = -1;
        }
        catch (Exception e)
        {
            Debug.LogError("Delete failed: " + e.Message);
        }
    }

    private static SaveFile BuildSaveFile(int sourceSlot)
    {
        SaveFile file = new SaveFile();
        CollectData(ref file.data);

        file.meta = new SaveMetadata
        {
            version = CurrentVersion,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            playTimeSeconds = PlayTime,
            areaName = string.IsNullOrEmpty(CurrentAreaName) ? SceneManager.GetActiveScene().name : CurrentAreaName,
            sourceSlot = sourceSlot
        };

        return file;
    }

    private static void WriteFile(string path, SaveFile file)
    {
        Directory.CreateDirectory(FolderPath);

        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(file, true));

        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }

    private static bool TryReadFile(string path, out SaveFile file)
    {
        file = null;
        if (!File.Exists(path)) return false;

        try
        {
            SaveFile parsed = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            if (parsed == null || parsed.meta.version <= 0) return false;
            if (!Migrate(parsed)) return false;

            file = parsed;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not read " + path + ": " + e.Message);
            return false;
        }
    }

    private static bool Migrate(SaveFile file)
    {
        if (file.meta.version > CurrentVersion) return false;

        while (file.meta.version < CurrentVersion)
        {
            if (!MigrateStep(file)) return false;
        }

        return true;
    }

    private static bool MigrateStep(SaveFile file)
    {
        switch (file.meta.version)
        {
            default: return false;
        }
    }

    private static bool Apply(SaveFile file, int slot)
    {
        try
        {
            ApplyData(file.data);
            CurrentSlot = slot;
            SetPlayTime(file.meta.playTimeSeconds);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Applying save failed: " + e.Message);
            return false;
        }
    }

    private static void CollectData(ref SaveData data)
    {
        if (GameManager.Instance.Player != null)
            GameManager.Instance.Player.Save(ref data.PlayerData);

        if (GameManager.Instance.Health != null)
            GameManager.Instance.Health.Save(ref data.HealthData);

        if (GameManager.Instance.Stamina != null)
            GameManager.Instance.Stamina.Save(ref data.StaminaData);

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.Save(ref data.InventoryData);

        if (GameManager.Instance.Quests != null)
            GameManager.Instance.Quests.Save(ref data.QuestsData);

        if (GameManager.Instance.Shops != null)
            GameManager.Instance.Shops.Save(ref data.ShopsData);

        if (GameManager.Instance.Reputation != null)
            GameManager.Instance.Reputation.Save(ref data.ReputationData);
    }

    private static void ApplyData(SaveData data)
    {
        if (GameManager.Instance.Player != null)
            GameManager.Instance.Player.Load(data.PlayerData);

        if (GameManager.Instance.Health != null)
            GameManager.Instance.Health.Load(data.HealthData);

        if (GameManager.Instance.Stamina != null)
            GameManager.Instance.Stamina.Load(data.StaminaData);

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.Load(data.InventoryData);

        if (GameManager.Instance.Quests != null)
            GameManager.Instance.Quests.Load(data.QuestsData);

        if (GameManager.Instance.Shops != null)
            GameManager.Instance.Shops.Load(data.ShopsData);

        if (GameManager.Instance.Reputation != null)
            GameManager.Instance.Reputation.Load(data.ReputationData);
    }
}
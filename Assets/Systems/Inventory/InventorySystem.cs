using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public const int Columns = 9;
    public const int Rows = 3;
    public const int MaxSlotsPerCategory = Columns * Rows;

    [SerializeField] private ItemDatabase itemDatabase;

    public List<InventoryItem> consumables = new List<InventoryItem>();
    public List<InventoryItem> weapons = new List<InventoryItem>();
    public List<InventoryItem> wearables = new List<InventoryItem>();
    public List<InventoryItem> others = new List<InventoryItem>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        GameManager.Instance.Inventory = this;
        itemDatabase.Initialize();
    }

    private List<InventoryItem> GetListForCategory(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Consumable: return consumables;
            case ItemCategory.Weapon: return weapons;
            case ItemCategory.Wearable: return wearables;
            default: return others;
        }
    }

    public bool HasSpaceFor(string itemId)
    {
        ItemData data = itemDatabase.GetItemById(itemId);
        if (data == null) return false;

        List<InventoryItem> list = GetListForCategory(data.category);

        if (data.isStackable && list.Exists(i => i.data.itemId == itemId))
            return true;

        return list.Count < MaxSlotsPerCategory;
    }

    public bool AddItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return false;

        ItemData data = itemDatabase.GetItemById(itemId);
        if (data == null) return false;

        List<InventoryItem> list = GetListForCategory(data.category);

        if (data.isStackable)
        {
            InventoryItem existing = list.Find(i => i.data.itemId == itemId);
            if (existing != null)
            {
                existing.quantity = Mathf.Min(existing.quantity + amount, data.maxStack);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        if (list.Count >= MaxSlotsPerCategory) return false;

        list.Add(new InventoryItem(data, amount));
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return false;

        ItemData data = itemDatabase.GetItemById(itemId);
        if (data == null) return false;

        List<InventoryItem> list = GetListForCategory(data.category);
        InventoryItem existing = list.Find(i => i.data.itemId == itemId);
        if (existing == null || existing.quantity < amount) return false;

        existing.quantity -= amount;
        if (existing.quantity <= 0)
            list.Remove(existing);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        ItemData data = itemDatabase.GetItemById(itemId);
        if (data == null) return false;

        List<InventoryItem> list = GetListForCategory(data.category);
        InventoryItem existing = list.Find(i => i.data.itemId == itemId);
        return existing != null && existing.quantity >= amount;
    }

    public InventoryItem FindItem(string itemId)
    {
        foreach (var list in new[] { consumables, weapons, wearables, others })
        {
            var found = list.Find(i => i.data.itemId == itemId);
            if (found != null) return found;
        }
        return null;
    }

    [System.Serializable]
    public struct SavedItemStack
    {
        public string itemId;
        public int quantity;

        public SavedItemStack(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }

    [System.Serializable]
    public struct InventorySaveData
    {
        public List<SavedItemStack> consumables;
        public List<SavedItemStack> weapons;
        public List<SavedItemStack> wearables;
        public List<SavedItemStack> others;
    }

    public void Save(ref InventorySaveData data)
    {
        data.consumables = ToSaveList(consumables);
        data.weapons = ToSaveList(weapons);
        data.wearables = ToSaveList(wearables);
        data.others = ToSaveList(others);
    }

    public void Load(InventorySaveData data)
    {
        consumables.Clear();
        weapons.Clear();
        wearables.Clear();
        others.Clear();

        FromSaveList(data.consumables);
        FromSaveList(data.weapons);
        FromSaveList(data.wearables);
        FromSaveList(data.others);

        OnInventoryChanged?.Invoke();
    }

    private List<SavedItemStack> ToSaveList(List<InventoryItem> items)
    {
        var result = new List<SavedItemStack>();
        foreach (var item in items)
            result.Add(new SavedItemStack(item.data.itemId, item.quantity));
        return result;
    }

    private void FromSaveList(List<SavedItemStack> savedItems)
    {
        if (savedItems == null) return;
        foreach (var saved in savedItems)
            AddItem(saved.itemId, saved.quantity);
    }
}
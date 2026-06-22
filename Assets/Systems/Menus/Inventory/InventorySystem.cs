using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [SerializeField] private ItemDatabase itemDatabase;

    public List<InventoryItem> consumables = new List<InventoryItem>();
    public List<InventoryItem> weapons = new List<InventoryItem>();
    public List<InventoryItem> wearables = new List<InventoryItem>();
    public List<InventoryItem> others = new List<InventoryItem>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        Instance = this;
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

    public void AddItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return;

        ItemData data = itemDatabase.GetItemById(itemId);

        List<InventoryItem> list = GetListForCategory(data.category);

        if (data.isStackable)
        {
            InventoryItem existing = list.Find(i => i.data.itemId == itemId);
            if (existing != null)
            {
                existing.quantity = Mathf.Min(existing.quantity + amount, data.maxStack);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        list.Add(new InventoryItem(data, amount));
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(string itemId, int amount = 1)
    {
        ItemData data = itemDatabase.GetItemById(itemId);
        if (data == null) return;

        List<InventoryItem> list = GetListForCategory(data.category);
        InventoryItem existing = list.Find(i => i.data.itemId == itemId);
        if (existing == null) return;

        existing.quantity -= amount;
        if (existing.quantity <= 0)
            list.Remove(existing);

        OnInventoryChanged?.Invoke();
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
}
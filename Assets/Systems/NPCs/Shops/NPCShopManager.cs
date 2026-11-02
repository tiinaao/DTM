using System.Collections.Generic;
using UnityEngine;

public class NPCShopManager : MonoBehaviour
{
    private static readonly Dictionary<string, NPCShopHandler> registry = new Dictionary<string, NPCShopHandler>();

    private void Awake()
    {
        GameManager.Instance.Shops = this;
    }

    public static void Register(string npcId, NPCShopHandler handler)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        registry[npcId] = handler;
    }

    public static void Unregister(string npcId, NPCShopHandler handler)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        if (registry.TryGetValue(npcId, out NPCShopHandler existing) && existing == handler)
            registry.Remove(npcId);
    }

    public void Save(ref NPCShopSaveData data)
    {
        List<ShopStockSaveEntry> entries = new List<ShopStockSaveEntry>();

        foreach (KeyValuePair<string, NPCShopHandler> pair in registry)
        {
            foreach (ShopStockEntry stockEntry in pair.Value.Stock)
            {
                if (stockEntry.item == null) continue;

                entries.Add(new ShopStockSaveEntry
                {
                    npcId = pair.Key,
                    itemId = stockEntry.item.itemId,
                    stock = stockEntry.stock
                });
            }
        }

        data.entries = entries;
    }

    public void Load(NPCShopSaveData data)
    {
        if (data.entries == null) return;

        foreach (ShopStockSaveEntry saved in data.entries)
        {
            if (registry.TryGetValue(saved.npcId, out NPCShopHandler handler))
                handler.ApplyStock(saved.itemId, saved.stock);
        }
    }
}

[System.Serializable]
public struct ShopStockSaveEntry
{
    public string npcId;
    public string itemId;
    public int stock;
}

[System.Serializable]
public struct NPCShopSaveData
{
    public List<ShopStockSaveEntry> entries;
}

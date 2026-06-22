using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems = new List<ItemData>();

    private Dictionary<string, ItemData> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<string, ItemData>();
        foreach (var item in allItems)
        {
            if (!_lookup.ContainsKey(item.itemId))
                _lookup.Add(item.itemId, item);
        }
    }

    public ItemData GetItemById(string id)
    {
        if (_lookup == null) Initialize();
        _lookup.TryGetValue(id, out var item);
        return item;
    }
}
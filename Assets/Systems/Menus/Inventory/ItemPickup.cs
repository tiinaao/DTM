using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemPickupEntry
{
    public ItemData itemData;
    public int amount = 1;
}

public class ItemPickup : MonoBehaviour
{
    [SerializeField] private List<ItemPickupEntry> items = new List<ItemPickupEntry>();

    public void GiveItem()
    {
        foreach (var entry in items)
        {
            if (entry.itemData != null)
                InventorySystem.Instance.AddItem(entry.itemData.itemId, entry.amount);
        }
    }
}
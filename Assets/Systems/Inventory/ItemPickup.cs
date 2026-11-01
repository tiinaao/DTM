using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemPickupEntry
{
    public ItemData itemData;
    public int amount = 1;
}

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private List<ItemPickupEntry> items = new List<ItemPickupEntry>();

    public string InteractionText
    {
        get
        {
            if (items.Count == 0 || items[0].itemData == null) return "Pick up";
            return items.Count == 1 ? $"Pick up {items[0].itemData.itemName}" : "Pick up items";
        }
    }

    public void Interact()
    {
        GiveItem();
    }

    public void GiveItem()
    {
        foreach (var entry in items)
        {
            if (entry.itemData != null)
                GameManager.Instance.Inventory.AddItem(entry.itemData.itemId, entry.amount);
        }
    }
}
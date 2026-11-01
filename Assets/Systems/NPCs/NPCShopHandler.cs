using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopStockEntry
{
    public ItemData item;
    public int price = 1;
    public int stock = -1;
}

public class NPCShopHandler : MonoBehaviour
{
    [SerializeField] private List<ShopStockEntry> stock = new List<ShopStockEntry>();
    [SerializeField] private bool allowBartering = true;
    [SerializeField] private bool applyDiscount = false;
    [SerializeField] private float sellMultiplier = 0.5f;

    private NPCBase npcBase;

    public IReadOnlyList<ShopStockEntry> Stock => stock;
    public bool AllowBartering => allowBartering;

    private void Awake()
    {
        npcBase = GetComponent<NPCBase>();
    }

    private void OnEnable()
    {
        if (npcBase != null) NPCShopManager.Register(npcBase.Id, this);
    }

    private void OnDisable()
    {
        if (npcBase != null) NPCShopManager.Unregister(npcBase.Id, this);
    }

    public ShopStockEntry FindEntry(string itemId)
    {
        for (int i = 0; i < stock.Count; i++)
        {
            if (stock[i].item != null && stock[i].item.itemId == itemId)
                return stock[i];
        }

        return null;
    }

    public void OpenShop()
    {
        ShopUI ui = FindAnyObjectByType<ShopUI>();
        if (ui == null) return;

        NPCInteraction npc = GetComponent<NPCInteraction>();
        ui.Open(this, () =>
        {
            if (npc != null && npc.IsInteracting) NPCInteractionUI.Get().ShowMenu();
        });
    }

    public int GetSellPrice(ItemData data)
    {
        return Mathf.Max(1, Mathf.RoundToInt(data.value * sellMultiplier));
    }

    public bool TryBuy(string itemId, int quantity = 1)
    {
        ShopStockEntry entry = FindEntry(itemId);
        if (entry == null || quantity <= 0 || entry.stock == 0) return false;
        if (entry.stock > 0 && entry.stock < quantity) return false;

        InventorySystem inventory = GameManager.Instance.Inventory;
        int totalPrice = entry.price * quantity;

        if (!inventory.SpendCurrency(totalPrice)) return false;

        if (!inventory.AddItem(itemId, quantity))
        {
            inventory.AddCurrency(totalPrice);
            return false;
        }

        if (entry.stock > 0) entry.stock -= quantity;
        return true;
    }

    public bool TrySell(string itemId, int quantity = 1)
    {
        InventorySystem inventory = GameManager.Instance.Inventory;
        InventoryItem item = inventory.FindItem(itemId);
        if (item == null || quantity <= 0 || item.quantity < quantity) return false;

        int payout = GetSellPrice(item.data) * quantity;
        if (!inventory.RemoveItem(itemId, quantity)) return false;

        inventory.AddCurrency(payout);
        return true;
    }

    public bool TryTrade(string offeredItemId, int offeredQuantity, string requestedItemId, int requestedQuantity)
    {
        if (!allowBartering) return false;

        ShopStockEntry requestedEntry = FindEntry(requestedItemId);
        if (requestedEntry == null || requestedEntry.item == null) return false;
        if (offeredQuantity <= 0 || requestedQuantity <= 0) return false;
        if (requestedEntry.stock == 0) return false;
        if (requestedEntry.stock > 0 && requestedEntry.stock < requestedQuantity) return false;

        InventorySystem inventory = GameManager.Instance.Inventory;
        if (!inventory.HasItem(offeredItemId, offeredQuantity)) return false;

        InventoryItem offered = inventory.FindItem(offeredItemId);
        if (offered == null || offered.data == null) return false;

        int offeredValue = offered.data.value * offeredQuantity;
        int requestedValue = requestedEntry.item.value * requestedQuantity;
        if (offeredValue < requestedValue) return false;

        if (!inventory.RemoveItem(offeredItemId, offeredQuantity)) return false;

        if (!inventory.AddItem(requestedItemId, requestedQuantity))
        {
            inventory.AddItem(offeredItemId, offeredQuantity);
            return false;
        }

        if (requestedEntry.stock > 0) requestedEntry.stock -= requestedQuantity;
        return true;
    }

    public void ApplyStock(string itemId, int newStock)
    {
        ShopStockEntry entry = FindEntry(itemId);
        if (entry != null) entry.stock = newStock;
    }
}
using System.Collections.Generic;
using UnityEngine;

public class NPCGiftHandler : MonoBehaviour
{
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private bool acceptsAnyItem = true;
    [SerializeField] private List<ItemData> acceptedItems = new List<ItemData>();

    public bool SupportsGifting => enabled;

    public void Gift()
    {
        InventoryUI ui = ResolveInventoryUI();
        if (ui == null) return;

        NPCInteraction npc = GetComponent<NPCInteraction>();

        ui.OpenForSelection(
            item => Complete(npc, item),
            () => Complete(npc, null));
    }

    private void Complete(NPCInteraction npc, InventoryItem item)
    {
        if (item != null && item.data != null && Accepts(item.data))
        {
            GameManager.Instance.Inventory.RemoveItem(item.data.itemId, 1);

            if (GameEventsManager.instance != null)
                GameEventsManager.instance.npcEvents.NpcGifted(npc != null ? npc.Id : string.Empty, item.data.itemId);
        }

        if (npc != null && npc.IsInteracting)
            NPCInteractionUI.Get().ShowMenu();
    }

    private bool Accepts(ItemData data)
    {
        if (acceptsAnyItem) return true;
        return acceptedItems.Exists(i => i != null && i.itemId == data.itemId);
    }

    private InventoryUI ResolveInventoryUI()
    {
        if (inventoryUI == null) inventoryUI = FindAnyObjectByType<InventoryUI>();
        return inventoryUI;
    }
}

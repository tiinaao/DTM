using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Quickslot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    public enum SlotType { Weapon, ConsumableR, ConsumableExtra }
    [SerializeField] private SlotType slotType;

    [SerializeField] private float scrollCooldown = 0.15f;
    private float scrollTimer = 0f;

    private void Start()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged += Refresh;

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged -= Refresh;

        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        if (playerInputHandler == null) return;

        bool triggered = slotType switch
        {
            SlotType.ConsumableR => playerInputHandler.UseLTriggered,
            SlotType.ConsumableExtra => playerInputHandler.UseRTriggered,
            _ => false
        };

        if (triggered) TryUse();

        if (slotType == SlotType.Weapon)
            HandleWeaponScroll();
    }

    private void HandleWeaponScroll()
    {
        if (scrollTimer > 0f)
        {
            scrollTimer -= Time.deltaTime;
            return;
        }

        if (playerInputHandler.ScrollInput != 0f)
        {
            bool cycled = EquipmentSystem.Instance != null && EquipmentSystem.Instance.CycleWeapon();
            if (cycled)
                UISoundManager.instance.Play("switch");

            scrollTimer = scrollCooldown;
        }
    }

    private void TryUse()
    {
        ItemData item = slotType switch
        {
            SlotType.ConsumableR => EquipmentSystem.Instance?.equippedConsumableR,
            SlotType.ConsumableExtra => EquipmentSystem.Instance?.equippedConsumableExtra,
            _ => null
        };

        if (item == null) return;

        string itemId = item.itemId;

        if (!GameManager.Instance.Inventory.HasItem(itemId)) return;

        item.Use();
        GameManager.Instance.Inventory.RemoveItem(itemId, 1);

        if (!GameManager.Instance.Inventory.HasItem(itemId))
        {
            if (slotType == SlotType.ConsumableR)
                EquipmentSystem.Instance.UnequipConsumableBySlot(true);
            else if (slotType == SlotType.ConsumableExtra)
                EquipmentSystem.Instance.UnequipConsumableBySlot(false);
        }
        else
        {
            EquipmentSystem.Instance.NotifyEquipmentChanged();
        }
    }

    public void Refresh()
    {
        if (EquipmentSystem.Instance == null)
        {
            icon.enabled = false;
            quantityText.text = "";
            return;
        }

        ItemData data = slotType switch
        {
            SlotType.Weapon => EquipmentSystem.Instance.equippedWeapon,
            SlotType.ConsumableR => EquipmentSystem.Instance.equippedConsumableR,
            SlotType.ConsumableExtra => EquipmentSystem.Instance.equippedConsumableExtra,
            _ => null
        };

        if (data == null)
        {
            icon.enabled = false;
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0f);
            quantityText.text = "";
            return;
        }

        icon.enabled = true;
        icon.sprite = data.icon;
        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1f);

        InventoryItem invItem = GameManager.Instance.Inventory.FindItem(data.itemId);
        quantityText.text = (invItem != null && data.isStackable && invItem.quantity > 0)
            ? invItem.quantity.ToString("D2")
            : "";
    }
}
using UnityEngine;
using System;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance { get; private set; }

    public ItemData equippedWeaponPrimary;
    public ItemData equippedWeaponSecondary;
    private int activeWeaponIndex = 0;

    public ItemData equippedWeapon => activeWeaponIndex == 0 ? equippedWeaponPrimary : equippedWeaponSecondary;

    public ItemData equippedConsumableR;
    public ItemData equippedConsumableExtra;

    public event Action OnEquipmentChanged;

    private void Awake() => Instance = this;

    private void Start()
    {
        GameManager.Instance.Inventory.OnInventoryChanged += CheckEquippedItems;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.OnInventoryChanged -= CheckEquippedItems;
    }

    private void CheckEquippedItems()
    {
        bool changed = false;

        if (equippedWeaponPrimary != null && !GameManager.Instance.Inventory.HasItem(equippedWeaponPrimary.itemId))
        {
            equippedWeaponPrimary = null;
            changed = true;
        }

        if (equippedWeaponSecondary != null && !GameManager.Instance.Inventory.HasItem(equippedWeaponSecondary.itemId))
        {
            equippedWeaponSecondary = null;
            changed = true;
        }

        if (activeWeaponIndex == 0 && equippedWeaponPrimary == null && equippedWeaponSecondary != null)
            activeWeaponIndex = 1;
        else if (activeWeaponIndex == 1 && equippedWeaponSecondary == null && equippedWeaponPrimary != null)
            activeWeaponIndex = 0;

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    public void EquipItem(ItemData data)
    {
        if (data.category == ItemCategory.Weapon)
        {
            if (equippedWeaponPrimary == null)
                equippedWeaponPrimary = data;
            else if (equippedWeaponSecondary == null)
                equippedWeaponSecondary = data;
            else
            {
                if (activeWeaponIndex == 0)
                    equippedWeaponPrimary = data;
                else
                    equippedWeaponSecondary = data;
            }
        }
        else if (data.category == ItemCategory.Consumable)
        {
            if (equippedConsumableR == null)
                equippedConsumableR = data;
            else if (equippedConsumableExtra == null)
                equippedConsumableExtra = data;
            else
            {
                equippedConsumableR = equippedConsumableExtra;
                equippedConsumableExtra = data;
            }
        }

        OnEquipmentChanged?.Invoke();
    }

    public bool CycleWeapon()
    {
        if (equippedWeaponPrimary == null || equippedWeaponSecondary == null) return false;

        activeWeaponIndex = activeWeaponIndex == 0 ? 1 : 0;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public void UnequipWeapon(ItemData data)
    {
        if (equippedWeaponPrimary == data)
            equippedWeaponPrimary = null;
        else if (equippedWeaponSecondary == data)
            equippedWeaponSecondary = null;

        if (activeWeaponIndex == 0 && equippedWeaponPrimary == null && equippedWeaponSecondary != null)
            activeWeaponIndex = 1;
        else if (activeWeaponIndex == 1 && equippedWeaponSecondary == null && equippedWeaponPrimary != null)
            activeWeaponIndex = 0;

        OnEquipmentChanged?.Invoke();
    }

    public void UnequipConsumable(ItemData data)
    {
        if (equippedConsumableR == data) equippedConsumableR = null;
        else if (equippedConsumableExtra == data) equippedConsumableExtra = null;

        OnEquipmentChanged?.Invoke();
    }

    public void UnequipConsumableBySlot(bool isSlotR)
    {
        if (isSlotR) equippedConsumableR = null;
        else equippedConsumableExtra = null;

        OnEquipmentChanged?.Invoke();
    }

    public void NotifyEquipmentChanged()
    {
        OnEquipmentChanged?.Invoke();
    }
}
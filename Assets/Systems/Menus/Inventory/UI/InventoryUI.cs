using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button dropButton;

    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PlayerModel playerModel;

    public bool IsOpen => inventoryPanel.activeSelf;

    private ItemCategory _currentCategory = ItemCategory.Consumable;
    private InventoryItem _detailedItem;

    private void Start()
    {
        GameManager.Instance.Inventory.OnInventoryChanged += RefreshCurrentCategory;
        EquipmentSystem.Instance.OnEquipmentChanged += RefreshDetailPanel;
        inventoryPanel.SetActive(false);
        detailPanel.SetActive(true);
        equipButton.onClick.AddListener(OnEquipClicked);
        removeButton.onClick.AddListener(OnRemoveClicked);
        dropButton.onClick.AddListener(OnDropClicked);
        ClearDetails();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.OnInventoryChanged -= RefreshCurrentCategory;
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged -= RefreshDetailPanel;
    }

    private void RefreshDetailPanel()
    {
        if (_detailedItem != null)
            ShowDetails(_detailedItem);
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (inputHandler.InventoryTriggered)
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);

        if (isOpening)
        {
            RefreshCurrentCategory();
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemListParent.GetComponent<RectTransform>());
        }

        Cursor.visible = isOpening;
        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;

        if (playerModel != null)
            playerModel.enabled = !isOpening;
    }

    public void ShowConsumables() => SwitchCategory(ItemCategory.Consumable);
    public void ShowWeapons() => SwitchCategory(ItemCategory.Weapon);
    public void ShowWearables() => SwitchCategory(ItemCategory.Wearable);
    public void ShowOther() => SwitchCategory(ItemCategory.Other);

    private void SwitchCategory(ItemCategory category)
    {
        _currentCategory = category;
        RefreshCurrentCategory();
        ResetScroll();
    }

    private void ResetScroll()
    {
        if (scrollRect == null) return;
        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshCurrentCategory()
    {
        for (int i = itemListParent.childCount - 1; i >= 0; i--)
            Destroy(itemListParent.GetChild(i).gameObject);

        List<InventoryItem> list = GetListForCategory(_currentCategory);

        foreach (var item in list)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemListParent);
            slotObj.GetComponent<ItemSlotUI>().Setup(item, this);
        }

        if (list.Count > 0)
        {
            detailPanel.SetActive(true);
            InventoryItem toShow = _detailedItem != null
                ? list.Find(i => i.data.itemId == _detailedItem.data.itemId) ?? list[0]
                : list[0];
            ShowDetails(toShow);
        }
        else
        {
            ClearDetails();
            detailPanel.SetActive(false);
        }
    }

    private List<InventoryItem> GetListForCategory(ItemCategory category)
    {
        var inv = GameManager.Instance.Inventory;
        switch (category)
        {
            case ItemCategory.Consumable: return inv.consumables;
            case ItemCategory.Weapon: return inv.weapons;
            case ItemCategory.Wearable: return inv.wearables;
            default: return inv.others;
        }
    }

    public void ShowDetails(InventoryItem item)
    {
        _detailedItem = item;

        detailIcon.enabled = true;
        detailIcon.sprite = item.data.icon;
        detailName.text = item.data.itemName;
        detailDescription.text = item.data.description;

        bool isEquipped = IsEquipped(item.data);

        equipButton.gameObject.SetActive(item.data.isEquippable && !isEquipped);
        removeButton.gameObject.SetActive(isEquipped);
    }

    private bool IsEquipped(ItemData data)
    {
        return EquipmentSystem.Instance.equippedWeaponPrimary == data ||
               EquipmentSystem.Instance.equippedWeaponSecondary == data ||
               EquipmentSystem.Instance.equippedConsumableR == data ||
               EquipmentSystem.Instance.equippedConsumableExtra == data;
    }

    private void ClearDetails()
    {
        _detailedItem = null;

        detailIcon.enabled = false;
        detailIcon.sprite = null;
        detailName.text = "";
        detailDescription.text = "";

        equipButton.gameObject.SetActive(false);
        removeButton.gameObject.SetActive(false);
    }

    private void OnEquipClicked()
    {
        if (_detailedItem == null) return;

        EquipmentSystem.Instance.EquipItem(_detailedItem.data);
        ShowDetails(_detailedItem);
    }

    private void OnRemoveClicked()
    {
        if (_detailedItem == null) return;

        if (_detailedItem.data.category == ItemCategory.Weapon)
            EquipmentSystem.Instance.UnequipWeapon(_detailedItem.data);
        else
            EquipmentSystem.Instance.UnequipConsumable(_detailedItem.data);

        ShowDetails(_detailedItem);
    }

    private void OnDropClicked()
    {
        if (_detailedItem == null) return;
        ItemData data = _detailedItem.data;
        int quantity = _detailedItem.quantity;

        if (IsEquipped(data))
        {
            if (data.category == ItemCategory.Weapon)
                EquipmentSystem.Instance.UnequipWeapon(data);
            else
                EquipmentSystem.Instance.UnequipConsumable(data);
        }

        GameManager.Instance.Inventory.RemoveItem(data.itemId, quantity);
    }
}
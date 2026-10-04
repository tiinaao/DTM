using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemSlotPrefab;

    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button dropButton;

    [SerializeField] private PlayerInputHandler inputHandler;

    [SerializeField] private float popupHideDelay = 0.15f;
    [SerializeField] private Vector2 popupOffset = new Vector2(-12f, -12f);

    public bool IsOpen => inventoryPanel.activeSelf;

    private ItemCategory _currentCategory = ItemCategory.Consumable;
    private InventoryItem _detailedItem;

    private ItemSlotUI _hoveredSlot;
    private bool _overPopup;
    private float _hideTimer;
    private Canvas _canvas;

    private const BlockFlags PauseBlocks =
        BlockFlags.Movement | BlockFlags.Camera | BlockFlags.Actions |
        BlockFlags.Interaction |
        BlockFlags.FreeCursor | BlockFlags.FreezeTime;

    private void Start()
    {
        Canvas parentCanvas = inventoryPanel.GetComponentInParent<Canvas>();
        _canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;

        if (itemListParent.TryGetComponent(out GridLayoutGroup grid))
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = InventorySystem.Columns;
        }

        GameManager.Instance.Inventory.OnInventoryChanged += RefreshCurrentCategory;
        EquipmentSystem.Instance.OnEquipmentChanged += RefreshDetailPanel;
        inventoryPanel.SetActive(false);

        SetupPopupHover();
        detailPanel.SetActive(false);

        equipButton.onClick.AddListener(OnEquipClicked);
        removeButton.onClick.AddListener(OnRemoveClicked);
        dropButton.onClick.AddListener(OnDropClicked);
        ClearDetails();
    }

    private void OnDestroy()
    {
        GameplayBlocker.Release(this);

        if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
            GameManager.Instance.Inventory.OnInventoryChanged -= RefreshCurrentCategory;
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged -= RefreshDetailPanel;
    }

    private void SetupPopupHover()
    {
        var trigger = detailPanel.GetComponent<EventTrigger>();
        if (trigger == null) trigger = detailPanel.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => _overPopup = true);
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => _overPopup = false);
        trigger.triggers.Add(exit);
    }

    private void RefreshDetailPanel()
    {
        if (_detailedItem != null && detailPanel.activeSelf)
            ShowDetails(_detailedItem);
    }

    private void Update()
    {
        UpdatePopupHide();

        if (!IsOpen && GameplayBlocker.IsBlocked(BlockFlags.Inventory)) return;

        if (inputHandler.InventoryTriggered)
            ToggleInventory();
    }

    private void UpdatePopupHide()
    {
        if (!detailPanel.activeSelf) return;

        if (_hoveredSlot != null || _overPopup)
        {
            _hideTimer = popupHideDelay;
            return;
        }

        _hideTimer -= Time.unscaledDeltaTime;
        if (_hideTimer <= 0f)
            HidePopup();
    }

    public void ToggleInventory()
    {
        bool isOpening = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isOpening);

        HidePopup();

        if (isOpening)
        {
            GameplayBlocker.Block(this, PauseBlocks);
            RefreshCurrentCategory();
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemListParent.GetComponent<RectTransform>());
        }
        else
        {
            GameplayBlocker.Release(this);
        }
    }

    public void ShowConsumables() => SwitchCategory(ItemCategory.Consumable);
    public void ShowWeapons() => SwitchCategory(ItemCategory.Weapon);
    public void ShowWearables() => SwitchCategory(ItemCategory.Wearable);
    public void ShowOther() => SwitchCategory(ItemCategory.Other);

    private void SwitchCategory(ItemCategory category)
    {
        _currentCategory = category;
        HidePopup();
        RefreshCurrentCategory();
    }

    private void RefreshCurrentCategory()
    {
        for (int i = itemListParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = itemListParent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        List<InventoryItem> list = GetListForCategory(_currentCategory);
        int totalSlots = Mathf.Max(list.Count, InventorySystem.MaxSlotsPerCategory);

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemListParent);
            ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();

            if (i < list.Count)
                slot.Setup(list[i], this);
            else
                slot.SetupEmpty();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(itemListParent.GetComponent<RectTransform>());

        if (_detailedItem != null && detailPanel.activeSelf)
        {
            InventoryItem match = list.Find(i => i.data.itemId == _detailedItem.data.itemId);
            if (match != null)
                ShowDetails(match);
            else
                HidePopup();
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

    public void OnSlotHover(ItemSlotUI slot, InventoryItem item)
    {
        _hoveredSlot = slot;
        _hideTimer = popupHideDelay;

        ShowDetails(item);

        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
        PositionPopup(slot);
    }

    public void OnSlotHoverEnd(ItemSlotUI slot)
    {
        if (_hoveredSlot == slot)
            _hoveredSlot = null;
    }

    private void HidePopup()
    {
        _hoveredSlot = null;
        _overPopup = false;
        detailPanel.SetActive(false);
        ClearDetails();
    }

    private void PositionPopup(ItemSlotUI slot)
    {
        var popupRect = (RectTransform)detailPanel.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);

        var slotCorners = new Vector3[4];
        slot.Rect.GetWorldCorners(slotCorners);

        Bounds visible = GetVisibleBounds(popupRect);
        Vector2 offset = popupOffset * popupRect.lossyScale.x;

        float minX = float.NegativeInfinity;
        float maxX = float.PositiveInfinity;
        float minY = float.NegativeInfinity;
        float maxY = float.PositiveInfinity;

        if (_canvas != null)
        {
            var canvasCorners = new Vector3[4];
            ((RectTransform)_canvas.transform).GetWorldCorners(canvasCorners);
            minX = canvasCorners[0].x;
            maxX = canvasCorners[2].x;
            minY = canvasCorners[0].y;
            maxY = canvasCorners[2].y;
        }

        bool placeRight = slotCorners[2].x + offset.x + visible.size.x <= maxX;
        bool placeAbove = slotCorners[2].y + offset.y + visible.size.y <= maxY;

        float targetX = placeRight ? slotCorners[2].x + offset.x : slotCorners[0].x - offset.x;
        float targetY = placeAbove ? slotCorners[2].y + offset.y : slotCorners[0].y - offset.y;

        float currentX = placeRight ? visible.min.x : visible.max.x;
        float currentY = placeAbove ? visible.min.y : visible.max.y;

        Vector3 shift = new Vector3(targetX - currentX, targetY - currentY, 0f);

        float overRight = visible.max.x + shift.x - maxX;
        if (overRight > 0f) shift.x -= overRight;
        float underLeft = minX - (visible.min.x + shift.x);
        if (underLeft > 0f) shift.x += underLeft;

        float overTop = visible.max.y + shift.y - maxY;
        if (overTop > 0f) shift.y -= overTop;
        float underBottom = minY - (visible.min.y + shift.y);
        if (underBottom > 0f) shift.y += underBottom;

        popupRect.position += shift;
    }

    private Bounds GetVisibleBounds(RectTransform root)
    {
        var corners = new Vector3[4];
        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);

        foreach (var graphic in root.GetComponentsInChildren<Graphic>())
        {
            if (!graphic.enabled || graphic.color.a <= 0.01f) continue;

            graphic.rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(corners[i], Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(corners[i]);
                }
            }
        }

        if (!hasBounds)
        {
            root.GetWorldCorners(corners);
            bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < 4; i++)
                bounds.Encapsulate(corners[i]);
        }

        return bounds;
    }

    public void ShowDetails(InventoryItem item)
    {
        _detailedItem = item;

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

        HidePopup();
        GameManager.Instance.Inventory.RemoveItem(data.itemId, quantity);
    }
}
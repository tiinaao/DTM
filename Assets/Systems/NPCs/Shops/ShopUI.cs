using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    private enum Side { Buy, Sell }

    private struct CartKey : IEquatable<CartKey>
    {
        public Side side;
        public string itemId;

        public CartKey(Side side, string itemId)
        {
            this.side = side;
            this.itemId = itemId;
        }

        public bool Equals(CartKey other) => side == other.side && itemId == other.itemId;
        public override bool Equals(object obj) => obj is CartKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(side, itemId);
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Transform buyGrid;
    [SerializeField] private Transform sellGrid;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI cartText;

    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;

    [SerializeField] private float popupHideDelay = 0.15f;
    [SerializeField] private Vector2 popupOffset = new Vector2(-12f, -12f);

    private NPCShopHandler shop;
    private Action onClose;

    private Dictionary<CartKey, int> cart = new Dictionary<CartKey, int>();

    private ItemSlotUI hoveredSlot;
    private bool overPopup;
    private float hideTimer;
    private Canvas canvasRoot;
    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        buyButton.onClick.AddListener(ConfirmCart);

        panel.SetActive(false);
        detailPanel.SetActive(false);

        Canvas parentCanvas = detailPanel.GetComponentInParent<Canvas>();
        canvasRoot = parentCanvas != null ? parentCanvas.rootCanvas : null;

        SetupPopupHover();
    }

    private void SetupPopupHover()
    {
        var trigger = detailPanel.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = detailPanel.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => overPopup = true);
        trigger.triggers.Add(enter);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => overPopup = false);
        trigger.triggers.Add(exit);
    }

    private void Update()
    {
        UpdatePopupHide();
    }

    private void UpdatePopupHide()
    {
        if (!detailPanel.activeSelf) return;

        if (hoveredSlot != null || overPopup)
        {
            hideTimer = popupHideDelay;
            return;
        }

        hideTimer -= Time.unscaledDeltaTime;
        if (hideTimer <= 0f)
            HidePopup();
    }

    public void Open(NPCShopHandler handler, Action closed)
    {
        shop = handler;
        onClose = closed;
        panel.SetActive(true);

        cart.Clear();
        RefreshGrids();
    }

    public void Close()
    {
        if (!panel.activeSelf) return;

        panel.SetActive(false);
        shop = null;
        cart.Clear();
        HidePopup();

        Action callback = onClose;
        onClose = null;
        callback?.Invoke();
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void RefreshGrids()
    {
        if (shop == null) return;

        ClearChildren(buyGrid);
        ClearChildren(sellGrid);

        foreach (ShopStockEntry entry in shop.Stock)
        {
            if (entry.item == null) continue;

            string itemId = entry.item.itemId;
            int price = shop.GetBuyPrice(entry);

            ItemSlotUI slot = Instantiate(itemSlotPrefab, buyGrid).GetComponent<ItemSlotUI>();
            slot.Setup(
                entry.item,
                entry.stock < 0 ? 0 : entry.stock,
                s => OnSlotHover(s, Side.Buy, itemId),
                s => OnSlotHoverEnd(s),
                s => AddToCart(Side.Buy, itemId),
                s => RemoveFromCart(Side.Buy, itemId),
                price,
                entry.stock);
        }

        foreach (InventoryItem item in GameManager.Instance.Inventory.GetAllItems())
        {
            string itemId = item.data.itemId;
            int price = shop.GetSellPrice(item.data);

            ItemSlotUI slot = Instantiate(itemSlotPrefab, sellGrid).GetComponent<ItemSlotUI>();
            slot.Setup(
                item.data,
                item.quantity,
                s => OnSlotHover(s, Side.Sell, itemId),
                s => OnSlotHoverEnd(s),
                s => AddToCart(Side.Sell, itemId),
                s => RemoveFromCart(Side.Sell, itemId),
                price);
        }

        UpdateCurrencyText();
        UpdateCartText();
    }

    private void UpdateCurrencyText()
    {
        if (currencyText != null)
            currencyText.text = GameManager.Instance.Inventory.Currency.ToString();
    }

    private int GetCartLimit(Side side, string itemId)
    {
        if (side == Side.Buy)
        {
            ShopStockEntry entry = shop.FindEntry(itemId);
            if (entry == null) return 0;
            return entry.stock < 0 ? int.MaxValue : entry.stock;
        }

        InventoryItem item = GameManager.Instance.Inventory.FindItem(itemId);
        return item?.quantity ?? 0;
    }

    private void AddToCart(Side side, string itemId)
    {
        if (shop == null) return;

        CartKey key = new CartKey(side, itemId);
        int limit = GetCartLimit(side, itemId);
        int current = cart.TryGetValue(key, out int qty) ? qty : 0;

        if (current >= limit) return;

        cart[key] = current + 1;
        UpdateCartText();
    }

    private void RemoveFromCart(Side side, string itemId)
    {
        CartKey key = new CartKey(side, itemId);
        if (!cart.TryGetValue(key, out int qty)) return;

        qty -= 1;
        if (qty <= 0)
            cart.Remove(key);
        else
            cart[key] = qty;

        UpdateCartText();
    }

    private int GetUnitPrice(Side side, string itemId)
    {
        if (side == Side.Buy)
        {
            ShopStockEntry entry = shop.FindEntry(itemId);
            return entry == null ? 0 : shop.GetBuyPrice(entry);
        }

        InventoryItem item = GameManager.Instance.Inventory.FindItem(itemId);
        return item == null ? 0 : shop.GetSellPrice(item.data);
    }

    private void UpdateCartText()
    {
        if (cartText == null) return;

        if (cart.Count == 0)
        {
            cartText.text = "";
            buyButton.interactable = false;
            return;
        }

        StringBuilder sb = new StringBuilder();
        int total = 0;

        foreach (var kvp in cart)
        {
            ItemData data = kvp.Key.side == Side.Buy
                ? shop.FindEntry(kvp.Key.itemId)?.item
                : GameManager.Instance.Inventory.FindItem(kvp.Key.itemId)?.data;

            if (data == null) continue;

            int unitPrice = GetUnitPrice(kvp.Key.side, kvp.Key.itemId);
            int lineTotal = unitPrice * kvp.Value;
            total += kvp.Key.side == Side.Buy ? lineTotal : -lineTotal;

            string sign = kvp.Key.side == Side.Buy ? "Buy" : "Sell";
            sb.AppendLine($"{sign} {data.itemName} x{kvp.Value} - {lineTotal}");
        }

        if (total >= 0)
            sb.AppendLine($"Total cost: {total}");
        else
            sb.AppendLine($"You receive: {-total}");

        cartText.text = sb.ToString();

        int costToPlayer = Mathf.Max(0, total);
        bool canAfford = costToPlayer <= GameManager.Instance.Inventory.Currency;
        buyButton.interactable = canAfford;
    }

    private void ConfirmCart()
    {
        if (shop == null || cart.Count == 0) return;

        foreach (var kvp in cart)
        {
            if (kvp.Key.side == Side.Buy)
                shop.TryBuy(kvp.Key.itemId, kvp.Value);
            else
                shop.TrySell(kvp.Key.itemId, kvp.Value);
        }

        cart.Clear();
        RefreshGrids();
    }

    private void OnSlotHover(ItemSlotUI slot, Side side, string itemId)
    {
        hoveredSlot = slot;
        hideTimer = popupHideDelay;

        ItemData data = side == Side.Buy
            ? shop.FindEntry(itemId)?.item
            : GameManager.Instance.Inventory.FindItem(itemId)?.data;

        if (data == null) return;

        detailName.text = data.itemName;
        detailDescription.text = data.description;

        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
        PositionPopup(slot);
    }

    private void OnSlotHoverEnd(ItemSlotUI slot)
    {
        if (hoveredSlot == slot)
            hoveredSlot = null;
    }

    private void HidePopup()
    {
        hoveredSlot = null;
        overPopup = false;
        detailPanel.SetActive(false);
    }

    private void PositionPopup(ItemSlotUI slot)
    {
        var popupRect = (RectTransform)detailPanel.transform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);

        Canvas canvas = popupRect.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Canvas root = canvas.rootCanvas;
        Camera cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

        var slotCorners = new Vector3[4];
        slot.Rect.GetWorldCorners(slotCorners);

        Vector2 slotTopRight = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[2]);
        Vector2 slotBottomLeft = RectTransformUtility.WorldToScreenPoint(cam, slotCorners[0]);

        Bounds visible = GetVisibleBounds(popupRect);
        var popupCorners = new Vector3[4];
        popupRect.GetWorldCorners(popupCorners);
        Vector2 popupMin = RectTransformUtility.WorldToScreenPoint(cam, visible.min);
        Vector2 popupMax = RectTransformUtility.WorldToScreenPoint(cam, visible.max);
        Vector2 popupSize = popupMax - popupMin;

        Vector2 offset = new Vector2(Mathf.Abs(popupOffset.x), Mathf.Abs(popupOffset.y)) * popupRect.lossyScale.x;

        float screenMinX = 0f, screenMaxX = Screen.width;
        float screenMinY = 0f, screenMaxY = Screen.height;

        bool placeRight = slotTopRight.x + offset.x + popupSize.x <= screenMaxX;
        bool placeAbove = slotTopRight.y + offset.y + popupSize.y <= screenMaxY;

        float targetX = placeRight ? slotTopRight.x + offset.x : slotBottomLeft.x - offset.x;
        float targetY = placeAbove ? slotTopRight.y + offset.y : slotBottomLeft.y - offset.y;

        float currentX = placeRight ? popupMin.x : popupMax.x;
        float currentY = placeAbove ? popupMin.y : popupMax.y;

        Vector2 shift = new Vector2(targetX - currentX, targetY - currentY);

        float overRight = popupMax.x + shift.x - screenMaxX;
        if (overRight > 0f) shift.x -= overRight;
        float underLeft = screenMinX - (popupMin.x + shift.x);
        if (underLeft > 0f) shift.x += underLeft;

        float overTop = popupMax.y + shift.y - screenMaxY;
        if (overTop > 0f) shift.y -= overTop;
        float underBottom = screenMinY - (popupMin.y + shift.y);
        if (underBottom > 0f) shift.y += underBottom;

        popupRect.position += (Vector3)shift;
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
}
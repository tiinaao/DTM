using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform listParent;
    [SerializeField] private Transform sellListParent;
    [SerializeField] private Button rowPrefab;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Button closeButton;

    private NPCShopHandler shop;
    private Action onClose;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    public void Open(NPCShopHandler handler, Action closed)
    {
        shop = handler;
        onClose = closed;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!panel.activeSelf) return;

        panel.SetActive(false);
        shop = null;

        Action callback = onClose;
        onClose = null;
        if (callback != null) callback();
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

    private void Refresh()
    {
        if (shop == null) return;

        ClearChildren(listParent);
        ClearChildren(sellListParent);

        foreach (ShopStockEntry entry in shop.Stock)
        {
            if (entry.item == null) continue;

            Button row = Instantiate(rowPrefab, listParent);

            string stockText = entry.stock < 0 ? "" : entry.stock == 0 ? " (sold out)" : " x" + entry.stock;
            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = entry.item.itemName + stockText + "  -  " + entry.price;

            string id = entry.item.itemId;
            row.onClick.AddListener(() =>
            {
                shop.TryBuy(id);
                Refresh();
            });
        }

        foreach (InventoryItem item in GameManager.Instance.Inventory.GetAllItems())
        {
            Button row = Instantiate(rowPrefab, sellListParent);

            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = item.data.itemName + " x" + item.quantity + "  -  sell " + shop.GetSellPrice(item.data);

            string sellId = item.data.itemId;
            row.onClick.AddListener(() =>
            {
                shop.TrySell(sellId);
                Refresh();
            });
        }

        currencyText.text = GameManager.Instance.Inventory.Currency.ToString();
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image outline;
    [SerializeField] private TextMeshProUGUI priceText;

    [SerializeField] private float fadeSpeed = 12f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 10f;

    public ItemData Data { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    private Action<ItemSlotUI> onHoverEnter;
    private Action<ItemSlotUI> onHoverExit;
    private Action<ItemSlotUI> onLeftClick;
    private Action<ItemSlotUI> onRightClick;

    private float targetAlpha;
    private Vector3 baseScale;
    private Vector3 targetScale;

    private void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;

        if (outline != null)
        {
            Color c = outline.color;
            c.a = 0f;
            outline.color = c;
        }

        if (priceText != null)
            priceText.gameObject.SetActive(false);
    }

    public void Setup(
        ItemData data,
        int quantity,
        Action<ItemSlotUI> onHoverEnter,
        Action<ItemSlotUI> onHoverExit,
        Action<ItemSlotUI> onLeftClick,
        Action<ItemSlotUI> onRightClick,
        int? price = null,
        int stock = -1)
    {
        Data = data;
        this.onHoverEnter = onHoverEnter;
        this.onHoverExit = onHoverExit;
        this.onLeftClick = onLeftClick;
        this.onRightClick = onRightClick;

        icon.enabled = true;
        icon.sprite = data.icon;

        bool soldOut = stock == 0;

        quantityText.text = data.isStackable && quantity > 0
            ? quantity.ToString("D2")
            : "";

        if (priceText != null)
        {
            priceText.gameObject.SetActive(price.HasValue);
            priceText.text = !price.HasValue ? "" : soldOut ? "Sold out" : price.Value.ToString();
        }
    }

    public void SetupEmpty()
    {
        Data = null;
        onHoverEnter = null;
        onHoverExit = null;
        onLeftClick = null;
        onRightClick = null;

        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";

        if (priceText != null)
        {
            priceText.gameObject.SetActive(false);
            priceText.text = "";
        }

        if (TryGetComponent(out Button button))
            button.interactable = false;
    }

    private void Update()
    {
        if (outline != null)
        {
            Color c = outline.color;
            float newAlpha = Mathf.Lerp(c.a, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            if (!Mathf.Approximately(c.a, newAlpha))
            {
                c.a = newAlpha;
                outline.color = c;
            }
        }

        Vector3 newScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
        if (newScale != transform.localScale)
            transform.localScale = newScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Data == null) return;

        UISoundManager.instance.Play("hover");
        targetAlpha = 1f;
        targetScale = baseScale * hoverScale;

        onHoverEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = 0f;
        targetScale = baseScale;

        if (Data != null)
            onHoverExit?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Data == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(this);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image outline;

    [SerializeField] private float fadeSpeed = 12f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 10f;

    private InventoryItem _item;
    private InventoryUI _ui;

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
    }

    public void Setup(InventoryItem item, InventoryUI ui)
    {
        _item = item;
        _ui = ui;

        icon.sprite = item.data.icon;

        quantityText.text = item.data.isStackable && item.quantity > 0
            ? item.quantity.ToString("D2")
            : "";
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
        UISoundManager.instance.Play("hover");
        targetAlpha = 1f;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = 0f;
        targetScale = baseScale;
    }

    public void OnClick()
    {
        _ui.ShowDetails(_item);
    }
}
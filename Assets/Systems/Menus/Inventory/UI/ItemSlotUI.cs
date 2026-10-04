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

    public RectTransform Rect => (RectTransform)transform;

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

        icon.enabled = true;
        icon.sprite = item.data.icon;

        quantityText.text = item.data.isStackable && item.quantity > 0
            ? item.quantity.ToString("D2")
            : "";

        if (TryGetComponent(out Button button))
            button.interactable = true;
    }

    public void SetupEmpty()
    {
        _item = null;
        _ui = null;

        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";

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
        if (_item == null) return;

        UISoundManager.instance.Play("hover");
        targetAlpha = 1f;
        targetScale = baseScale * hoverScale;

        _ui.OnSlotHover(this, _item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = 0f;
        targetScale = baseScale;

        if (_item != null && _ui != null)
            _ui.OnSlotHoverEnd(this);
    }

    public void OnClick()
    {
    }
}
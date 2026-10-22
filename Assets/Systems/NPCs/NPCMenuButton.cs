using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NPCMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private const float IntroDuration = 0.4f;

    public int Index;
    public Action<NPCMenuButton> Hovered;
    public Action<NPCMenuButton> Clicked;

    public RectTransform Rect;
    public RectTransform LabelRect;
    public Image Background;
    public Image AccentBar;
    public TextMeshProUGUI Label;
    public TextMeshProUGUI Hint;
    public CanvasGroup Group;

    public Vector2 BasePosition;
    public Color NormalColor;
    public Color HighlightColor;
    public Color AccentColor;

    private bool selected;
    private float highlight;
    private float introTime;
    private float introDelay;

    public void SetSelected(bool value)
    {
        selected = value;
    }

    public void PlayIntro(float delay)
    {
        introDelay = delay;
        introTime = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        introTime += dt;
        float t = Mathf.Clamp01((introTime - introDelay) / IntroDuration);
        float e = 1f - Mathf.Pow(1f - t, 3f);

        Group.alpha = e;
        bool ready = t > 0.5f;
        Group.interactable = ready;
        Group.blocksRaycasts = ready;

        highlight = Mathf.MoveTowards(highlight, selected ? 1f : 0f, dt * 9f);
        float h = highlight * highlight * (3f - 2f * highlight);

        Rect.anchoredPosition = BasePosition + new Vector2((1f - e) * 40f, 0f);
        Background.color = Color.Lerp(NormalColor, HighlightColor, h);

        AccentBar.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, Mathf.Lerp(0.35f, 1f, h));
        AccentBar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(4f, 8f, h), 0f);

        LabelRect.offsetMin = new Vector2(Mathf.Lerp(16f, 28f, h), LabelRect.offsetMin.y);
        Label.color = Color.Lerp(Color.white, AccentColor, h);
        Hint.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.35f, 0.8f, h));

        transform.localScale = Vector3.one * (1f + 0.025f * h);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Hovered != null) Hovered(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Clicked != null) Clicked(this);
    }
}

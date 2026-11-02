using System;
using System.Collections.Generic;
using UnityEngine;

public enum GiftPreference
{
    Hated,
    Neutral,
    Loved
}

[CreateAssetMenu(fileName = "NewDialogueCharacter", menuName = "Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public List<EmotionPortrait> emotions = new List<EmotionPortrait>();

    public List<GiftPreferenceEntry> giftPreferences = new List<GiftPreferenceEntry>();

    public Sprite GetPortrait(string emotionName)
    {
        if (string.IsNullOrEmpty(emotionName)) return null;

        foreach (var entry in emotions)
        {
            if (entry.emotionName == emotionName)
                return entry.sprite;
        }

        return null;
    }

    public GiftPreference GetGiftPreference(ItemData item)
    {
        return item == null ? GiftPreference.Neutral : GetGiftPreference(item.itemId);
    }

    public GiftPreference GetGiftPreference(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return GiftPreference.Neutral;

        foreach (var entry in giftPreferences)
        {
            if (entry.item != null && entry.item.itemId == itemId)
                return entry.preference;
        }

        return GiftPreference.Neutral;
    }
}

[Serializable]
public class EmotionPortrait
{
    public string emotionName;
    public Sprite sprite;
}

[Serializable]
public class GiftPreferenceEntry
{
    public ItemData item;
    public GiftPreference preference = GiftPreference.Neutral;
}
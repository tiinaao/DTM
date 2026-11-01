using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueCharacter", menuName = "Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public List<EmotionPortrait> emotions = new List<EmotionPortrait>();

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
}

[Serializable]
public class EmotionPortrait
{
    public string emotionName;
    public Sprite sprite;
}

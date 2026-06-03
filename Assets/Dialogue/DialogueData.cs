[System.Serializable]
public class DialogueLine
{
    public string text;
    public string audioFile;
}

[System.Serializable]
public class DialogueData
{
    public string npcName;
    public DialogueLine[] lines;
}
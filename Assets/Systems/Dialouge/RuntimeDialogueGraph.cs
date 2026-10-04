using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueNodeType
{
    Dialogue,
    Choice,
    Item,
    Condition
}

public enum ItemAction
{
    Add,
    Remove
}

public class RuntimeDialogueGraph : ScriptableObject
{
    public string EntryNodeID;
    public List<RuntimeDialogueNode> AllNodes = new List<RuntimeDialogueNode>();
}

[Serializable]
public class RuntimeDialogueNode
{
    public string NodeID;
    public DialogueNodeType NodeType;
    public string SpeakerName;
    public string DialogueText;
    public List<ChoiceData> Choices = new List<ChoiceData>();
    public string NextNodeID;

    public string ItemName;
    public int Amount = 1;
    public ItemAction ItemOperation;

    public string TrueNodeID;
    public string FalseNodeID;
}

[Serializable]
public class ChoiceData
{
    public string ChoiceText;
    public string DestinationNodeID;
}
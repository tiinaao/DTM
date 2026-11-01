using UnityEngine;
using Unity.GraphToolkit.Editor;
using System;

[Serializable]
public class StartNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddOutputPort("out").Build();
    }
}

[Serializable]
public class EndNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
    }
}

internal static class DialoguePresentationOptions
{
    public const string CharacterOption = "character";
    public const string EmotionOption = "emotion";
    public const string VoiceLineOption = "voiceLine";

    public static void Define(Node.IOptionDefinitionContext context)
    {
        context.AddOption<DialogueCharacter>(CharacterOption).WithDefaultValue(null);
        context.AddOption<string>(EmotionOption).WithDefaultValue("");
        context.AddOption<AudioClip>(VoiceLineOption).WithDefaultValue(null);
    }
}

[Serializable]
public class DialogueNode : Node
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
        context.AddOutputPort("out").Build();

        context.AddInputPort<string>("Speaker").Build();
        context.AddInputPort<string>("Dialogue").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        DialoguePresentationOptions.Define(context);
    }
}

[Serializable]
public class ChoiceNode : Node
{
    const string optionID = "portCount";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();

        context.AddInputPort<string>("Speaker").Build();
        context.AddInputPort<string>("Dialogue").Build();

        var option = GetNodeOptionByName(optionID);
        option.TryGetValue(out int portCount);

        for (int i = 0; i < portCount; i++)
        {
            context.AddInputPort<string>($"Choice Text {i}").Build();
            context.AddInputPort<int>($"Choice Rep {i}").WithDefaultValue(0).Build();
            context.AddOutputPort($"Choice {i}").Build();
        }
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        DialoguePresentationOptions.Define(context);
        context.AddOption<int>(optionID).WithDefaultValue(2).Delayed();
    }
}

[Serializable]
public class ItemNode : Node
{
    public const string ActionOption = "action";
    public const string ItemIDOption = "itemID";
    public const string AmountOption = "amount";

    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort("in").Build();
        context.AddOutputPort("out").Build();
    }

    protected override void OnDefineOptions(IOptionDefinitionContext context)
    {
        context.AddOption<ItemAction>(ActionOption).WithDefaultValue(ItemAction.Add);
        context.AddOption<string>(ItemIDOption).WithDefaultValue("");
        context.AddOption<int>(AmountOption).WithDefaultValue(1);
    }
}
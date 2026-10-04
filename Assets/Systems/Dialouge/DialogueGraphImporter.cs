using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.AssetImporters;
using Unity.GraphToolkit.Editor;

[ScriptedImporter(1, DialogueGraph.AssetExtension)]
public class DialogueGraphImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        DialogueGraph editorGraph = GraphDatabase.LoadGraphForImporter<DialogueGraph>(ctx.assetPath);
        RuntimeDialogueGraph runtimeGraph = ScriptableObject.CreateInstance<RuntimeDialogueGraph>();
        var nodeIDMap = new Dictionary<INode, string>();

        foreach (var node in editorGraph.GetNodes())
            nodeIDMap[node] = Guid.NewGuid().ToString();

        var startNode = editorGraph.GetNodes().OfType<StartNode>().FirstOrDefault();
        if (startNode != null)
            runtimeGraph.EntryNodeID = GetNextID(startNode.GetOutputPortByName("out"), nodeIDMap);

        foreach (var iNode in editorGraph.GetNodes())
        {
            if (iNode is StartNode || iNode is EndNode) continue;

            var runtimeNode = new RuntimeDialogueNode { NodeID = nodeIDMap[iNode] };

            if (iNode is DialogueNode dialogueNode)
                ProcessDialogueNode(dialogueNode, runtimeNode, nodeIDMap);
            else if (iNode is ChoiceNode choiceNode)
                ProcessChoiceNode(choiceNode, runtimeNode, nodeIDMap);
            else if (iNode is ItemNode itemNode)
                ProcessItemNode(itemNode, runtimeNode, nodeIDMap);
            else
                continue;

            runtimeGraph.AllNodes.Add(runtimeNode);
        }

        ctx.AddObjectToAsset("RuntimeData", runtimeGraph);
        ctx.SetMainObject(runtimeGraph);
    }

    private void ProcessDialogueNode(DialogueNode node, RuntimeDialogueNode runtimeNode,
                                     Dictionary<INode, string> nodeIDMap)
    {
        runtimeNode.NodeType = DialogueNodeType.Dialogue;
        runtimeNode.SpeakerName = GetPortValue<string>(node.GetInputPortByName("Speaker"));
        runtimeNode.DialogueText = GetPortValue<string>(node.GetInputPortByName("Dialogue"));
        runtimeNode.NextNodeID = GetNextID(node.GetOutputPortByName("out"), nodeIDMap);
    }

    private void ProcessChoiceNode(ChoiceNode node, RuntimeDialogueNode runtimeNode, Dictionary<INode, string> nodeIDMap)
    {
        runtimeNode.NodeType = DialogueNodeType.Choice;
        runtimeNode.SpeakerName = GetPortValue<string>(node.GetInputPortByName("Speaker"));
        runtimeNode.DialogueText = GetPortValue<string>(node.GetInputPortByName("Dialogue"));

        var choiceOutputPorts = node.GetOutputPorts().Where(p => p.Name.StartsWith("Choice "));

        foreach (var outputPort in choiceOutputPorts)
        {
            var index = outputPort.Name.Substring("Choice ".Length);
            var textPort = node.GetInputPortByName($"Choice Text {index}");

            var choiceData = new ChoiceData
            {
                ChoiceText = GetPortValue<string>(textPort),
                DestinationNodeID = GetNextID(outputPort, nodeIDMap)
            };

            runtimeNode.Choices.Add(choiceData);
        }
    }

    private void ProcessItemNode(ItemNode node, RuntimeDialogueNode runtimeNode, Dictionary<INode, string> nodeIDMap)
    {
        runtimeNode.NodeType = DialogueNodeType.Item;
        runtimeNode.ItemOperation = GetOptionValue<ItemAction>(node, ItemNode.ActionOption);
        runtimeNode.ItemName = GetOptionValue<string>(node, ItemNode.ItemIDOption);
        runtimeNode.Amount = Mathf.Max(1, GetOptionValue<int>(node, ItemNode.AmountOption));
        runtimeNode.NextNodeID = GetNextID(node.GetOutputPortByName("out"), nodeIDMap);
    }

    private string GetNextID(IPort outputPort, Dictionary<INode, string> nodeIDMap)
    {
        var connected = outputPort?.FirstConnectedPort;
        if (connected == null) return null;

        var next = connected.GetNode();
        if (next is EndNode) return null;

        return nodeIDMap[next];
    }

    private T GetOptionValue<T>(INode node, string optionName)
    {
        var option = node.GetNodeOptionByName(optionName);
        if (option == null) return default;

        option.TryGetValue(out T value);
        return value;
    }

    private T GetPortValue<T>(IPort port)
    {
        if (port == null) return default;

        if (port.IsConnected)
        {
            if (port.FirstConnectedPort.GetNode() is IVariableNode variableNode)
            {
                variableNode.Variable.TryGetDefaultValue(out T value);
                return value;
            }
        }

        port.TryGetValue(out T fallbackValue);
        return fallbackValue;
    }
}
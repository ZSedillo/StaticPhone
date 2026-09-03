using System;
using System.Collections.Generic;
using UnityEngine;

// --- Node-Based JSON Dialogue System ---

[Serializable]
public class DialogueChoiceData
{
    public string choiceText;
    public string nextId; 
}

[Serializable]
public class DialogueNodeData
{
    public string id;
    public string partnerMessage;
    public string triggerEvent;
    public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
}

[Serializable]
public class CharacterDialogueTree
{
    public string girlName;
    public int age;
    public string personality;
    [TextArea(2, 4)] public string bio;
    public int avatarIndex;
    public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
}

// --- Legacy Dialogue Structures (Required by CharacterProfileData) ---

[Serializable]
public class PlayerChoice
{
    [TextArea(1, 2)] public string choiceText;
    [SerializeReference] public DialogueStep nextStep;
}

[Serializable]
public class DialogueStep
{
    [TextArea(2, 4)] public string partnerMessage;
    public List<PlayerChoice> choices = new List<PlayerChoice>();
}
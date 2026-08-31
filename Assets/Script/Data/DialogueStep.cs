using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerChoice
{
    [TextArea(1, 2)] public string choiceText;
    
    // [SerializeReference] allows Unity to serialize polymorphic/nested tree structures without infinite loops
    [SerializeReference] public DialogueStep nextStep;
}

[System.Serializable]
public class DialogueStep
{
    [TextArea(2, 4)] public string partnerMessage;
    public List<PlayerChoice> choices = new List<PlayerChoice>();
}
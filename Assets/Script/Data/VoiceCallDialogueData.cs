using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VoiceCallChoice
{
    public string choiceText;
    public string nextNodeId; // Empty or "END_CALL" to hang up
}

[Serializable]
public class VoiceCallNode
{
    public string nodeId;
    [TextArea(2, 4)]
    public string partnerDialogue;
    public List<VoiceCallChoice> choices = new List<VoiceCallChoice>();
}

[Serializable]
public class VoiceCallConversation
{
    public string callerName;
    public string startNodeId;
    public List<string> timeoutPrompts = new List<string>
    {
        "Hello? Are you there?",
        "Am I speaking to someone...?",
        "Did you drop your phone or something?",
        "Hello? Look, if you're not going to talk, I'm hanging up."
    };
    public List<VoiceCallNode> nodes = new List<VoiceCallNode>();
}
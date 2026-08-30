using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserProfileData
{
    public string playerName = "Player";
    public int playerAge = 20;
    public string playerBio = "Just here to meet new people.";
    public string playerPersonality = "Ambivert";
    public int avatarIndex = 0;
}

[Serializable]
public class ChatMessageData
{
    public bool isSenderPlayer;
    public string messageText;
    public string timestamp;
}

[Serializable]
public class ContactChatData
{
    public string contactId;
    public string contactName;
    public string contactBio;
    public string contactPersonality;
    public int avatarIndex;
    public string lastMessageTime;
    public List<ChatMessageData> conversationHistory = new List<ChatMessageData>();
}
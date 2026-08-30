using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    public UserProfileData currentUser = new UserProfileData();

    [Header("Chats & Matches Data")]
    public List<ContactChatData> activeChats = new List<ContactChatData>();

    // Events (Header attribute removed)
    public event Action OnUserDataUpdated;
    public event Action OnChatsUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerBasicInfo(string newName, int newAge)
    {
        currentUser.playerName = string.IsNullOrEmpty(newName) ? "Player" : newName;
        currentUser.playerAge = newAge > 0 ? newAge : 18;
        OnUserDataUpdated?.Invoke();
    }

    public void AddOrUpdateChat(ContactChatData chat)
    {
        int existingIndex = activeChats.FindIndex(c => c.contactName == chat.contactName);
        if (existingIndex >= 0)
        {
            activeChats[existingIndex] = chat;
        }
        else
        {
            activeChats.Add(chat);
        }
        OnChatsUpdated?.Invoke();
    }

    public ContactChatData GetChatByContactName(string contactName)
    {
        return activeChats.Find(c => c.contactName == contactName);
    }


    public void AddMatch(string name, string bio, string personality, int avatarIdx, string initialMessage = null)
    {
        // Avoid duplicate matches
        if (activeChats.Exists(c => c.contactName == name)) return;

        ContactChatData newMatch = new ContactChatData
        {
            contactId = System.Guid.NewGuid().ToString(),
            contactName = name,
            contactBio = bio,
            contactPersonality = personality,
            avatarIndex = avatarIdx,
            lastMessageTime = System.DateTime.Now.ToString("h:mm tt")
        };

        // Add an opening greeting message from the matched character
        string firstMsg = string.IsNullOrEmpty(initialMessage) 
            ? "Hey there! Nice to match with you." 
            : initialMessage;

        newMatch.conversationHistory.Add(new ChatMessageData
        {
            isSenderPlayer = false,
            messageText = firstMsg,
            timestamp = newMatch.lastMessageTime
        });

        activeChats.Insert(0, newMatch); // Newest match on top
        OnChatsUpdated?.Invoke();
    }
}
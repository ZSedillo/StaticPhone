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

        // Load persistent data from disk on game launch
        LoadAllDataFromSave();
    }

    public void LoadAllDataFromSave()
    {
        ChatSaveSystem.Load();
        activeChats.Clear();

        // Reconstruct activeChats from the saved database
        foreach (var savedContact in ChatSaveSystem.DB.savedContacts)
        {
            ContactChatData chat = new ContactChatData
            {
                contactId = System.Guid.NewGuid().ToString(),
                contactName = savedContact.contactName,
                contactBio = savedContact.contactBio,
                avatarIndex = savedContact.avatarIndex,
                lastMessageTime = savedContact.lastMessageTime,
                conversationHistory = new List<ChatMessageData>()
            };

            foreach (var msg in savedContact.chatHistory)
            {
                chat.conversationHistory.Add(new ChatMessageData
                {
                    messageText = msg.messageText,
                    isSenderPlayer = msg.isPlayer,
                    timestamp = savedContact.lastMessageTime
                });
            }

            activeChats.Add(chat);
        }

        OnChatsUpdated?.Invoke();
    }

    public void SetPlayerBasicInfo(string newName, int newAge)
    {
        currentUser.playerName = string.IsNullOrEmpty(newName) ? "Player" : newName;
        currentUser.playerAge = newAge > 0 ? newAge : 18;
        OnUserDataUpdated?.Invoke();
    }

    public void AddMatch(string name, string bio, string personality, int avatarIdx, string initialMessage = null)
    {
        if (activeChats.Exists(c => c.contactName.Equals(name, StringComparison.OrdinalIgnoreCase))) 
            return;

        string timeNow = DateTime.Now.ToString("h:mm tt");

        // 1. Persist contact to Disk Database
        SavedContactData saved = ChatSaveSystem.AddOrGetContact(name, bio, avatarIdx);
        saved.lastMessageTime = timeNow;

        // GUARD: Only add an initial chat if it is NOT identical to the bio
        if (!string.IsNullOrEmpty(initialMessage) && initialMessage.Trim() != bio.Trim())
        {
            if (saved.chatHistory.Count == 0)
            {
                saved.chatHistory.Add(new SavedChatMessage { messageText = initialMessage, isPlayer = false });
            }
        }
        ChatSaveSystem.Save();

        // 2. Add to active in-memory list
        ContactChatData newMatch = new ContactChatData
        {
            contactId = Guid.NewGuid().ToString(),
            contactName = name,
            contactBio = bio,
            contactPersonality = personality,
            avatarIndex = avatarIdx,
            lastMessageTime = timeNow,
            conversationHistory = new List<ChatMessageData>()
        };

        if (saved.chatHistory.Count > 0)
        {
            newMatch.conversationHistory.Add(new ChatMessageData
            {
                isSenderPlayer = false,
                messageText = saved.chatHistory[0].messageText,
                timestamp = timeNow
            });
        }

        activeChats.Insert(0, newMatch);
        OnChatsUpdated?.Invoke();
    }

    public void UpdateLastMessage(string contactName, string lastMessage)
    {
        ContactChatData match = activeChats.Find(c => c.contactName.Equals(contactName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.lastMessageTime = DateTime.Now.ToString("h:mm tt");
            
            if (match.conversationHistory == null)
            {
                match.conversationHistory = new List<ChatMessageData>();
            }

            if (match.conversationHistory.Count == 0)
            {
                match.conversationHistory.Add(new ChatMessageData
                {
                    messageText = lastMessage,
                    isSenderPlayer = false,
                    timestamp = match.lastMessageTime
                });
            }
            else
            {
                match.conversationHistory[match.conversationHistory.Count - 1].messageText = lastMessage;
            }

            // Move the active conversation to index 0 so it displays at the top of the chat list
            activeChats.Remove(match);
            activeChats.Insert(0, match);

            OnChatsUpdated?.Invoke();
        }
    }

    public void ResetAllProgress()
    {
        activeChats.Clear();
        ChatSaveSystem.DeleteAllProgress();
        OnChatsUpdated?.Invoke();
    }
}
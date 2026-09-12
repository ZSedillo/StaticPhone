using UnityEngine;
using System.Collections.Generic;

public class ChatsViewController : MonoBehaviour
{
    [Header("App Type")]
    [SerializeField] private bool isOnlyYapsView = false;

    [Header("UI References")]
    public Transform chatsContentParent;
    public GameObject chatItemPrefab;

    [Header("Avatar Sprites Pool")]
    public List<Sprite> profilePhotos = new List<Sprite>();

    [Header("Direct Chat Room Reference")]
    public DirectChatRoomController directChatRoom;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChatsUpdated += RefreshChatsUI;
            RefreshChatsUI();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChatsUpdated -= RefreshChatsUI;
        }
    }

    private void Start()
    {
        RefreshChatsUI();
    }

    public void RefreshChatsUI()
    {
        if (chatsContentParent == null || chatItemPrefab == null) return;

        // Clear previous cards
        for (int i = chatsContentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(chatsContentParent.GetChild(i).gameObject);
        }

        if (GameManager.Instance == null) return;

        HashSet<string> spawnedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < GameManager.Instance.activeChats.Count; i++)
        {
            ContactChatData chatData = GameManager.Instance.activeChats[i];
            if (chatData == null) continue;

            // Normalize name: "OY_Andiva" -> "Andiva"
            string cleanName = chatData.contactName.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase)
                ? chatData.contactName.Substring(3)
                : chatData.contactName;

            // Check unlock state using both clean name and raw name
            SavedContactData saved = ChatSaveSystem.GetContact(cleanName) ?? ChatSaveSystem.GetContact(chatData.contactName);
            bool isUnlocked = saved != null && saved.isUnlockedInOnlyYaps;

            // 1. In OnlyYaps: Only show if she unlocked OnlyYaps
            if (isOnlyYapsView && !isUnlocked)
            {
                continue;
            }

            // 2. In Dating App: Never show pure OY_ contacts
            if (!isOnlyYapsView && chatData.contactName.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 3. Prevent duplicate cards for the same character
            if (!spawnedNames.Add(cleanName))
            {
                continue;
            }

            GameObject newChat = Instantiate(chatItemPrefab, chatsContentParent);
            ChatItemUI ui = newChat.GetComponent<ChatItemUI>();

            if (ui != null)
            {
                string lastMsg = "New match! Say hi.";
                if (chatData.conversationHistory != null && chatData.conversationHistory.Count > 0)
                {
                    lastMsg = chatData.conversationHistory[chatData.conversationHistory.Count - 1].messageText;
                }
                else if (saved != null && saved.chatHistory != null && saved.chatHistory.Count > 0)
                {
                    lastMsg = saved.chatHistory[saved.chatHistory.Count - 1].messageText;
                }

                Sprite avatar = (chatData.avatarIndex >= 0 && chatData.avatarIndex < profilePhotos.Count)
                    ? profilePhotos[chatData.avatarIndex]
                    : null;

                int index = i;
                ui.Setup(
                    cleanName, // Always displays clean "Andiva" without "OY_"
                    lastMsg, 
                    chatData.lastMessageTime, 
                    avatar, 
                    () => OnChatSelected(cleanName, index)
                );
            }
        }
    }

    private void OnChatSelected(string contactName, int index)
    {
        if (GameManager.Instance == null || directChatRoom == null) return;

        // Look up by clean name or OY_ name
        ContactChatData selectedChat = GameManager.Instance.activeChats.Find(c => 
            c.contactName.Equals(contactName, System.StringComparison.OrdinalIgnoreCase) ||
            c.contactName.Equals("OY_" + contactName, System.StringComparison.OrdinalIgnoreCase));

        if (selectedChat != null)
        {
            Sprite avatar = (selectedChat.avatarIndex >= 0 && selectedChat.avatarIndex < profilePhotos.Count)
                ? profilePhotos[selectedChat.avatarIndex]
                : null;

            directChatRoom.OpenChatRoom(selectedChat, avatar);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

public class ChatsViewController : MonoBehaviour
{
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

        // Populate dynamic matches from GameManager
        for (int i = 0; i < GameManager.Instance.activeChats.Count; i++)
        {
            ContactChatData chatData = GameManager.Instance.activeChats[i];
            GameObject newChat = Instantiate(chatItemPrefab, chatsContentParent);
            ChatItemUI ui = newChat.GetComponent<ChatItemUI>();

            if (ui != null)
            {
                // Prioritize live conversation history, then saved database history, else display "New match! Say hi."
                string lastMsg = "New match! Say hi.";
                
                SavedContactData savedContact = ChatSaveSystem.GetContact(chatData.contactName);
                if (chatData.conversationHistory != null && chatData.conversationHistory.Count > 0)
                {
                    lastMsg = chatData.conversationHistory[chatData.conversationHistory.Count - 1].messageText;
                }
                else if (savedContact != null && savedContact.chatHistory.Count > 0)
                {
                    lastMsg = savedContact.chatHistory[savedContact.chatHistory.Count - 1].messageText;
                }

                Sprite avatar = (chatData.avatarIndex >= 0 && chatData.avatarIndex < profilePhotos.Count)
                    ? profilePhotos[chatData.avatarIndex]
                    : null;

                int index = i;
                ui.Setup(
                    chatData.contactName, 
                    lastMsg, 
                    chatData.lastMessageTime, 
                    avatar, 
                    () => OnChatSelected(chatData.contactName, index)
                );
            }
        }
    }

    private void OnChatSelected(string contactName, int index)
    {
        if (GameManager.Instance == null || directChatRoom == null) return;

        ContactChatData selectedChat = GameManager.Instance.activeChats.Find(c => 
            c.contactName.Equals(contactName, System.StringComparison.OrdinalIgnoreCase));

        if (selectedChat != null)
        {
            Sprite avatar = (selectedChat.avatarIndex >= 0 && selectedChat.avatarIndex < profilePhotos.Count)
                ? profilePhotos[selectedChat.avatarIndex]
                : null;

            directChatRoom.OpenChatRoom(selectedChat, avatar);
        }
    }
}
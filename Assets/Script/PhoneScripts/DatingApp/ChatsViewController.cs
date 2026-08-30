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
                string lastMsg = chatData.conversationHistory.Count > 0
                    ? chatData.conversationHistory[chatData.conversationHistory.Count - 1].messageText
                    : chatData.contactBio;

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

        ContactChatData selectedChat = GameManager.Instance.activeChats.Find(c => c.contactName == contactName);
        if (selectedChat != null)
        {
            Sprite avatar = (selectedChat.avatarIndex >= 0 && selectedChat.avatarIndex < profilePhotos.Count)
                ? profilePhotos[selectedChat.avatarIndex]
                : null;

            // Opens the direct 1-on-1 Messenger-style room
            directChatRoom.OpenChatRoom(selectedChat, avatar);
        }
    }
}
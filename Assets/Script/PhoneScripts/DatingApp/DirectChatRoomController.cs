using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DirectChatRoomController : MonoBehaviour
{
    [Header("Header References")]
    public Button btnBack;
    public Image partnerAvatar;
    public TextMeshProUGUI txtPartnerName;

    [Header("Feed Scroll Area")]
    public ScrollRect messageScrollRect;
    public Transform messageFeedContent;
    public GameObject directMessagePrefab;

    [Header("Input Bar")]
    public TMP_InputField inputField;
    public Button btnSend;

    [Header("Screen Navigation")]
    public GameObject chatsListPanel;
    public GameObject bottomNav; // Optional: hide the bottom tab bar when inside a direct chat room

    private ContactChatData currentChat;

    private void Start()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(CloseChatRoom);

        if (btnSend != null)
            btnSend.onClick.AddListener(SendPlayerMessage);
    }

    /// <summary>
    /// Opens the full chat view for the selected partner
    /// </summary>
    public void OpenChatRoom(ContactChatData chat, Sprite avatarSprite = null)
    {
        currentChat = chat;
        gameObject.SetActive(true);

        if (chatsListPanel != null)
            chatsListPanel.SetActive(false);

        if (bottomNav != null)
            bottomNav.SetActive(false);

        if (txtPartnerName != null)
            txtPartnerName.text = chat.contactName;

        if (partnerAvatar != null && avatarSprite != null)
            partnerAvatar.sprite = avatarSprite;

        PopulateMessageFeed();
    }

    private void PopulateMessageFeed()
    {
        // Clear previous conversation lines
        for (int i = messageFeedContent.childCount - 1; i >= 0; i--)
        {
            Destroy(messageFeedContent.GetChild(i).gameObject);
        }

        if (currentChat == null) return;

        // Populate existing conversation
        foreach (ChatMessageData msg in currentChat.conversationHistory)
        {
            SpawnMessageItem(msg.messageText, msg.isSenderPlayer);
        }

        StartCoroutine(ScrollToBottom());
    }

    public void SendPlayerMessage()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text) || currentChat == null) return;

        string messageText = inputField.text.Trim();
        string timeNow = System.DateTime.Now.ToString("h:mm tt");

        // 1. Add to conversation history
        ChatMessageData newMsg = new ChatMessageData
        {
            isSenderPlayer = true,
            messageText = messageText,
            timestamp = timeNow
        };

        currentChat.conversationHistory.Add(newMsg);
        currentChat.lastMessageTime = timeNow;

        // 2. Spawn in UI
        SpawnMessageItem(messageText, true);
        inputField.text = "";

        // 3. Update GameManager so the overview list reflects the newest message
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddOrUpdateChat(currentChat);
        }

        StartCoroutine(ScrollToBottom());
    }

    private void SpawnMessageItem(string message, bool isPlayer)
    {
        GameObject lineObj = Instantiate(directMessagePrefab, messageFeedContent);
        DirectMessageUI lineUI = lineObj.GetComponent<DirectMessageUI>();
        if (lineUI != null)
        {
            lineUI.Setup(message, isPlayer);
        }
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait two frames so ContentSizeFitter and LayoutGroup calculate heights
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        if (messageScrollRect != null && messageFeedContent != null)
        {
            RectTransform contentRT = messageFeedContent.GetComponent<RectTransform>();
            RectTransform viewportRT = messageScrollRect.viewport != null 
                ? messageScrollRect.viewport 
                : messageScrollRect.GetComponent<RectTransform>();

            // Only snap to the bottom (0f) if messages actually exceed the viewport height!
            // If content is shorter than the screen, keep it at the top (1f).
            if (contentRT.rect.height > viewportRT.rect.height)
            {
                messageScrollRect.verticalNormalizedPosition = 0f;
            }
            else
            {
                messageScrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }

    public void CloseChatRoom()
    {
        gameObject.SetActive(false);

        if (chatsListPanel != null)
            chatsListPanel.SetActive(true);

        if (bottomNav != null)
            bottomNav.SetActive(true);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DirectChatRoomController : MonoBehaviour
{
    [Header("Header References")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Image partnerAvatar;
    [SerializeField] private TextMeshProUGUI txtPartnerName;

    [Header("Feed Scroll Area")]
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private Transform messageFeedContent;
    [SerializeField] private GameObject directMessagePrefab;

    [Header("Choice Container")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    [Header("Screen Navigation")]
    [SerializeField] private GameObject chatsListPanel;
    [SerializeField] private GameObject bottomNav;

    [Header("Chat Settings")]
    [SerializeField] private float partnerReplyDelay = 2.5f; // 2 to 5 seconds realistic wait
    [SerializeField] private float scrollDuration = 0.25f;

    private string activeGirlName;
    private DialogueNodeData currentNode;
    private SavedContactData activeContact;
    private Coroutine scrollCoroutine;
    private Coroutine partnerReplyCoroutine;
    private GameObject currentTypingIndicatorObj;

    private void Awake()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(CloseChatRoom);
        }
    }

    public void OpenChatRoom(ContactChatData contactData, Sprite avatarSprite)
    {
        string pName = contactData != null ? contactData.contactName : "Match";
        OpenChatRoom(pName, avatarSprite);
    }

    public void OpenChatRoom(string partnerName, Sprite avatarSprite)
    {
        gameObject.SetActive(true);
        activeGirlName = partnerName.Trim();
        activeContact = ChatSaveSystem.AddOrGetContact(activeGirlName, "", 0);

        if (txtPartnerName != null) txtPartnerName.text = activeGirlName;
        if (partnerAvatar != null && avatarSprite != null) partnerAvatar.sprite = avatarSprite;
        if (chatsListPanel != null) chatsListPanel.SetActive(false);
        if (bottomNav != null) bottomNav.SetActive(false);

        ClearChatUI();

        // 1. Rebuild previously saved conversation
        foreach (var msg in activeContact.chatHistory)
        {
            InstantiateBubble(msg.messageText, msg.isPlayer, autoScroll: false);
        }

        // 2. Fetch current active node from JSON
        currentNode = DialogueLoader.GetNode(activeGirlName, activeContact.currentNodeId);

        if (activeContact.chatHistory.Count == 0)
        {
            if (currentNode != null && !string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                partnerReplyCoroutine = StartCoroutine(DelayedPartnerReply(currentNode.partnerMessage));
            }
            else
            {
                StartCoroutine(DisplayChoicesCoroutine());
            }
        }
        else
        {
            StartCoroutine(DisplayChoicesCoroutine());
        }

        TriggerSmoothScroll();
    }

    public void CloseChatRoom()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        RemoveTypingIndicator();

        if (partnerReplyCoroutine != null)
        {
            StopCoroutine(partnerReplyCoroutine);
            partnerReplyCoroutine = null;

            if (currentNode != null && !string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                string formatted = FormatDialogueText(currentNode.partnerMessage);
                activeContact.chatHistory.Add(new SavedChatMessage { messageText = formatted, isPlayer = false });
                activeContact.lastMessageTime = System.DateTime.Now.ToString("h:mm tt");
                ChatSaveSystem.Save();

                if (GameManager.Instance != null)
                    GameManager.Instance.UpdateLastMessage(activeGirlName, formatted);
            }
        }

        ClearChatUI();
        gameObject.SetActive(false);
        if (chatsListPanel != null) chatsListPanel.SetActive(true);
        if (bottomNav != null) bottomNav.SetActive(true);
    }

    private void ReceivePartnerMessage(string message)
    {
        RemoveTypingIndicator();

        string formattedMessage = FormatDialogueText(message);

        activeContact.chatHistory.Add(new SavedChatMessage { messageText = formattedMessage, isPlayer = false });
        activeContact.lastMessageTime = System.DateTime.Now.ToString("h:mm tt");
        ChatSaveSystem.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(activeGirlName, formattedMessage);
        }

        InstantiateBubble(formattedMessage, isPlayer: false, autoScroll: true);
        StartCoroutine(DisplayChoicesCoroutine());
    }

    private IEnumerator DisplayChoicesCoroutine()
    {
        ClearChoices();
        yield return new WaitForSeconds(0.2f);

        if (currentNode == null || currentNode.choices == null || currentNode.choices.Count == 0)
            yield break;

        foreach (DialogueChoiceData choice in currentNode.choices)
        {
            if (choiceButtonPrefab == null || choiceContainer == null) break;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            string formattedChoiceText = FormatDialogueText(choice.choiceText);
            if (btnText != null) btnText.text = formattedChoiceText;

            Button btn = btnObj.GetComponent<Button>();
            string nextTargetId = choice.nextId;
            btn.onClick.AddListener(() => OnPlayerSelectedChoice(formattedChoiceText, nextTargetId));
        }

        TriggerSmoothScroll();
    }

    private void OnPlayerSelectedChoice(string playerText, string nextNodeId)
    {
        ClearChoices();

        activeContact.chatHistory.Add(new SavedChatMessage { messageText = playerText, isPlayer = true });
        activeContact.currentNodeId = nextNodeId;
        activeContact.lastMessageTime = System.DateTime.Now.ToString("h:mm tt");
        ChatSaveSystem.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(activeGirlName, playerText);
        }

        InstantiateBubble(playerText, isPlayer: true, autoScroll: true);

        currentNode = DialogueLoader.GetNode(activeGirlName, nextNodeId);

        if (currentNode != null)
        {
            if (!string.IsNullOrEmpty(currentNode.triggerEvent) && currentNode.triggerEvent == "UNLOCK_ONLYYAPS")
            {
                activeContact.isUnlockedInOnlyYaps = true;
                ChatSaveSystem.Save();
                Debug.Log($"[OnlyYaps] Unlocked {activeGirlName} for OnlyYaps!");
            }

            if (!string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                partnerReplyCoroutine = StartCoroutine(DelayedPartnerReply(currentNode.partnerMessage));
            }
        }
    }

    private IEnumerator DelayedPartnerReply(string message)
    {
        // 1. Show Typing in ChatsViewPanel preview
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(activeGirlName, "typing...");
        }

        // 2. Spawn "..." bubble inside chat
        ShowTypingIndicator();

        // 3. Wait 2.5s while animating dots
        float timer = 0f;
        int dotCount = 1;
        while (timer < partnerReplyDelay)
        {
            timer += 0.4f;
            dotCount = (dotCount % 3) + 1;
            UpdateTypingText(new string('.', dotCount));
            yield return new WaitForSeconds(0.4f);
        }

        partnerReplyCoroutine = null;
        ReceivePartnerMessage(message);
    }

    private void ShowTypingIndicator()
    {
        RemoveTypingIndicator();
        if (directMessagePrefab == null || messageFeedContent == null) return;

        currentTypingIndicatorObj = Instantiate(directMessagePrefab, messageFeedContent);
        DirectMessageUI msgUI = currentTypingIndicatorObj.GetComponent<DirectMessageUI>();
        if (msgUI != null)
        {
            msgUI.Setup("...", false);
        }
        TriggerSmoothScroll();
    }

    private void UpdateTypingText(string dots)
    {
        if (currentTypingIndicatorObj != null)
        {
            TextMeshProUGUI tmp = currentTypingIndicatorObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = dots;
        }
    }

    private void RemoveTypingIndicator()
    {
        if (currentTypingIndicatorObj != null)
        {
            Destroy(currentTypingIndicatorObj);
            currentTypingIndicatorObj = null;
        }
    }

    private void InstantiateBubble(string text, bool isPlayer, bool autoScroll = true)
    {
        if (directMessagePrefab == null || messageFeedContent == null) return;

        GameObject newMsg = Instantiate(directMessagePrefab, messageFeedContent);
        DirectMessageUI msgUI = newMsg.GetComponent<DirectMessageUI>();

        if (msgUI != null)
        {
            msgUI.Setup(text, isPlayer);
        }

        if (autoScroll)
        {
            TriggerSmoothScroll();
        }
    }

    private void ClearChoices()
    {
        if (choiceContainer == null) return;
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearChatUI()
    {
        RemoveTypingIndicator();
        if (messageFeedContent != null)
        {
            foreach (Transform child in messageFeedContent)
            {
                Destroy(child.gameObject);
            }
        }
        ClearChoices();
    }

    private void TriggerSmoothScroll()
    {
        if (scrollCoroutine != null)
            StopCoroutine(scrollCoroutine);

        if (gameObject.activeInHierarchy)
            scrollCoroutine = StartCoroutine(SmoothScrollToBottomCoroutine(scrollDuration));
    }

    private IEnumerator SmoothScrollToBottomCoroutine(float duration)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (messageFeedContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageFeedContent.GetComponent<RectTransform>());

        yield return new WaitForEndOfFrame();

        if (messageScrollRect == null) yield break;

        float startPos = messageScrollRect.verticalNormalizedPosition;
        float targetPos = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            messageScrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            yield return null;
        }

        messageScrollRect.verticalNormalizedPosition = targetPos;
        scrollCoroutine = null;
    }

    private string FormatDialogueText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        string playerName = "Player";
        if (GameManager.Instance != null && GameManager.Instance.currentUser != null)
        {
            if (!string.IsNullOrEmpty(GameManager.Instance.currentUser.playerName))
            {
                playerName = GameManager.Instance.currentUser.playerName;
            }
        }

        return rawText.Replace("{PlayerName}", playerName);
    }
}
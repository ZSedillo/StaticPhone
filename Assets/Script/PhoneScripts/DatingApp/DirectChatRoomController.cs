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
    [SerializeField] private float partnerReplyDelay = 2.5f;
    [SerializeField] private float scrollDuration = 0.25f;

[Header("App Body Panels to Hide On Chat Open")]
    [SerializeField] private GameObject profileViewPanel;
    [SerializeField] private GameObject exploreViewPanel;
    [SerializeField] private GameObject likesViewPanel;

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

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.SetCurrentOpenChat(activeGirlName);
        }

        if (txtPartnerName != null) txtPartnerName.text = activeGirlName;
        if (partnerAvatar != null && avatarSprite != null) partnerAvatar.sprite = avatarSprite;

        // Turn OFF other views so they don't stack behind chat
        if (chatsListPanel != null) chatsListPanel.SetActive(false);
        if (bottomNav != null) bottomNav.SetActive(false);
        if (profileViewPanel != null) profileViewPanel.SetActive(false);
        if (exploreViewPanel != null) exploreViewPanel.SetActive(false);
        if (likesViewPanel != null) likesViewPanel.SetActive(false);

        ClearChatUI();

        foreach (var msg in activeContact.chatHistory)
        {
            InstantiateBubble(msg.messageText, msg.isPlayer, autoScroll: false);
        }

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
        // Clear active conversation view so notifications can fire again
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ClearCurrentOpenChat();
        }

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

                // Notify if message finalized after backing out
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.TriggerNotification(activeGirlName, formatted, activeContact.avatarIndex);
                }
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

        // Trigger notification banner/shade (auto-ignored if player is currently in this room)
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.TriggerNotification(activeGirlName, formattedMessage, activeContact.avatarIndex);
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
            }

            if (!string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                string replyText = currentNode.partnerMessage;
                string currentGirl = activeGirlName;
                int avatarIdx = activeContact.avatarIndex;

                // Run reply on persistent GameManager so minimizing the app does not freeze it!
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartCoroutine(GlobalPartnerReplyRoutine(currentGirl, replyText, avatarIdx));
                }
                else
                {
                    partnerReplyCoroutine = StartCoroutine(DelayedPartnerReply(replyText));
                }
            }
        }
    }


    private IEnumerator GlobalPartnerReplyRoutine(string girlName, string message, int avatarIdx)
    {
        ShowTypingIndicator();

        yield return new WaitForSeconds(partnerReplyDelay);

        RemoveTypingIndicator();

        string formatted = FormatDialogueText(message);
        SavedContactData contact = ChatSaveSystem.AddOrGetContact(girlName, "", 0);
        contact.chatHistory.Add(new SavedChatMessage { messageText = formatted, isPlayer = false });
        contact.lastMessageTime = System.DateTime.Now.ToString("h:mm tt");
        ChatSaveSystem.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(girlName, formatted);
        }

        // If user is currently looking at her chat, spawn the bubble live
        if (gameObject.activeInHierarchy && activeGirlName.Equals(girlName, System.StringComparison.OrdinalIgnoreCase))
        {
            InstantiateBubble(formatted, isPlayer: false, autoScroll: true);
            StartCoroutine(DisplayChoicesCoroutine());
        }

        // Always trigger notification (auto-suppressed if player is actively looking at this screen)
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.TriggerNotification(girlName, formatted, avatarIdx);
        }
    }


    private IEnumerator DelayedPartnerReply(string message)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(activeGirlName, "typing...");
        }

        ShowTypingIndicator();

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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DirectChatRoomController : MonoBehaviour
{
    [Header("App Type")]
    [Tooltip("Check this ONLY on the DirectChatPanel inside OnlyYapsAppWindow")]
    [SerializeField] private bool isOnlyYaps = false;

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
    [SerializeField] private float minReplyDelay = 3f;
    [SerializeField] private float maxReplyDelay = 10f;
    [SerializeField] private float scrollDuration = 0.25f;

    [Header("App Body Panels to Hide On Chat Open (Dating App Only)")]
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

        // Separate save key so OnlyYaps and Dating App do not share conversation history
        string saveKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;
        activeContact = ChatSaveSystem.AddOrGetContact(saveKey, "", 0);

        if (NotificationManager.Instance != null)
        {
            // Mute active chat notifications while looking at this room
            NotificationManager.Instance.SetCurrentOpenChat(isOnlyYaps ? "OnlyYaps" : activeGirlName);
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

        // 1. Load history for this specific app
        foreach (var msg in activeContact.chatHistory)
        {
            InstantiateBubble(msg.messageText, msg.isPlayer, autoScroll: false);
        }

        // 2. Resolve dialogue path based on folder and starting node
        string dialoguePath = isOnlyYaps
            ? ("DialoguesOnlyYaps/" + activeGirlName)
            : ("Dialogues/" + activeGirlName + "Dialogue");

        string startNodeId = string.IsNullOrEmpty(activeContact.currentNodeId)
            ? (isOnlyYaps ? ("oy_" + activeGirlName.ToLower() + "_start") : "start")
            : activeContact.currentNodeId;

        currentNode = DialogueLoader.GetNode(dialoguePath, startNodeId);

        // Safety verification log
        if (currentNode == null)
        {
            Debug.LogError($"[DirectChatRoom] Failed to load any node from Resources/{dialoguePath}");
            return;
        }

        if (activeContact.chatHistory.Count == 0)
        {
            if (!string.IsNullOrEmpty(currentNode.partnerMessage))
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

                if (NotificationManager.Instance != null)
                {
                    string notifSource = isOnlyYaps ? "OnlyYaps" : activeGirlName;
                    NotificationManager.Instance.TriggerNotification(notifSource, formatted, activeContact.avatarIndex);
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

        string currentLink = currentNode != null ? currentNode.linkUrl : "";
        string currentEvent = currentNode != null ? currentNode.triggerEvent : "";

        if (NotificationManager.Instance != null)
        {
            string notifSource = isOnlyYaps ? "OnlyYaps" : activeGirlName;
            NotificationManager.Instance.TriggerNotification(notifSource, formattedMessage, activeContact.avatarIndex);
        }

        InstantiateBubble(formattedMessage, isPlayer: false, linkUrl: currentLink, eventTrigger: currentEvent, autoScroll: true);
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
            string choiceLink = choice.linkUrl;
            string choiceEvent = choice.triggerEvent;
            btn.onClick.AddListener(() => OnPlayerSelectedChoice(formattedChoiceText, nextTargetId, choiceLink, choiceEvent));
        }

        TriggerSmoothScroll();
    }

    private void OnPlayerSelectedChoice(string playerText, string nextNodeId, string linkUrl = "", string eventTrigger = "")
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

        InstantiateBubble(playerText, isPlayer: true, linkUrl: linkUrl, eventTrigger: eventTrigger, autoScroll: true);

        string dialoguePath = isOnlyYaps
            ? ("DialoguesOnlyYaps/" + activeGirlName)
            : ("Dialogues/" + activeGirlName + "Dialogue");

        currentNode = DialogueLoader.GetNode(dialoguePath, nextNodeId);

        if (currentNode != null)
        {
            if (!string.IsNullOrEmpty(currentNode.triggerEvent) && currentNode.triggerEvent == "UNLOCK_ONLYYAPS")
            {
                SavedContactData rootContact = ChatSaveSystem.AddOrGetContact(activeGirlName, "", 0);
                rootContact.isUnlockedInOnlyYaps = true;
                ChatSaveSystem.Save();
            }

            if (!string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                string replyText = currentNode.partnerMessage;
                string currentGirl = activeGirlName;
                int avatarIdx = activeContact.avatarIndex;

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

        float randomDelay = Random.Range(minReplyDelay, maxReplyDelay);
        yield return new WaitForSeconds(randomDelay);

        RemoveTypingIndicator();

        string formatted = FormatDialogueText(message);
        string saveKey = isOnlyYaps ? ("OY_" + girlName) : girlName;
        SavedContactData contact = ChatSaveSystem.AddOrGetContact(saveKey, "", 0);
        contact.chatHistory.Add(new SavedChatMessage { messageText = formatted, isPlayer = false });
        contact.lastMessageTime = System.DateTime.Now.ToString("h:mm tt");
        ChatSaveSystem.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(girlName, formatted);
        }

        if (gameObject.activeInHierarchy && activeGirlName.Equals(girlName, System.StringComparison.OrdinalIgnoreCase))
        {
            string currentLink = currentNode != null ? currentNode.linkUrl : "";
            string currentEvent = currentNode != null ? currentNode.triggerEvent : "";
            InstantiateBubble(formatted, isPlayer: false, linkUrl: currentLink, eventTrigger: currentEvent, autoScroll: true);
            StartCoroutine(DisplayChoicesCoroutine());
        }

        if (NotificationManager.Instance != null)
        {
            string notifSource = isOnlyYaps ? "OnlyYaps" : girlName;
            NotificationManager.Instance.TriggerNotification(notifSource, formatted, avatarIdx);
        }
    }

    private IEnumerator DelayedPartnerReply(string message)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(activeGirlName, "typing...");
        }

        ShowTypingIndicator();

        float min = Mathf.Max(1f, minReplyDelay);
        float max = Mathf.Max(min, maxReplyDelay);
        float totalWaitTime = Random.Range(min, max);
        float timer = 0f;
        int dotCount = 1;

        while (timer < totalWaitTime)
        {
            yield return new WaitForSeconds(0.4f);
            timer += 0.4f;
            dotCount = (dotCount % 3) + 1;
            UpdateTypingText(new string('.', dotCount));
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

    private void InstantiateBubble(string text, bool isPlayer, string linkUrl = "", string eventTrigger = "", bool autoScroll = true)
    {
        if (directMessagePrefab == null || messageFeedContent == null) return;

        GameObject newMsg = Instantiate(directMessagePrefab, messageFeedContent);
        DirectMessageUI msgUI = newMsg.GetComponent<DirectMessageUI>();

        string finalText = text;

        if (!string.IsNullOrEmpty(linkUrl))
        {
            finalText += $"\n<link=\"{linkUrl}\"><u><color=#38E54D>{linkUrl}</color></u></link>";
        }

        if (msgUI != null)
        {
            msgUI.Setup(finalText, isPlayer);

            TMP_Text tmpText = newMsg.GetComponentInChildren<TMP_Text>();
            if (tmpText != null && !string.IsNullOrEmpty(linkUrl))
            {
                var linkHandler = tmpText.gameObject.AddComponent<ChatLinkClickReceiver>();
                linkHandler.Initialize(linkUrl, () => UnlockOnlyYapsContact(activeGirlName));
            }
        }

        if (!string.IsNullOrEmpty(eventTrigger))
        {
            DialogueEventManager.TriggerEvent(eventTrigger, activeGirlName);
        }

        if (autoScroll) TriggerSmoothScroll();
    }

    public void UnlockOnlyYapsContact(string girlName)
    {
        SavedContactData contact = ChatSaveSystem.AddOrGetContact(girlName, "", 0);
        contact.isUnlockedInOnlyYaps = true;
        ChatSaveSystem.Save();

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.TriggerNotification("OnlyYaps", $"{girlName} shared her private link! Added to OnlyYaps.", contact.avatarIndex);
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
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
    [SerializeField] private Button btnCall;
    [SerializeField] private Image partnerAvatar;
    [SerializeField] private TextMeshProUGUI txtPartnerName;

    [Header("Voice Call Overlay")]
    [SerializeField] private VoiceCallOverlayController callOverlayController;

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

    // Tracks which call we are currently on during the session (1 or 2)
    private int completedCallCount = 0;

    private void Awake()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(CloseChatRoom);
        }

        if (btnCall != null)
        {
            btnCall.onClick.RemoveAllListeners();
            btnCall.onClick.AddListener(OnCallButtonClicked);
        }
    }

    private void OnEnable()
    {
        // Register call triggers and post-call resume listeners
        DialogueEventManager.Register("TRIGGER_CALL_1", HandleCall1Trigger);
        DialogueEventManager.Register("TRIGGER_CALL_2", HandleCall2Trigger);
        DialogueEventManager.Register("CALL_COMPLETED", HandleCallCompleted);
        DialogueEventManager.Register("RESET_CALL_DIALOGUE", HandleCallResetOrDropped);
    }

    private void OnDisable()
    {
        // Unregister listeners to avoid memory leaks or duplicate calls
        DialogueEventManager.Unregister("TRIGGER_CALL_1", HandleCall1Trigger);
        DialogueEventManager.Unregister("TRIGGER_CALL_2", HandleCall2Trigger);
        DialogueEventManager.Unregister("CALL_COMPLETED", HandleCallCompleted);
        DialogueEventManager.Unregister("RESET_CALL_DIALOGUE", HandleCallResetOrDropped);
    }

    // =========================================================================
    // VOICE CALL ROUTING & RESUME LOGIC
    // =========================================================================

    private void HandleCall1Trigger(string characterName)
    {
        LaunchSpecificCall(characterName, 1);
    }

    private void HandleCall2Trigger(string characterName)
    {
        LaunchSpecificCall(characterName, 2);
    }

    private void OnCallButtonClicked()
    {
        if (activeContact != null && activeContact.canVoiceCall)
        {
            // Call next appropriate sequence (Call 1 or Call 2)
            int nextCallIndex = completedCallCount >= 1 ? 2 : 1;
            LaunchSpecificCall(activeGirlName, nextCallIndex);
        }
        else
        {
            Debug.LogWarning($"[DirectChatRoom] Voice call blocked: No consent from {activeGirlName} yet.");
        }
    }

    private string GetCallStartNode(string cleanName, int callIndex)
    {
        if (cleanName.Equals("Andiva", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.Equals("Daisy", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.Equals("Zephyrine", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.Equals("Trixie", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.Equals("Seraphine", System.StringComparison.OrdinalIgnoreCase))
        {
            return (callIndex == 2) ? "call2_start" : "call1_start";
        }

        return "start";
    }

    private void LaunchSpecificCall(string characterName, int callIndex)
    {
        string cleanName = CleanCharacterName(characterName);
        if (string.IsNullOrEmpty(cleanName)) cleanName = activeGirlName;

        // Junia doesn't do calls
        if (cleanName.Equals("Junia", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning("[DirectChatRoom] Junia does not accept voice calls.");
            return;
        }

        Sprite avatar = partnerAvatar != null ? partnerAvatar.sprite : null;
        string startNode = GetCallStartNode(cleanName, callIndex);

        Debug.Log($"[DirectChatRoom] Starting Call {callIndex} for {cleanName} at node '{startNode}'");

        if (callOverlayController != null)
        {
            callOverlayController.StartOutgoingCall(cleanName, avatar, startNode);
        }
        else if (VoiceCallOverlayController.Instance != null)
        {
            VoiceCallOverlayController.Instance.StartOutgoingCall(cleanName, avatar, startNode);
        }
        else
        {
            Debug.LogError("[DirectChatRoom] VoiceCallOverlayController reference is missing!");
        }
    }

    private void HandleCallCompleted(string characterName)
    {
        string cleanName = CleanCharacterName(characterName);
        if (!cleanName.Equals(activeGirlName, System.StringComparison.OrdinalIgnoreCase)) return;

        completedCallCount++;
        Debug.Log($"[DirectChatRoom] Call completed with {cleanName}. Completed call count: {completedCallCount}");

        // Resume chat at the appropriate post-call hub node
        string postCallNodeId = (completedCallCount == 1) ? "oy_post_call_1_hub" : "oy_post_call_2_hub";

        // Evelyn uses "oy_ending_romance_safe_harbor" progression
        if (cleanName.Equals("Evelyn", System.StringComparison.OrdinalIgnoreCase))
        {
            postCallNodeId = "oy_ending_romance_safe_harbor";
        }

        AdvanceChatToNode(postCallNodeId);
    }

    private void HandleCallResetOrDropped(string characterName)
    {
        string cleanName = CleanCharacterName(characterName);
        if (!cleanName.Equals(activeGirlName, System.StringComparison.OrdinalIgnoreCase)) return;

        // If the call dropped prematurely, transition to reset node if available
        AdvanceChatToNode("oy_call_missed_reset");
    }

    private void AdvanceChatToNode(string targetNodeId)
    {
        string dialoguePath = isOnlyYaps
            ? ("DialoguesOnlyYaps/" + activeGirlName)
            : ("Dialogues/" + activeGirlName + "Dialogue");

        DialogueNodeData nextNode = DialogueLoader.GetNode(dialoguePath, targetNodeId);
        if (nextNode != null)
        {
            currentNode = nextNode;
            activeContact.currentNodeId = targetNodeId;
            ChatSaveSystem.Save();

            if (!string.IsNullOrEmpty(currentNode.partnerMessage))
            {
                partnerReplyCoroutine = StartCoroutine(DelayedPartnerReply(currentNode.partnerMessage));
            }
            else
            {
                StartCoroutine(DisplayChoicesCoroutine());
            }
        }
    }

    // =========================================================================
    // ROOM LIFECYCLE & CORE CHAT
    // =========================================================================

    public void OpenChatRoom(ContactChatData contactData, Sprite avatarSprite)
    {
        string pName = contactData != null ? contactData.contactName : "Match";
        OpenChatRoom(pName, avatarSprite);
    }

    public void OpenChatRoom(string partnerName, Sprite avatarSprite)
    {
        gameObject.SetActive(true);

        activeGirlName = CleanCharacterName(partnerName);
        completedCallCount = 0; // Reset call progress for the open session

        string saveKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;
        activeContact = ChatSaveSystem.AddOrGetContact(saveKey, "", 0);

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.SetCurrentOpenChat(isOnlyYaps ? "OnlyYaps" : activeGirlName);
        }

        if (txtPartnerName != null) txtPartnerName.text = activeGirlName;
        if (partnerAvatar != null && avatarSprite != null) partnerAvatar.sprite = avatarSprite;

        if (btnCall != null)
        {
            // Junia never has a call button active
            bool canCall = isOnlyYaps && !activeGirlName.Equals("Junia", System.StringComparison.OrdinalIgnoreCase);
            btnCall.gameObject.SetActive(canCall);
            if (canCall)
            {
                btnCall.onClick.RemoveAllListeners();
                btnCall.onClick.AddListener(OnCallButtonClicked);
                btnCall.interactable = activeContact.canVoiceCall;
            }
        }

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

        string dialoguePath = isOnlyYaps
            ? ("DialoguesOnlyYaps/" + activeGirlName)
            : ("Dialogues/" + activeGirlName + "Dialogue");

        string startNodeId = string.IsNullOrEmpty(activeContact.currentNodeId)
            ? (isOnlyYaps ? ("oy_" + activeGirlName.ToLower() + "_start") : "start")
            : activeContact.currentNodeId;

        currentNode = DialogueLoader.GetNode(dialoguePath, startNodeId);

        if (currentNode == null)
        {
            Debug.LogError($"[DirectChatRoom] Failed to load any node from Resources/{dialoguePath}");
            return;
        }

        CheckAndApplyNodeEvents(currentNode);

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

    private void CheckAndApplyNodeEvents(DialogueNodeData node)
    {
        if (node == null || string.IsNullOrEmpty(node.triggerEvent)) return;

        if (node.triggerEvent == "ENABLE_VOICE_CALL")
        {
            if (activeContact != null)
            {
                activeContact.canVoiceCall = true;
                ChatSaveSystem.Save();
            }
            if (btnCall != null && !activeGirlName.Equals("Junia", System.StringComparison.OrdinalIgnoreCase))
            {
                btnCall.interactable = true;
            }
            Debug.Log($"[DirectChatRoom] Voice call permission activated for {activeGirlName}!");
        }
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

                string notifyContactKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;

                if (GameManager.Instance != null)
                    GameManager.Instance.UpdateLastMessage(notifyContactKey, formatted);

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

        string updateKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(updateKey, formattedMessage);
        }

        CheckAndApplyNodeEvents(currentNode);

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

        string updateKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(updateKey, playerText);
        }

        InstantiateBubble(playerText, isPlayer: true, linkUrl: linkUrl, eventTrigger: eventTrigger, autoScroll: true);

        string dialoguePath = isOnlyYaps
            ? ("DialoguesOnlyYaps/" + activeGirlName)
            : ("Dialogues/" + activeGirlName + "Dialogue");

        currentNode = DialogueLoader.GetNode(dialoguePath, nextNodeId);

        if (currentNode != null)
        {
            if (!string.IsNullOrEmpty(currentNode.triggerEvent))
            {
                if (currentNode.triggerEvent == "UNLOCK_ONLYYAPS")
                {
                    SavedContactData rootContact = ChatSaveSystem.AddOrGetContact(activeGirlName, "", 0);
                    rootContact.isUnlockedInOnlyYaps = true;
                    ChatSaveSystem.Save();
                }
                else if (currentNode.triggerEvent == "ENABLE_VOICE_CALL")
                {
                    activeContact.canVoiceCall = true;
                    ChatSaveSystem.Save();
                    if (btnCall != null && !activeGirlName.Equals("Junia", System.StringComparison.OrdinalIgnoreCase))
                    {
                        btnCall.interactable = true;
                    }
                    Debug.Log($"[DirectChatRoom] Voice call permission granted by {activeGirlName}!");
                }
                else if (currentNode.triggerEvent == "TRIGGER_CALL_1")
                {
                    DialogueEventManager.TriggerEvent("TRIGGER_CALL_1", activeGirlName);
                }
                else if (currentNode.triggerEvent == "TRIGGER_CALL_2")
                {
                    DialogueEventManager.TriggerEvent("TRIGGER_CALL_2", activeGirlName);
                }
                else if (currentNode.triggerEvent == "START_CALL_RINGTONE")
                {
                    StartCoroutine(DelayedIncomingCallTrigger(activeGirlName, 2f));
                }
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

    private IEnumerator DelayedIncomingCallTrigger(string girlName, float delay)
    {
        yield return new WaitForSeconds(delay);

        string cleanName = CleanCharacterName(girlName);
        if (string.IsNullOrEmpty(cleanName)) cleanName = activeGirlName;

        int nextCallIndex = completedCallCount >= 1 ? 2 : 1;
        string startNode = GetCallStartNode(cleanName, nextCallIndex);

        Sprite avatar = partnerAvatar != null ? partnerAvatar.sprite : null;
        Debug.Log($"[DirectChatRoom] Incoming call initiated by character: {cleanName} at node '{startNode}'");

        if (callOverlayController != null)
        {
            callOverlayController.TriggerIncomingCall(cleanName, avatar, startNode);
        }
        else if (VoiceCallOverlayController.Instance != null)
        {
            VoiceCallOverlayController.Instance.TriggerIncomingCall(cleanName, avatar, startNode);
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
            GameManager.Instance.UpdateLastMessage(saveKey, formatted);
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
        string updateKey = isOnlyYaps ? ("OY_" + activeGirlName) : activeGirlName;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateLastMessage(updateKey, "typing...");
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

    private string CleanCharacterName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "Match";
        string trimmed = rawName.Trim();
        return trimmed.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase)
            ? trimmed.Substring(3)
            : trimmed;
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
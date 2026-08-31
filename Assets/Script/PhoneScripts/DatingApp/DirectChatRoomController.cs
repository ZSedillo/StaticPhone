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

    [Header("Chat Flow")]
    [SerializeField] private float partnerReplyDelay = 1.0f;
    private DialogueStep currentDialogueStep;

    private void Awake()
    {
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(CloseChatRoom);
        }
    }

    // EXACT OVERLOAD CALLED BY ChatsViewController (selectedChat, avatar)
    public void OpenChatRoom(ContactChatData contactData, Sprite avatarSprite, DialogueStep startingStep = null)
    {
        string partnerName = contactData != null ? contactData.contactName : "Match";
        OpenChatRoomInternal(partnerName, avatarSprite, startingStep);
    }

    // Overload for manual name + sprite
    public void OpenChatRoom(string partnerName, Sprite avatarSprite, DialogueStep startingStep = null)
    {
        OpenChatRoomInternal(partnerName, avatarSprite, startingStep);
    }

    private void OpenChatRoomInternal(string partnerName, Sprite avatarSprite, DialogueStep startingStep)
    {
        gameObject.SetActive(true);

        if (txtPartnerName != null) txtPartnerName.text = partnerName;
        if (partnerAvatar != null && avatarSprite != null) partnerAvatar.sprite = avatarSprite;
        if (chatsListPanel != null) chatsListPanel.SetActive(false);
        if (bottomNav != null) bottomNav.SetActive(false);

        ClearChat();

        // If no custom dialogue step was passed from contact data, create a test conversation
        if (startingStep == null)
        {
            startingStep = CreateTestDialogue(partnerName);
        }

        currentDialogueStep = startingStep;

        if (currentDialogueStep != null && !string.IsNullOrEmpty(currentDialogueStep.partnerMessage))
        {
            ReceivePartnerMessage(currentDialogueStep.partnerMessage);
        }
    }

// Temporary test dialogue to immediately see choices working
    private DialogueStep CreateTestDialogue(string partnerName)
    {
        DialogueStep step1 = new DialogueStep();
        step1.partnerMessage = $"Hey! Thanks for matching with me.";

        DialogueStep branchA = new DialogueStep();
        branchA.partnerMessage = "Haha, I'm doing great! Just working on some projects.";

        DialogueStep branchB = new DialogueStep();
        branchB.partnerMessage = "Smooth line! Tell me about yourself.";

        step1.choices = new List<PlayerChoice>()
        {
            new PlayerChoice() { choiceText = "Hey there! How's your day going?", nextStep = branchA },
            new PlayerChoice() { choiceText = "I couldn't resist saying hi to you.", nextStep = branchB },
            new PlayerChoice() { choiceText = "Hi! What kind of music do you like?", nextStep = null }
        };

        return step1;
    }

    public void CloseChatRoom()
    {
        ClearChat();
        gameObject.SetActive(false);
        if (chatsListPanel != null) chatsListPanel.SetActive(true);
        if (bottomNav != null) bottomNav.SetActive(true);
    }

    public void ReceivePartnerMessage(string message)
    {
        SpawnMessage(message, isPlayer: false);
        StartCoroutine(DisplayChoicesCoroutine());
    }

    private IEnumerator DisplayChoicesCoroutine()
    {
        ClearChoices();
        yield return new WaitForSeconds(0.3f);

        if (currentDialogueStep == null || currentDialogueStep.choices == null || choiceContainer == null) yield break;

        foreach (PlayerChoice choice in currentDialogueStep.choices)
        {
            if (choiceButtonPrefab == null) break;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = choice.choiceText;

            Button btn = btnObj.GetComponent<Button>();
            PlayerChoice capturedChoice = choice;
            btn.onClick.AddListener(() => OnPlayerSelectedChoice(capturedChoice));
        }

        StartCoroutine(ScrollToBottom());
    }

    private void OnPlayerSelectedChoice(PlayerChoice choice)
    {
        ClearChoices();
        SpawnMessage(choice.choiceText, isPlayer: true);

        currentDialogueStep = choice.nextStep;

        if (currentDialogueStep != null && !string.IsNullOrEmpty(currentDialogueStep.partnerMessage))
        {
            StartCoroutine(DelayedPartnerReply(currentDialogueStep.partnerMessage));
        }
    }

    private IEnumerator DelayedPartnerReply(string message)
    {
        yield return new WaitForSeconds(partnerReplyDelay);
        ReceivePartnerMessage(message);
    }

    private void SpawnMessage(string text, bool isPlayer)
    {
        if (directMessagePrefab == null || messageFeedContent == null) return;

        GameObject newMsg = Instantiate(directMessagePrefab, messageFeedContent);
        DirectMessageUI msgUI = newMsg.GetComponent<DirectMessageUI>();

        if (msgUI != null)
        {
            msgUI.Setup(text, isPlayer);
        }

        StartCoroutine(ScrollToBottom());
    }

    private void ClearChoices()
    {
        if (choiceContainer == null) return;
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearChat()
    {
        if (messageFeedContent != null)
        {
            foreach (Transform child in messageFeedContent)
            {
                Destroy(child.gameObject);
            }
        }
        ClearChoices();
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        if (messageScrollRect != null && messageFeedContent != null)
        {
            RectTransform contentRT = messageFeedContent.GetComponent<RectTransform>();
            RectTransform viewportRT = messageScrollRect.viewport != null
                ? messageScrollRect.viewport
                : messageScrollRect.GetComponent<RectTransform>();

            if (contentRT.rect.height > viewportRT.rect.height)
                messageScrollRect.verticalNormalizedPosition = 0f;
            else
                messageScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
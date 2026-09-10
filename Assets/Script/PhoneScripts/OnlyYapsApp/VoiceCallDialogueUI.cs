using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceCallDialogueUI : MonoBehaviour
{
    public static VoiceCallDialogueUI Instance { get; private set; }

    [Header("Root Canvas/Panel")]
    [SerializeField] private GameObject screenRoot;

    [Header("Left Side - Header & Scroll Feed")]
    [SerializeField] private Image imgPartnerAvatar;
    [SerializeField] private TextMeshProUGUI txtPartnerSpeakerName;
    [SerializeField] private ScrollRect dialogueScrollRect;
    [SerializeField] private Transform dialogueFeedContent;
    [SerializeField] private GameObject messageBubblePrefab; // DirectMessagePrefab

    [Header("Right Side - Choices & Pressure Timer")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Slider timerBar;
    [SerializeField] private TextMeshProUGUI txtTimerCounter;

    [Header("Speech & Timing Settings")]
    [SerializeField] private float charactersPerSecond = 35f; // Speed of speech/typing animation
    [SerializeField] private float partnerReplyDelay = 0.8f;   // Pause before she starts speaking
    [SerializeField] private float responseTimeout = 10f;       // Player response timer

    private VoiceCallConversation activeConvo;
    private VoiceCallNode currentNode;
    private Coroutine pressureTimerCoroutine;
    private Coroutine replyCoroutine;
    private Coroutine typewriterCoroutine;
    private int timeoutCounter = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (screenRoot != null) screenRoot.SetActive(false);
    }

    public void StartCallDialogue(string callerName, Sprite avatar)
    {
        if (string.IsNullOrEmpty(callerName))
        {
            callerName = "Seraphine"; // Fallback safety
        }

        activeConvo = LoadConversation(callerName);
        timeoutCounter = 0;

        if (screenRoot != null) screenRoot.SetActive(true);
        if (txtPartnerSpeakerName != null) txtPartnerSpeakerName.text = activeConvo.callerName;
        if (imgPartnerAvatar != null && avatar != null) imgPartnerAvatar.sprite = avatar;

        ClearFeed();
        ClearChoices();
        ResetTimerUI();

        string startNode = !string.IsNullOrEmpty(activeConvo.startNodeId) ? activeConvo.startNodeId : "start";
        GoToNode(startNode, isInitial: true);
    }

    private void OnPlayerChoiceSelected(string choiceText, string nextNodeId)
    {
        StopTimer();
        timeoutCounter = 0;

        // 1. Post player's message immediately
        AddPlayerBubble(choiceText);

        // 2. Clear options while partner is speaking
        ClearChoices();
        ResetTimerUI();

        // 3. Trigger partner reply routine
        if (replyCoroutine != null) StopCoroutine(replyCoroutine);
        replyCoroutine = StartCoroutine(DelayedPartnerReplyRoutine(nextNodeId));
    }

    private IEnumerator DelayedPartnerReplyRoutine(string targetNodeId)
    {
        yield return new WaitForSeconds(partnerReplyDelay);
        GoToNode(targetNodeId, isInitial: false);
    }

    public void GoToNode(string nodeId, bool isInitial = false)
    {
        if (nodeId == "END_CALL" || string.IsNullOrEmpty(nodeId))
        {
            EndCallDialogue();
            return;
        }

        currentNode = activeConvo?.nodes.Find(n => n.nodeId.Equals(nodeId, System.StringComparison.OrdinalIgnoreCase));
        if (currentNode == null)
        {
            EndCallDialogue();
            return;
        }

        // Run typing animation for partner speech; choices & timer start upon completion
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterPartnerRoutine(currentNode.partnerDialogue, currentNode.choices));
    }

    private IEnumerator TypewriterPartnerRoutine(string fullText, List<VoiceCallChoice> choicesToDisplay)
    {
        // Prevent player choices or timer from running while she is speaking
        ClearChoices();
        ResetTimerUI();

        if (dialogueFeedContent == null || messageBubblePrefab == null) yield break;

        GameObject bubble = Instantiate(messageBubblePrefab, dialogueFeedContent);
        DirectMessageUI msgUI = bubble.GetComponent<DirectMessageUI>();
        
        // Initialize partner bubble styling with blank text
        if (msgUI != null) msgUI.Setup("", false);

        TextMeshProUGUI textComp = bubble.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null) textComp.text = "";

        float delayPerChar = 1f / Mathf.Max(1f, charactersPerSecond);
        string currentText = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            currentText += fullText[i];

            if (textComp != null) textComp.text = currentText;

            // Rebuild layout and snap scroll down as the text expands
            if (i % 5 == 0 || i == fullText.Length - 1)
            {
                Canvas.ForceUpdateCanvases();
                if (dialogueFeedContent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueFeedContent.GetComponent<RectTransform>());
                }
                if (dialogueScrollRect != null)
                {
                    dialogueScrollRect.verticalNormalizedPosition = 0f;
                }
            }

            yield return new WaitForSeconds(delayPerChar);
        }

        // Brief breath after she finishes speaking
        yield return new WaitForSeconds(0.25f);

        // Populate choices on the right
        if (choicesToDisplay != null && choicesToDisplay.Count > 0)
        {
            PopulateChoices(choicesToDisplay);
            StartCountdownTimer();
        }
        else
        {
            // If no choices remain, only render the hang up option
            PopulateChoices(new List<VoiceCallChoice>());
            StartCountdownTimer();
        }

        typewriterCoroutine = null;
    }

    private void AddPlayerBubble(string text)
    {
        if (dialogueFeedContent == null || messageBubblePrefab == null) return;

        GameObject bubble = Instantiate(messageBubblePrefab, dialogueFeedContent);
        DirectMessageUI msgUI = bubble.GetComponent<DirectMessageUI>();

        if (msgUI != null)
        {
            msgUI.Setup(text, true);
        }
        else
        {
            TextMeshProUGUI tmp = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        if (dialogueFeedContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueFeedContent.GetComponent<RectTransform>());
        }

        if (dialogueScrollRect != null)
        {
            dialogueScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void PopulateChoices(List<VoiceCallChoice> choices)
    {
        ClearChoices();
        if (choicesContainer == null || choiceButtonPrefab == null) return;

        int choiceLimit = Mathf.Min(choices.Count, 3);
        for (int i = 0; i < choiceLimit; i++)
        {
            CreateChoiceButton(choices[i].choiceText, choices[i].nextNodeId, isGoodbye: false);
        }

        // Permanent hang-up option
        CreateChoiceButton("I have to hang up now. Talk later.", "END_CALL", isGoodbye: true);
    }

    private void CreateChoiceButton(string label, string targetNodeId, bool isGoodbye)
    {
        GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
        TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.text = isGoodbye ? $"<color=#FF7675>{label}</color>" : label;
        }

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() => OnPlayerChoiceSelected(label, targetNodeId));
    }

    private void StartCountdownTimer()
    {
        if (pressureTimerCoroutine != null) StopCoroutine(pressureTimerCoroutine);
        pressureTimerCoroutine = StartCoroutine(ResponseCountdownRoutine());
    }

    private void StopTimer()
    {
        if (pressureTimerCoroutine != null)
        {
            StopCoroutine(pressureTimerCoroutine);
            pressureTimerCoroutine = null;
        }
    }

    private void ResetTimerUI()
    {
        if (timerBar != null) timerBar.value = 1f;
        if (txtTimerCounter != null) txtTimerCounter.text = responseTimeout + "s";
    }

    private IEnumerator ResponseCountdownRoutine()
    {
        float timer = responseTimeout;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / responseTimeout);

            if (timerBar != null) timerBar.value = normalized;
            if (txtTimerCounter != null) txtTimerCounter.text = Mathf.CeilToInt(timer) + "s";

            yield return null;
        }

        OnSilenceTimeout();
    }

    private void OnSilenceTimeout()
    {
        if (activeConvo == null || activeConvo.timeoutPrompts.Count == 0)
        {
            EndCallDialogue();
            return;
        }

        if (timeoutCounter < activeConvo.timeoutPrompts.Count)
        {
            string prompt = activeConvo.timeoutPrompts[timeoutCounter];
            timeoutCounter++;

            // Partner speaks silence prompt using typewriter effect, choices re-open afterwards
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterPartnerRoutine(prompt, currentNode?.choices));
        }
        else
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterPartnerRoutine("...*click* (She hung up)", null));
            StartCoroutine(DelayedHangup(2.0f));
        }
    }

    private IEnumerator DelayedHangup(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndCallDialogue();
    }

    public void EndCallDialogue()
    {
        StopTimer();
        if (replyCoroutine != null) StopCoroutine(replyCoroutine);
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (screenRoot != null) screenRoot.SetActive(false);

        if (VoiceCallOverlayController.Instance != null)
        {
            VoiceCallOverlayController.Instance.EndOrRejectCall();
        }
    }

    private void ClearChoices()
    {
        if (choicesContainer == null) return;
        for (int i = choicesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesContainer.GetChild(i).gameObject);
        }
    }

    private void ClearFeed()
    {
        if (dialogueFeedContent == null) return;
        for (int i = dialogueFeedContent.childCount - 1; i >= 0; i--)
        {
            Destroy(dialogueFeedContent.GetChild(i).gameObject);
        }
    }

    private VoiceCallConversation LoadConversation(string callerName)
    {
        string cleanName = callerName.Replace("OY_", "").Trim();

        // 1. Try loading dedicated file: Resources/CallDialogues/{cleanName}Call
        TextAsset jsonAsset = Resources.Load<TextAsset>("CallDialogues/" + cleanName + "Call");
        
        // 2. Fallback check for without "Call" suffix
        if (jsonAsset == null)
        {
            jsonAsset = Resources.Load<TextAsset>("CallDialogues/" + cleanName);
        }

        if (jsonAsset != null && !string.IsNullOrEmpty(jsonAsset.text))
        {
            VoiceCallConversation convo = JsonUtility.FromJson<VoiceCallConversation>(jsonAsset.text);
            
            string pName = "Player";
            if (GameManager.Instance != null && GameManager.Instance.currentUser != null && !string.IsNullOrEmpty(GameManager.Instance.currentUser.playerName))
            {
                pName = GameManager.Instance.currentUser.playerName;
            }

            foreach (var node in convo.nodes)
            {
                node.partnerDialogue = node.partnerDialogue.Replace("{PlayerName}", pName);
            }
            for (int i = 0; i < convo.timeoutPrompts.Count; i++)
            {
                convo.timeoutPrompts[i] = convo.timeoutPrompts[i].Replace("{PlayerName}", pName);
            }

            return convo;
        }

        Debug.LogError($"[VoiceCallDialogueUI] FAILED to load CallDialogues/{cleanName}Call.json! Check file name in Resources/CallDialogues/.");
        
        // Extended multi-branch fallback so conversations never drop instantly
        VoiceCallConversation fallback = new VoiceCallConversation();
        fallback.callerName = cleanName;
        fallback.startNodeId = "start";
        fallback.nodes.Add(new VoiceCallNode
        {
            nodeId = "start",
            partnerDialogue = $"{cleanName}: Hey! You actually picked up. How are you holding up right now?",
            choices = new List<VoiceCallChoice>
            {
                new VoiceCallChoice { choiceText = "Doing good. Glad to hear your voice.", nextNodeId = "chat_good" },
                new VoiceCallChoice { choiceText = "A bit stressed with everything going on.", nextNodeId = "chat_busy" }
            }
        });
        fallback.nodes.Add(new VoiceCallNode
        {
            nodeId = "chat_good",
            partnerDialogue = $"{cleanName}: Glad to hear. I was thinking about our conversation earlier today.",
            choices = new List<VoiceCallChoice>
            {
                new VoiceCallChoice { choiceText = "Tell me what's on your mind.", nextNodeId = "chat_more" },
                new VoiceCallChoice { choiceText = "I have to get back to work soon.", nextNodeId = "END_CALL" }
            }
        });
        fallback.nodes.Add(new VoiceCallNode
        {
            nodeId = "chat_busy",
            partnerDialogue = $"{cleanName}: Take a deep breath. Don't let the noise get to you. I'm here if you need to talk.",
            choices = new List<VoiceCallChoice>
            {
                new VoiceCallChoice { choiceText = "Thanks, that means a lot.", nextNodeId = "chat_more" },
                new VoiceCallChoice { choiceText = "I'll talk to you later.", nextNodeId = "END_CALL" }
            }
        });
        fallback.nodes.Add(new VoiceCallNode
        {
            nodeId = "chat_more",
            partnerDialogue = $"{cleanName}: Let's catch up properly once things quiet down. Don't stay up too late tonight, okay?",
            choices = new List<VoiceCallChoice>
            {
                new VoiceCallChoice { choiceText = "You too. Goodnight.", nextNodeId = "END_CALL" }
            }
        });
        return fallback;
    }
}
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
    
    [Header("UI To Hide During Call")]
    [SerializeField] private GameObject normalChatAppUI; 

    [Header("Left Side - Header & Scroll Feed")]
    [SerializeField] private Image imgPartnerAvatar;
    [SerializeField] private TextMeshProUGUI txtPartnerSpeakerName;
    [SerializeField] private ScrollRect dialogueScrollRect;
    [SerializeField] private Transform dialogueFeedContent;
    [SerializeField] private GameObject messageBubblePrefab;

    [Header("Right Side - Choices & Pressure Timer")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Slider timerBar;
    [SerializeField] private TextMeshProUGUI txtTimerCounter;

    [Header("Speech & Timing Settings")]
    [SerializeField] private float charactersPerSecond = 35f;
    [SerializeField] private float partnerReplyDelay = 0.8f;   
    [SerializeField] private float responseTimeout = 10f;      

    [Header("Audio Settings")]
    [SerializeField] private AudioSource callDialogueAudioSource;
    [SerializeField] private AudioClip choiceClickSound;
    [SerializeField] private AudioClip[] typewriterSounds;
    
    [Header("Typewriter Audio Tweaks")]
    [SerializeField, Range(1, 10)] private int playSoundEveryXLetters = 3; 
    [SerializeField, Range(0.1f, 1f)] private float typingVolume = 0.4f;

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

    public void StartCallDialogue(string callerName, Sprite avatar, string entryNodeId = "")    
    {
        if (string.IsNullOrEmpty(callerName))
        {
            callerName = "Seraphine";
        }

        activeConvo = LoadConversation(callerName);
        timeoutCounter = 0;

        if (normalChatAppUI != null) normalChatAppUI.SetActive(false);

        if (screenRoot != null) screenRoot.SetActive(true);
        if (txtPartnerSpeakerName != null) txtPartnerSpeakerName.text = activeConvo.callerName;
        if (imgPartnerAvatar != null && avatar != null) imgPartnerAvatar.sprite = avatar;

        ClearFeed();
        ClearChoices();
        ResetTimerUI();

        string targetNode = !string.IsNullOrEmpty(entryNodeId) ? entryNodeId : activeConvo.startNodeId;
        if (string.IsNullOrEmpty(targetNode)) targetNode = "start";

        GoToNode(targetNode, isInitial: true);
    }

    private void OnPlayerChoiceSelected(string choiceText, string nextNodeId)
    {
        if (callDialogueAudioSource != null && choiceClickSound != null)
        {
            callDialogueAudioSource.pitch = 1f; 
            callDialogueAudioSource.PlayOneShot(choiceClickSound);
        }

        StopTimer();
        timeoutCounter = 0;

        AddPlayerBubble(choiceText);
        ClearChoices();
        ResetTimerUI();

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
        // NEW: Check if this is the literal end of the story
        if (nodeId == "END_ROUTE")
        {
            EndRouteCompletely();
            return;
        }

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

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterPartnerRoutine(currentNode.partnerDialogue, currentNode.choices));
    }

    private IEnumerator TypewriterPartnerRoutine(string fullText, List<VoiceCallChoice> choicesToDisplay)
    {
        ClearChoices();
        ResetTimerUI();

        if (dialogueFeedContent == null || messageBubblePrefab == null) yield break;

        GameObject bubble = Instantiate(messageBubblePrefab, dialogueFeedContent);
        DirectMessageUI msgUI = bubble.GetComponent<DirectMessageUI>();
        
        if (msgUI != null) msgUI.Setup("", false);

        TextMeshProUGUI textComp = bubble.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null) textComp.text = "";

        float delayPerChar = 1f / Mathf.Max(1f, charactersPerSecond);
        string currentText = "";
        int letterCount = 0; 

        for (int i = 0; i < fullText.Length; i++)
        {
            currentText += fullText[i];
            if (textComp != null) textComp.text = currentText;

            if (fullText[i] != ' ')
            {
                letterCount++;
                if (letterCount % playSoundEveryXLetters == 0 && callDialogueAudioSource != null && typewriterSounds.Length > 0)
                {
                    AudioClip randomClip = typewriterSounds[Random.Range(0, typewriterSounds.Length)];
                    callDialogueAudioSource.pitch = Random.Range(0.92f, 1.08f);
                    callDialogueAudioSource.PlayOneShot(randomClip, typingVolume);
                }
            }

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

        if (callDialogueAudioSource != null) callDialogueAudioSource.pitch = 1f;

        yield return new WaitForSeconds(0.25f);

        // NEW: Properly differentiates between choices, Game Over, and silence hangup
        if (choicesToDisplay != null && choicesToDisplay.Count > 0)
        {
            PopulateChoices(choicesToDisplay);
            StartCountdownTimer();
        }
        else if (choicesToDisplay != null && choicesToDisplay.Count == 0)
        {
            // This is a Terminal Node (End of Story). Generates a red finish button.
            ClearChoices();
            CreateChoiceButton("Finish Story", "END_ROUTE", isGoodbye: true);
        }
        else
        {
            // Forced silence hang-up. Wait for DelayedHangup.
            ClearChoices();
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

        if (normalChatAppUI != null) normalChatAppUI.SetActive(true);

        if (VoiceCallOverlayController.Instance != null)
        {
            VoiceCallOverlayController.Instance.EndOrRejectCall();
        }
    }

    // NEW: Actual Game Over / Route Completed
    public void EndRouteCompletely()
    {
        StopTimer();
        if (replyCoroutine != null) StopCoroutine(replyCoroutine);
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (screenRoot != null) screenRoot.SetActive(false);

        // FIX: We deliberately do NOT turn the normalChatAppUI back on! The loop is broken.
        
        if (VoiceCallOverlayController.Instance != null)
        {
            VoiceCallOverlayController.Instance.EndOrRejectCall();
        }
        
        Debug.Log("Route Completed Successfully!");
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
        TextAsset jsonAsset = Resources.Load<TextAsset>("CallDialogues/" + cleanName + "Call");
        
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

        Debug.LogError($"[VoiceCallDialogueUI] FAILED to load CallDialogues/{cleanName}Call.json!");
        return new VoiceCallConversation();
    }
}
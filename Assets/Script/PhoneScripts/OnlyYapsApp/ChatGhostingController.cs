using System.Collections;
using UnityEngine;
using TMPro;

public class ChatGhostingController : MonoBehaviour
{
    public static ChatGhostingController Instance { get; private set; }

    [Header("UI Status Elements")]
    [Tooltip("Text displaying the online/typing status in your chat header")]
    [SerializeField] private TextMeshProUGUI statusIndicatorText;

    [Tooltip("The three-dot typing indicator GameObject")]
    [SerializeField] private GameObject typingBubble;

    [Tooltip("The parent container holding the player's choice buttons")]
    [SerializeField] private RectTransform choiceContainer;

    [Header("Timings")]
    [SerializeField] private float readDelay = 1.2f;
    [SerializeField] private float typingDuration = 4.0f;
    [SerializeField] private float offlineDelay = 1.5f;

    private Coroutine ghostCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Listen to events triggered from your dialogue JSON
        DialogueEventManager.Register("GHOST_SEQUENCE_START", OnGhostSequenceTriggered);
        DialogueEventManager.Register("GET_GHOSTED", OnGetGhostedTriggered);
    }

    private void OnDisable()
    {
        DialogueEventManager.Unregister("GHOST_SEQUENCE_START", OnGhostSequenceTriggered);
        DialogueEventManager.Unregister("GET_GHOSTED", OnGetGhostedTriggered);
    }

    private void OnGhostSequenceTriggered(string characterName)
    {
        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);
        ghostCoroutine = StartCoroutine(GhostTypingRoutine(characterName));
    }

    private void OnGetGhostedTriggered(string characterName)
    {
        if (ghostCoroutine != null) StopCoroutine(ghostCoroutine);

        if (typingBubble != null) typingBubble.SetActive(false);
        if (choiceContainer != null) choiceContainer.gameObject.SetActive(false);

        if (statusIndicatorText != null)
        {
            statusIndicatorText.text = "Active 2h ago";
            statusIndicatorText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }

        // Notify TaskManager if present
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CompleteTaskByEvent("GET_GHOSTED");
        }
    }

    private IEnumerator GhostTypingRoutine(string characterName)
    {
        // 1. Message status turns to Read
        yield return new WaitForSeconds(readDelay);
        if (statusIndicatorText != null)
        {
            statusIndicatorText.text = "Read just now";
            statusIndicatorText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        // 2. Typing indicator appears
        yield return new WaitForSeconds(1.0f);
        if (typingBubble != null) typingBubble.SetActive(true);
        if (statusIndicatorText != null)
        {
            statusIndicatorText.text = $"{characterName} is typing...";
        }

        // 3. Daisy reconsiders and stops typing
        yield return new WaitForSeconds(typingDuration);
        if (typingBubble != null) typingBubble.SetActive(false);

        // 4. Status switches to playing games instead of replying
        yield return new WaitForSeconds(offlineDelay);
        if (statusIndicatorText != null)
        {
            statusIndicatorText.text = "Playing Valiant Protocol";
            statusIndicatorText.color = new Color(0.85f, 0.35f, 0.35f, 1f);
        }

        ghostCoroutine = null;
    }

    /// <summary>
    /// Call this whenever switching characters or loading a fresh chat
    /// </summary>
    public void ResetChatStatus(string defaultText = "Active now")
    {
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
            ghostCoroutine = null;
        }

        if (typingBubble != null) typingBubble.SetActive(false);
        if (choiceContainer != null) choiceContainer.gameObject.SetActive(true);

        if (statusIndicatorText != null)
        {
            statusIndicatorText.text = defaultText;
            statusIndicatorText.color = new Color(0.2f, 0.85f, 0.3f, 1f); // Online Green
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DirectMessageUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private HorizontalLayoutGroup rowLayoutGroup;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private TextMeshProUGUI txtMessageBody;
    [SerializeField] private LayoutElement textLayoutElement;

    [Header("Settings")]
    [SerializeField] private float maxBubbleWidth = 240f;
    [SerializeField] private Color partnerBubbleColor = new Color(0.35f, 0.85f, 0.45f); // Green
    [SerializeField] private Color playerBubbleColor = new Color(0.25f, 0.55f, 0.95f);  // Blue

    [Header("Audio Settings")]
    [SerializeField] private AudioSource messageAudioSource;
    [SerializeField] private AudioClip incomingMessageSound;

    public void Setup(string message, bool isPlayer = false)
    {
        // 1. Play sound
        bool isCallActive = VoiceCallOverlayController.Instance != null && VoiceCallOverlayController.Instance.gameObject.activeInHierarchy;
        if (!isPlayer && !isCallActive && messageAudioSource != null && incomingMessageSound != null)
        {
            messageAudioSource.PlayOneShot(incomingMessageSound);
        }

        // 2. Text
        if (txtMessageBody == null) txtMessageBody = GetComponentInChildren<TextMeshProUGUI>();
        if (txtMessageBody != null) txtMessageBody.text = message;

        // 3. Bubble Color & Flipping
        if (bubbleImage != null)
        {
            bubbleImage.color = isPlayer ? playerBubbleColor : partnerBubbleColor;
            bubbleImage.transform.localScale = new Vector3(isPlayer ? -1 : 1, 1, 1);
        }

        // 4. Row Layout & Alignment
        if (rowLayoutGroup == null) rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (rowLayoutGroup != null)
        {
            rowLayoutGroup.childControlWidth = false;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;
            rowLayoutGroup.childForceExpandHeight = false;
            rowLayoutGroup.childAlignment = isPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }

        // 5. Width constraint
        if (textLayoutElement == null && txtMessageBody != null)
            textLayoutElement = txtMessageBody.GetComponent<LayoutElement>();

        if (textLayoutElement != null && txtMessageBody != null)
        {
            Vector2 preferred = txtMessageBody.GetPreferredValues(message, maxBubbleWidth, float.PositiveInfinity);
            textLayoutElement.preferredWidth = preferred.x > maxBubbleWidth ? maxBubbleWidth : -1;
        }

        // 6. Rebuild Layout
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        
        // 7. FIX TEXT ALIGNMENT OVERLAP
        if (txtMessageBody != null)
        {
            if (isPlayer)
            {
                 // Un-flip the text so it is readable
                 txtMessageBody.transform.localScale = new Vector3(-1, 1, 1);
                 // Force text to align to the left side (Changed from TopRight)
                 txtMessageBody.alignment = TextAlignmentOptions.TopLeft; 
            }
            else
            {
                 txtMessageBody.transform.localScale = new Vector3(1, 1, 1);
                 // Force text to align to the left side
                 txtMessageBody.alignment = TextAlignmentOptions.TopLeft;
            }
        }
    }
}
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

    public void Setup(string message, bool isPlayer = false)
    {
        // 1. Text
        if (txtMessageBody == null)
            txtMessageBody = GetComponentInChildren<TextMeshProUGUI>();
        
        if (txtMessageBody != null)
            txtMessageBody.text = message;

        // 2. Bubble Color
        if (bubbleImage != null)
            bubbleImage.color = isPlayer ? playerBubbleColor : partnerBubbleColor;

        // 3. Row Layout & Alignment
        if (rowLayoutGroup == null)
            rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();

        if (rowLayoutGroup != null)
        {
            rowLayoutGroup.childControlWidth = false;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;
            rowLayoutGroup.childForceExpandHeight = false;
            
            // Left for partner, Right for player
            rowLayoutGroup.childAlignment = isPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }

        // 4. Width constraint
        if (textLayoutElement == null && txtMessageBody != null)
            textLayoutElement = txtMessageBody.GetComponent<LayoutElement>();

        if (textLayoutElement != null && txtMessageBody != null)
        {
            Vector2 preferred = txtMessageBody.GetPreferredValues(message, maxBubbleWidth, float.PositiveInfinity);
            textLayoutElement.preferredWidth = preferred.x > maxBubbleWidth ? maxBubbleWidth : -1;
        }

        // 5. Rebuild
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
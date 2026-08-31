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
    [SerializeField] private float maxBubbleWidth = 280f;
    [SerializeField] private Color partnerBubbleColor = new Color(0.35f, 0.85f, 0.45f); // Green
    [SerializeField] private Color playerBubbleColor = new Color(0.25f, 0.55f, 0.95f);  // Blue

    public void Setup(string message, bool isPlayer = false)
    {
        // 1. Set text
        if (txtMessageBody == null)
            txtMessageBody = GetComponentInChildren<TextMeshProUGUI>();
        txtMessageBody.text = message;

        // 2. Set bubble color
        if (bubbleImage != null)
            bubbleImage.color = isPlayer ? playerBubbleColor : partnerBubbleColor;

        // 3. Align row: Left for partner, Right for player
        if (rowLayoutGroup == null)
            rowLayoutGroup = GetComponent<HorizontalLayoutGroup>();

        if (rowLayoutGroup != null)
        {
            rowLayoutGroup.childAlignment = isPlayer ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        }

        // 4. Dynamic width constraint
        if (textLayoutElement == null && txtMessageBody != null)
            textLayoutElement = txtMessageBody.GetComponent<LayoutElement>();

        if (textLayoutElement != null && txtMessageBody != null)
        {
            Vector2 preferredValues = txtMessageBody.GetPreferredValues(message, float.PositiveInfinity, float.PositiveInfinity);
            textLayoutElement.preferredWidth = preferredValues.x > maxBubbleWidth ? maxBubbleWidth : -1;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}
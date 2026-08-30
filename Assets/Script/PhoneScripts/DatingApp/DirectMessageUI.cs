using UnityEngine;
using TMPro;

public class DirectMessageUI : MonoBehaviour
{
    public TextMeshProUGUI txtMessageBody;

    public void Setup(string message, bool isPlayer)
    {
        if (txtMessageBody != null)
        {
            txtMessageBody.text = message;
            // Align text right if it's from the player, left if from the partner
            txtMessageBody.alignment = isPlayer ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            txtMessageBody.color = isPlayer ? new Color(0.2f, 0.7f, 1f) : Color.white;
        }
    }
}
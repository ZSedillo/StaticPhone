using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image avatarImage;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLastMessage;
    public TextMeshProUGUI txtTimestamp;
    public Button btnOpenChat;

    public void Setup(string partnerName, string lastMessage, string timestamp, Sprite avatarSprite = null, System.Action onClick = null)
    {
        if (txtName != null) 
            txtName.text = partnerName;

        if (txtLastMessage != null)
        {
            int maxCharLength = 22; // Limit: 22 characters + "..."
            if (!string.IsNullOrEmpty(lastMessage) && lastMessage.Length > maxCharLength)
            {
                txtLastMessage.text = lastMessage.Substring(0, maxCharLength) + "...";
            }
            else
            {
                txtLastMessage.text = lastMessage;
            }
        }

        if (txtTimestamp != null) 
            txtTimestamp.text = timestamp;

        if (avatarImage != null && avatarSprite != null)
            avatarImage.sprite = avatarSprite;

        // Auto-detect Button component if not manually assigned in inspector
        if (btnOpenChat == null)
            btnOpenChat = GetComponent<Button>();

        if (btnOpenChat != null)
        {
            btnOpenChat.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                btnOpenChat.onClick.AddListener(() => onClick.Invoke());
            }
        }
    }
}
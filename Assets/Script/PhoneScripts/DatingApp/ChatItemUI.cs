using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image avatarImage;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtLastMessage;
    public TextMeshProUGUI txtTimestamp;
    public Button btnOpenChat;

    [Header("Box Border-Radius (Sliced UISprite Curve)")]
    [Tooltip("Lower number = rounder corners (e.g., 0.3). Higher number = sharper corners (e.g., 2.0).")]
    [Range(0.1f, 3f)]
    public float cornerCurveMultiplier = 0.4f;

    [Header("Dating App Theme (Yellow App)")]
    [Tooltip("Optional custom sprite. Leave empty to use the default rounded UISprite.")]
    public Sprite datingBgSprite;
    public Color datingBgColor = new Color(1f, 0.92f, 0.35f, 1f); // Yellow
    public Color datingNameColor = Color.black;
    public Color datingMessageColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color datingTimestampColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("OnlyYaps Theme (Blue App)")]
    [Tooltip("Optional custom sprite. Leave empty to use the default rounded UISprite.")]
    public Sprite onlyYapsBgSprite;
    public Color onlyYapsBgColor = new Color(0.14f, 0.20f, 0.36f, 1f); // Dark Blue
    public Color onlyYapsNameColor = Color.white;
    public Color onlyYapsMessageColor = new Color(0.8f, 0.85f, 0.95f, 1f);
    public Color onlyYapsTimestampColor = new Color(0.65f, 0.75f, 0.9f, 1f);

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }

    private void Start()
    {
        ApplyAppTheme();
    }

    private void OnEnable()
    {
        ApplyAppTheme();
    }

    public void Setup(string partnerName, string lastMessage, string timestamp, Sprite avatarSprite = null, System.Action onClick = null)
    {
        ApplyAppTheme();

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

    public void ApplyAppTheme()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        bool isOnlyYaps = IsInsideOnlyYaps();

        if (backgroundImage != null)
        {
            Sprite targetSprite = isOnlyYaps ? onlyYapsBgSprite : datingBgSprite;
            if (targetSprite != null)
            {
                backgroundImage.sprite = targetSprite;
            }

            backgroundImage.color = isOnlyYaps ? onlyYapsBgColor : datingBgColor;

            // Apply border-radius curve if the Image is set to Sliced
            if (backgroundImage.type == Image.Type.Sliced)
            {
                backgroundImage.pixelsPerUnitMultiplier = cornerCurveMultiplier;
                backgroundImage.SetVerticesDirty();
            }
        }

        if (txtName != null)
            txtName.color = isOnlyYaps ? onlyYapsNameColor : datingNameColor;

        if (txtLastMessage != null)
            txtLastMessage.color = isOnlyYaps ? onlyYapsMessageColor : datingMessageColor;

        if (txtTimestamp != null)
            txtTimestamp.color = isOnlyYaps ? onlyYapsTimestampColor : datingTimestampColor;
    }

    private bool IsInsideOnlyYaps()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.IndexOf("OnlyYaps", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }
        return false;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceOptionButtonUI : MonoBehaviour
{
    [Header("UI References (Auto-Detected if left empty)")]
    public Image backgroundImage;
    public TMP_Text buttonText;

    [Header("Dating App Theme (Yellow App)")]
    [Tooltip("Leave empty to keep the default UISprite")]
    public Sprite datingBgSprite;
    [Tooltip("Set to White if using a custom pre-colored Sprite!")]
    public Color datingBgColor = new Color(1f, 0.85f, 0.2f, 1f); // Yellow
    public Color datingTextColor = Color.black;

    [Header("OnlyYaps Theme (Blue App)")]
    [Tooltip("Leave empty to keep the default UISprite")]
    public Sprite onlyYapsBgSprite;
    [Tooltip("Set to White if using a custom pre-colored Sprite!")]
    public Color onlyYapsBgColor = new Color(0.15f, 0.22f, 0.38f, 1f); // Dark Blue
    public Color onlyYapsTextColor = Color.white;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Start()
    {
        ApplyTheme();
    }

    private void OnEnable()
    {
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        bool isOnlyYaps = IsInsideOnlyYaps();

        if (backgroundImage != null)
        {
            Sprite targetSprite = isOnlyYaps ? onlyYapsBgSprite : datingBgSprite;
            if (targetSprite != null)
            {
                backgroundImage.sprite = targetSprite;
            }

            backgroundImage.color = isOnlyYaps ? onlyYapsBgColor : datingBgColor;
        }

        if (buttonText != null)
        {
            buttonText.color = isOnlyYaps ? onlyYapsTextColor : datingTextColor;
        }
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
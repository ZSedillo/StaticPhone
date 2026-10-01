using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceOptionButtonUI : MonoBehaviour
{
    [Header("UI References (Auto-Detected if left empty)")]
    public Image backgroundImage;
    public TMP_Text buttonText;

    [Header("Dating App Theme (Yellow App)")]
    public Sprite datingBgSprite;
    public Color datingBgColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color datingTextColor = Color.black;

    [Header("OnlyYaps Theme (Blue App)")]
    public Sprite onlyYapsBgSprite;
    public Color onlyYapsBgColor = new Color(0.15f, 0.22f, 0.38f, 1f);
    public Color onlyYapsTextColor = Color.white;

    private LayoutElement layoutElement;
    private RectTransform rectTransform;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>(true);

        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        ApplyTheme();
        AdjustHeightToFitText();
    }

    private void OnEnable()
    {
        ApplyTheme();
        AdjustHeightToFitText();
    }

    private void LateUpdate()
    {
        AdjustHeightToFitText();
    }

    private void AdjustHeightToFitText()
    {
        if (buttonText == null || rectTransform == null) return;

        // Ensure text wraps inside the button width instead of overflowing out the sides/top
        buttonText.textWrappingMode = TextWrappingModes.Normal;
        buttonText.overflowMode = TextOverflowModes.Overflow;

        float neededHeight = Mathf.Max(45f, buttonText.preferredHeight + 20f);

        if (layoutElement != null && Mathf.Abs(layoutElement.preferredHeight - neededHeight) > 1f)
        {
            layoutElement.minHeight = neededHeight;
            layoutElement.preferredHeight = neededHeight;
        }

        if (Mathf.Abs(rectTransform.sizeDelta.y - neededHeight) > 1f)
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, neededHeight);
        }
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
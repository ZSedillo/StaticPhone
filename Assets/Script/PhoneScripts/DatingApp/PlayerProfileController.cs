using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerProfileController : MonoBehaviour
{
    [Header("Scroll Container to Resize")]
    [SerializeField] private RectTransform profileContentRect;
    [SerializeField] private float extraBottomPadding = 60f;

    [Header("View Mode References")]
    [SerializeField] private Image avatarDisplay;
    [SerializeField] private TextMeshProUGUI displayNameAge;
    [SerializeField] private TextMeshProUGUI displayPersonality;
    [SerializeField] private TextMeshProUGUI displayBio;

    public static Sprite CurrentAvatarSprite;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUserDataUpdated += RefreshViewDisplay;
            RefreshViewDisplay();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUserDataUpdated -= RefreshViewDisplay;
        }
    }

    public void RefreshViewDisplay()
    {
        UserProfileData user = GameManager.Instance != null ? GameManager.Instance.currentUser : new UserProfileData();

        if (displayNameAge != null) 
            displayNameAge.text = $"{user.playerName}, {user.playerAge}";

        if (displayPersonality != null) 
            displayPersonality.text = user.playerPersonality;

        if (displayBio != null) 
            displayBio.text = user.playerBio;

        if (avatarDisplay != null && CurrentAvatarSprite != null)
            avatarDisplay.sprite = CurrentAvatarSprite;

        StartCoroutine(RecalculateContentHeightNextFrame());
    }

    private IEnumerator RecalculateContentHeightNextFrame()
    {
        yield return null;

        if (profileContentRect == null || displayBio == null) yield break;

        float textHeight = displayBio.preferredHeight;
        displayBio.rectTransform.sizeDelta = new Vector2(displayBio.rectTransform.sizeDelta.x, textHeight);

        float bioLocalBottomY = Mathf.Abs(displayBio.transform.localPosition.y) + textHeight;
        float infoSectionTopOffset = 315f;
        float totalCalculatedHeight = infoSectionTopOffset + bioLocalBottomY + extraBottomPadding;
        
        // Uncomment if you want to enforce a minimum height
        // float finalHeight = Mathf.Max(totalCalculatedHeight, 750f);
        // profileContentRect.sizeDelta = new Vector2(profileContentRect.sizeDelta.x, finalHeight);
    }
}
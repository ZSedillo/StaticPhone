using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DatingCardController : MonoBehaviour
{
    [Header("Active Card Reference")]
    public RectTransform activeCardRect;

    [Header("Action Buttons")]
    public Button btnPass;
    public Button btnLike;

    [Header("RNG Visuals Pool")]
    public List<Sprite> profilePhotos = new List<Sprite>();

    [Header("Animation Settings")]
    public float flyDistance = 800f;
    public float animationDuration = 0.35f;
    public float rotationAmount = 20f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CharacterDialogueTree currentActiveProfile;

    void Start()
    {
        if (btnPass != null) btnPass.onClick.AddListener(OnPassClicked);
        if (btnLike != null) btnLike.onClick.AddListener(OnLikeClicked);

        // Ensure character dialogue trees are loaded into memory
        DialogueLoader.InitializeAllCharacters();

        if (activeCardRect != null)
        {
            currentActiveProfile = GetNextAvailableProfile();
            if (currentActiveProfile != null)
            {
                activeCardRect.gameObject.SetActive(true);
                PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);
            }
            else
            {
                activeCardRect.gameObject.SetActive(false);
            }
        }
    }

    public CharacterDialogueTree GetNextAvailableProfile()
    {
        // 1. Fetch all self-contained characters directly from the Dialogues folder
        List<CharacterDialogueTree> allCharacters = DialogueLoader.GetAllCharacters();
        if (allCharacters == null || allCharacters.Count == 0) return null;

        // 2. Filter out characters the player has already matched with
        List<CharacterDialogueTree> availableProfiles = allCharacters;

        if (GameManager.Instance != null && GameManager.Instance.activeChats != null)
        {
            availableProfiles = allCharacters
                .Where(c => !GameManager.Instance.activeChats.Any(chat => chat.contactName.Equals(c.girlName, System.StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // 3. Check if all characters have been matched
        if (availableProfiles.Count == 0)
        {
            Debug.Log("No more new profiles available. You matched with everyone!");
            return null;
        }

        // 4. Return a random unliked character
        return availableProfiles[Random.Range(0, availableProfiles.Count)];
    }

    public void OnPassClicked()
    {
        ProcessSwipe(false);
    }

    public void OnLikeClicked()
    {
        if (currentActiveProfile == null) return;

        // Add match directly with the specific traits authored in her JSON
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMatch(
                currentActiveProfile.girlName,
                currentActiveProfile.bio,
                currentActiveProfile.personality,
                currentActiveProfile.avatarIndex
            );
        }

        ProcessSwipe(true);
    }

    private void ProcessSwipe(bool isLike)
    {
        if (activeCardRect == null) return;

        // Duplicate the current card for fly-away animation
        GameObject flyingClone = Instantiate(activeCardRect.gameObject, activeCardRect.parent);
        RectTransform cloneRect = flyingClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = activeCardRect.anchoredPosition;
        cloneRect.localRotation = activeCardRect.localRotation;
        cloneRect.localScale = activeCardRect.localScale;
        cloneRect.SetAsLastSibling();

        StartCoroutine(AnimateFlyAndDestroy(cloneRect, isLike));

        // Pull the next unswiped character
        currentActiveProfile = GetNextAvailableProfile();

        if (currentActiveProfile != null)
        {
            activeCardRect.gameObject.SetActive(true);
            PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);
        }
        else
        {
            activeCardRect.gameObject.SetActive(false);
            if (btnLike != null) btnLike.interactable = false;
            if (btnPass != null) btnPass.interactable = false;
        }
    }

    private void PopulateCardUI(GameObject cardObj, CharacterDialogueTree profile)
    {
        if (profile == null) return;

        Image photo = cardObj.transform.Find("Contents/ProfilePhoto")?.GetComponent<Image>();
        TextMeshProUGUI nameAge = cardObj.transform.Find("Contents/NameAgeText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI bio = cardObj.transform.Find("Contents/BioDetailsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI personality = cardObj.transform.Find("Contents/PersonalityTypeText")?.GetComponent<TextMeshProUGUI>();

        if (photo != null && profilePhotos.Count > 0 && profile.avatarIndex >= 0 && profile.avatarIndex < profilePhotos.Count)
        {
            photo.sprite = profilePhotos[profile.avatarIndex];
        }

        if (nameAge != null) nameAge.text = $"{profile.girlName}, {profile.age}";
        if (bio != null) bio.text = profile.bio;
        if (personality != null) personality.text = profile.personality;
    }

    private IEnumerator AnimateFlyAndDestroy(RectTransform cardToFly, bool isLike)
    {
        Vector2 startPos = cardToFly.anchoredPosition;
        Vector2 targetPos = new Vector2(isLike ? flyDistance : -flyDistance, startPos.y - 60f);
        float targetRotZ = isLike ? -rotationAmount : rotationAmount;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = flyCurve.Evaluate(elapsed / animationDuration);

            if (cardToFly != null)
            {
                cardToFly.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                cardToFly.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetRotZ, t));
            }
            yield return null;
        }

        if (cardToFly != null)
        {
            Destroy(cardToFly.gameObject);
        }
    }
}
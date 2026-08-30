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

    private ProfilePoolData poolData;
    private GeneratedProfile currentActiveProfile;

    void Start()
    {
        if (btnPass != null) btnPass.onClick.AddListener(OnPassClicked);
        if (btnLike != null) btnLike.onClick.AddListener(OnLikeClicked);

        LoadJSONPools();

        if (activeCardRect != null)
        {
            currentActiveProfile = GenerateRandomProfile();
            PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);
        }
    }

    private void LoadJSONPools()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("ProfilePools");
        if (jsonTextAsset != null)
        {
            poolData = JsonUtility.FromJson<ProfilePoolData>(jsonTextAsset.text);
        }
        else
        {
            Debug.LogError("ProfilePools.json not found in Assets/Resources!");
        }
    }

    public GeneratedProfile GenerateRandomProfile()
    {
        if (poolData == null || poolData.names == null || poolData.names.Count == 0) return null;

        // 1. Get list of names that have NOT been liked yet
        List<string> availableNames = new List<string>(poolData.names);

        if (GameManager.Instance != null && GameManager.Instance.activeChats != null)
        {
            // Exclude anyone already matched/liked in GameManager
            availableNames = poolData.names
                .Where(name => !GameManager.Instance.activeChats.Any(chat => chat.contactName == name))
                .ToList();
        }

        // 2. If all characters have been liked, show empty / fallback
        if (availableNames.Count == 0)
        {
            Debug.Log("No more new profiles available. You matched with everyone!");
            return null;
        }

        // 3. Randomly generate from available names
        GeneratedProfile profile = new GeneratedProfile();
        profile.profileName = availableNames[Random.Range(0, availableNames.Count)];
        profile.age = Random.Range(poolData.minAge, poolData.maxAge + 1);
        profile.personalityType = poolData.personalityTypes[Random.Range(0, poolData.personalityTypes.Count)];
        profile.bio = poolData.bios[Random.Range(0, poolData.bios.Count)];
        profile.avatarIndex = profilePhotos.Count > 0 ? Random.Range(0, profilePhotos.Count) : 0;

        return profile;
    }

    public void OnPassClicked()
    {
        // Simply skip to the next profile. (The passed name stays in the pool)
        ProcessSwipe(false);
    }

    public void OnLikeClicked()
    {
        if (currentActiveProfile == null) return;

        // 1. Add matched profile to GameManager (this permanently marks them as liked)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMatch(
                currentActiveProfile.profileName,
                currentActiveProfile.bio,
                currentActiveProfile.personalityType,
                currentActiveProfile.avatarIndex,
                currentActiveProfile.bio
            );
        }

        // 2. Animate and cycle card
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

        // Generate next available profile
        currentActiveProfile = GenerateRandomProfile();

        if (currentActiveProfile != null)
        {
            activeCardRect.gameObject.SetActive(true);
            PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);
        }
        else
        {
            // Hide card and disable buttons if no profiles are left
            activeCardRect.gameObject.SetActive(false);
            if (btnLike != null) btnLike.interactable = false;
            if (btnPass != null) btnPass.interactable = false;
        }
    }

    private void PopulateCardUI(GameObject cardObj, GeneratedProfile profile)
    {
        if (profile == null) return;

        Image photo = cardObj.transform.Find("Contents/ProfilePhoto")?.GetComponent<Image>();
        TextMeshProUGUI nameAge = cardObj.transform.Find("Contents/NameAgeText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI bio = cardObj.transform.Find("Contents/BioDetailsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI personality = cardObj.transform.Find("Contents/PersonalityTypeText")?.GetComponent<TextMeshProUGUI>();

        if (photo != null && profilePhotos.Count > 0 && profile.avatarIndex < profilePhotos.Count)
        {
            photo.sprite = profilePhotos[profile.avatarIndex];
        }

        if (nameAge != null) nameAge.text = $"{profile.profileName}, {profile.age}";
        if (bio != null) bio.text = profile.bio;
        if (personality != null) personality.text = profile.personalityType;
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
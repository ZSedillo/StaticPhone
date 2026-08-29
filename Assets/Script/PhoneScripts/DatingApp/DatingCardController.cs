using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    void Start()
    {
        if (btnPass != null) btnPass.onClick.AddListener(OnPassClicked);
        if (btnLike != null) btnLike.onClick.AddListener(OnLikeClicked);

        LoadJSONPools();

        if (activeCardRect != null)
        {
            GeneratedProfile firstProfile = GenerateRandomProfile();
            PopulateCardUI(activeCardRect.gameObject, firstProfile);
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
        if (poolData == null) return null;

        GeneratedProfile profile = new GeneratedProfile();
        profile.profileName = poolData.names[Random.Range(0, poolData.names.Count)];
        profile.age = Random.Range(poolData.minAge, poolData.maxAge + 1);
        profile.personalityType = poolData.personalityTypes[Random.Range(0, poolData.personalityTypes.Count)];
        profile.bio = poolData.bios[Random.Range(0, poolData.bios.Count)];

        return profile;
    }

    public void OnPassClicked()
    {
        ProcessSwipe(false);
    }

    public void OnLikeClicked()
    {
        ProcessSwipe(true);
    }

    private void ProcessSwipe(bool isLike)
    {
        if (activeCardRect == null) return;

        // 1. Duplicate the current card to do the fly-away animation
        GameObject flyingClone = Instantiate(activeCardRect.gameObject, activeCardRect.parent);
        RectTransform cloneRect = flyingClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = activeCardRect.anchoredPosition;
        cloneRect.localRotation = activeCardRect.localRotation;
        cloneRect.localScale = activeCardRect.localScale;
        cloneRect.SetAsLastSibling(); // Renders on top while flying away

        // 2. Animate and destroy the clone
        StartCoroutine(AnimateFlyAndDestroy(cloneRect, isLike));

        // 3. Immediately refresh the stationary active card with a new RNG profile
        GeneratedProfile newProfile = GenerateRandomProfile();
        PopulateCardUI(activeCardRect.gameObject, newProfile);
    }

    private void PopulateCardUI(GameObject cardObj, GeneratedProfile profile)
    {
        if (profile == null) return;

        Image photo = cardObj.transform.Find("Contents/ProfilePhoto")?.GetComponent<Image>();
        TextMeshProUGUI nameAge = cardObj.transform.Find("Contents/NameAgeText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI bio = cardObj.transform.Find("Contents/BioDetailsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI personality = cardObj.transform.Find("Contents/PersonalityTypeText")?.GetComponent<TextMeshProUGUI>();

        if (photo != null && profilePhotos.Count > 0)
        {
            photo.sprite = profilePhotos[Random.Range(0, profilePhotos.Count)];
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
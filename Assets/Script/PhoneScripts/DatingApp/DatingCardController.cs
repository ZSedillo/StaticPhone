using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class DummyProfile
{
    public string profileName;
    public int age;
    public string personality;
    [TextArea(2, 3)] public string bio;
    public int avatarIndex;
}

[System.Serializable]
public class DummyProfileListWrapper
{
    public List<DummyProfile> profiles = new List<DummyProfile>();
}

public class DatingCardController : MonoBehaviour
{
    [Header("Active Card Reference")]
    public RectTransform activeCardRect;

    [Header("Scroll Reference")]
    public ScrollRect cardScrollRect; // Assign the ActiveCard ScrollRect here, or it will auto-find

    [Header("Action Buttons")]
    public Button btnPass;
    public Button btnLike;

    [Header("UI Text & Image References (Drag & Drop)")]
    public Image profilePhotoRef;
    public TextMeshProUGUI nameAgeTextRef;
    public TextMeshProUGUI bioTextRef;
    public TextMeshProUGUI personalityTextRef;

    [Header("RNG Visuals Pool")]
    public List<Sprite> profilePhotos = new List<Sprite>();

    [Header("Animation Settings")]
    public float flyDistance = 800f;
    public float animationDuration = 0.35f;
    public float rotationAmount = 20f;
    public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Match Delay Settings (Guaranteed Characters)")]
    [SerializeField] private float minMatchDelay = 5f;
    [SerializeField] private float maxMatchDelay = 10f;

    public class CardDeckItem
    {
        public string name;
        public int age;
        public string personality;
        public string bio;
        public int avatarIndex;
        public bool isGuaranteedMatch;
    }

    private List<DummyProfile> dummyProfiles = new List<DummyProfile>();
    private CardDeckItem currentActiveProfile;
    
    // Tracks swiped profiles so they don't repeat in the current session
    private static HashSet<string> seenProfileNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        seenProfileNames.Clear();

        if (GameManager.Instance != null && GameManager.Instance.activeChats != null)
        {
            foreach (var chat in GameManager.Instance.activeChats)
            {
                seenProfileNames.Add(chat.contactName);
            }
        }
    }

    private void Start()
    {
        if (btnPass != null) btnPass.onClick.AddListener(OnPassClicked);
        if (btnLike != null) btnLike.onClick.AddListener(OnLikeClicked);

        DialogueLoader.InitializeAllCharacters();
        LoadDummyProfilesFromJSON();

        RefreshCurrentCard();
    }

    private void OnEnable()
    {
        if (currentActiveProfile == null)
        {
            RefreshCurrentCard();
        }
    }

    public void RefreshCurrentCard()
    {
        if (activeCardRect == null) return;

        currentActiveProfile = GetNextAvailableProfile();
        if (currentActiveProfile != null)
        {
            activeCardRect.gameObject.SetActive(true);
            PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);

            // Reset scroll position back to the top for the new person
            if (cardScrollRect != null)
            {
                cardScrollRect.verticalNormalizedPosition = 1f;
            }
            else if (activeCardRect.TryGetComponent<ScrollRect>(out var localScroll))
            {
                localScroll.verticalNormalizedPosition = 1f;
            }

            if (btnLike != null) btnLike.interactable = true;
            if (btnPass != null) btnPass.interactable = true;
        }
        else
        {
            activeCardRect.gameObject.SetActive(false);
            if (btnLike != null) btnLike.interactable = false;
            if (btnPass != null) btnPass.interactable = false;
        }
    }

    private void LoadDummyProfilesFromJSON()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("DummyProfiles");
        if (jsonAsset != null)
        {
            DummyProfileListWrapper wrapper = JsonUtility.FromJson<DummyProfileListWrapper>(jsonAsset.text);
            if (wrapper != null && wrapper.profiles != null)
            {
                dummyProfiles = wrapper.profiles;
            }
        }
    }

    public CardDeckItem GetNextAvailableProfile()
    {
        List<CardDeckItem> deck = new List<CardDeckItem>();

        // 1. Real Cast
        List<CharacterDialogueTree> realGirls = DialogueLoader.GetAllCharacters();
        if (realGirls != null)
        {
            foreach (var girl in realGirls)
            {
                bool alreadySeen = seenProfileNames.Contains(girl.girlName);
                bool alreadyMatched = GameManager.Instance != null &&
                    GameManager.Instance.activeChats != null &&
                    GameManager.Instance.activeChats.Any(c => c.contactName.Equals(girl.girlName, System.StringComparison.OrdinalIgnoreCase));

                if (!alreadySeen && !alreadyMatched)
                {
                    deck.Add(new CardDeckItem
                    {
                        name = girl.girlName,
                        age = girl.age,
                        personality = girl.personality,
                        bio = girl.bio,
                        avatarIndex = girl.avatarIndex,
                        isGuaranteedMatch = true
                    });
                }
            }
        }

        // 2. Dummy Profiles
        if (dummyProfiles != null)
        {
            foreach (var dummy in dummyProfiles)
            {
                if (!seenProfileNames.Contains(dummy.profileName))
                {
                    deck.Add(new CardDeckItem
                    {
                        name = dummy.profileName,
                        age = dummy.age,
                        personality = dummy.personality,
                        bio = dummy.bio,
                        avatarIndex = dummy.avatarIndex,
                        isGuaranteedMatch = false
                    });
                }
            }
        }

        if (deck.Count == 0)
        {
            Debug.Log("[DatingDeck] All profiles have been swiped. No cards remaining.");
            return null;
        }

        return deck[Random.Range(0, deck.Count)];
    }

    public void OnPassClicked()
    {
        if (currentActiveProfile == null) return;

        seenProfileNames.Add(currentActiveProfile.name);
        ProcessSwipe(false);
    }

    public void OnLikeClicked()
    {
        if (currentActiveProfile == null) return;

        seenProfileNames.Add(currentActiveProfile.name);

        if (currentActiveProfile.isGuaranteedMatch)
        {
            CardDeckItem matchedGirl = currentActiveProfile;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(DelayedMatchRoutine(matchedGirl));
            }
            else
            {
                StartCoroutine(DelayedMatchRoutine(matchedGirl));
            }
        }

        ProcessSwipe(true);
    }

    private IEnumerator DelayedMatchRoutine(CardDeckItem girl)
    {
        float delay = Random.Range(minMatchDelay, maxMatchDelay);
        yield return new WaitForSeconds(delay);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddMatch(
                girl.name,
                girl.bio,
                girl.personality,
                girl.avatarIndex
            );
        }

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.TriggerNotification(
                girl.name,
                "It's a Match! Say hi to your new match! ✨",
                girl.avatarIndex
            );
        }
    }

    private void ProcessSwipe(bool isLike)
    {
        if (activeCardRect == null) return;

        GameObject flyingClone = Instantiate(activeCardRect.gameObject, activeCardRect.parent);
        RectTransform cloneRect = flyingClone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = activeCardRect.anchoredPosition;
        cloneRect.localRotation = activeCardRect.localRotation;
        cloneRect.localScale = activeCardRect.localScale;
        cloneRect.SetAsLastSibling();

        StartCoroutine(AnimateFlyAndDestroy(cloneRect, isLike));

        RefreshCurrentCard();
    }

    private void PopulateCardUI(GameObject cardObj, CardDeckItem profile)
    {
        if (profile == null || cardObj == null) return;

        Image photo = profilePhotoRef != null ? profilePhotoRef : cardObj.GetComponentsInChildren<Image>(true).FirstOrDefault(img => img.gameObject.name == "ProfilePhoto");
        TextMeshProUGUI nameAge = nameAgeTextRef != null ? nameAgeTextRef : cardObj.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "NameAgeText");
        TextMeshProUGUI bio = bioTextRef != null ? bioTextRef : cardObj.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "BioDetailsText");
        TextMeshProUGUI personality = personalityTextRef != null ? personalityTextRef : cardObj.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "PersonalityTypeText");

        if (photo != null)
        {
            if (profilePhotos != null && profilePhotos.Count > 0 && profile.avatarIndex >= 0 && profile.avatarIndex < profilePhotos.Count)
            {
                photo.sprite = profilePhotos[profile.avatarIndex];
                photo.color = Color.white;
            }
        }

        if (nameAge != null) nameAge.text = $"{profile.name}, {profile.age}";
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

    public static void ResetSeenProfiles()
    {
        seenProfileNames.Clear();
    }
}
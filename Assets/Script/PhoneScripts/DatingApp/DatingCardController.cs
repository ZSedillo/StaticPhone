using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public ScrollRect cardScrollRect;

    [Header("Action Buttons")]
    public Button btnPass;
    public Button btnLike;

    [Header("UI Text & Image References (Drag & Drop)")]
    public Image profilePhotoRef;
    public TextMeshProUGUI nameAgeTextRef;
    public TextMeshProUGUI bioTextRef;
    public TextMeshProUGUI personalityTextRef;

    [Header("Empty Queue / End of Deck Display")]
    [Tooltip("Optional sprite to show when out of profiles. Leave empty to hide the photo box.")]
    public Sprite emptyQueueSprite;
    public string emptyQueueTitle = "No More Profiles";
    public string emptyQueueSubtitle = "Area Queue Empty";
    [TextArea(2, 3)]
    public string emptyQueueBio = "You've swiped through everyone in your area! Check your Matches tab to chat with your connections.";

    [Header("8 Main Character Avatars (Characters_1 to Characters_8)")]
    public List<ChatsViewController.NamedAvatar> mainCharacterAvatars = new List<ChatsViewController.NamedAvatar>()
    {
        new ChatsViewController.NamedAvatar { characterName = "Andiva" },
        new ChatsViewController.NamedAvatar { characterName = "Daisy" },
        new ChatsViewController.NamedAvatar { characterName = "Evelyn" },
        new ChatsViewController.NamedAvatar { characterName = "Junia" },
        new ChatsViewController.NamedAvatar { characterName = "Masie" },
        new ChatsViewController.NamedAvatar { characterName = "Seraphine" },
        new ChatsViewController.NamedAvatar { characterName = "Trixie" },
        new ChatsViewController.NamedAvatar { characterName = "Zephyrine" }
    };

    [Header("5 Common Avatars for Random Non-Match Girls (Characters_9 to Characters_13)")]
    public List<Sprite> commonAvatars = new List<Sprite>();

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
    private ChatsViewController cachedChatsController;

    // Tracks swiped profiles so they don't repeat in the current session
    private static HashSet<string> seenProfileNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoPopulateSpritesFromSheet();
    }

    private void AutoPopulateSpritesFromSheet()
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Game Assets/Profile Pictures/Characters.png");
        if (allAssets == null || allAssets.Length == 0) return;

        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
        foreach (Object obj in allAssets)
        {
            if (obj is Sprite s)
            {
                spriteMap[s.name] = s;
            }
        }

        string[] mainNames = { "Andiva", "Daisy", "Evelyn", "Junia", "Masie", "Seraphine", "Trixie", "Zephyrine" };
        if (mainCharacterAvatars == null || mainCharacterAvatars.Count != 8)
        {
            mainCharacterAvatars = new List<ChatsViewController.NamedAvatar>();
            for (int i = 0; i < mainNames.Length; i++)
                mainCharacterAvatars.Add(new ChatsViewController.NamedAvatar { characterName = mainNames[i] });
        }

        // Characters_1 to Characters_8 -> 8 Main Girls
        for (int i = 0; i < 8; i++)
        {
            mainCharacterAvatars[i].characterName = mainNames[i];
            if (mainCharacterAvatars[i].avatarSprite == null && spriteMap.TryGetValue($"Characters_{i + 1}", out Sprite mainSprite))
            {
                mainCharacterAvatars[i].avatarSprite = mainSprite;
            }
        }

        // Characters_9 to Characters_13 -> 5 Common Girls
        if (commonAvatars == null) commonAvatars = new List<Sprite>();
        bool needsCommonFill = commonAvatars.Count < 5 || commonAvatars.Exists(s => s == null);
        if (needsCommonFill)
        {
            commonAvatars.Clear();
            for (int i = 9; i <= 13; i++)
            {
                if (spriteMap.TryGetValue($"Characters_{i}", out Sprite commonSprite))
                {
                    commonAvatars.Add(commonSprite);
                }
            }
        }
    }
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        AutoPopulateSpritesFromSheet();
#endif
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

    public Sprite ResolveCardSprite(CardDeckItem profile)
    {
        if (profile == null) return null;

        string cleanName = profile.name != null ? profile.name.Trim() : "";

        // 1. If she is one of the 8 Guaranteed Main Girls, ONLY check mainCharacterAvatars
        if (profile.isGuaranteedMatch)
        {
            if (mainCharacterAvatars != null)
            {
                for (int i = 0; i < mainCharacterAvatars.Count; i++)
                {
                    if (mainCharacterAvatars[i] != null &&
                        mainCharacterAvatars[i].avatarSprite != null &&
                        cleanName.Equals(mainCharacterAvatars[i].characterName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return mainCharacterAvatars[i].avatarSprite;
                    }
                }
            }
        }
        else
        {
            // 2. If she is a Random Non-Match Girl, ONLY use the 5 Common Avatars (Characters_9 to Characters_13)
            if (commonAvatars != null && commonAvatars.Count > 0)
            {
                int idx = profile.avatarIndex >= 0 ? profile.avatarIndex : Mathf.Abs(cleanName.ToLowerInvariant().GetHashCode());
                Sprite s = commonAvatars[idx % commonAvatars.Count];
                if (s != null) return s;
            }
        }

        // 3. Fallback to ChatsViewController
        if (cachedChatsController == null)
        {
            var allControllers = FindObjectsByType<ChatsViewController>(FindObjectsInactive.Include);
            foreach (var c in allControllers)
            {
                if (c != null)
                {
                    cachedChatsController = c;
                    Sprite found = c.GetAvatarForCharacter(cleanName, profile.avatarIndex);
                    if (found != null) return found;
                }
            }
        }
        else
        {
            Sprite found = cachedChatsController.GetAvatarForCharacter(cleanName, profile.avatarIndex);
            if (found != null) return found;
        }

        return null;
    }

    public void RefreshCurrentCard()
    {
        if (activeCardRect == null) return;

        currentActiveProfile = GetNextAvailableProfile();
        if (currentActiveProfile != null)
        {
            activeCardRect.gameObject.SetActive(true);
            PopulateCardUI(activeCardRect.gameObject, currentActiveProfile);

            if (cardScrollRect != null)
            {
                cardScrollRect.verticalNormalizedPosition = 1f;
            }
            else if (activeCardRect.TryGetComponent<ScrollRect>(out var localScroll))
            {
                localScroll.verticalNormalizedPosition = 1f;
            }

            if (btnLike != null)
            {
                btnLike.gameObject.SetActive(true);
                btnLike.interactable = true;
            }
            if (btnPass != null)
            {
                btnPass.gameObject.SetActive(true);
                btnPass.interactable = true;
            }
        }
        else
        {
            ShowEmptyQueueState();
        }
    }

    private void ShowEmptyQueueState()
    {
        if (activeCardRect == null) return;

        activeCardRect.gameObject.SetActive(true);

        Image photo = profilePhotoRef != null ? profilePhotoRef : activeCardRect.GetComponentsInChildren<Image>(true).FirstOrDefault(img => img.gameObject.name == "ProfilePhoto");
        TextMeshProUGUI nameAge = nameAgeTextRef != null ? nameAgeTextRef : activeCardRect.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "NameAgeText");
        TextMeshProUGUI bio = bioTextRef != null ? bioTextRef : activeCardRect.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "BioDetailsText");
        TextMeshProUGUI personality = personalityTextRef != null ? personalityTextRef : activeCardRect.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.gameObject.name == "PersonalityTypeText");

        if (photo != null)
        {
            if (emptyQueueSprite != null)
            {
                photo.gameObject.SetActive(true);
                photo.sprite = emptyQueueSprite;
                photo.color = Color.white;
            }
            else
            {
                // Hide the photo box cleanly so the text moves up
                photo.gameObject.SetActive(false);
            }
        }

        if (nameAge != null) nameAge.text = emptyQueueTitle;
        if (personality != null) personality.text = emptyQueueSubtitle;
        if (bio != null) bio.text = emptyQueueBio;

        if (cardScrollRect != null)
        {
            cardScrollRect.verticalNormalizedPosition = 1f;
        }

        // Hide and disable Pass / Like buttons since there is no one left to swipe
        if (btnLike != null)
        {
            btnLike.interactable = false;
            btnLike.gameObject.SetActive(false);
        }
        if (btnPass != null)
        {
            btnPass.interactable = false;
            btnPass.gameObject.SetActive(false);
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

        // 1. Real Cast (8 Guaranteed Main Girls)
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

        // 2. Dummy Profiles (Random Non-Match Girls)
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
            photo.gameObject.SetActive(true);
            Sprite resolvedSprite = ResolveCardSprite(profile);
            if (resolvedSprite != null)
            {
                photo.sprite = resolvedSprite;
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
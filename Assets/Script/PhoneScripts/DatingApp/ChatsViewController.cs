using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChatsViewController : MonoBehaviour
{
    [System.Serializable]
    public class NamedAvatar
    {
        public string characterName;
        public Sprite avatarSprite;
    }

    [Header("App Type")]
    [SerializeField] private bool isOnlyYapsView = false;

    [Header("UI References")]
    public Transform chatsContentParent;
    public GameObject chatItemPrefab;

    [Header("8 Main Character Avatars (Characters_1 to Characters_8)")]
    public List<NamedAvatar> mainCharacterAvatars = new List<NamedAvatar>()
    {
        new NamedAvatar { characterName = "Andiva" },
        new NamedAvatar { characterName = "Daisy" },
        new NamedAvatar { characterName = "Evelyn" },
        new NamedAvatar { characterName = "Junia" },
        new NamedAvatar { characterName = "Masie" },
        new NamedAvatar { characterName = "Seraphine" },
        new NamedAvatar { characterName = "Trixie" },
        new NamedAvatar { characterName = "Zephyrine" }
    };

    [Header("5 Common Avatars for Random Girls (Characters_9 to Characters_13)")]
    public List<Sprite> commonAvatars = new List<Sprite>();

    [Header("Direct Chat Room Reference")]
    public DirectChatRoomController directChatRoom;

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoPopulateSpritesFromSheet();
    }
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        AutoPopulateSpritesFromSheet();
#endif
    }

#if UNITY_EDITOR
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
            mainCharacterAvatars = new List<NamedAvatar>();
            for (int i = 0; i < mainNames.Length; i++)
                mainCharacterAvatars.Add(new NamedAvatar { characterName = mainNames[i] });
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

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChatsUpdated += RefreshChatsUI;
            RefreshChatsUI();
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChatsUpdated -= RefreshChatsUI;
        }
    }

    private void Start()
    {
        RefreshChatsUI();
    }

    public Sprite GetAvatarForCharacter(string rawOrCleanName, int avatarIndex = -1)
    {
        if (string.IsNullOrEmpty(rawOrCleanName)) return null;

        string cleanName = rawOrCleanName.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase)
            ? rawOrCleanName.Substring(3).Trim()
            : rawOrCleanName.Trim();

        // 1. Check if she is one of the 8 Main Characters by name
        for (int i = 0; i < mainCharacterAvatars.Count; i++)
        {
            if (mainCharacterAvatars[i] != null &&
                mainCharacterAvatars[i].avatarSprite != null &&
                cleanName.Equals(mainCharacterAvatars[i].characterName, System.StringComparison.OrdinalIgnoreCase))
            {
                return mainCharacterAvatars[i].avatarSprite;
            }
        }

        // 2. Otherwise, ONLY use the 5 Common Avatars (never main character sprites)
        if (commonAvatars != null && commonAvatars.Count > 0)
        {
            if (avatarIndex >= 0)
            {
                return commonAvatars[avatarIndex % commonAvatars.Count];
            }

            int hash = Mathf.Abs(cleanName.ToLowerInvariant().GetHashCode());
            return commonAvatars[hash % commonAvatars.Count];
        }

        return null;
    }

    public void RefreshChatsUI()
    {
        if (chatsContentParent == null || chatItemPrefab == null) return;

        for (int i = chatsContentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = chatsContentParent.GetChild(i);
            if (child.GetComponent<ChatItemUI>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        if (GameManager.Instance == null) return;

        HashSet<string> spawnedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < GameManager.Instance.activeChats.Count; i++)
        {
            ContactChatData chatData = GameManager.Instance.activeChats[i];
            if (chatData == null) continue;

            string cleanName = chatData.contactName.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase)
                ? chatData.contactName.Substring(3)
                : chatData.contactName;

            SavedContactData saved = ChatSaveSystem.GetContact(cleanName) ?? ChatSaveSystem.GetContact(chatData.contactName);
            bool isUnlocked = saved != null && saved.isUnlockedInOnlyYaps;

            if (isOnlyYapsView && !isUnlocked) continue;
            if (!isOnlyYapsView && chatData.contactName.StartsWith("OY_", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!spawnedNames.Add(cleanName)) continue;

            GameObject newChat = Instantiate(chatItemPrefab, chatsContentParent);
            ChatItemUI ui = newChat.GetComponent<ChatItemUI>();

            if (ui != null)
            {
                string lastMsg = "New match! Say hi.";
                if (chatData.conversationHistory != null && chatData.conversationHistory.Count > 0)
                {
                    lastMsg = chatData.conversationHistory[chatData.conversationHistory.Count - 1].messageText;
                }
                else if (saved != null && saved.chatHistory != null && saved.chatHistory.Count > 0)
                {
                    lastMsg = saved.chatHistory[saved.chatHistory.Count - 1].messageText;
                }

                Sprite avatar = GetAvatarForCharacter(cleanName, chatData.avatarIndex);

                int index = i;
                ui.Setup(
                    cleanName,
                    lastMsg,
                    chatData.lastMessageTime,
                    avatar,
                    () => OnChatSelected(cleanName, index)
                );
            }
        }
    }

    private void OnChatSelected(string contactName, int index)
    {
        if (GameManager.Instance == null || directChatRoom == null) return;

        ContactChatData selectedChat = GameManager.Instance.activeChats.Find(c =>
            c.contactName.Equals(contactName, System.StringComparison.OrdinalIgnoreCase) ||
            c.contactName.Equals("OY_" + contactName, System.StringComparison.OrdinalIgnoreCase));

        if (selectedChat != null)
        {
            Sprite avatar = GetAvatarForCharacter(contactName, selectedChat.avatarIndex);
            directChatRoom.OpenChatRoom(selectedChat, avatar);
        }
    }
}
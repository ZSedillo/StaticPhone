using UnityEngine;
using System.Collections.Generic;

public class ChatsViewController : MonoBehaviour
{
    [Header("UI References")]
    public Transform chatsContentParent;
    public GameObject chatItemPrefab;
    public TextAsset profileJsonFile; // Drag your JSON file here

    [Header("Developer Testing")]
    [Range(1, 30)] public int devSpawnCount = 10;
    public bool spawnOnStart = true;

    private ProfileDataWrapper loadedData;

    void Start()
    {
        LoadDataFromJson();

        if (spawnOnStart)
        {
            GenerateChats(devSpawnCount);
        }
    }

    private void LoadDataFromJson()
    {
        if (profileJsonFile != null)
        {
            try
            {
                loadedData = JsonUtility.FromJson<ProfileDataWrapper>(profileJsonFile.text);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to parse JSON for chats: " + e.Message);
            }
        }
    }

    [ContextMenu("Regenerate Chats")]
    public void RegenerateChats()
    {
        GenerateChats(devSpawnCount);
    }

    public void GenerateChats(int count)
    {
        if (chatsContentParent == null || chatItemPrefab == null) return;

        // Clear existing spawned panels
        for (int i = chatsContentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(chatsContentParent.GetChild(i).gameObject);
        }

        List<string> names = (loadedData != null && loadedData.names != null && loadedData.names.Count > 0)
            ? loadedData.names
            : new List<string> { "Elena", "Chloe", "Mina", "Rhea", "Yuna", "Sora", "Hana", "Maya", "Kira", "Aria" };

        List<string> sampleMessages = (loadedData != null && loadedData.bios != null && loadedData.bios.Count > 0)
            ? loadedData.bios
            : new List<string> {
                "Always tired, fueled entirely by iced coffee.",
                "Looking for someone who replies fast.",
                "My social battery lasts approximately 23 minutes.",
                "Let's skip small talk and tell me your existential dread."
            };

        for (int i = 0; i < count; i++)
        {
            GameObject newChat = Instantiate(chatItemPrefab, chatsContentParent);
            ChatItemUI ui = newChat.GetComponent<ChatItemUI>();

            if (ui != null)
            {
                string contactName = names[i % names.Count] + (i >= names.Count ? $" {i / names.Count + 1}" : "");
                string message = sampleMessages[i % sampleMessages.Count];
                string time = $"{Random.Range(1, 12)}:{Random.Range(10, 59):D2} PM";

                int index = i;
                ui.Setup(contactName, message, time, null, () => OnChatSelected(contactName, index));
            }
        }
    }

    private void OnChatSelected(string contactName, int index)
    {
        Debug.Log($"Opened chat with: {contactName} (ID: {index})");
    }
}
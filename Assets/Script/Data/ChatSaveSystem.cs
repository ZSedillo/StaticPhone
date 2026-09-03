using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SavedChatMessage
{
    public string messageText;
    public bool isPlayer;
}

[Serializable]
public class SavedContactData
{
    public string contactName;
    public string contactBio;
    public int avatarIndex;
    public string lastMessageTime;
    public string currentNodeId = "start";
    public bool isUnlockedInOnlyYaps = false;
    public List<SavedChatMessage> chatHistory = new List<SavedChatMessage>();
}

[Serializable]
public class UserDatabase
{
    public List<SavedContactData> savedContacts = new List<SavedContactData>();
}

public static class ChatSaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "user_game_data.json");
    private static UserDatabase database;

    public static UserDatabase DB
    {
        get
        {
            if (database == null) Load();
            return database;
        }
    }

    public static SavedContactData GetContact(string contactName)
    {
        contactName = contactName.Trim();
        SavedContactData contact = DB.savedContacts.Find(c => c.contactName.Equals(contactName, StringComparison.OrdinalIgnoreCase));
        return contact;
    }

    public static SavedContactData AddOrGetContact(string name, string bio, int avatarIndex)
    {
        SavedContactData contact = GetContact(name);
        if (contact == null)
        {
            contact = new SavedContactData
            {
                contactName = name.Trim(),
                contactBio = bio,
                avatarIndex = avatarIndex,
                lastMessageTime = "Just now",
                currentNodeId = "start"
            };
            DB.savedContacts.Add(contact);
            Save();
        }
        return contact;
    }

    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(DB, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChatSaveSystem] Save Failed: {ex.Message}");
        }
    }

    public static void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                database = JsonUtility.FromJson<UserDatabase>(json);
            }
            catch
            {
                database = new UserDatabase();
            }
        }
        else
        {
            database = new UserDatabase();
        }
    }

    public static void DeleteAllProgress()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        database = new UserDatabase();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[ChatSaveSystem] Entire user database wiped clean.");
    }
}
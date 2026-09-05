using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueLoader
{
    // Caches trees by path (e.g., "Dialogues/ZephyrineDialogue" or "DialoguesOnlyYaps/Zephyrine")
    private static Dictionary<string, CharacterDialogueTree> pathTreeCache = new Dictionary<string, CharacterDialogueTree>(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, CharacterDialogueTree> datingAppCharacters = new Dictionary<string, CharacterDialogueTree>(StringComparer.OrdinalIgnoreCase);

    public static void InitializeAllCharacters()
    {
        datingAppCharacters.Clear();
        TextAsset[] files = Resources.LoadAll<TextAsset>("Dialogues");

        foreach (TextAsset file in files)
        {
            if (file.name.EndsWith("Dialogue", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CharacterDialogueTree tree = JsonUtility.FromJson<CharacterDialogueTree>(file.text);
                    if (tree != null && !string.IsNullOrEmpty(tree.girlName))
                    {
                        datingAppCharacters[tree.girlName.Trim()] = tree;
                        pathTreeCache["Dialogues/" + file.name] = tree;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DialogueLoader] Failed to parse {file.name}: {e.Message}");
                }
            }
        }
    }

    public static List<CharacterDialogueTree> GetAllCharacters()
    {
        if (datingAppCharacters.Count == 0) InitializeAllCharacters();
        return new List<CharacterDialogueTree>(datingAppCharacters.Values);
    }

    public static CharacterDialogueTree GetCharacter(string girlName)
    {
        if (datingAppCharacters.Count == 0) InitializeAllCharacters();
        datingAppCharacters.TryGetValue(girlName.Trim(), out var tree);
        return tree;
    }

    public static DialogueNodeData GetNode(string path, string nodeId)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (!pathTreeCache.TryGetValue(path, out CharacterDialogueTree tree))
        {
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"[DialogueLoader] Missing file: Resources/{path}");
                return null;
            }

            try
            {
                tree = JsonUtility.FromJson<CharacterDialogueTree>(json.text);
                if (tree != null)
                {
                    pathTreeCache[path] = tree;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueLoader] Failed to parse JSON at Resources/{path}: {e.Message}");
                return null;
            }
        }

        if (tree?.nodes == null || tree.nodes.Count == 0) return null;

        // 1. Try finding exact node by ID
        if (!string.IsNullOrEmpty(nodeId))
        {
            var foundNode = tree.nodes.Find(n => n.id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
            if (foundNode != null) return foundNode;
        }

        // 2. Fallback: return the first node if ID wasn't found or was empty
        return tree.nodes[0];
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueLoader
{
    private static Dictionary<string, CharacterDialogueTree> characterTrees = new Dictionary<string, CharacterDialogueTree>(StringComparer.OrdinalIgnoreCase);

    public static void InitializeAllCharacters()
    {
        characterTrees.Clear();
        TextAsset[] files = Resources.LoadAll<TextAsset>("Dialogues");

        foreach (TextAsset file in files)
        {
            if (file.name.EndsWith("Dialogue"))
            {
                try
                {
                    CharacterDialogueTree tree = JsonUtility.FromJson<CharacterDialogueTree>(file.text);
                    if (tree != null && !string.IsNullOrEmpty(tree.girlName))
                    {
                        characterTrees[tree.girlName.Trim()] = tree;
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
        if (characterTrees.Count == 0) InitializeAllCharacters();
        return new List<CharacterDialogueTree>(characterTrees.Values);
    }

    public static CharacterDialogueTree GetCharacter(string girlName)
    {
        if (characterTrees.Count == 0) InitializeAllCharacters();
        characterTrees.TryGetValue(girlName.Trim(), out var tree);
        return tree;
    }

    public static DialogueNodeData GetNode(string girlName, string nodeId)
    {
        CharacterDialogueTree tree = GetCharacter(girlName);
        if (tree != null && tree.nodes != null)
        {
            return tree.nodes.Find(n => n.id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
        }
        return null;
    }
}
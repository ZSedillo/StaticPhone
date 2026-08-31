using System.Collections.Generic;
using UnityEngine;

public class DatingProfileRandomizer : MonoBehaviour
{
    // --- 1. THE TRAIT POOLS ---
    
    private readonly List<string> archetypes = new List<string>
    {
        "Ambitious Hustler",
        "Creative Bohemian",
        "Cozy Homebody",
        "Adrenaline Junkie",
        "Hopeless Romantic",
        "Chronically Online Cynic",
        "Academic Intellectual",
        "Social Butterfly",
        "Alt Subculture Loyalist",
        "Zen Minimalist"
    };

    private readonly List<string> politicalViews = new List<string>
    {
        "Grassroots Progressive",
        "Fiscal Conservative",
        "Pragmatic Centrist",
        "Apolitical Doomer",
        "Techno-Optimist",
        "Eco-Activist",
        "Spiritual New-Ager"
    };

    private readonly List<string> commStyles = new List<string>
    {
        "Voice-Note Spammer",
        "Formal & Proper Texter",
        "Dry One-Liner / Sarcastic",
        "Rapid-Fire Multi-Texter",
        "Lagged Replier / Ghoster",
        "Deep Questioner",
        "Meme & GIF Communicator"
    };

    private readonly List<string> quirks = new List<string>
    {
        "Spreadsheet Organizer",
        "Menu Indecisive",
        "Astrology Judge",
        "Music Snob",
        "Obsessive Pet Parent",
        "Strict Punctuality Cop",
        "Perpetual Low-Battery Menace",
        "Early Oversharer"
    };

    private readonly List<string> hobbies = new List<string>
    {
        "Specialty Coffee Snob",
        "Gym & Fitness Devotee",
        "Vintage / Thrift Hunter",
        "Board Game Strategist",
        "Film Essay Buff",
        "Culinary Experimenter",
        "Cozy Gamer"
    };

    private void Start()
    {
        // Example test run
        List<string> sampleGirlProfile = GenerateProfileTraits();
        
        Debug.Log("--- Generated Match Profile ---");
        foreach (string trait in sampleGirlProfile)
        {
            Debug.Log($"- {trait}");
        }
    }

    // --- 2. RANDOMIZER FUNCTION ---

    public List<string> GenerateProfileTraits()
    {
        List<string> selectedTraits = new List<string>();

        // Guaranteed Base: 1 Archetype + 1 Texting Style
        selectedTraits.Add(GetRandomItem(archetypes));
        selectedTraits.Add(GetRandomItem(commStyles));

        // Combine the remaining categories into a single secondary pool
        List<string> optionalPool = new List<string>();
        optionalPool.AddRange(politicalViews);
        optionalPool.AddRange(quirks);
        optionalPool.AddRange(hobbies);

        // Decide total trait count: 3, 4, or 5 (Random.Range max is exclusive)
        int totalTargetCount = Random.Range(3, 6); 
        int extraNeeded = totalTargetCount - selectedTraits.Count;

        // Pull unique random traits from the optional pool
        for (int i = 0; i < extraNeeded; i++)
        {
            if (optionalPool.Count == 0) break;

            int randomIndex = Random.Range(0, optionalPool.Count);
            selectedTraits.Add(optionalPool[randomIndex]);
            
            // Remove to prevent duplicate traits on the same person
            optionalPool.RemoveAt(randomIndex); 
        }

        return selectedTraits;
    }

    private string GetRandomItem(List<string> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    public DialogueStep GenerateDialogueForTraits(List<string> traits, string partnerName)
    {
        DialogueStep root = new DialogueStep();
        
        // Choose dialogue flavor based on personality/texting style
        if (traits.Contains("Dry One-Liner / Sarcastic"))
        {
            root.partnerMessage = "don't be boring.";
        }
        else if (traits.Contains("Voice-Note Spammer") || traits.Contains("Social Butterfly"))
        {
            root.partnerMessage = $"Heyyy {partnerName} here! So excited we matched 😊";
        }
        else
        {
            root.partnerMessage = "Hey! What caught your eye on my profile?";
        }

        // Branching options for the player
        DialogueStep goodBranch = new DialogueStep { partnerMessage = "Haha, good answer! You pass the vibe check." };
        DialogueStep playfulBranch = new DialogueStep { partnerMessage = "Bold move, but I like the confidence." };

        root.choices = new List<PlayerChoice>
        {
            new PlayerChoice { choiceText = "Your bio made me laugh, had to say hi.", nextStep = goodBranch },
            new PlayerChoice { choiceText = "Honestly? You seemed way too cool to pass up.", nextStep = playfulBranch },
            new PlayerChoice { choiceText = "Just seeing what's out there.", nextStep = null }
        };

        return root;
    }
}
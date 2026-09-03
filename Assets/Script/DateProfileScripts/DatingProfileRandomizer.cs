using System.Collections.Generic;
using UnityEngine;

public class DatingProfileRandomizer : MonoBehaviour
{
    private readonly List<string> archetypes = new List<string>
    {
        "Ambitious Hustler", "Creative Bohemian", "Cozy Homebody", "Adrenaline Junkie",
        "Hopeless Romantic", "Chronically Online Cynic", "Academic Intellectual",
        "Social Butterfly", "Alt Subculture Loyalist", "Zen Minimalist"
    };

    private readonly List<string> politicalViews = new List<string>
    {
        "Grassroots Progressive", "Fiscal Conservative", "Pragmatic Centrist",
        "Apolitical Doomer", "Techno-Optimist", "Eco-Activist", "Spiritual New-Ager"
    };

    private readonly List<string> commStyles = new List<string>
    {
        "Voice-Note Spammer", "Formal & Proper Texter", "Dry One-Liner / Sarcastic",
        "Rapid-Fire Multi-Texter", "Lagged Replier / Ghoster", "Deep Questioner", "Meme & GIF Communicator"
    };

    private readonly List<string> quirks = new List<string>
    {
        "Spreadsheet Organizer", "Menu Indecisive", "Astrology Judge", "Music Snob",
        "Obsessive Pet Parent", "Strict Punctuality Cop", "Perpetual Low-Battery Menace", "Early Oversharer"
    };

    private readonly List<string> hobbies = new List<string>
    {
        "Specialty Coffee Snob", "Gym & Fitness Devotee", "Vintage / Thrift Hunter",
        "Board Game Strategist", "Film Essay Buff", "Culinary Experimenter", "Cozy Gamer"
    };

    public List<string> GenerateProfileTraits()
    {
        List<string> selectedTraits = new List<string>();

        selectedTraits.Add(GetRandomItem(archetypes));
        selectedTraits.Add(GetRandomItem(commStyles));

        List<string> optionalPool = new List<string>();
        optionalPool.AddRange(politicalViews);
        optionalPool.AddRange(quirks);
        optionalPool.AddRange(hobbies);

        int totalTargetCount = Random.Range(3, 6); 
        int extraNeeded = totalTargetCount - selectedTraits.Count;

        for (int i = 0; i < extraNeeded; i++)
        {
            if (optionalPool.Count == 0) break;

            int randomIndex = Random.Range(0, optionalPool.Count);
            selectedTraits.Add(optionalPool[randomIndex]);
            optionalPool.RemoveAt(randomIndex); 
        }

        return selectedTraits;
    }

    private string GetRandomItem(List<string> list)
    {
        return list[Random.Range(0, list.Count)];
    }
}
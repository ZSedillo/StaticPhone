using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "DatingSim/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterProfileData> allCharacters = new List<CharacterProfileData>();

    public CharacterProfileData GetCharacterByName(string targetName)
    {
        return allCharacters.Find(c => c.characterName.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));
    }
}
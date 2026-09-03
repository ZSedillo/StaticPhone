using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterProfile", menuName = "DatingSim/Character Profile")]
public class CharacterProfileData : ScriptableObject
{
    [Header("Profile Info")]
    public string characterName;
    public int age;
    public Sprite avatarSprite;
    public string personalitySummary;
    [TextArea(2, 4)] public string bio;

    [Header("Unique Branching Dialogue Tree")]
    public DialogueStep startingDialogue;
}
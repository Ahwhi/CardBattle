using System;
using UnityEngine;
using static Enums;

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
[Serializable]
public class CardData : ScriptableObject
{
    public int ID;
    public CardType Type;
    public SpecialSkill Skill;
    public string Name;
    public string SkillDesc;
    public int BaseHP;
    public Sprite sprite;
    public Sprite TypeIcon;
}

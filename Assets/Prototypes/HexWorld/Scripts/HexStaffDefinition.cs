using System;
using UnityEngine;

public enum HexStaffGrade { F, E, D, C, B, A, S }
public enum HexStaffJob { Standby, Kitchen, Cleaning, Recruiting, Security }
public enum HexStaffAbility { Cooking, Cleaning, Persuasion, Strength }
public enum HexStaffFlawKind { CookingSlow, CleaningSlow, PersuasionPenalty, WaitingGuestEjection, WeakStrength }
public enum HexStaffSkillKind { CookingBonus, PersuasionBonus, WorkBonus, WageDiscount }

[CreateAssetMenu(menuName="Stack Store/Hex World/Employee")]
public sealed class HexStaffDefinition : ScriptableObject
{
    public string key,nameKey,descriptionKey;
    public Sprite picture;
    public Color color=Color.cyan;
    [Min(0)] public int hirePrice=3,wage=1;
    public HexStaffGrade cooking,cleaning,persuasion,strength;
    public HexStaffFlaw[] flaws=Array.Empty<HexStaffFlaw>();
    public HexStaffGrade Initial(HexStaffAbility ability)=>ability==HexStaffAbility.Cooking?cooking:ability==HexStaffAbility.Cleaning?cleaning:ability==HexStaffAbility.Persuasion?persuasion:strength;
}
[Serializable] public sealed class HexStaffFlaw
{
    public string descriptionKey;
    public HexStaffFlawKind kind;
    [Range(0,1)] public float amount;
}
public enum HexStaffTargetKind { Trash, Disruption }

using UnityEngine;

[CreateAssetMenu(menuName="Stack Store/Hex World/Employee Skill")]
public sealed class HexStaffSkill : ScriptableObject
{
    public string key,nameKey,descriptionKey;
    public HexStaffGrade requiredGrade=HexStaffGrade.B;
    public HexStaffSkillKind kind;
    [Range(0,1)] public float amount=.2f;
}

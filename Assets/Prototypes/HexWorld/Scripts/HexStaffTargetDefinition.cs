using UnityEngine;

[CreateAssetMenu(menuName="Stack Store/Hex World/Employee Task Target")]
public sealed class HexStaffTargetDefinition : ScriptableObject
{
    public string nameKey;
    public HexStaffTargetKind kind;
    [Min(.1f)] public float workSeconds=8;
    public HexStaffGrade requiredStrength=HexStaffGrade.E;
}

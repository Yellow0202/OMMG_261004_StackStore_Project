using System;
using UnityEngine;

/// <summary>One replaceable location's art and common environment tuning.</summary>
[CreateAssetMenu(menuName="Stack Store/Hex World/Environment Theme")]
public sealed class HexEnvironmentTheme : ScriptableObject
{
    public string key="village_market";
    [Serializable] public sealed class FloorArt { public HexTileKind kind; public Material material; }
    [Header("타일 종류별 바닥")]
    public FloorArt[] floors;
    public Mesh floorMesh;
    public float floorHeight=.215f;
    [Header("바닥 경계 융화 / 월드 단위 / 0이면 기존 경계")]
    [Range(0,1)] public float floorBlendWidth=.45f;
    [Header("시장 지면 / 월드 단위")]
    public Material groundMaterial;
    public Color groundTint=Color.white;
    [Range(0,1)] public float groundContrast=.5f;
    public float groundHeight=.18f;
    [Min(10)] public float minimumGroundSpan=180,expansionMargin=50;
    [Min(.1f)] public float pavingRepeatMeters=2.8f;
    [Header("원경 / 카메라 각도와 독립된 수직 배경")]
    public Material backdropMaterial;
    public Color backdropTint=Color.white;
    [Min(1)] public float backdropDistance=12,backdropWidth=80,backdropHeight=26.66667f;
    public float backdropBaseHeight=-.6f;
    [Header("시장 입체 배경 / 건물 배치")]
    public bool showDepthBuildings=true;
    [Min(5)] public float buildingClearance=9;
    [Min(1)] public float buildingDepthSpacing=4;
    [Range(.5f,2)] public float buildingScale=1;
    public Material Floor(HexTileKind kind)
    {
        if(floors!=null)foreach(var entry in floors)if(entry!=null&&entry.kind==kind)return entry.material;
        return null;
    }
}

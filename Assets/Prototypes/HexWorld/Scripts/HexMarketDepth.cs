using UnityEngine;

/// <summary>Authored scenery anchors expressed outside the current owned shop bounds.</summary>
[ExecuteAlways]
public sealed class HexMarketDepth : MonoBehaviour
{
    public HexMarketEnvironment environment;
    public Transform[] buildings;
    public Vector3[] anchors;
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        if(!environment||!environment.Theme)return;
        var theme=environment.Theme;var board=Object.FindFirstObjectByType<HexTileBoard>();
        var bounds=new Bounds(Vector3.zero,Vector3.zero);
        if(board&&board.Model!=null)foreach(var cell in board.Model.Owned)bounds.Encapsulate(HexBoardModel.World(cell));
        for(int i=0;i<buildings.Length&&i<anchors.Length;i++)
        {
            if(!buildings[i])continue;var a=anchors[i];
            buildings[i].localPosition=new Vector3(bounds.center.x+a.x*(bounds.extents.x+theme.buildingClearance),theme.groundHeight,bounds.max.z+theme.buildingClearance+a.z*theme.buildingDepthSpacing);
            buildings[i].localScale=Vector3.one*theme.buildingScale;
            buildings[i].gameObject.SetActive(theme.showDepthBuildings);
        }
    }
}

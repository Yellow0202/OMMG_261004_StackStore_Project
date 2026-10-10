using UnityEngine;

/// <summary>Saved environment geometry; follows shop extent, never owns or blocks cells.</summary>
[ExecuteAlways]
public sealed class HexMarketEnvironment : MonoBehaviour
{
    public HexTestSettings settings;
    public MeshRenderer ground,backdrop;
    HexTileBoard board;
    MaterialPropertyBlock properties;
    public HexEnvironmentTheme Theme => HexTestSettings.Current&&Application.isPlaying?HexTestSettings.Current.environment:settings?settings.environment:null;
    void OnEnable(){Refresh();}
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        var theme=Theme;if(!theme||!ground||!backdrop)return;
        if(properties==null)properties=new MaterialPropertyBlock();
        if(!board)board=Object.FindFirstObjectByType<HexTileBoard>();
        var center=Vector3.zero;var size=Vector3.zero;
        if(board&&board.Model!=null)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var cell in board.Model.Owned){var p=HexBoardModel.World(cell);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            center=bounds.center;size=bounds.size;
        }
        float span=Mathf.Max(theme.minimumGroundSpan,Mathf.Max(size.x,size.z)+theme.expansionMargin*2);
        ground.transform.SetPositionAndRotation(new Vector3(center.x,theme.groundHeight,center.z),Quaternion.identity);
        ground.transform.localScale=new Vector3(span,1,span);ground.sharedMaterial=theme.groundMaterial;
        properties.Clear();properties.SetColor("_BaseColor",theme.groundTint);
        properties.SetFloat("_Contrast",theme.groundContrast);
        float repeat=span/Mathf.Max(.1f,theme.pavingRepeatMeters);
        float meters=Mathf.Max(.1f,theme.pavingRepeatMeters);
        properties.SetVector("_BaseMap_ST",new Vector4(repeat,repeat,(center.x-span*.5f)/meters,(center.z-span*.5f)/meters));ground.SetPropertyBlock(properties);
        float yaw=settings?settings.camera.yaw:0;
        var rotation=Quaternion.Euler(0,yaw,0);
        var forward=rotation*Vector3.forward;
        float extent=(Mathf.Abs(forward.x)*size.x+Mathf.Abs(forward.z)*size.z)*.5f;
        backdrop.transform.SetPositionAndRotation(new Vector3(center.x,theme.backdropBaseHeight+theme.backdropHeight*.5f,center.z)+forward*(theme.backdropDistance+extent),rotation);
        backdrop.transform.localScale=new Vector3(theme.backdropWidth,theme.backdropHeight,1);backdrop.sharedMaterial=theme.backdropMaterial;
        properties.Clear();properties.SetColor("_BaseColor",theme.backdropTint);backdrop.SetPropertyBlock(properties);
        // Saved scene starts with one owned stall. Preview this in Edit mode as well.
        if(!Application.isPlaying&&board&&board.authoredTiles!=null)
            foreach(var tile in board.authoredTiles)if(tile)tile.ApplyFloor(theme,tile.coordinate==Vector2Int.zero,null);
    }
}

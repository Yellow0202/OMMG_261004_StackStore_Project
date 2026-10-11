using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>World-space visible-tile mask shared by ground and sprite shaders.</summary>
public sealed class HexExteriorFog
{
    readonly HexRaisedFog raised=new HexRaisedFog();
    Texture2D mask;
    Vector4 grid;
    readonly HashSet<Vector2Int> revealed=new HashSet<Vector2Int>();
    readonly Dictionary<CanvasGroup,float> canvasAlphas=new Dictionary<CanvasGroup,float>();
    readonly List<CanvasGroup> expired=new List<CanvasGroup>();
    readonly HashSet<Vector2Int> nextCells=new HashSet<Vector2Int>();
    public bool Reveals(Vector2Int cell)=>revealed.Contains(cell);
    public void Refresh(HexTileBoard board,HexTestSettings settings,Camera activeCamera)
    {
        var shared=Application.isPlaying&&HexTestSettings.Current?HexTestSettings.Current:settings;
        if(!shared||!shared.exteriorFog.enabled)
        {
            Shader.SetGlobalVector("_HexFogOptions",Vector4.zero);raised.Dispose();RestoreCanvases();return;
        }
        var options=shared.exteriorFog;
        nextCells.Clear();
        if(board)
        {
            foreach(var tile in board.VisibleTiles)
                if(tile&&tile.gameObject.activeInHierarchy)nextCells.Add(tile.coordinate);
        }
        if(nextCells.Count==0)nextCells.Add(Vector2Int.zero);
        if(!mask||!revealed.SetEquals(nextCells))
        {
            revealed.Clear();revealed.UnionWith(nextCells);BuildMask();
        }
        Shader.SetGlobalTexture("_HexFogOwnership",mask);Shader.SetGlobalVector("_HexFogGrid",grid);
        Shader.SetGlobalColor("_HexFogColor",options.color);
        Shader.SetGlobalVector("_HexFogOptions",new Vector4(1,options.opacity,options.featherWidth,options.buildingFrontierOpacity));
        Shader.SetGlobalVector("_HexFogMotion",new Vector4(options.edgeNoise,options.driftSpeed,board&&board.Mode!=HexBuildMode.None?1:0,0));
        bool preview=options.revealPlacementPreview&&board&&board.preview&&board.preview.gameObject.activeInHierarchy;
        var position=preview?HexBoardModel.World(board.preview.coordinate):Vector3.zero;
        Shader.SetGlobalVector("_HexFogPreview",new Vector4(position.x,position.z,1.4f,preview?1:0));
        raised.Refresh(revealed,options,board?board.transform:null);
        if(Application.isPlaying)
        {
            expired.Clear();foreach(var pair in canvasAlphas)if(!pair.Key)expired.Add(pair.Key);foreach(var key in expired)canvasAlphas.Remove(key);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas.renderMode!=RenderMode.WorldSpace)continue;
                var actor=canvas.GetComponentInParent<HexWorldActor>();if(!actor)continue;
                var group=canvas.GetComponent<CanvasGroup>();if(!group)group=canvas.gameObject.AddComponent<CanvasGroup>();
                if(!canvasAlphas.ContainsKey(group))canvasAlphas.Add(group,group.alpha);
                group.alpha=canvasAlphas[group]*(1-Amount(actor.transform.position,options,board));
            }
        }
    }
    void BuildMask()
    {
        int minQ=0,minR=0,maxQ=0,maxR=0;
        foreach(var cell in revealed){minQ=Mathf.Min(minQ,cell.x);minR=Mathf.Min(minR,cell.y);maxQ=Mathf.Max(maxQ,cell.x);maxR=Mathf.Max(maxR,cell.y);}
        minQ--;minR--;maxQ++;maxR++;
        int width=maxQ-minQ+1,height=maxR-minR+1;
        if(mask&&(mask.width!=width||mask.height!=height)){DestroyMask();}
        if(!mask)mask=new Texture2D(width,height,TextureFormat.RGBA32,false,true){name="Runtime exterior ownership",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
        var pixels=new Color32[width*height];foreach(var cell in revealed)pixels[(cell.y-minR)*width+cell.x-minQ]=new Color32(255,255,255,255);
        mask.SetPixels32(pixels);mask.Apply(false,false);
        grid=new Vector4(minQ,minR,width,height);
    }
    public float Amount(Vector3 position,HexTestSettings.ExteriorFogOptions options,HexTileBoard board=null)
    {
        if(!options.enabled)return 0;
        float distance=1000;
        foreach(var cell in revealed)distance=Mathf.Min(distance,Distance(position,HexBoardModel.World(cell)));
        float value=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Max(0,distance)/Mathf.Max(.01f,options.featherWidth)))*options.opacity;
        if(board&&board.Mode!=HexBuildMode.None)
        {
            var cell=HexShopLayout.Cell(position);if(!revealed.Contains(cell))foreach(var direction in HexBoardModel.Directions)
                if(revealed.Contains(cell+direction)){value=Mathf.Min(value,options.buildingFrontierOpacity);break;}
        }
        if(options.revealPlacementPreview&&board&&board.preview&&board.preview.gameObject.activeInHierarchy)
            value*=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Max(0,Distance(position,HexBoardModel.World(board.preview.coordinate)))/Mathf.Max(.003f,options.featherWidth*.3f)));
        return value;
    }
    public static float Distance(Vector3 p,Vector3 center)
    {
        float x=Mathf.Abs(p.x-center.x),z=Mathf.Abs(p.z-center.z);
        return Mathf.Max(x,x*.5f+z*.866025404f)-1.212435565f;
    }
    void RestoreCanvases(){foreach(var pair in canvasAlphas)if(pair.Key)pair.Key.alpha=pair.Value;canvasAlphas.Clear();}
    void DestroyMask(){if(mask){if(Application.isPlaying)Object.Destroy(mask);else Object.DestroyImmediate(mask);}mask=null;}
    public void Dispose(){Shader.SetGlobalVector("_HexFogOptions",Vector4.zero);raised.Dispose();RestoreCanvases();DestroyMask();revealed.Clear();}
}

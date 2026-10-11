using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class HexExteriorFogChecks
{
    const string Pending="HexExteriorFogChecks";
    const string Root="Assets/Prototypes/HexWorld";
    static int frames;
    static HexExteriorFogChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run()
    {
        EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");
        SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Check()
    {
        if(!EditorApplication.isPlaying||!Application.isPlaying||Time.deltaTime<=0||++frames<20)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);int result=0;
        var fog=new HexExteriorFog();
        try
        {
            var board=UnityEngine.Object.FindFirstObjectByType<HexTileBoard>();
            var settings=HexTestSettings.Current;var camera=Camera.main;var options=settings.exteriorFog;
            foreach(string name in new[]{"EnvironmentUnlit","EnvironmentLayer","HexFloorBlend","HexFogSprite","HexRaisedFog"})
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Art/Environment/"+name+".shader");
                Require(shader&&!ShaderUtil.ShaderHasError(shader),"Shader errors: "+name);
            }
            fog.Refresh(board,settings,camera);
            Require(fog.Amount(Vector3.zero,options)==0,"Root is obscured");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var raised=typeof(HexExteriorFog).GetField("raised",flags).GetValue(fog);
            var rimObject=(GameObject)typeof(HexRaisedFog).GetField("root",flags).GetValue(raised);
            var rim=rimObject?rimObject.GetComponent<MeshFilter>():null;
            Require(rim&&rim.sharedMesh.vertexCount>0&&rim.sharedMesh.bounds.size.y>=options.height,"Raised fog has no height");
            Require(!rim.GetComponent<Collider>(),"Mist blocks movement");
            options.raisedFog=false;fog.Refresh(board,settings,camera);Require(!rim.gameObject.activeSelf,"Raised toggle failed");
            options.raisedFog=true;fog.Refresh(board,settings,camera);Require(rim.gameObject.activeSelf,"Raised toggle restore failed");
            var originalBackground=camera.backgroundColor;
            foreach(var tile in board.VisibleTiles.Where(x=>x&&x.gameObject.activeInHierarchy))
                Require(fog.Amount(HexBoardModel.World(tile.coordinate),options)==0,"Unowned visible tile obscured");
            Require(fog.Amount(new Vector3(40,0,40),options)>.99f,"Outside is not white");
            var table=board.tileTypes.First(x=>x.kind==HexTileKind.Table);var cell=new Vector2Int(1,0);
            board.Grant(table,1);Require(board.Layout.TryPlace(0,cell,table),"Place failed");board.Refresh();fog.Refresh(board,settings,camera);
            Require(fog.Amount(HexBoardModel.World(cell),options)==0,"Placed tile is obscured");
            var moved=new Vector2Int(0,1);Require(board.Layout.TryMove(0,cell,moved),"Move failed");fog.Refresh(board,settings,camera);
            Require(fog.Reveals(cell)&&fog.Reveals(moved),"Existing tiles should remain revealed after move");
            Require(board.Layout.TryRecover(0,moved),"Recover failed");fog.Refresh(board,settings,camera);
            Require(fog.Reveals(moved)&&fog.Amount(HexBoardModel.World(moved),options)==0,"Recovered tile should remain visible");
            board.SetMode(HexBuildMode.Place);fog.Refresh(board,settings,camera);
            Require(fog.Amount(HexBoardModel.World(cell),options,board)<=options.buildingFrontierOpacity+.001f,"Frontier hidden");
            board.preview.coordinate=cell;board.preview.gameObject.SetActive(true);fog.Refresh(board,settings,camera);
            Require(fog.Amount(HexBoardModel.World(cell),options,board)==0,"Preview hidden");
            board.SetMode(HexBuildMode.None);
            var stairs=board.tileTypes.First(x=>x.kind==HexTileKind.Stairs);board.Grant(stairs,1);
            Require(board.Layout.TryPlace(0,new Vector2Int(-1,0),stairs),"Stairs failed");board.SetFloor(1);fog.Refresh(board,settings,camera);
            Require(fog.Reveals(board.Model.Root)&&fog.Amount(HexBoardModel.World(board.Model.Root),options)==0,"Upper landing hidden");
            board.SetFloor(0);board.Grant(table,1);Require(board.Layout.TryPlace(0,cell,table),"Capture tile failed");board.Refresh();
            // Existing scene controller owns the normal fog instance. Refresh it after helper checks.
            fog.Dispose();var environment=UnityEngine.Object.FindFirstObjectByType<HexMarketEnvironment>();environment.Refresh();
            Require(camera.backgroundColor==originalBackground,"Camera background altered by fog");
            var backdropProperties=new MaterialPropertyBlock();environment.backdrop.GetPropertyBlock(backdropProperties);
            Require(backdropProperties.GetFloat("_ExteriorFog")==0,"Backdrop fog not bypassed");
            foreach(float pitch in new[]{4f,30f,50f})
            {
                camera.orthographicSize=5;camera.transform.rotation=Quaternion.Euler(pitch,0,0);
                camera.transform.position=new Vector3(0,.8f,0)-camera.transform.forward*14;
                foreach(var face in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))face.FaceCamera();
                environment.Refresh();Capture(camera,"Fog-Pitch-"+pitch+".png");
            }
            options.enabled=false;environment.Refresh();Require(Shader.GetGlobalVector("_HexFogOptions").x==0,"Disable failed");
            options.enabled=true;environment.Refresh();
            Debug.Log("HEX_EXTERIOR_FOG_VALIDATED: raised mesh height and toggle; no collider; shaders; owned and unowned visible tiles/outside; placement/move/recovery; build frontier/preview; stairs/floors; unaffected backdrop and camera; disable; captures at 4/30/50 degrees.");
        }
        catch(Exception exception){Debug.LogException(exception);result=1;}
        finally{fog.Dispose();EditorApplication.Exit(result);}
    }
    static void Capture(Camera camera,string name)
    {
        var target=new RenderTexture(1280,720,24);var picture=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            target.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
            RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,1280,720),0,0);picture.Apply();
            string project=Path.GetDirectoryName(Application.dataPath);
            if(project.EndsWith(Path.Combine("Temp","AnimationValidation")))project=Directory.GetParent(project).Parent.FullName;
            var directory=Path.Combine(project,"ArtReferences","2026-10-11-Raised-Fog");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name),picture.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(picture);}
    }
}

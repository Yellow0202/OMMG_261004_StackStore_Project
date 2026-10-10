using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class HexEnvironmentChecks
{
    const string Pending="HexEnvironmentChecks";
    const string Root="Assets/Prototypes/HexWorld";
    static int frames;
    static HexEnvironmentChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run()
    {
        EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");
        SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();
    }
    static void Check()
    {
        if(!EditorApplication.isPlaying||!Application.isPlaying||Time.deltaTime<=0||++frames<12)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);int result=0;
        try
        {
            var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();var board=game.board;
            var environment=UnityEngine.Object.FindFirstObjectByType<HexMarketEnvironment>();
            if(!environment)throw new Exception("Saved scene is missing market environment");
            var theme=environment.Theme;if(!theme)throw new Exception("Theme not linked");
            var test=UnityEngine.Object.Instantiate(board.tilePrefab);
            try
            {
                foreach(var type in board.tileTypes)
                {
                    var position=test.transform.position;test.Show(true,type);
                    if(!test.surface.enabled||test.surface.sharedMaterial!=theme.Floor(type.kind))throw new Exception("Incorrect floor "+type.kind);
                    if(test.transform.position!=position)throw new Exception("Floor altered navigation anchor");
                    if(test.GetComponent<MeshCollider>().sharedMesh==theme.floorMesh)throw new Exception("Visual mesh replaced navigation collider");
                }
                test.Show(false,null);if(test.surface.enabled)throw new Exception("Unowned floor visible");
                test.Show(true,board.tileTypes[0],new Color(0,1,0,.4f));if(test.surface.enabled)throw new Exception("Preview covers installed floor");
            }
            finally{UnityEngine.Object.Destroy(test.gameObject);}
            foreach(var kind in new[]{HexTileKind.Table,HexTileKind.Kitchen,HexTileKind.Storage,HexTileKind.Lodging})
            {
                var definition=board.tileTypes.First(x=>x.kind==kind);int index=Array.IndexOf(new[]{HexTileKind.Table,HexTileKind.Kitchen,HexTileKind.Storage,HexTileKind.Lodging},kind);
                board.Grant(definition,1);if(!board.Layout.TryPlace(0,HexBoardModel.Directions[index],definition))throw new Exception("Placement failed");
            }
            board.Refresh();environment.Refresh();
            if(board.authoredTiles.Count(x=>x.surface.enabled)!=board.Model.OwnedCount)throw new Exception("Owned floor visibility mismatch");
            if(environment.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Environment blocks movement");
            // Render at camera limits as well as the initial angle, using the actual URP camera.
            foreach(float pitch in new[]{4f,30f,50f})
            {
                game.orbit.pitch=pitch;
                typeof(HexOrbitCamera).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game.orbit,null);
                foreach(var facing in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))facing.FaceCamera();
                environment.Refresh();Capture(Camera.main,"Market-Pitch-"+pitch.ToString("0")+".png");
            }
            game.orbit.zoom=game.orbit.maximumZoom;
            foreach(float pitch in new[]{4f,50f})
            {
                game.orbit.pitch=pitch;
                typeof(HexOrbitCamera).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game.orbit,null);
                foreach(var facing in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))facing.FaceCamera();
                Capture(Camera.main,"Market-FarZoom-Pitch-"+pitch.ToString("0")+".png");
            }
            Debug.Log("MARKET_ENVIRONMENT_PLAY_PASSED: seven floor mappings, ownership, ghost visibility, stable anchors/colliders, actual placements, three angles and far-zoom renders.");
        }
        catch(Exception exception){Debug.LogException(exception);result=1;}
        finally{EditorApplication.Exit(result);}
    }
    static void Capture(Camera camera,string name)
    {
        var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            target.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            string directory=Path.GetFullPath("../../ArtReferences/2026-10-10-Market-Environment");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name),image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}

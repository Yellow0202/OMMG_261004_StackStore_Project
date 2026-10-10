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
            finally{UnityEngine.Object.DestroyImmediate(test.gameObject);}
            foreach(var kind in new[]{HexTileKind.Table,HexTileKind.Kitchen,HexTileKind.Storage,HexTileKind.Lodging})
            {
                var definition=board.tileTypes.First(x=>x.kind==kind);int index=Array.IndexOf(new[]{HexTileKind.Table,HexTileKind.Kitchen,HexTileKind.Storage,HexTileKind.Lodging},kind);
                board.Grant(definition,1);if(!board.Layout.TryPlace(0,HexBoardModel.Directions[index],definition))throw new Exception("Placement failed");
            }
            board.Refresh();environment.Refresh();
            foreach(var tile in board.authoredTiles.Where(x=>board.Model.IsOwned(x.coordinate)))
            {
                var properties=new MaterialPropertyBlock();tile.surface.GetPropertyBlock(properties);
                var flagsA=properties.GetVector("_OwnedA");var flagsB=properties.GetVector("_OwnedB");
                for(int d=0;d<6;d++)
                {
                    var neighbor=tile.coordinate+HexBoardModel.Directions[d];bool owned=board.Model.IsOwned(neighbor);
                    if((d<4?flagsA[d]:flagsB[d-4])!=(owned?1:0))throw new Exception("Wrong blend neighbour "+d);
                    if(owned)
                    {
                        var definition=board.Model.Definition(neighbor);
                        if(properties.GetTexture("_Neighbor"+d)!=theme.Floor(definition?definition.kind:HexTileKind.DisplayShelf).GetTexture("_BaseMap"))throw new Exception("Wrong neighbour texture");
                    }
                }
            }
            if(board.authoredTiles.Count(x=>x.surface.enabled)!=board.Model.OwnedCount)throw new Exception("Owned floor visibility mismatch");
            if(environment.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Environment blocks movement");
            HexMarketLayerAuthoring.Validate();
            CheckLayerParallax(environment);
            CaptureFloorComparison(board,theme,environment);
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
            game.orbit.pitch=4;
            typeof(HexOrbitCamera).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game.orbit,null);
            var savedCameraPosition=Camera.main.transform.position;
            foreach(int direction in new[]{-1,1})
            {
                Camera.main.transform.position=savedCameraPosition+Camera.main.transform.right*(game.orbit.PanBounds.extents.x+game.orbit.overscroll)*direction;
                Capture(Camera.main,"Market-FarZoom-Pan-"+direction+".png");
            }
            Camera.main.transform.position=savedCameraPosition;
            Debug.Log("MARKET_ENVIRONMENT_PLAY_PASSED: seven floor mappings, six neighbour flags/textures, ownership, ghost visibility, stable anchors/colliders, before/after blending, three angles and far-zoom renders.");
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
            target.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            string directory=Path.GetFullPath("../../ArtReferences/2026-10-10-Market-Layers");Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,name),image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
    static void CheckLayerParallax(HexMarketEnvironment environment)
    {
        var theme=environment.Theme;var camera=Camera.main;
        if(camera.orthographic||environment.backdrop.enabled)throw new Exception("Perspective/layer setup invalid");
        var position=camera.transform.position;var rotation=camera.transform.rotation;
        var layers=environment.backgroundLayers;
        var board=UnityEngine.Object.FindFirstObjectByType<HexTileBoard>();
        var forward=Quaternion.Euler(0,environment.settings.camera.yaw,0)*Vector3.forward;
        float shopEdge=board.Model.Owned.Max(c=>Vector3.Dot(HexBoardModel.World(c),forward));
        foreach(var layer in layers)if(!layer.enabled||Vector3.Dot(layer.transform.position,forward)<=shopEdge)throw new Exception("Background intersects owned shop bounds");
        var original=layers.Select(x=>x.transform.position).ToArray();
        var before=layers.Select(x=>camera.WorldToViewportPoint(x.bounds.center).x).ToArray();
        try
        {
            Capture(camera,"Parallax-Centre.png");
            camera.transform.position+=camera.transform.right*2;
            environment.Refresh();
            var after=layers.Select(x=>camera.WorldToViewportPoint(x.bounds.center).x).ToArray();
            for(int i=0;i<layers.Length;i++)if(layers[i].transform.position!=original[i])throw new Exception("Background follows camera instead of remaining in world");
            if(Mathf.Abs(after[0]-before[0])<=Mathf.Abs(after[2]-before[2]))throw new Exception("Near layer does not show greater parallax");
            Capture(camera,"Parallax-Pan-Right.png");
            theme.useLayeredBackground=false;environment.Refresh();
            if(!environment.backdrop.enabled||layers.Any(x=>x.enabled))throw new Exception("Fallback panorama invalid");
            Debug.Log("MARKET_LAYERS_PARALLAX_PASSED: fixed world planes, near shift greater than far, expansion clearance and legacy fallback.");
        }
        finally{theme.useLayeredBackground=true;environment.Refresh();camera.transform.SetPositionAndRotation(position,rotation);}
    }
    static void CaptureFloorComparison(HexTileBoard board,HexEnvironmentTheme theme,HexMarketEnvironment environment)
    {
        var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
        bool orthographic=camera.orthographic;float size=camera.orthographicSize,width=theme.floorBlendWidth;int mask=camera.cullingMask;
        var objects=board.authoredTiles.Select(x=>x.surface.gameObject).Append(environment.ground.gameObject).ToArray();
        var layers=objects.Select(x=>x.layer).ToArray();
        try
        {
            foreach(var go in objects)go.layer=31;
            camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=3.3f;
            camera.transform.SetPositionAndRotation(new Vector3(0,12,-.9f),Quaternion.Euler(90,0,0));
            theme.floorBlendWidth=0;board.Refresh();Capture(camera,"Floor-Before.png");
            theme.floorBlendWidth=width;board.Refresh();Capture(camera,"Floor-After.png");
        }
        finally
        {
            theme.floorBlendWidth=width;board.Refresh();camera.cullingMask=mask;camera.orthographic=orthographic;camera.orthographicSize=size;
            camera.transform.SetPositionAndRotation(position,rotation);for(int i=0;i<objects.Length;i++)objects[i].layer=layers[i];
        }
    }
}

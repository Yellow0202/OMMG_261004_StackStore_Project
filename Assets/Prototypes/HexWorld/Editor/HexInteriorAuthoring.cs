using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Imports generated art and persists editable interior prefabs without rewriting the game scene.</summary>
public static class HexInteriorAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    const string Art=Root+"/Art/Interior";
    static readonly string[] Names={"Shelf","Table","Kitchen","Storage","Stairs","Lodging","Entrance"};
    static readonly string[] Characters={"Male-01","Male-02","Male-03","Female-01","Female-02","Female-03"};
    static string captureFolder="2026-10-11-Tavern-Interiors";
    static void SetCurrent(HexTestSettings value)=>typeof(HexTestSettings).GetProperty("Current").SetValue(null,value);
    static Sprite Import(string name,float width=1)
    {
        string path=Art+"/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);importer.spritePixelsPerUnit=texture.width/width;
        var options=new TextureImporterSettings();importer.ReadTextureSettings(options);
        options.spriteAlignment=(int)SpriteAlignment.Custom;options.spritePivot=new Vector2(.5f,0);importer.SetTextureSettings(options);importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static SpriteRenderer Visual(Transform parent,string name,Sprite sprite,Vector3 position)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;
        image.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/CharacterSprite.mat");
        var face=go.AddComponent<HexCameraFacingSprite>();face.sprite=image;face.groundAnchor=go.transform;
        face.settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        return image;
    }
    [MenuItem("Stack Store/Hex World/Author Tavern Interiors")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring interiors.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        SetCurrent(settings);
        Directory.CreateDirectory(Root+"/Prefabs/Interiors");AssetDatabase.Refresh();
        var front=Import("Chair",settings.interior.chairWidth);var back=Import("ChairBack",settings.interior.chairWidth);var side=Import("ChairSide",settings.interior.chairWidth);
        var widths=new[]{1.25f,1.4f,1.5f,1.25f,1.3f,1.45f,1.6f};
        for(int i=0;i<Names.Length;i++)
        {
            var art=Import(Names[i],widths[i]);
            var go=new GameObject(Names[i]+" Interior");var interior=go.AddComponent<HexTileInterior>();interior.kind=(HexTileKind)i;
            interior.furnishing=Visual(go.transform,Names[i]+" / pixel art",art,Vector3.up*settings.interior.groundHeight).transform;
            interior.chairFront=front;interior.chairBack=back;interior.chairSide=side;
            if(i==1)
            {
                interior.chairs=new SpriteRenderer[4];
                for(int s=0;s<4;s++)interior.chairs[s]=Visual(go.transform,"Chair "+(s+1),front,HexShopLayout.SeatOffset(s,4));
            }
            interior.Refresh();
            string path=Root+"/Prefabs/Interiors/"+Names[i]+"Interior.prefab";
            PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
            string definitionName=i==0?"DisplayShelf":Names[i];
            var definition=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/"+definitionName+"Tile.asset");
            definition.interiorPrefab=AssetDatabase.LoadAssetAtPath<HexTileInterior>(path);definition.worldSprite=art;EditorUtility.SetDirty(definition);
        }
        for(int c=0;c<Characters.Length;c++)
        {
            var character=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/CustomerAnimation/"+Characters[c]+".asset");
            character.seated=new Sprite[4];
            for(int d=0;d<4;d++)character.seated[d]=Import(Characters[c]+"-Seated-"+d,.65f);
            EditorUtility.SetDirty(character);
        }
        string wallPath=Art+"/Wall.png";AssetDatabase.ImportAsset(wallPath);
        var wallImporter=(TextureImporter)AssetImporter.GetAtPath(wallPath);wallImporter.textureType=TextureImporterType.Default;
        wallImporter.filterMode=FilterMode.Point;wallImporter.mipmapEnabled=false;wallImporter.textureCompression=TextureImporterCompression.Uncompressed;wallImporter.npotScale=TextureImporterNPOTScale.None;wallImporter.SaveAndReimport();
        var wallMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/SolidWall.mat");
        wallMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(wallPath);EditorUtility.SetDirty(wallMaterial);
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        Validate();CreatePreview(settings);CapturePreview();SetCurrent(null);
        Debug.Log("HEX_INTERIOR_AUTHORED: seven saved furniture prefabs, timber walls, four chairs and six seated character identities.");
    }
    public static void Validate()
    {
        foreach(string name in Names)
        {
            var interior=AssetDatabase.LoadAssetAtPath<HexTileInterior>(Root+"/Prefabs/Interiors/"+name+"Interior.prefab");
            if(!interior||!interior.furnishing.GetComponent<SpriteRenderer>().sprite)throw new Exception("Missing furniture "+name);
        }
        foreach(string name in Characters)
        {
            var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/CustomerAnimation/"+name+".asset");
            if(!set.IsValid||set.seated.Length!=4||Array.Exists(set.seated,s=>!s))throw new Exception("Incomplete seated identity "+name);
        }
        var table=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/TableTile.asset");
        var layout=new HexShopLayout();layout.Floor(0).model.Grant(table,1);
        if(!layout.TryPlace(0,new Vector2Int(1,0),table))throw new Exception("Table placement failed");
        var node=new HexNavNode(0,new Vector2Int(1,0));
        for(int capacity=2;capacity<=4;capacity++)
        {
            layout.Floor(0).use[node.cell]=new HexTileUse(capacity);
            for(int s=0;s<capacity;s++)
            {
                if(!layout.Reserve(101+s,HexTileKind.Table,new HexNavNode(0,Vector2Int.zero),out var reserved,out int index)||index!=s||!reserved.Equals(node))throw new Exception("Seat capacity mismatch");
                if(Vector3.Distance(layout.SeatPosition(node,s),HexBoardModel.World(node.cell)+HexShopLayout.SeatOffset(s,capacity))>.001f)throw new Exception("Chair/guest alignment");
                if(HexShopLayout.Cell(layout.MealPosition(node,s))!=node.cell)throw new Exception("Food outside tabletop cell");
            }
            if(layout.Reserve(999,HexTileKind.Table,new HexNavNode(0,Vector2Int.zero),out _,out _))throw new Exception("Overbooked table");
            layout.Release(101);if(!layout.Reserve(999,HexTileKind.Table,new HexNavNode(0,Vector2Int.zero),out _,out int freed)||freed!=0)throw new Exception("Seat release failed");
        }
        HexDirectServiceChecks.Run();Debug.Log("HEX_INTERIOR_VALIDATED: art references, seating capacities 2/3/4, shared chair anchors, tabletop delivery, full capacity and reuse; existing wall/navigation/service rules.");
    }
    [MenuItem("Stack Store/Hex World/Upgrade Chair Side Views")]
    public static void UpgradeChairSides()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        var previous=HexTestSettings.Current;SetCurrent(settings);
        try
        {
            var side=Import("ChairSide",settings.interior.chairWidth);
            string path=Root+"/Prefabs/Interiors/TableInterior.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try{var interior=prefab.GetComponent<HexTileInterior>();interior.chairSide=side;interior.Refresh();PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var scene=EditorSceneManager.OpenScene(Root+"/Scenes/InteriorPreview.unity");
            foreach(var interior in UnityEngine.Object.FindObjectsByType<HexTileInterior>(FindObjectsSortMode.None))
            {
                if(interior.kind!=HexTileKind.Table)continue;interior.chairSide=side;interior.Refresh();EditorUtility.SetDirty(interior);
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();captureFolder="2026-10-11-Chair-Sides";CapturePreview();
            Debug.Log("HEX_CHAIR_SIDES_AUTHORED: left chair faces right, right chair faces left; shared front/profile height and saved preview.");
        }
        finally{SetCurrent(previous);captureFolder="2026-10-11-Tavern-Interiors";}
    }
    static void CreatePreview(HexTestSettings settings)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Preview Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";
        camera.transform.rotation=Quaternion.Euler(30,0,0);camera.transform.position=new Vector3(0,8,-13);
        camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.19f,.21f);
        new GameObject("Preview Light",typeof(Light)).GetComponent<Light>().type=LightType.Directional;
        var layout=new HexShopLayout();
        for(int i=0;i<Names.Length;i++)
        {
            var definition=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/"+(i==0?"DisplayShelf":Names[i])+"Tile.asset");
            var coordinate=new Vector2Int(i%4,i/4);
            if(i==0)layout.Floor(0).model.SetRootDefinition(definition);
            else {layout.Floor(0).model.Grant(definition,1);if(!layout.TryPlace(0,coordinate,definition))throw new Exception("Preview placement failed");}
            if(i==1)layout.Floor(0).use[coordinate]=new HexTileUse(4);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/HexTile.prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=Names[i]+" Preview Tile";
            var tile=go.GetComponent<HexTileView>();tile.coordinate=coordinate;
            go.transform.position=HexBoardModel.World(coordinate);tile.Show(true,definition);tile.ShowShop(layout,0,true);
            if(tile.labelCanvas)tile.labelCanvas.gameObject.SetActive(false);
        }
        // Refresh shared edges after all neighbours exist; leave the near side open for inspection.
        foreach(var tile in UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None))
        {
            tile.ShowShop(layout,0,true);tile.labelCanvas.gameObject.SetActive(false);
            for(int d=0;d<6;d++)if(HexBoardModel.Directions[d].y<0)tile.walls[d].Show(false);
        }
        var table=GameObject.Find("Table Preview Tile");
        var positions=new[]{Vector3.back,Vector3.forward,Vector3.left,Vector3.right};
        for(int s=0;s<4;s++)
        {
            var set=settings.customerAnimation.catalog.characters[s];var point=table.transform.position+HexShopLayout.SeatOffset(s,4);
            int d=HexCustomerAnimation.SeatedDirection(-positions[s]);
            var image=Visual(table.transform,"Seated guest "+s,set.seated[d],point-table.transform.position);
            image.transform.localScale=Vector3.one*settings.interior.seatedHeight/image.sprite.bounds.size.y;
            var mealPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ServedMeal.prefab");
            var meal=(GameObject)PrefabUtility.InstantiatePrefab(mealPrefab);meal.name="Tabletop meal "+s;
            meal.transform.position=layout.MealPosition(new HexNavNode(0,new Vector2Int(1,0)),s);
            meal.transform.localScale*=settings.interior.mealScale;
        }
        camera.transform.position=new Vector3(4,8,-12);
        foreach(var face in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))face.FaceCamera();
        EditorSceneManager.SaveScene(scene,Root+"/Scenes/InteriorPreview.unity");
    }
    public static void CapturePreview()
    {
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");SetCurrent(settings);
        // Per-renderer shader property blocks are not serialized with a preview scene.
        // Rebuild only that transient rendering input after reopening it for capture.
        var tiles=UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
        Array.Sort(tiles,(a,b)=>a.coordinate.y==b.coordinate.y?a.coordinate.x.CompareTo(b.coordinate.x):a.coordinate.y.CompareTo(b.coordinate.y));
        var layout=new HexShopLayout();
        foreach(var tile in tiles)
        {
            int role=Array.FindIndex(Names,name=>tile.name.StartsWith(name+" Preview"));if(role<0)continue;
            var definition=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/"+(role==0?"DisplayShelf":Names[role])+"Tile.asset");
            if(tile.coordinate==Vector2Int.zero)layout.Floor(0).model.SetRootDefinition(definition);
            else {layout.Floor(0).model.Grant(definition,1);layout.TryPlace(0,tile.coordinate,definition);}
        }
        foreach(var tile in tiles)
        {
            var properties=new MaterialPropertyBlock();properties.SetFloat("_Height",settings.environment.floorHeight);
            HexFloorBlend.Apply(properties,settings.environment,layout,0,tile.coordinate);tile.surface.SetPropertyBlock(properties);
        }
        var camera=Camera.main;var table=GameObject.Find("Table Preview Tile");
        Capture(camera,"Interior-Overview.png");
        foreach(float pitch in new[]{4f,30f,50f})
        {
            camera.orthographicSize=1.9f;camera.transform.rotation=Quaternion.Euler(pitch,0,0);
            camera.transform.position=table.transform.position+Vector3.up*.7f-camera.transform.forward*8;
            foreach(var face in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))face.FaceCamera();
            Capture(camera,"Table-Pitch-"+pitch+".png");
        }
    }
    static void Capture(Camera camera,string name)
    {
        var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try
        {
            target.Create();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            string project=Path.GetDirectoryName(Application.dataPath);
            if(project.EndsWith(Path.Combine("Temp","AnimationValidation")))project=Directory.GetParent(project).Parent.FullName;
            var directory=Path.Combine(project,"ArtReferences",captureFolder);Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name),image.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);}
    }
}

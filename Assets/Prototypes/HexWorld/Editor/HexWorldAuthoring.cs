using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>Editor-only authoring. The resulting scene, UI and prefabs are saved assets.</summary>
public static class HexWorldAuthoring
{
    const string Root = "Assets/Prototypes/HexWorld";
    static Font font;
    static Material tileMaterial, wood, roof, spriteMaterial, ghostMaterial;
    static Sprite[] frames;
    static Sprite whiteSprite;
    static Color Navy = new Color(.055f,.10f,.15f,.94f), Teal = new Color(.10f,.48f,.44f), Ink = new Color(.91f,.96f,.95f);

    [MenuItem("Stack Store/Hex World/Create Authored Prototype")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Root+"/Scenes/HexWorld.unity"))throw new InvalidOperationException("Scene already exists; preserve authored changes.");
        foreach(string dir in new[]{"Scenes","Prefabs","Art","Data"})Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();
        font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
        tileMaterial=Material("TileSurface",new Color(.20f,.27f,.33f));wood=Material("Wood",new Color(.56f,.30f,.13f));roof=Material("Awning",Teal);
        ghostMaterial=Material("PlacementGhost",new Color(.2f,1,.55f,.4f));
        ghostMaterial.SetFloat("_Surface",1);ghostMaterial.SetFloat("_ZWrite",0);ghostMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghostMaterial.renderQueue=3000;
        spriteMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(spriteMaterial,Root+"/Art/CharacterSprite.mat");
        GenerateSprites();
        HexPresentationAuthoring.GenerateArt();
        var tilePrefab=MakeTile();var actorPrefab=MakeActor();
        var table=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prototypes/LegacyUGUI/Localization/PrototypeStrings.asset"));
        table.name="HexWorldStrings";AddStrings(table);
        var empty=table.entries.Find(e=>e.key=="inventory.empty");empty.english="Hold {gold} gold to choose an item.";empty.korean="보유 골드가 {gold}골드에 도달하면 아이템을 선택합니다.";AssetDatabase.CreateAsset(table,Root+"/Data/HexWorldStrings.asset");
        var tile=ScriptableObject.CreateInstance<HexTileDefinition>();tile.icon=Icon("Shelf");AssetDatabase.CreateAsset(tile,Root+"/Data/DisplayShelfTile.asset");
        var catalog=ScriptableObject.CreateInstance<ItemCatalog>();
        foreach(string name in new[]{"food_warm_soup","ability_quick_service","part_display_shelf"})
        {
            var item=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Prototypes/LegacyUGUI/Items/"+name+".asset"));
            item.name=name;item.picture=Icon(item.kind==ItemKind.Food?"Soup":item.kind==ItemKind.Ability?"Service":"Shelf");
            if(item.kind==ItemKind.ShopPart)item.descriptionKey="hex.item.part.description";
            AssetDatabase.CreateAsset(item,Root+"/Data/"+name+".asset");catalog.items.Add(item);
        }
        AssetDatabase.CreateAsset(catalog,Root+"/Data/HexCatalog.asset");var curve=ScriptableObject.CreateInstance<GoldLevelCurve>();AssetDatabase.CreateAsset(curve,Root+"/Data/GoldCurve.asset");
        var actions=MakeActions();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        // Reload persistent prefab assets after replacing the temporary authoring scene.
        tilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/HexTile.prefab").GetComponent<HexTileView>();
        actorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/WorldCustomer.prefab").GetComponent<HexWorldActor>();
        var main=new GameObject("Hex World Prototype");var local=main.AddComponent<LocalizationService>();local.table=table;
        var game=main.AddComponent<HexPrototype>();var board=main.AddComponent<HexTileBoard>();game.board=board;board.game=game;
        game.catalog=catalog;game.curve=curve;game.tileRewards=new[]{new HexTileReward{item=catalog.items[2],tile=tile,count=1}};
        board.tilePrefab=tilePrefab;board.tileTypes=new[]{tile};board.tileRoot=new GameObject("Hex Grid - neutral cells are not owned").transform;board.tileRoot.SetParent(main.transform);
        var views=new List<HexTileView>();
        for(int q=-4;q<=4;q++)for(int r=-4;r<=4;r++)if(Mathf.Abs(q+r)<=4)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(tilePrefab.gameObject);instance.transform.SetParent(board.tileRoot);instance.transform.position=HexBoardModel.World(new Vector2Int(q,r));
            var view=instance.GetComponent<HexTileView>();view.coordinate=new Vector2Int(q,r);view.Show(q==0&&r==0,null);views.Add(view);
        }
        board.authoredTiles=views.ToArray();
        var preview=(GameObject)PrefabUtility.InstantiatePrefab(tilePrefab.gameObject);preview.name="Placement Preview";preview.transform.SetParent(main.transform);board.preview=preview.GetComponent<HexTileView>();
        board.preview.surface.sharedMaterial=ghostMaterial; foreach(var renderer in board.preview.furniture.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=ghostMaterial;
        foreach(var collider in preview.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);preview.SetActive(false);
        var stall=MakeStall();stall.transform.SetParent(main.transform);stall.transform.position=Vector3.up*.22f;
        var player=(GameObject)PrefabUtility.InstantiatePrefab(actorPrefab.gameObject);player.name="Player - fixed starting tile";player.transform.SetParent(main.transform);player.transform.position=new Vector3(0,.22f,-.95f);
        player.GetComponent<HexWorldActor>().body.color=new Color(.95f,.69f,.22f);game.customerPrefab=actorPrefab;
        game.customerRoot=new GameObject("World Customers").transform;game.customerRoot.SetParent(main.transform);
        var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.08f);camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=150;
        var orbit=cameraObject.AddComponent<HexOrbitCamera>();orbit.target=player.transform;game.orbit=orbit;
        cameraObject.transform.rotation=Quaternion.Euler(30,0,0);cameraObject.transform.position=player.transform.position+Vector3.up*.3f-cameraObject.transform.forward*18;
        var lightObject=new GameObject("Warm Directional Light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.color=new Color(1,.91f,.77f);light.shadows=LightShadows.Soft;lightObject.transform.rotation=Quaternion.Euler(50,-35,0);
        RenderSettings.ambientLight=new Color(.50f,.58f,.65f);
        var input=main.AddComponent<HexWorldInput>();input.actions=actions;input.board=board;input.game=game;input.orbit=orbit;
        MakeUI(game);
        foreach(var label in game.GetComponentsInChildren<LocalizedLabel>(true))label.GetComponent<Text>().text=table.Resolve(label.key,DisplayLanguage.Korean);
        game.goldLabel.text=local.Format("hud.gold",new Dictionary<string,object>{{"gold",0}});
        game.levelLabel.text=local.Format("hex.level",new Dictionary<string,object>{{"level",1},{"gold",0},{"next",3}});
        game.serviceLabel.text=local.Format("hex.service",new Dictionary<string,object>{{"seconds","5.0"},{"queue",0}});
        game.tileLabel.text=local.Format("hex.stock",new Dictionary<string,object>{{"name",table.Resolve(tile.nameKey,DisplayLanguage.Korean)},{"stock",2},{"owned",1}});
        game.inventoryLabel.text=local.Format("inventory.empty",new Dictionary<string,object>{{"gold",3}});
        game.statusLabel.text=table.Resolve("hex.help.None",DisplayLanguage.Korean);
        game.levelBar.fillAmount=game.cooldownBar.fillAmount=0;
        game.tileDropdown.ClearOptions();game.tileDropdown.AddOptions(new List<string>{table.Resolve(tile.nameKey,DisplayLanguage.Korean)});
        PrefabUtility.SaveAsPrefabAsset(game.GetComponentInChildren<Canvas>().gameObject,Root+"/Prefabs/HexWorldUI.prefab");
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene,Root+"/Scenes/HexWorld.unity");AssetDatabase.SaveAssets();
        Debug.Log("HEX_AUTHORING_OK: saved 3D scene, independent art/data, UGUI, prefabs and semantic input.");
        HexShopAuthoring.Upgrade();
        HexSurroundAuthoring.Upgrade();
        HexUpgradeItemsAuthoring.Upgrade();
        HexShopExpansionAuthoring.Upgrade();
        HexDirectServiceAuthoring.Upgrade();
    }
    static Material Material(string name,Color color)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(mat,Root+"/Art/"+name+".mat");return mat;
    }
    static Sprite Icon(string name)
    {
        string path=Root+"/Art/"+name+".png";File.Copy("Assets/Prototypes/LegacyUGUI/Items/Icons/"+name+".png",path,true);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void GenerateSprites()
    {
        frames=new Sprite[2];
        for(int frame=0;frame<2;frame++)
        {
            var texture=new Texture2D(40,56,TextureFormat.RGBA32,false);var pixels=new Color[40*56];
            for(int y=0;y<56;y++)for(int x=0;x<40;x++)
            {
                Color c=Color.clear;
                if(x>=11&&x<=28&&y>=31&&y<=47)c=new Color(1,.78f,.57f);
                if(x>=10&&x<=29&&y>=45&&y<=50)c=new Color(.2f,.16f,.16f);
                if(x>=10&&x<=29&&y>=14&&y<=31)c=Color.white;
                if((x>=6&&x<=9||x>=30&&x<=33)&&y>=16&&y<=28)c=new Color(1,.78f,.57f);
                if((x>=11&&x<=17||x>=23&&x<=29)&&y>=3+(frame==0?0:(x<20?2:-2))&&y<14)c=new Color(.15f,.20f,.29f);
                if((x==16||x==25)&&y==39)c=new Color(.14f,.13f,.14f);
                pixels[y*40+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();string path=Root+"/Art/CharacterWalk"+frame+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=42;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();frames[frame]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var white=new Texture2D(2,2);white.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});white.Apply();string whitePath=Root+"/Art/White.png";File.WriteAllBytes(whitePath,white.EncodeToPNG());UnityEngine.Object.DestroyImmediate(white);AssetDatabase.ImportAsset(whitePath);
        var wi=(TextureImporter)AssetImporter.GetAtPath(whitePath);wi.textureType=TextureImporterType.Sprite;wi.spriteImportMode=SpriteImportMode.Single;wi.spritePixelsPerUnit=2;wi.SaveAndReimport();whiteSprite=AssetDatabase.LoadAssetAtPath<Sprite>(whitePath);
    }
    static HexTileView MakeTile()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        vertices.Add(new Vector3(0,.22f,0));
        for(int i=0;i<6;i++){float angle=(60*i+30)*Mathf.Deg2Rad;vertices.Add(new Vector3(Mathf.Cos(angle)*1.4f,.22f,Mathf.Sin(angle)*1.4f));}
        for(int i=0;i<6;i++) {triangles.Add(0);triangles.Add((i+1)%6+1);triangles.Add(i+1);}
        for(int i=0;i<6;i++)
        {
            Vector3 a=vertices[i+1],b=vertices[(i+1)%6+1];int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(new Vector3(a.x,0,a.z));vertices.Add(new Vector3(b.x,0,b.z));
            triangles.AddRange(new[]{start,start+1,start+2,start+1,start+3,start+2});
        }
        var mesh=new Mesh{name="Hexagonal Prism"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Root+"/Art/HexPrism.asset");
        var go=new GameObject("Hex Tile");var view=go.AddComponent<HexTileView>();go.AddComponent<MeshFilter>().sharedMesh=mesh;view.surface=go.AddComponent<MeshRenderer>();view.surface.sharedMaterial=tileMaterial;go.AddComponent<MeshCollider>().sharedMesh=mesh;
        var edge=new GameObject("Tile Edge").AddComponent<LineRenderer>();edge.transform.SetParent(go.transform);edge.useWorldSpace=false;edge.positionCount=7;edge.widthMultiplier=.025f;edge.sharedMaterial=roof;
        for(int i=0;i<7;i++){Vector3 v=vertices[i%6+1];v.y+=.008f;edge.SetPosition(i,v);}
        view.furniture=new GameObject("Installed Shelf");view.furniture.transform.SetParent(go.transform);
        HexPresentationAuthoring.ConfigureTile(view);
        view.furniture.SetActive(false);
        var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/HexTile.prefab").GetComponent<HexTileView>();UnityEngine.Object.DestroyImmediate(go);return saved;
    }
    static HexWorldActor MakeActor()
    {
        var go=new GameObject("2D World Customer");var actor=go.AddComponent<HexWorldActor>();actor.walkFrames=frames;
        var body=new GameObject("Sprite Body");body.transform.SetParent(go.transform);body.transform.localPosition=Vector3.up*.66f;actor.body=body.AddComponent<SpriteRenderer>();actor.body.sprite=frames[0];actor.body.sharedMaterial=spriteMaterial;
        var patience=new GameObject("World Patience UGUI",typeof(RectTransform),typeof(Canvas));patience.transform.SetParent(go.transform);patience.transform.localPosition=new Vector3(0,1.5f,0);patience.transform.localScale=Vector3.one*.006f;
        patience.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;patience.GetComponent<RectTransform>().sizeDelta=new Vector2(105,12);
        actor.patience=Gauge("Patience",patience.transform,Vector2.zero,new Vector2(105,12));actor.patienceCanvas=patience.transform;patience.SetActive(false);
        var shadow=new GameObject("Ground Shadow");shadow.transform.SetParent(go.transform);shadow.transform.localPosition=Vector3.up*.01f;shadow.transform.localRotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(.6f,.30f,1);var sr=shadow.AddComponent<SpriteRenderer>();sr.sprite=whiteSprite;sr.color=new Color(0,0,0,.24f);sr.sharedMaterial=spriteMaterial;actor.shadow=shadow.transform;
        HexPresentationAuthoring.ConfigureActor(actor);
        var saved=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/WorldCustomer.prefab").GetComponent<HexWorldActor>();UnityEngine.Object.DestroyImmediate(go);return saved;
    }
    static GameObject MakeStall()
    {
        var stall=new GameObject("Starting Shop - fixed");HexPresentationAuthoring.ConfigureShop(stall);
        PrefabUtility.SaveAsPrefabAsset(stall,Root+"/Prefabs/StartingShop.prefab");return stall;
    }
    static void Cube(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent);cube.transform.localPosition=position;cube.transform.localScale=scale;cube.GetComponent<MeshRenderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
    }
    static InputActionAsset MakeActions()
    {
        var asset=ScriptableObject.CreateInstance<InputActionAsset>();var map=asset.AddActionMap("World");
        map.AddAction("Point",InputActionType.Value,"<Pointer>/position",expectedControlLayout:"Vector2");map.AddAction("Confirm",InputActionType.Button,"<Mouse>/leftButton");
        map.AddAction("Cancel",InputActionType.Button,"<Keyboard>/escape");map.AddAction("Orbit",InputActionType.Button,"<Mouse>/leftButton");
        map.AddAction("Look",InputActionType.Value,"<Pointer>/delta",expectedControlLayout:"Vector2");map.AddAction("Zoom",InputActionType.Value,"<Mouse>/scroll",expectedControlLayout:"Vector2");
        string path=Root+"/Data/HexWorld.inputactions";File.WriteAllText(path,asset.ToJson());UnityEngine.Object.DestroyImmediate(asset);AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static RectTransform Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
    {
        var rect=Rect(name,parent,pos,size);rect.gameObject.AddComponent<Image>().color=color;return rect;
    }
    static Text Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontsize,string key=null)
    {
        var rect=Rect(name,parent,pos,size);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.fontSize=fontsize;text.color=Ink;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;
        if(key!=null){var localized=rect.gameObject.AddComponent<LocalizedLabel>();localized.key=key;text.text=key;}return text;
    }
    static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string key)
    {
        var rect=Panel(name,parent,pos,size,Teal);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();var text=Label("Label",rect,Vector2.zero,size-new Vector2(8,4),18,key);text.alignment=TextAnchor.MiddleCenter;return button;
    }
    static Image Gauge(string name,Transform parent,Vector2 pos,Vector2 size)
    {
        var background=Panel(name,parent,pos,size,new Color(.18f,.26f,.30f));var rect=Rect("Fill",background,Vector2.zero,size);var image=rect.gameObject.AddComponent<Image>();image.sprite=whiteSprite;image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.color=new Color(.32f,.79f,.65f);image.raycastTarget=false;return image;
    }
    static Slider Slider(string name,Transform parent,Vector2 pos,float min,float max,float value)
    {
        var root=Rect(name,parent,pos,new Vector2(190,30));var slider=root.gameObject.AddComponent<Slider>();Panel("Track",root,Vector2.zero,new Vector2(190,6),new Color(.25f,.35f,.40f));
        var handle=Panel("Handle",root,Vector2.zero,new Vector2(18,25),Teal);slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=min;slider.maxValue=max;slider.value=value;return slider;
    }
    static void MakeUI(HexPrototype game)
    {
        var root=new GameObject("Hex World UGUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(game.transform);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var top=Panel("Header",root.transform,new Vector2(0,395),new Vector2(1560,90),Navy);Label("Title",top,new Vector2(-540,15),new Vector2(420,35),25,"hex.title");
        game.levelLabel=Label("Level",top,new Vector2(-50,17),new Vector2(480,34),20);game.levelBar=Gauge("Gold Level Progress",top,new Vector2(-70,-18),new Vector2(420,8));game.goldLabel=Label("Gold",top,new Vector2(590,0),new Vector2(230,50),30);
        var left=Panel("Inventory",root.transform,new Vector2(-620,-265),new Vector2(310,300),Navy);Label("Title",left,new Vector2(0,119),new Vector2(280,30),21,"hex.inventory");
        game.tileLabel=Label("Tile Stock",left,new Vector2(0,67),new Vector2(278,58),17);game.inventoryLabel=Label("Owned Items",left,new Vector2(0,-30),new Vector2(278,122),17);game.inventoryLabel.alignment=TextAnchor.UpperLeft;
        game.buildButton=Button("Build",left,new Vector2(0,-118),new Vector2(270,40),"hex.button.build");
        var right=Panel("Camera Controls",root.transform,new Vector2(650,-270),new Vector2(250,290),Navy);Label("Title",right,new Vector2(0,112),new Vector2(210,32),20,"hex.camera");
        Label("Pitch",right,new Vector2(0,74),new Vector2(210,26),16,"hex.camera.pitch");game.pitchSlider=Slider("Pitch Slider",right,new Vector2(0,40),25,65,30);
        Label("Zoom",right,new Vector2(0,2),new Vector2(210,26),16,"hex.camera.zoom");game.zoomSlider=Slider("Zoom Slider",right,new Vector2(0,-32),.6f,1.8f,1);
        game.resetCameraButton=Button("Reset Camera",right,new Vector2(0,-100),new Vector2(210,38),"hex.camera.reset");
        var service=Panel("Service Status",root.transform,new Vector2(0,-365),new Vector2(560,100),Navy);game.serviceLabel=Label("Service",service,new Vector2(0,15),new Vector2(520,34),18);game.cooldownBar=Gauge("Cooldown",service,new Vector2(0,-20),new Vector2(510,10));
        game.testGoldButton=Button("Test Gold",root.transform,new Vector2(645,293),new Vector2(245,40),"hex.test.gold");
        var help=Panel("Status",root.transform,new Vector2(0,303),new Vector2(1000,63),Navy);game.statusLabel=Label("Status Text",help,Vector2.zero,new Vector2(950,60),17);game.statusLabel.alignment=TextAnchor.MiddleCenter;
        Label("Camera Input Help",root.transform,new Vector2(0,-437),new Vector2(1000,24),15,"hex.camera.help").alignment=TextAnchor.MiddleCenter;
        var building=Panel("Build Toolbar",root.transform,new Vector2(0,-265),new Vector2(590,95),Navy);game.buildingPanel=building.gameObject;
        game.placeButton=Button("Place Mode",building,new Vector2(-218,15),new Vector2(130,37),"hex.button.place");game.moveButton=Button("Move Mode",building,new Vector2(-75,15),new Vector2(130,37),"hex.button.move");
        game.recoverButton=Button("Recover Mode",building,new Vector2(68,15),new Vector2(130,37),"hex.button.recover");game.endBuildButton=Button("Finish Building",building,new Vector2(211,15),new Vector2(130,37),"hex.button.finish");
        var dropdown=DefaultControls.CreateDropdown(new DefaultControls.Resources());dropdown.name="Tile Type";dropdown.transform.SetParent(building,false);var dropdownRect=dropdown.GetComponent<RectTransform>();dropdownRect.anchoredPosition=new Vector2(0,-28);dropdownRect.sizeDelta=new Vector2(330,27);game.tileDropdown=dropdown.GetComponent<Dropdown>();
        var modal=Panel("Level Up Choice",root.transform,Vector2.zero,new Vector2(1600,900),new Color(0,.02f,.03f,.84f));game.choicePanel=modal.gameObject;
        Label("Title",modal,new Vector2(0,245),new Vector2(800,70),32,"hex.choice.title").alignment=TextAnchor.MiddleCenter;
        Label("Explanation",modal,new Vector2(0,183),new Vector2(1000,50),19,"hex.choice.help").alignment=TextAnchor.MiddleCenter;
        game.cards=new HexChoiceCard[3];
        for(int i=0;i<3;i++)
        {
            var button=Button("Choice Card "+i,modal,new Vector2((i-1)*385,-5),new Vector2(350,320),"button.select");button.GetComponentInChildren<Text>().GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-128);
            var card=new HexChoiceCard{button=button};card.icon=Panel("Icon",button.transform,new Vector2(0,89),new Vector2(85,85),Color.white).GetComponent<Image>();card.icon.preserveAspect=true;card.icon.raycastTarget=false;
            card.title=Label("Item Name and Type",button.transform,new Vector2(0,15),new Vector2(318,64),20);card.title.alignment=TextAnchor.MiddleCenter;
            card.description=Label("Description",button.transform,new Vector2(0,-70),new Vector2(306,100),18);card.description.alignment=TextAnchor.MiddleCenter;game.cards[i]=card;
        }
        game.continueButton=Button("Continue",modal,new Vector2(0,-240),new Vector2(260,45),"button.continue");
        root.AddComponent<PrototypeFontBinding>();building.gameObject.SetActive(false);modal.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/HexWorldUI.prefab");
    }
    static void AddStrings(LocalizationTable table)
    {
        void Add(string key,string en,string ko){table.entries.Add(new TranslationEntry{key=key,english=en,korean=ko});}
        Add("hex.title","STACK STORE / HEX WORLD","스택 스토어 / 육각 영역");Add("hex.inventory","TILES & OWNED ITEMS","보관 타일 · 보유 아이템");
        Add("hex.tile.shelf","Display shelf tile","진열 선반 타일");Add("hex.tile.description","Expand your shop with an adjacent shelf tile.","소유 영역에 인접한 칸에 진열 선반 타일을 설치합니다.");
        Add("hex.level","LEVEL {level} / GOLD {gold} / NEXT {next}","레벨 {level} / 보유 {gold} / 다음 {next}골드");Add("hex.stock","{name}: {stock}\nOwned tiles: {owned}","{name}: {stock}개\n소유 타일: {owned}칸");
        Add("hex.service","Next throw {seconds}s / Waiting {queue}","다음 배부 {seconds}초 / 대기 {queue}명");
        Add("hex.button.build","BUILD / PAUSE","타일 배치 · 일시정지");Add("hex.button.place","PLACE","설치");Add("hex.button.move","MOVE","이동");Add("hex.button.recover","RECOVER","회수");Add("hex.button.finish","FINISH","배치 종료");
        Add("hex.help.None","Only installed tiles belong to you. Expand from your starting shop.","부품이 설치된 타일만 소유 영역입니다. 시작 가게에서 이어서 확장하세요.");
        Add("hex.help.Place","PAUSED: select a green adjacent cell. Red cells cannot be placed.","일시정지 중 · 녹색 인접 칸을 클릭해 설치하세요. 빨간 칸에는 설치할 수 없습니다.");
        Add("hex.help.Move","PAUSED: select an installed part, then its new location. Keep the shop connected.","일시정지 중 · 설치된 부품을 선택한 후 이동할 칸을 클릭하세요. 영역은 연결되어야 합니다.");
        Add("hex.help.Recover","PAUSED: click a part to return it to inventory. The starting tile is fixed.","일시정지 중 · 부품을 클릭하면 보관함으로 회수합니다. 시작 타일은 고정입니다.");
        Add("hex.move.destination","Select a destination. The original part remains until confirmed.","이동할 위치를 선택하세요. 확정 전까지 기존 타일은 유지됩니다.");
        Add("hex.build.success","Done. The shop remains connected.","완료했습니다. 소유 영역의 연결을 유지했습니다.");Add("hex.build.invalid","Unavailable: check stock, adjacency, occupied cells and connectivity.","설치할 수 없습니다. 보관 수량·인접 여부·빈 칸·영역 연결을 확인하세요.");
        Add("hex.camera","CAMERA","카메라");Add("hex.camera.pitch","Viewing angle 25–65°","내려다보기 각도 25–65°");Add("hex.camera.zoom","Zoom distance 0.6–1.8×","줌 거리 0.6–1.8배");Add("hex.camera.reset","RESET VIEW","시점 초기화");
        Add("hex.camera.help","Scroll: zoom / Right drag: viewing angle / Build mode: simulation paused","휠: 줌 · 오른쪽 드래그: 시선 높낮이 · 배치 모드: 게임 일시정지");
        Add("hex.test.gold","TEST: GOLD +1","테스트: 골드 +1");Add("hex.choice.title","LEVEL UP / CHOOSE ONE","레벨 상승 / 아이템 선택");
        Add("hex.choice.help","Gold is retained. Shop parts add a tile to inventory.","골드는 소비되지 않습니다. 가게 부품을 선택하면 보관 타일을 얻습니다.");
        Add("hex.item.title","{name}\n{kind} · LEVEL {level}","{name}\n{kind} · 레벨 {level}");
        Add("hex.item.part.description","Gain 1 display shelf tile. Visitor chance +{VisitorChance}% per level.","진열 선반 타일 1개 획득. 레벨당 가게 방문 확률 +{VisitorChance}%.");
    }
}

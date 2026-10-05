using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Upgrade saved art and prefabs without rebuilding UI or gameplay data.</summary>
public static class HexPresentationAuthoring
{
    const string Root = "Assets/Prototypes/HexWorld";
    static Material SpriteMaterial => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/CharacterSprite.mat");

    public static void GenerateArt()
    {
        Paint("ShopFront",96,96,(x,y) =>
        {
            Color color=Color.clear;
            if(x>=10&&x<86&&y>=6&&y<42)color=new Color(.57f,.30f,.14f);
            if(x>=15&&x<81&&y>=11&&y<37)color=new Color(.72f,.43f,.21f);
            if(x>=9&&x<87&&y>=39&&y<46)color=new Color(.92f,.65f,.31f);
            if((x>=13&&x<19||x>=77&&x<83)&&y>=42&&y<76)color=new Color(.34f,.20f,.13f);
            if(x>=5&&x<91&&y>=71&&y<88)color=new Color(.12f,.57f,.47f);
            if(x>=8&&x<88&&y>=83&&y<89)color=new Color(.28f,.76f,.61f);
            if(x>=5&&x<91&&y>=68&&y<75)color=((x-5)/12)%2==0?new Color(.91f,.83f,.57f):new Color(.13f,.47f,.38f);
            if(x>=35&&x<62&&y>=51&&y<65)color=new Color(.96f,.83f,.51f);
            if(x>=40&&x<57&&y>=56&&y<60)color=new Color(.32f,.21f,.13f);
            if(x>=25&&x<39&&y>=46&&y<54)color=new Color(.9f,.37f,.16f);
            if(x>=60&&x<72&&y>=46&&y<58)color=new Color(.68f,.83f,.45f);
            if(x>=23&&x<74&&y==12)color=new Color(.45f,.24f,.13f);
            return color;
        },40);
        Paint("ShelfFront",64,72,(x,y) =>
        {
            Color color=Color.clear;
            if((x>=8&&x<14||x>=50&&x<56)&&y>=4&&y<65)color=new Color(.69f,.40f,.19f);
            if(x>=8&&x<56&&(y>=7&&y<13||y>=33&&y<39||y>=61&&y<66))color=new Color(.89f,.64f,.30f);
            if(x>=15&&x<49&&y>=14&&y<32)color=new Color(.18f,.28f,.27f);
            if(x>=15&&x<49&&y>=40&&y<60)color=new Color(.19f,.31f,.29f);
            if(x>=18&&x<28&&y>=16&&y<27)color=new Color(.91f,.42f,.22f);
            if(x>=33&&x<45&&y>=16&&y<28)color=new Color(.77f,.84f,.40f);
            if(x>=17&&x<26&&y>=42&&y<55)color=new Color(.37f,.78f,.65f);
            if(x>=31&&x<42&&y>=42&&y<55)color=new Color(.96f,.79f,.34f);
            return color;
        },40);
        Paint("GroundContactShadow",64,32,(x,y) =>
        {
            float radius=Mathf.Pow((x-31.5f)/31.5f,2)+Mathf.Pow((y-15.5f)/15.5f,2);
            return new Color(0,0,0,Mathf.Clamp01(1-radius)*.33f);
        },32,false);
        foreach(string name in new[]{"CharacterWalk0","CharacterWalk1"})SetBottomPivot(Root+"/Art/"+name+".png");
    }
    static void Paint(string name,int width,int height,System.Func<int,int,Color> pixel,int ppu,bool bottom=true)
    {
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=pixel(x,y);
        texture.SetPixels(pixels);texture.Apply();string path=Root+"/Art/"+name+".png";
        File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=ppu;importer.filterMode=bottom?FilterMode.Point:FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();if(bottom)SetBottomPivot(path);
    }
    static void SetBottomPivot(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);importer.SetTextureSettings(settings);importer.SaveAndReimport();
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+name+".png");
    static SpriteRenderer Visual(Transform parent,string name,string art,Vector3 offset)
    {
        var child=new GameObject(name);child.transform.SetParent(parent,false);child.transform.localPosition=offset;
        var renderer=child.AddComponent<SpriteRenderer>();renderer.sprite=Sprite(art);renderer.sharedMaterial=SpriteMaterial;
        var facing=child.AddComponent<HexCameraFacingSprite>();facing.sprite=renderer;facing.groundAnchor=child.transform;return renderer;
    }
    static void Shadow(Transform parent,Vector3 offset,Vector3 scale)
    {
        var child=new GameObject("Ground Contact Shadow");child.transform.SetParent(parent,false);child.transform.localPosition=offset;child.transform.localRotation=Quaternion.Euler(90,0,0);child.transform.localScale=scale;
        var renderer=child.AddComponent<SpriteRenderer>();renderer.sprite=Sprite("GroundContactShadow");renderer.sharedMaterial=SpriteMaterial;
    }
    static void ClearChildren(Transform parent){for(int i=parent.childCount-1;i>=0;i--)Object.DestroyImmediate(parent.GetChild(i).gameObject);}
    public static void ConfigureTile(HexTileView tile)
    {
        ClearChildren(tile.furniture.transform);
        tile.buildingSprite=Visual(tile.furniture.transform,"Shelf Front Sprite","ShelfFront",new Vector3(0,.22f,.15f));
        Shadow(tile.furniture.transform,new Vector3(0,.228f,.15f),new Vector3(.8f,.65f,1));tile.furniture.SetActive(false);
    }
    public static void ConfigureActor(HexWorldActor actor)
    {
        actor.body.transform.localPosition=Vector3.zero;
        var facing=actor.body.GetComponent<HexCameraFacingSprite>();if(!facing)facing=actor.body.gameObject.AddComponent<HexCameraFacingSprite>();
        facing.sprite=actor.body;facing.groundAnchor=actor.transform;
        var shadow=actor.shadow.GetComponent<SpriteRenderer>();shadow.sprite=Sprite("GroundContactShadow");shadow.color=Color.white;actor.shadow.localScale=new Vector3(.35f,.45f,1);
    }
    public static void ConfigureShop(GameObject shop)
    {
        ClearChildren(shop.transform);Visual(shop.transform,"Shop Front Sprite","ShopFront",Vector3.zero);
        Shadow(shop.transform,Vector3.up*.008f,new Vector3(1.15f,.8f,1));
    }
    [MenuItem("Stack Store/Hex World/Apply 2D Front Presentation")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode before authoring.");
        GenerateArt();
        foreach(string name in new[]{"HexTile","WorldCustomer","StartingShop"})
        {
            string path=Root+"/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            if(name=="HexTile")ConfigureTile(root.GetComponent<HexTileView>());
            if(name=="WorldCustomer")ConfigureActor(root.GetComponent<HexWorldActor>());
            if(name=="StartingShop")ConfigureShop(root);
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        string uiPath=Root+"/Prefabs/HexWorldUI.prefab";var ui=PrefabUtility.LoadPrefabContents(uiPath);
        foreach(var slider in ui.GetComponentsInChildren<UnityEngine.UI.Slider>(true))if(slider.name=="Pitch Slider")slider.SetValueWithoutNotify(30);
        PrefabUtility.SaveAsPrefabAsset(ui,uiPath);PrefabUtility.UnloadPrefabContents(ui);
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=Object.FindFirstObjectByType<HexPrototype>();
        ConfigureShop(GameObject.Find("Starting Shop - fixed"));
        ConfigureActor(GameObject.Find("Player - fixed starting tile").GetComponent<HexWorldActor>());
        // Preview was a scene instance with old mesh material overrides; clear the obsolete geometry.
        if(PrefabUtility.IsPartOfPrefabInstance(game.board.preview))PrefabUtility.UnpackPrefabInstance(game.board.preview.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        ConfigureTile(game.board.preview);game.board.preview.furniture.SetActive(false);
        var orbit=game.orbit;orbit.pitch=30;orbit.yaw=0;orbit.zoom=1;
        orbit.transform.rotation=Quaternion.Euler(30,0,0);orbit.transform.position=orbit.target.position+Vector3.up*.3f-orbit.transform.forward*orbit.baseDistance;
        game.pitchSlider.SetValueWithoutNotify(30);
        foreach(var facing in Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsInactive.Include,FindObjectsSortMode.None))facing.FaceCamera();
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_2D_AUTHORING_PASS: front sprites, bottom pivots, camera-facing art and frontal starting view saved.");
    }
}

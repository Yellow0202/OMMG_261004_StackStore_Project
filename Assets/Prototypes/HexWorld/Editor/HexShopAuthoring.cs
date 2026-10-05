using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Save UGUI shop controls and product data in assets; no runtime UI generation.</summary>
public static class HexShopAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static Font font;
    static Color Navy=new Color(.045f,.08f,.12f,.97f), Teal=new Color(.10f,.48f,.44f);
    [MenuItem("Stack Store/Hex World/Add Shop And Outline Presentation")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode before authoring.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/ShopUI.prefab"))throw new System.InvalidOperationException("Shop already exists; preserve authored changes.");
        font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
        var outlineMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/TileOutline.mat");
        if(!outlineMaterial){outlineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));outlineMaterial.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(outlineMaterial,Root+"/Art/TileOutline.mat");}
        // Sprite shader multiplies vertex colors, so LineRenderer colors represent ownership and hover.
        outlineMaterial.shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        outlineMaterial.SetColor("_Color",Color.white);
        var tileRoot=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/HexTile.prefab");ConfigureOutline(tileRoot.GetComponent<HexTileView>(),outlineMaterial);
        PrefabUtility.SaveAsPrefabAsset(tileRoot,Root+"/Prefabs/HexTile.prefab");PrefabUtility.UnloadPrefabContents(tileRoot);
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        Add(table,"hex.level","LEVEL {level} / EARNED {earned} / NEXT {next}","레벨 {level} / 누적 {earned} / 다음 {next}골드");
        Add(table,"inventory.empty","Reach {gold} total earned gold to choose an item.","누적 {gold}골드에 도달하면 아이템을 선택합니다.");
        Add(table,"hex.camera.pitch","Viewing angle 0–65°","내려다보기 각도 0–65°");
        Add(table,"shop.title","SHOP","상점");Add(table,"shop.open","OPEN SHOP","상점 열기");Add(table,"shop.close","CLOSE / RESUME","닫기 · 게임 재개");
        Add(table,"shop.buy","BUY","구매");Add(table,"shop.use","USE ONE","1개 사용");Add(table,"shop.price","{price} GOLD","{price}골드");
        Add(table,"shop.balance","Available: {gold} / Total earned: {earned}","보유 골드: {gold} / 누적 획득: {earned}");
        Add(table,"shop.help","Shopping pauses the game. Spending never reduces level progress.","상점에서는 게임이 멈춥니다. 구매해도 누적 골드와 레벨 진행은 유지됩니다.");
        Add(table,"shop.insufficient","Not enough gold.","보유 골드가 부족합니다.");Add(table,"shop.max","Already at maximum item level.","최대 레벨에 도달한 아이템입니다.");
        Add(table,"shop.purchased","Purchased. Check your tile, upgrade or consumable inventory.","구매했습니다. 보관 타일·보유 강화·소모품 수량을 확인하세요.");Add(table,"shop.used","Buff activated. Its timer starts when gameplay resumes.","버프를 사용했습니다. 게임이 재개되면 남은 시간이 감소합니다.");
        Add(table,"shop.buff.active","A service buff is already active.","접대 버프가 이미 적용 중입니다.");
        Add(table,"shop.quick","USE SPEED POTION ({count})","가속 물약 사용 ({count}개)");Add(table,"shop.buff.status","Speed buff: {seconds}s left","접대 가속 남은 시간: {seconds}초");
        Add(table,"shop.tile.name","SHOP PART / Shelf tile","가게 부품 · 진열 선반 타일");Add(table,"shop.tile.desc","Gain {count} shelf tile. Place it next to an owned tile.","진열 선반 타일 {count}개 획득. 소유 타일에 인접한 칸에 설치합니다.");
        Add(table,"shop.upgrade.name","PERMANENT / Quick service","영구 강화 · 빠른 접대");Add(table,"shop.upgrade.desc","Increase the quick service item level by 1. Service speed +15% per level.","빠른 접대 아이템의 레벨을 1 올립니다. 레벨당 접대 속도 +15%.");
        Add(table,"shop.potion.name","CONSUMABLE / Speed potion","일회성 버프 · 접대 가속 물약");Add(table,"shop.potion.desc","Store {count} potion. Use for +{speed}% service speed for {seconds}s. Does not stack while active.","물약 {count}개 보관. 사용 시 {seconds}초 동안 접대 속도 +{speed}%. 효과 중에는 중복 사용하지 않습니다.");
        EditorUtility.SetDirty(table);table.Rebuild();
        var offers=new[]{Offer("ShelfOffer",HexShopKind.Tile,3,"shop.tile", "Shelf"),Offer("ServiceOffer",HexShopKind.Upgrade,5,"shop.upgrade","Service"),Offer("PotionOffer",HexShopKind.Consumable,2,"shop.potion","Soup")};
        offers[0].tile=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/DisplayShelfTile.asset");offers[1].upgrade=AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/ability_quick_service.asset");
        foreach(var offer in offers)EditorUtility.SetDirty(offer);
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=Object.FindFirstObjectByType<HexPrototype>();
        game.orbit.minimumPitch=0;game.pitchSlider.minValue=0;ConfigureOutline(game.board.preview,outlineMaterial);
        foreach(var view in game.board.authoredTiles)ConfigureOutline(view,outlineMaterial);
        foreach(var label in game.GetComponentsInChildren<LocalizedLabel>(true))label.GetComponent<Text>().text=table.Resolve(label.key,DisplayLanguage.Korean);
        var canvas=game.GetComponentInChildren<Canvas>();
        if(game.GetComponentInChildren<HexShopSystem>(true))throw new System.InvalidOperationException("Shop already exists; preserve authored changes.");
        var root=Rect("Shop UGUI",canvas.transform,Vector2.zero,new Vector2(1600,900));var shop=root.gameObject.AddComponent<HexShopSystem>();shop.game=game;shop.quickConsumable=offers[2];
        shop.openButton=Button("Open Shop",root,new Vector2(645,242),new Vector2(245,40),"shop.open");
        shop.quickUseButton=Button("Use Consumable",root,new Vector2(645,193),new Vector2(245,40),null);shop.quickUseLabel=shop.quickUseButton.GetComponentInChildren<Text>();
        shop.buffLabel=Text("Active Buff",root,new Vector2(645,151),new Vector2(260,30),15);
        var panel=Panel("Shop Modal",root,Vector2.zero,new Vector2(1600,900),new Color(.01f,.025f,.04f,.95f));shop.panel=panel.gameObject;
        Text("Title",panel,new Vector2(0,287),new Vector2(700,60),32,"shop.title").alignment=TextAnchor.MiddleCenter;
        shop.balanceLabel=Text("Balance",panel,new Vector2(0,229),new Vector2(1000,44),23);shop.balanceLabel.alignment=TextAnchor.MiddleCenter;
        shop.messageLabel=Text("Message",panel,new Vector2(0,-228),new Vector2(1300,56),20);shop.messageLabel.alignment=TextAnchor.MiddleCenter;
        shop.rows=new HexShopRow[3];
        for(int i=0;i<3;i++)
        {
            var card=Panel("Product "+i,panel,new Vector2((i-1)*400,0),new Vector2(375,370),Navy);var row=new HexShopRow{offer=offers[i]};
            row.icon=Panel("Icon",card,new Vector2(0,120),new Vector2(72,72),Color.white).GetComponent<Image>();row.icon.preserveAspect=true;row.icon.sprite=offers[i].picture;
            row.title=Text("Name",card,new Vector2(0,58),new Vector2(345,55),20);row.title.alignment=TextAnchor.MiddleCenter;
            row.description=Text("Description",card,new Vector2(0,-13),new Vector2(345,96),18);row.description.alignment=TextAnchor.MiddleCenter;
            row.price=Text("Price",card,new Vector2(0,-82),new Vector2(300,30),22);row.price.alignment=TextAnchor.MiddleCenter;
            row.buy=Button("Buy",card,new Vector2(i==2?-83:0,-134),new Vector2(i==2?155:280,40),"shop.buy");
            row.use=Button("Use",card,new Vector2(83,-134),new Vector2(155,40),"shop.use");row.use.gameObject.SetActive(i==2);shop.rows[i]=row;
            row.title.text=table.Resolve(offers[i].nameKey,DisplayLanguage.Korean);row.description.text=game.GetComponent<LocalizationService>().Format(offers[i].descriptionKey,new Dictionary<string,object>{{"count",1},{"speed",100},{"seconds",20}});row.price.text=offers[i].price+"";
        }
        shop.closeButton=Button("Close Shop",panel,new Vector2(0,-295),new Vector2(330,48),"shop.close");
        root.gameObject.AddComponent<PrototypeFontBinding>();
        foreach(var label in root.GetComponentsInChildren<LocalizedLabel>(true))label.GetComponent<Text>().text=table.Resolve(label.key,DisplayLanguage.Korean);
        shop.quickUseLabel.text=table.Resolve("shop.quick",DisplayLanguage.Korean).Replace("{count}","0");shop.buffLabel.text=table.Resolve("shop.buff.status",DisplayLanguage.Korean).Replace("{seconds}","0.0");
        shop.balanceLabel.text=table.Resolve("shop.balance",DisplayLanguage.Korean).Replace("{gold}","0").Replace("{earned}","0");shop.messageLabel.text=table.Resolve("shop.help",DisplayLanguage.Korean);
        panel.gameObject.SetActive(false);shop.game=null;PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/ShopUI.prefab");shop.game=game;
        var ui=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/HexWorldUI.prefab");foreach(var slider in ui.GetComponentsInChildren<Slider>(true))if(slider.name=="Pitch Slider")slider.minValue=0;
        foreach(var label in ui.GetComponentsInChildren<LocalizedLabel>(true))label.GetComponent<Text>().text=table.Resolve(label.key,DisplayLanguage.Korean);
        PrefabUtility.SaveAsPrefabAsset(ui,Root+"/Prefabs/HexWorldUI.prefab");PrefabUtility.UnloadPrefabContents(ui);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_SHOP_AUTHORING_PASS: outline tiles, horizontal camera and authored three-product shop.");
        RefreshProductData();
    }
    public static void RefreshProductData()
    {
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            Color c=Color.clear;
            if(x>=9&&x<=23&&y>=4&&y<=19)c=new Color(.26f,.87f,.85f);
            if(x>=13&&x<=19&&y>=20&&y<=25)c=new Color(.75f,.98f,1);
            if(x>=12&&x<=20&&y>=25&&y<=28)c=new Color(.75f,.49f,.23f);
            if(x>=11&&x<=13&&y>=8&&y<=16)c=new Color(.8f,1,1);
            pixels[y*32+x]=c;
        }
        texture.SetPixels(pixels);texture.Apply();string path=Root+"/Art/SpeedPotion.png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.SaveAndReimport();
        var potion=AssetDatabase.LoadAssetAtPath<HexShopOffer>(Root+"/Data/PotionOffer.asset");potion.picture=AssetDatabase.LoadAssetAtPath<Sprite>(path);EditorUtility.SetDirty(potion);
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");Add(table,"shop.upgrade.desc","Gain 1 permanent item level.\n{effect}","영구 강화 아이템 레벨 +1.\n{effect}");table.Rebuild();EditorUtility.SetDirty(table);
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=Object.FindFirstObjectByType<HexPrototype>();var shop=game.GetComponentInChildren<HexShopSystem>(true);var local=game.GetComponent<LocalizationService>();
        foreach(var row in shop.rows)
        {
            var data=new Dictionary<string,object>{{"count",row.offer.quantity},{"seconds",row.offer.buffSeconds},{"speed",row.offer.speedBonus*100},{"price",row.offer.price},{"effect",""}};
            if(row.offer.upgrade){var effects=new Dictionary<string,object>();foreach(var effect in row.offer.upgrade.effects)effects[effect.kind.ToString()]=effect.amountPerLevel*(effect.kind==ItemEffectKind.CustomerPatienceSeconds?1:100);data["effect"]=local.Format(row.offer.upgrade.descriptionKey,effects);}
            row.icon.sprite=row.offer.picture;row.price.text=local.Format("shop.price",data);row.description.text=local.Format(row.offer.descriptionKey,data);
        }
        var root=shop.gameObject;shop.game=null;PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/ShopUI.prefab");shop.game=game;EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    static void ConfigureOutline(HexTileView view,Material material)
    {
        view.surface.enabled=false;view.outline=view.GetComponentInChildren<LineRenderer>();view.outline.sharedMaterial=material;view.outline.widthMultiplier=.035f;view.outline.sortingOrder=-32000;
        Color color=view.coordinate==Vector2Int.zero?new Color(.25f,.85f,.65f):new Color(.4f,.48f,.55f);view.outline.startColor=view.outline.endColor=color;
    }
    static HexShopOffer Offer(string key,HexShopKind kind,int price,string strings,string icon)
    {
        var offer=AssetDatabase.LoadAssetAtPath<HexShopOffer>(Root+"/Data/"+key+".asset");if(!offer){offer=ScriptableObject.CreateInstance<HexShopOffer>();AssetDatabase.CreateAsset(offer,Root+"/Data/"+key+".asset");}
        offer.key=key;offer.kind=kind;offer.price=price;offer.nameKey=strings+".name";offer.descriptionKey=strings+".desc";offer.picture=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+icon+".png");return offer;
    }
    static void Add(LocalizationTable table,string key,string en,string ko){var entry=table.entries.Find(x=>x.key==key);if(entry==null){entry=new TranslationEntry{key=key};table.entries.Add(entry);}entry.english=en;entry.korean=ko;}
    static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;}
    static RectTransform Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color){var rect=Rect(name,parent,pos,size);rect.gameObject.AddComponent<Image>().color=color;return rect;}
    static Text Text(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize,string key=null){var rect=Rect(name,parent,pos,size);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;if(key!=null)rect.gameObject.AddComponent<LocalizedLabel>().key=key;return text;}
    static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string key){var rect=Panel(name,parent,pos,size,Teal);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();Text("Label",rect,Vector2.zero,size,18,key).alignment=TextAnchor.MiddleCenter;return button;}
}

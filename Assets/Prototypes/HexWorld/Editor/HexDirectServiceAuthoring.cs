using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Save direct service, individual gauges and edge controls as editable UGUI assets.</summary>
public static class HexDirectServiceAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static Font font;
    static LocalizationTable strings;
    static Sprite white;
    [MenuItem("Stack Store/Hex World/Upgrade Direct Service And Edge Controls")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
        font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);
        white=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/White.png");
        strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        Add("wall.icon.add","+","+");Add("wall.icon.remove","−","−");
        Add("service.food.slot","FOOD {number}\n{state}","음식 {number}\n{state}");
        Add("service.ready","READY","준비 완료");Add("service.cooking","PREPARING","준비 중");Add("service.paused","AT STATION","현장 준비 대기");Add("service.blocked","CHECK ROUTE","통로 확인");
        Add("service.summary","Waiting {queue} / Ready {ready} of {count}","배달 대기 {queue}명 / 준비 {ready}·{count}개");
        Add("service.build.worker","The worker is on this tile. Wait until they leave before moving or recovering it.","플레이어가 서 있는 타일입니다. 이동한 뒤 옮기거나 회수하세요.");
        Add("hex.camera.pitch","Viewing angle 4–50°","내려다보기 각도 4–50°");
        Add("hex.camera.help","Wheel: zoom / Left drag: viewing angle / Click: tile / Build mode: paused","휠: 줌 · 왼쪽 드래그: 시선 높낮이 · 클릭: 타일 선택 · 배치 중 일시정지");
        Add("hex.help.Walls","Select an owned tile. Green + builds; red − removes a wall.","소유 타일 선택 · 외곽의 녹색 +는 벽 설치, 빨간 −는 벽 철거입니다.");
        Add("hex.item.extra_servings.description","Prepare +{FoodThrowCount} additional dish per level. Each dish has its own gauge. MAX 3.","레벨당 준비 가능한 음식 +{FoodThrowCount}개. 음식마다 준비 게이지를 따로 관리합니다. 최대 3레벨.");
        var extra=AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/ExtraServingsItem.asset");
        if(!extra){foreach(var item in AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root+"/Data/HexCatalog.asset").items)if(item.key=="ability_extra_servings")extra=item;}
        if(extra)Add(extra.descriptionKey,"Prepare +{FoodThrowCount} additional dish per level. Each dish has its own gauge. MAX 3.","레벨당 준비 가능한 음식 +{FoodThrowCount}개. 음식마다 준비 게이지를 따로 관리합니다. 최대 3레벨.");
        strings.Rebuild();EditorUtility.SetDirty(strings);

        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");
        var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        var service=game.GetComponent<HexFoodService>();if(!service)service=game.gameObject.AddComponent<HexFoodService>();game.service=service;service.game=game;service.worker=game.board.player.GetComponent<HexWorldActor>();
        service.servedFoodPrefab=MakeMeal();
        var held=service.worker.transform.Find("Carried Food");
        if(!held){var instance=(GameObject)PrefabUtility.InstantiatePrefab(service.servedFoodPrefab.gameObject,service.worker.transform);instance.name="Carried Food";instance.transform.localPosition=new Vector3(.4f,.85f,0);held=instance.transform;}
        service.carriedFood=held.GetComponent<SpriteRenderer>();held.gameObject.SetActive(false);

        var canvas=game.buildButton.GetComponentInParent<Canvas>();
        Transform existing=null;var duplicates=new System.Collections.Generic.List<GameObject>();
        foreach(Transform child in canvas.transform)if(child.name=="Individual Food Gauges"||child.name=="FoodPreparationUI")
        {if(!existing)existing=child;else duplicates.Add(child.gameObject);}
        foreach(var duplicate in duplicates)UnityEngine.Object.DestroyImmediate(duplicate);
        if(!existing)
        {
            var ui=FoodUI(canvas.transform);var prefab=PrefabUtility.SaveAsPrefabAsset(ui,Root+"/Prefabs/FoodPreparationUI.prefab");UnityEngine.Object.DestroyImmediate(ui);existing=((GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform)).transform;
        }
        existing.name="Individual Food Gauges";
        service.gauges=new Image[4];service.labels=new Text[4];service.slotViews=new GameObject[4];
        for(int i=0;i<4;i++){var slot=existing.GetChild(i);service.slotViews[i]=slot.gameObject;service.labels[i]=slot.GetComponentInChildren<Text>(true);service.gauges[i]=slot.Find("Gauge/Fill").GetComponent<Image>();}
        game.cooldownBar.transform.parent.gameObject.SetActive(false);
        var labelRect=game.serviceLabel.GetComponent<RectTransform>();labelRect.anchoredPosition=new Vector2(0,32);game.serviceLabel.fontSize=16;
        var edge=UnityEngine.Object.FindFirstObjectByType<HexWallEdgeUI>();
        if(!edge)
        {
            var ui=EdgesUI(canvas.transform);var prefab=PrefabUtility.SaveAsPrefabAsset(ui,Root+"/Prefabs/WallEdgeControls.prefab");UnityEngine.Object.DestroyImmediate(ui);edge=((GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform)).GetComponent<HexWallEdgeUI>();
        }
        edge.board=game.board;foreach(var button in edge.buttons)button.gameObject.SetActive(false);foreach(var line in edge.connectors)line.gameObject.SetActive(false);edge.transform.SetSiblingIndex(game.choicePanel.transform.GetSiblingIndex());existing.SetSiblingIndex(game.choicePanel.transform.GetSiblingIndex());
        var buildings=UnityEngine.Object.FindFirstObjectByType<HexShopBuildingUI>();foreach(var button in buildings.sides)button.gameObject.SetActive(false);buildings.wallMode.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(250,110);
        var anchor=game.transform.Find("Camera View Center");if(!anchor){var go=new GameObject("Camera View Center");go.transform.SetParent(game.transform);go.transform.position=service.worker.transform.position;anchor=go.transform;}game.orbit.target=anchor;
        game.orbit.minimumPitch=4;game.orbit.maximumPitch=50;game.pitchSlider.minValue=4;game.pitchSlider.maxValue=50;game.orbit.SetPitch(game.orbit.pitch);
        foreach(var dropdown in game.GetComponentsInChildren<Dropdown>(true))Style(dropdown);
        StylePrefab(Root+"/Prefabs/HexWorldUI.prefab");StylePrefab(Root+"/Prefabs/ShopBuildingUI.prefab");
        var binding=game.GetComponent<PrototypeFontBinding>();binding.Apply();EditorUtility.SetDirty(game);EditorUtility.SetDirty(service);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_DIRECT_SERVICE_AUTHORED");
    }
    static SpriteRenderer MakeMeal()
    {
        string path=Root+"/Prefabs/ServedMeal.prefab";var saved=AssetDatabase.LoadAssetAtPath<SpriteRenderer>(path);if(saved)return saved;
        var go=new GameObject("Served Meal");var image=go.AddComponent<SpriteRenderer>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Soup.png");image.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/CharacterSprite.mat");go.transform.localScale=Vector3.one*.65f;
        var facing=go.AddComponent<HexCameraFacingSprite>();facing.sprite=image;facing.groundAnchor=go.transform;saved=PrefabUtility.SaveAsPrefabAsset(go,path).GetComponent<SpriteRenderer>();UnityEngine.Object.DestroyImmediate(go);return saved;
    }
    static GameObject FoodUI(Transform parent)
    {
        var go=Rect("Individual Food Gauges",parent,new Vector2(0,-365),new Vector2(560,100));
        for(int i=0;i<4;i++)
        {
            var slot=Rect("Food Slot "+(i+1),go.transform,new Vector2((i-1.5f)*130,-6),new Vector2(126,64));
            var label=Text("State",slot.transform,new Vector2(0,5),new Vector2(126,38),14);label.text=strings.Resolve("service.food.slot",DisplayLanguage.Korean).Replace("{number}",(i+1).ToString()).Replace("{state}",strings.Resolve("service.paused",DisplayLanguage.Korean));
            var background=Rect("Gauge",slot.transform,new Vector2(0,-22),new Vector2(120,9));background.AddComponent<Image>().color=new Color(.2f,.3f,.35f);
            var fill=Rect("Fill",background.transform,Vector2.zero,new Vector2(120,9));var image=fill.AddComponent<Image>();image.sprite=white;image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.fillAmount=0;image.color=new Color(.3f,.85f,.65f);image.raycastTarget=false;
        }
        go.AddComponent<PrototypeFontBinding>();return go;
    }
    static GameObject EdgesUI(Transform parent)
    {
        var go=Rect("Wall Edge Icons",parent,Vector2.zero,new Vector2(1600,900));var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var ui=go.AddComponent<HexWallEdgeUI>();ui.root=rect;ui.buttons=new Button[6];ui.icons=new Image[6];ui.connectors=new Image[6];ui.symbols=new Text[6];
        for(int d=0;d<6;d++)
        {
            var line=Rect("Edge Link "+d,go.transform,Vector2.zero,new Vector2(1,2));ui.connectors[d]=line.AddComponent<Image>();ui.connectors[d].color=new Color(.6f,.95f,.8f,.9f);ui.connectors[d].raycastTarget=false;
            var button=Rect("Wall Side "+d,go.transform,Vector2.zero,new Vector2(38,38));ui.icons[d]=button.AddComponent<Image>();ui.icons[d].color=new Color(.13f,.38f,.35f);ui.buttons[d]=button.AddComponent<Button>();ui.buttons[d].targetGraphic=ui.icons[d];ui.symbols[d]=Text("Symbol",button.transform,Vector2.zero,new Vector2(38,38),28);ui.symbols[d].color=new Color(.03f,.1f,.13f);ui.symbols[d].text=strings.Resolve("wall.icon.add",DisplayLanguage.Korean);
        }
        go.AddComponent<PrototypeFontBinding>();return go;
    }
    static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return go;
    }
    static Text Text(string name,Transform parent,Vector2 position,Vector2 size,int fontSize)
    {
        var go=Rect(name,parent,position,size);var text=go.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
    static void Style(Dropdown dropdown)
    {
        var colors=dropdown.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.82f,.9f,.94f);colors.pressedColor=new Color(.7f,.85f,.9f);dropdown.colors=colors;
        foreach(var text in dropdown.GetComponentsInChildren<Text>(true)){text.color=new Color(.04f,.08f,.12f);text.fontSize=18;text.verticalOverflow=VerticalWrapMode.Overflow;}
        dropdown.GetComponent<Image>().color=Color.white;foreach(var image in dropdown.template.GetComponentsInChildren<Image>(true))if(image.gameObject.name=="Background"||image.gameObject==dropdown.template.gameObject)image.color=Color.white;
        var item=dropdown.template.GetComponentInChildren<Toggle>(true);if(item){var rect=item.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(rect.sizeDelta.x,36);}
        var templateSize=dropdown.template.sizeDelta;dropdown.template.sizeDelta=new Vector2(templateSize.x,260);
        dropdown.RefreshShownValue();
    }
    static void StylePrefab(string path){var go=PrefabUtility.LoadPrefabContents(path);foreach(var dropdown in go.GetComponentsInChildren<Dropdown>(true))Style(dropdown);PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);}
    static void Add(string key,string english,string korean){var entry=strings.entries.Find(e=>e.key==key);if(entry==null)strings.entries.Add(new TranslationEntry{key=key,english=english,korean=korean});else{entry.english=english;entry.korean=korean;}}
}

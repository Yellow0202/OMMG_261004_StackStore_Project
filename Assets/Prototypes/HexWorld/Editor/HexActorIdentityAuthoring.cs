using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HexActorIdentityAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    [MenuItem("Stack Store/Hex World/Add Actor Role Markers")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before authoring.");
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=Object.FindFirstObjectByType<HexPrototype>();
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(HexTestSettingsAuthoring.SettingsPath);
        var strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        Add(strings,"actor.role.player","PLAYER","플레이어");Add(strings,"actor.role.employee","EMPLOYEE","직원");Add(strings,"actor.role.employee.id","EMPLOYEE #{id}","직원 #{id}");strings.Rebuild();EditorUtility.SetDirty(strings);
        var playerPrefab=Make("PlayerIdentity",HexActorRole.Player,settings,strings);
        var employeePrefab=Make("EmployeeIdentity",HexActorRole.Employee,settings,strings);
        if(!game.service.worker.GetComponentInChildren<HexActorIdentity>(true))PrefabUtility.InstantiatePrefab(playerPrefab,game.service.worker.transform);
        var employee=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Employee.prefab");
        if(!employee.GetComponentInChildren<HexActorIdentity>(true))PrefabUtility.InstantiatePrefab(employeePrefab,employee.transform);
        PrefabUtility.SaveAsPrefabAsset(employee,Root+"/Prefabs/Employee.prefab");PrefabUtility.UnloadPrefabContents(employee);
        EditorUtility.SetDirty(settings);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_ACTOR_IDENTITY_AUTHORED: saved player/employee UGUI plates and diamond/circle ground markers; customer prefab unchanged.");
    }
    static GameObject Make(string name,HexActorRole role,HexTestSettings settings,LocalizationTable table)
    {
        string path=Root+"/Prefabs/"+name+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var root=new GameObject(name);var identity=root.AddComponent<HexActorIdentity>();identity.settings=settings;identity.role=role;
        var canvasObject=new GameObject("Role Nameplate",typeof(RectTransform),typeof(Canvas));canvasObject.transform.SetParent(root.transform,false);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;identity.labelCanvas=canvas;
        var rect=(RectTransform)canvasObject.transform;rect.sizeDelta=settings.identity.labelSize;rect.localScale=Vector3.one*settings.identity.labelScale;rect.localPosition=Vector3.up*settings.identity.labelHeight;
        var background=new GameObject("Role Color",typeof(RectTransform),typeof(Image));background.transform.SetParent(rect,false);Stretch((RectTransform)background.transform);
        identity.background=background.GetComponent<Image>();identity.background.raycastTarget=false;Color color=role==HexActorRole.Player?settings.identity.playerColor:settings.identity.employeeColor;color.a=settings.identity.backgroundOpacity;identity.background.color=color;
        var textObject=new GameObject("Role Text",typeof(RectTransform),typeof(Text),typeof(Outline));textObject.transform.SetParent(rect,false);Stretch((RectTransform)textObject.transform);
        var text=textObject.GetComponent<Text>();text.alignment=TextAnchor.MiddleCenter;text.color=settings.identity.textColor;text.fontSize=settings.identity.fontSize;text.raycastTarget=false;
        text.text=table.Resolve(role==HexActorRole.Player?"actor.role.player":"actor.role.employee",DisplayLanguage.Korean);text.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);textObject.GetComponent<Outline>().effectColor=Color.black;identity.label=text;
        canvasObject.AddComponent<PrototypeFontBinding>();
        var marker=new GameObject(role==HexActorRole.Player?"Player Diamond":"Employee Circle",typeof(LineRenderer));marker.transform.SetParent(root.transform,false);marker.transform.localPosition=Vector3.up*settings.identity.groundHeight;marker.transform.localScale=Vector3.one*settings.identity.groundRadius;
        var line=marker.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/CharacterSprite.mat");line.widthMultiplier=settings.identity.groundWidth;line.sortingOrder=-31000;
        color.a=1;line.startColor=line.endColor=color;int points=role==HexActorRole.Player?4:32;line.positionCount=points;
        for(int i=0;i<points;i++){float angle=i*Mathf.PI*2/points;line.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)));}identity.groundMarker=line;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static void Add(LocalizationTable table,string key,string en,string ko){if(table.entries.Exists(e=>e.key==key))return;table.entries.Add(new TranslationEntry{key=key,english=en,korean=ko});}
}

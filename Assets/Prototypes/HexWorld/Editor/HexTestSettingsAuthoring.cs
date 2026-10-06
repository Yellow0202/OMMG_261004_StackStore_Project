using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HexTestSettingsAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    public const string SettingsPath=Root+"/Data/PrototypeTestSettings.asset";
    [MenuItem("Stack Store/Hex World/Connect Shared Test Settings")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(SettingsPath);
        if(!settings)
        {
            settings=ScriptableObject.CreateInstance<HexTestSettings>();var c=settings.camera;var orbit=game.orbit;
            c.minimumPitch=orbit.minimumPitch;c.maximumPitch=orbit.maximumPitch;c.initialPitch=orbit.pitch;
            c.baseDistance=orbit.baseDistance;c.minimumZoom=orbit.minimumZoom;c.maximumZoom=orbit.maximumZoom;c.initialZoom=orbit.zoom;
            c.yaw=orbit.yaw;c.fieldOfView=orbit.GetComponent<Camera>().fieldOfView;c.zoomSpeed=orbit.zoomSpeed;
            c.panMargin=orbit.panMargin;c.overscroll=orbit.overscroll;c.returnSpeed=orbit.returnSpeed;
            // Capture the current edited tile value rather than overwriting it with the old default.
            var facing=game.board.authoredTiles[0].GetComponentInChildren<HexCameraFacingSprite>(true);
            settings.visual.pitchFollow=facing.pitchFollow;settings.visual.maximumTilt=facing.maximumTilt;settings.visual.maximumGroundLean=facing.maximumGroundLean;
            var g=settings.guests;g.spawnSeconds=game.spawnSeconds;g.maxCustomers=game.maxCustomers;g.visitorChance=game.visitorChance;
            g.directQueueChance=game.directQueueChance;g.browsingQueueChance=game.browsingQueueChance;g.browseDecisionSeconds=game.browseDecisionSeconds;
            settings.service.preparationSeconds=game.serviceSeconds;settings.service.patienceSeconds=game.patienceSeconds;settings.service.walkSpeed=game.service.walkSpeed;
            settings.start.tilesPerType=game.board.initialStock;settings.start.gold=game.Gold;settings.start.level=game.Level;
            settings.levelCurve=game.curve;settings.itemCatalog=game.catalog;settings.tileTypes=game.board.tileTypes;
            var shop=game.GetComponentInChildren<HexShopSystem>(true);if(shop)settings.shopOffers=shop.rows.Select(row=>row.offer).Distinct().ToArray();
            AssetDatabase.CreateAsset(settings,SettingsPath);
        }
        string baseline=Root+"/Data/PrototypeTestSettings_Baseline.asset";
        if(!AssetDatabase.LoadAssetAtPath<HexTestSettings>(baseline))AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(settings),baseline);
        var binding=game.GetComponent<HexSettingsBinding>();if(!binding)binding=game.gameObject.AddComponent<HexSettingsBinding>();
        binding.settings=settings;binding.game=game;
        binding.zoomLabel=game.zoomSlider.transform.parent.GetComponentsInChildren<Text>(true).First(t=>t.name=="Zoom");
        binding.pitchLabel=game.pitchSlider.transform.parent.GetComponentsInChildren<Text>(true).First(t=>t.name=="Pitch");
        foreach(var facing in game.GetComponentsInChildren<HexCameraFacingSprite>(true)){facing.settings=settings;EditorUtility.SetDirty(facing);}
        foreach(var name in new[]{"StartingShop","WorldCustomer","HexTile","ServedMeal","FlyingFood"})
        {
            string path=Root+"/Prefabs/"+name+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            foreach(var facing in prefab.GetComponentsInChildren<HexCameraFacingSprite>(true)){facing.settings=settings;EditorUtility.SetDirty(facing);}
            PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
        }
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        var entry=table.entries.Find(e=>e.key=="hex.camera.pitch");entry.english="Viewing angle {min}–{max}°";entry.korean="내려다보기 각도 {min}–{max}°";var zoomEntry=table.entries.Find(e=>e.key=="hex.camera.zoom");zoomEntry.english="Zoom distance {min}–{max}×";zoomEntry.korean="줌 거리 {min}–{max}배";
        table.Rebuild();EditorUtility.SetDirty(table);
        // The binding controls this data-dependent label; avoid a second component replacing it with an unformatted template.
        var label=binding.pitchLabel.GetComponent<LocalizedLabel>();if(label)UnityEngine.Object.DestroyImmediate(label);
        binding.pitchLabel.text=table.Resolve("hex.camera.pitch",DisplayLanguage.Korean).Replace("{min}",settings.camera.minimumPitch.ToString()).Replace("{max}",settings.camera.maximumPitch.ToString());
        var zoomLabel=binding.zoomLabel.GetComponent<LocalizedLabel>();if(zoomLabel)UnityEngine.Object.DestroyImmediate(zoomLabel);
        binding.zoomLabel.text=table.Resolve("hex.camera.zoom",DisplayLanguage.Korean).Replace("{min}",settings.camera.minimumZoom.ToString()).Replace("{max}",settings.camera.maximumZoom.ToString());
        EditorUtility.SetDirty(binding);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("HEX_SHARED_SETTINGS_AUTHORED: "+SettingsPath+", captured pitch follow "+settings.visual.pitchFollow);
    }
}

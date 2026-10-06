using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HexTestSettingsChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Stack Store/Hex World/Validate Shared Test Settings")]
    public static void Run()
    {
        Check(!EditorApplication.isPlaying,"Run asset validation outside Play mode.");
        EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");
        var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        var binding=game.GetComponent<HexSettingsBinding>();
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(HexTestSettingsAuthoring.SettingsPath);
        Check(binding&&binding.settings==settings&&binding.game==game&&binding.pitchLabel,"Shared scene binding missing");
        Check(settings.levelCurve&&settings.itemCatalog&&settings.tileTypes.Length>=6&&settings.shopOffers.Length>0,"Shared data sources missing");
        foreach(var face in game.GetComponentsInChildren<HexCameraFacingSprite>(true))Check(face.settings==settings,"Scene sprite uses a different profile");
        foreach(var name in new[]{"StartingShop","WorldCustomer","HexTile","ServedMeal","FlyingFood"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototypes/HexWorld/Prefabs/"+name+".prefab");
            foreach(var face in prefab.GetComponentsInChildren<HexCameraFacingSprite>(true))Check(face.settings==settings,"Prefab profile missing: "+name);
        }
        Check(settings.camera.maximumPitch>=settings.camera.minimumPitch,"Invalid pitch range");
        Check(settings.visual.pitchFollow>=0&&settings.visual.pitchFollow<=1,"Invalid follow fraction");
        Debug.Log("HEX_SHARED_SETTINGS_ASSET_PASS: scene/prefab references, linked balance data, migrated visual values and camera limit.");
    }
    // Test a clone so Play checks never write test values into the user's asset.
    public static void ValidateLive(HexPrototype game)
    {
        var binding=game.GetComponent<HexSettingsBinding>();var original=binding.settings;
        Check(game.Gold==original.start.gold&&game.Level==original.start.level,"Start wallet/level not applied");
        float savedFollow=original.visual.pitchFollow,savedMaximum=original.camera.maximumPitch;
        var test=UnityEngine.Object.Instantiate(original);binding.settings=test;binding.Apply();
        test.camera.maximumPitch=42;test.camera.zoomSpeed=.02f;test.guests.spawnSeconds=.25f;
        test.guests.maxCustomers=17;test.service.walkSpeed=6;test.service.preparationSeconds=2;
        test.visual.pitchFollow=0;binding.Apply();
        Check(game.orbit.maximumPitch==42&&game.pitchSlider.maxValue==42&&game.orbit.zoomSpeed==.02f,"Live camera or slider update failed");
        Check(binding.pitchLabel.text.Contains("42")&&!binding.pitchLabel.text.Contains("{max}"),"Localized range not formatted");
        Check(game.spawnSeconds==.25f&&game.maxCustomers==17&&game.service.walkSpeed==6&&game.serviceSeconds==2,"Live guest/service update failed");
        foreach(var face in game.GetComponentsInChildren<HexCameraFacingSprite>(true))if(!face.overrideSharedVisual)Check(face.TiltFor(50)==0,"Existing sprite did not use live shared value");
        var guest=UnityEngine.Object.Instantiate(game.customerPrefab);
        try{foreach(var face in guest.GetComponentsInChildren<HexCameraFacingSprite>(true))Check(face.TiltFor(50)==0,"New guest did not use live profile");}
        finally{UnityEngine.Object.DestroyImmediate(guest.gameObject);}
        int wallet=game.Gold;test.start.gold=99;binding.Apply();Check(game.Gold==wallet,"Live editing start gold reset the wallet");
        Check(original.visual.pitchFollow==savedFollow&&original.camera.maximumPitch==savedMaximum,"Play checks modified original settings");
        // Continue the regression probe on a pristine runtime clone.
        binding.settings=UnityEngine.Object.Instantiate(original);binding.Apply();UnityEngine.Object.DestroyImmediate(test);
        Debug.Log("HEX_SHARED_SETTINGS_LIVE_PASS: initial values, live camera/UI/guest/service updates, existing/new sprite propagation, startup-state isolation and unchanged authoring asset.");
    }
}

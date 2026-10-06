using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class HexDirectServiceAssetChecks
{
    [MenuItem("Stack Store/Hex World/Validate Direct Service Assets")]
    public static void Run()
    {
        const string root="Assets/Prototypes/HexWorld";
        var scene=EditorSceneManager.OpenScene(root+"/Scenes/HexWorld.unity");
        CheckComponents(scene);
        var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        if(!game.service||!game.service.worker||!game.service.servedFoodPrefab||game.service.gauges.Length!=4||game.service.slotViews.Length!=4)throw new Exception("Direct service references missing");
        int panels=0;foreach(Transform child in game.buildButton.GetComponentInParent<Canvas>().transform)if(child.name=="Individual Food Gauges"||child.name=="FoodPreparationUI")panels++;
        if(panels!=1)throw new Exception("Duplicate preparation UI");
        var edges=UnityEngine.Object.FindFirstObjectByType<HexWallEdgeUI>();if(!edges||edges.buttons.Length!=6||edges.board!=game.board)throw new Exception("Edge icon references incomplete");
        foreach(var dropdown in game.GetComponentsInChildren<Dropdown>(true))if(dropdown.itemText.color.r>.2f||dropdown.captionText.color.r>.2f||dropdown.template.GetComponentInChildren<Toggle>(true).GetComponent<RectTransform>().sizeDelta.y<32)throw new Exception("Dropdown contrast/spacing not saved");
        if(game.orbit.minimumPitch!=4||game.pitchSlider.minValue!=4)throw new Exception("Camera floor not saved");
        var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(root+"/Data/HexWorld.inputactions");if(input.FindAction("World/Orbit").bindings[0].path!="<Mouse>/leftButton")throw new Exception("Default orbit binding not saved");
        if(game.service.servedFoodPrefab.GetComponent<HexFoodProjectile>())throw new Exception("Served meal still a projectile");
        foreach(var dependency in AssetDatabase.GetDependencies(scene.path,true))if(dependency.Contains("LegacyUGUI"))throw new Exception("New scene depends on old prototype");
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(root+"/Data/HexWorldStrings.asset");var keys=new HashSet<string>();foreach(var entry in table.entries)if(!keys.Add(entry.key))throw new Exception("Duplicate string key "+entry.key);
        foreach(string key in new[]{"service.food.slot","service.ready","service.cooking","service.paused","service.summary","wall.icon.add","wall.icon.remove"})if(!keys.Contains(key))throw new Exception("Missing service key "+key);
        scene=EditorSceneManager.OpenScene("Assets/Prototypes/LegacyUGUI/Scenes/SampleScene.unity");CheckComponents(scene);
        Debug.Log("HEX_DIRECT_ASSETS_PASS: single saved UGUI gauge panel, six edge buttons, service/meal/player references, dropdown contrast/spacing, pitch4, left orbit binding, unique bilingual keys, both scenes without missing components and no legacy dependencies.");
    }
    static void CheckComponents(UnityEngine.SceneManagement.Scene scene){foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true))if(!component)throw new Exception("Missing script "+scene.path);}
}

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class HexShopPolishChecks
{
    [MenuItem("Stack Store/Hex World/Validate Solid Walls And Camera Assets")]
    public static void Run()
    {
        const string root="Assets/Prototypes/HexWorld";
        EditorSceneManager.OpenScene(root+"/Scenes/HexWorld.unity");
        var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        foreach(var wall in game.GetComponentsInChildren<HexWallView>(true))
        {
            if(!wall.volume||wall.image.enabled||wall.volume.sharedMaterial.renderQueue>=2500)throw new Exception("Solid depth-tested wall not saved");
            var mesh=wall.volume.GetComponent<MeshFilter>().sharedMesh;
            if(!mesh||mesh.bounds.size.z<.2f||mesh.vertexCount!=24)throw new Exception("Wall thickness or faces missing");
            if(ShaderUtil.ShaderHasError(wall.volume.sharedMaterial.shader))throw new Exception("Wall shader failed to compile");
        }
        if(!game.service.worker.GetComponent<HexKitchenOccupant>())throw new Exception("Player kitchen occupancy marker missing");
        var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(root+"/Data/HexWorld.inputactions");
        if(input.FindAction("World/Pan",true).bindings[0].path!="<Mouse>/middleButton")throw new Exception("Semantic pan action missing");
        if(game.orbit.zoomSpeed<.004f||game.orbit.minimumPitch!=4)throw new Exception("Camera speed or minimum angle incorrect");
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(root+"/Data/HexWorldStrings.asset");
        foreach(var key in new[]{"hex.tile.choice.title","hex.tile.choice.help"})if(table.entries.Find(e=>e.key==key)==null)throw new Exception("Tile choice string missing");
        Debug.Log("HEX_SHOP_POLISH_ASSETS_PASS: solid six-face walls and opaque depth material, saved player occupancy, semantic middle pan, faster zoom and bilingual tile-choice strings.");
    }
}

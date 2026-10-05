using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Persist the food visual and launch point without rebuilding existing scene UI.</summary>
public static class HexSurroundAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    [MenuItem("Stack Store/Hex World/Add Surround Food Delivery")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode before authoring.");
        string path=Root+"/Prefabs/FlyingFood.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<HexFoodProjectile>(path);
        if(!prefab)
        {
            var food=new GameObject("Flying Food");food.transform.localScale=Vector3.one;
            var renderer=food.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Soup.png");
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/CharacterSprite.mat");
            var facing=food.AddComponent<HexCameraFacingSprite>();facing.sprite=renderer;facing.groundAnchor=food.transform;
            food.AddComponent<HexFoodProjectile>();
            prefab=PrefabUtility.SaveAsPrefabAsset(food,path).GetComponent<HexFoodProjectile>();Object.DestroyImmediate(food);
        }
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");
        var game=Object.FindFirstObjectByType<HexPrototype>();game.foodPrefab=prefab;
        if(!game.foodOrigin)
        {
            var origin=new GameObject("Food Launch Point");origin.transform.SetParent(GameObject.Find("Starting Shop - fixed").transform,false);
            origin.transform.localPosition=new Vector3(0,1.4f,-.45f);game.foodOrigin=origin.transform;
        }
        EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("HEX_SURROUND_AUTHORED: saved food prefab and launch point.");
    }
}

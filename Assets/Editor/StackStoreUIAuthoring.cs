using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Installs the existing authored prefab; does not generate UI layouts.</summary>
public static class StackStoreUIAuthoring
{
    [MenuItem("Stack Store/Install Authored UI")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop Play mode before installing the UI.");
            return;
        }
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StackStoreUI.prefab");
        if (!asset)
        {
            Debug.LogError("Assets/Prefabs/StackStoreUI.prefab is missing.");
            return;
        }
        var existing = Object.FindFirstObjectByType<StackStorePrototype>();
        if (existing)
        {
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        Undo.RegisterCreatedObjectUndo(instance, "Install authored UGUI");
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
    }
}
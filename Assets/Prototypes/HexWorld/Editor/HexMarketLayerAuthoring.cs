using System;
using UnityEditor;
using UnityEngine;

/// <summary>Persists separate 2D market depth layers without building volumes or runtime creation.</summary>
public static class HexMarketLayerAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    const string Art=Root+"/Art/Environment";
    [MenuItem("Stack Store/Hex World/Apply Layered Market Background")]
    public static void Build()
    {
        var theme=AssetDatabase.LoadAssetAtPath<HexEnvironmentTheme>(Root+"/Data/Environments/MarketEnvironment.asset");
        var previous=theme.backgroundLayers;
        var names=new[]{"Near","Middle","Far"};var distances=new[]{26f,50f,90f};var widths=new[]{72f,100f,180f};
        theme.backgroundLayers=new HexEnvironmentTheme.BackgroundLayer[3];theme.useLayeredBackground=true;
        var shader=Shader.Find("StackStore/EnvironmentLayer");if(!shader)throw new Exception("Missing background shader");
        for(int i=0;i<3;i++)
        {
            string path=Art+"/Market/Market"+names[i]+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
            importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            string materialPath=Art+"/MarketLayer"+names[i]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}material.shader=shader;material.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(material);
            var layer=previous!=null&&i<previous.Length&&previous[i]!=null?previous[i]:new HexEnvironmentTheme.BackgroundLayer{key=names[i].ToLowerInvariant(),distance=distances[i],width=widths[i],height=widths[i]/3,baseHeight=i==1?1:-.6f};
            layer.material=material;theme.backgroundLayers[i]=layer;
        }
        EditorUtility.SetDirty(theme);
        string prefabPath=Root+"/Prefabs/MarketEnvironment.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var old=root.transform.Find("Distance Background Layers");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var group=new GameObject("Distance Background Layers");group.transform.SetParent(root.transform,false);
            var environment=root.GetComponent<HexMarketEnvironment>();environment.backgroundLayers=new MeshRenderer[3];
            for(int i=0;i<3;i++)
            {
                var go=new GameObject(names[i]+" - 2D Background",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group.transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/BackdropPlane.asset");
                var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                environment.backgroundLayers[i]=renderer;
            }
            environment.Refresh();PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Validate();
    }
    public static void Validate()
    {
        var theme=AssetDatabase.LoadAssetAtPath<HexEnvironmentTheme>(Root+"/Data/Environments/MarketEnvironment.asset");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MarketEnvironment.prefab");
        if(prefab.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Scenery blocks movement");
        if(prefab.transform.Find("Market Depth Buildings"))throw new Exception("Old building geometry remains");
        var environment=prefab.GetComponent<HexMarketEnvironment>();
        if(environment.backgroundLayers.Length!=3||theme.backgroundLayers.Length!=3)throw new Exception("Missing layers");
        float distance=0;
        foreach(var layer in theme.backgroundLayers)
        {
            if(layer.distance<=distance||!layer.material||!layer.material.GetTexture("_BaseMap"))throw new Exception("Invalid depth order or material");
            if(ShaderUtil.ShaderHasError(layer.material.shader))throw new Exception("Layer shader error");distance=layer.distance;
        }
        Debug.Log("MARKET_LAYERS_BUILD_PASSED: three saved 2D depth planes, ascending distance, materials, no old volumes or colliders.");
    }
}

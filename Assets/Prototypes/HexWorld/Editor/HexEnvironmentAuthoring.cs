using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Save generated bitmap textures, unlit materials, flat meshes and the market prefab.</summary>
public static class HexEnvironmentAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    const string Art=Root+"/Art/Environment";
    static readonly string[] Names={"Stall","Table","Kitchen","Storage","Stairs","Lodging","Entrance"};
    static T Asset<T>(string path,Func<T> create) where T:UnityEngine.Object
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(!asset){asset=create();AssetDatabase.CreateAsset(asset,path);}return asset;
    }
    static Texture2D Import(string name,bool repeat)
    {
        string path=Art+"/Market/"+name+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=repeat;importer.npotScale=TextureImporterNPOTScale.None;
        importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name,Texture2D texture)
    {
        var shader=Shader.Find(name.StartsWith("Floor_")?"StackStore/HexFloorBlend":"StackStore/EnvironmentUnlit");if(!shader)throw new Exception("Environment shader missing");
        var material=Asset(Art+"/"+name+".mat",()=>new Material(shader));
        material.shader=shader;material.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(material);return material;
    }
    static Mesh Mesh(string name,bool hex,bool vertical=false)
    {
        var mesh=Asset(Art+"/"+name+".asset",()=>new Mesh());mesh.Clear();mesh.name=name;
        if(hex)
        {
            var vertices=new Vector3[7];var uv=new Vector2[7];var triangles=new int[18];uv[0]=new Vector2(.5f,.5f);
            for(int d=0;d<6;d++)
            {
                float angle=(60*d+30)*Mathf.Deg2Rad;var p=new Vector3(Mathf.Cos(angle)*1.4f,0,Mathf.Sin(angle)*1.4f);
                vertices[d+1]=p;uv[d+1]=new Vector2(p.x/2.8f+.5f,p.z/2.8f+.5f);
                triangles[d*3]=0;triangles[d*3+1]=(d+1)%6+1;triangles[d*3+2]=d+1;
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        }
        else
        {
            mesh.vertices=vertical?new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)}:
                new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};
        }
        mesh.RecalculateNormals();mesh.RecalculateBounds();if(hex)mesh.bounds=new Bounds(Vector3.zero,new Vector3(2.8f,2,2.8f));
        EditorUtility.SetDirty(mesh);return mesh;
    }
    static MeshRenderer Surface(Transform root,string name,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
    }
    [MenuItem("Stack Store/Hex World/Apply Market Environment Art")]
    public static void Build()
    {
        Directory.CreateDirectory(Root+"/Data/Environments");AssetDatabase.Refresh();
        var theme=Asset(Root+"/Data/Environments/MarketEnvironment.asset",()=>ScriptableObject.CreateInstance<HexEnvironmentTheme>());
        theme.floors=new HexEnvironmentTheme.FloorArt[7];
        for(int i=0;i<7;i++)theme.floors[i]=new HexEnvironmentTheme.FloorArt{kind=(HexTileKind)i,material=Material("Floor_"+Names[i],Import(Names[i],true))};
        theme.floorMesh=Mesh("HexFloor",true);theme.groundMaterial=Material("MarketGround",Import("Entrance",true));
        theme.backdropMaterial=Material("MarketBackdrop",Import("MarketBackground",false));EditorUtility.SetDirty(theme);
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        foreach(string name in new[]{"PrototypeTestSettings","PrototypeTestSettings_Baseline"})
        {
            var shared=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/"+name+".asset");shared.environment=theme;EditorUtility.SetDirty(shared);
        }
        var root=new GameObject("Village Market Environment");
        try
        {
            var environment=root.AddComponent<HexMarketEnvironment>();environment.settings=settings;
            environment.ground=Surface(root.transform,"Market Paving",Mesh("GroundPlane",false),theme.groundMaterial);
            environment.backdrop=Surface(root.transform,"Distant Market Panorama",Mesh("BackdropPlane",false,true),theme.backdropMaterial);
            environment.Refresh();PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/MarketEnvironment.prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();HexMarketDepthAuthoring.Build();Validate();
    }
    public static void Validate()
    {
        var theme=AssetDatabase.LoadAssetAtPath<HexEnvironmentTheme>(Root+"/Data/Environments/MarketEnvironment.asset");
        if(!theme||theme.floors.Length!=7||!theme.floorMesh||theme.floorMesh.vertexCount!=7)throw new Exception("Incomplete floor art");
        foreach(HexTileKind kind in Enum.GetValues(typeof(HexTileKind)))
        {
            var material=theme.Floor(kind);
            if(!material||!material.GetTexture("_BaseMap"))throw new Exception("Missing "+kind);
            if(ShaderUtil.ShaderHasError(material.shader))throw new Exception("Floor shader errors: "+kind);
        }
        if(!theme.backdropMaterial||!theme.groundMaterial)throw new Exception("Missing market art");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/MarketEnvironment.prefab");
        if(!prefab||prefab.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Decorations must not block paths");
        if(UnityEditor.ShaderUtil.ShaderHasError(theme.groundMaterial.shader))throw new Exception("Environment shader has errors");
        Debug.Log("MARKET_ENVIRONMENT_ASSETS_PASSED: seven floors, UV hex, market panorama/paving, collider-free saved prefab and shader.");
    }
}

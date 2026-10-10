using System;
using UnityEditor;
using UnityEngine;

/// <summary>Builds editable collider-free houses from facade art and actual roof/awning geometry.</summary>
public static class HexMarketDepthAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    const string Art=Root+"/Art/Environment";
    static Material Mat(string name,Texture texture,Color color)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("StackStore/EnvironmentUnlit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",texture);m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }
    public static void Build()
    {
        string texturePath=Art+"/Market/MarketFacade.png";AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;
        importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var facade=Mat("MarketHouseFacade",texture,Color.white);
        var side=Mat("MarketHouseSide",texture,new Color(.72f,.77f,.82f));
        var roof=Mat("MarketHouseRoof",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Market/Stall.png"),new Color(.65f,.29f,.23f));
        var timber=Mat("MarketTimber",Texture2D.whiteTexture,new Color(.27f,.15f,.10f));
        var red=Mat("MarketAwningRed",Texture2D.whiteTexture,new Color(.65f,.21f,.20f));
        var green=Mat("MarketAwningGreen",Texture2D.whiteTexture,new Color(.30f,.44f,.28f));
        string path=Root+"/Prefabs/MarketEnvironment.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var old=root.transform.Find("Market Depth Buildings");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var group=new GameObject("Market Depth Buildings");group.transform.SetParent(root.transform,false);
            var depth=group.AddComponent<HexMarketDepth>();depth.environment=root.GetComponent<HexMarketEnvironment>();
            depth.anchors=new[]{new Vector3(-1,0,0),new Vector3(1,0,.6f),new Vector3(-.35f,0,1.5f),new Vector3(.55f,0,2.2f),new Vector3(-1.7f,0,2.7f),new Vector3(1.7f,0,3.1f)};
            depth.buildings=new Transform[depth.anchors.Length];
            for(int i=0;i<depth.buildings.Length;i++)
            {
                var house=new GameObject("Market House "+(i+1));house.transform.SetParent(group.transform,false);depth.buildings[i]=house.transform;
                float height=4+(i%3)*.6f,width=4.8f,thickness=3.2f;
                Box(house.transform,"Timber house volume",new Vector3(0,height/2,thickness/2),new Vector3(width,height,thickness),side);
                var front=new GameObject("Painted shopfront",typeof(MeshFilter),typeof(MeshRenderer));front.transform.SetParent(house.transform,false);
                front.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/BackdropPlane.asset");front.GetComponent<MeshRenderer>().sharedMaterial=facade;
                front.transform.localPosition=new Vector3(0,height/2,-.015f);front.transform.localScale=new Vector3(width,height,1);
                var gablePath=Art+"/MarketGable.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(gablePath); if(!mesh){mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(0,1,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,new Vector2(.5f,1)};mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,gablePath);}
                foreach(float z in new[]{-.02f,thickness+.02f}){var gable=new GameObject("Timber roof gable",typeof(MeshFilter),typeof(MeshRenderer));gable.transform.SetParent(house.transform,false);gable.transform.localPosition=new Vector3(0,height,z);gable.transform.localScale=new Vector3(width,1.3f,1);gable.GetComponent<MeshFilter>().sharedMesh=mesh;gable.GetComponent<MeshRenderer>().sharedMaterial=side;}
                for(int slope=0;slope<2;slope++)
                {
                    var part=Box(house.transform,"Sloping tiled roof",new Vector3((slope==0?-1:1)*width/4,height+.65f,thickness/2),new Vector3(width*.62f,.16f,thickness+.6f),roof);
                    part.transform.localRotation=Quaternion.Euler(0,0,slope==0?28:-28);
                }
                var awning=Box(house.transform,"Projecting fabric awning",new Vector3(0,height*.46f,-.7f),new Vector3(width*.94f,.12f,1.5f),i%2==0?red:green);awning.transform.localRotation=Quaternion.Euler(-12,0,0);
                for(int s=-1;s<=1;s+=2)Box(house.transform,"Awning post",new Vector3(s*width*.43f,height*.23f,-1.25f),new Vector3(.10f,height*.46f,.10f),timber);
                Box(house.transform,"Market crate",new Vector3(width*.36f,.35f,-1.5f),new Vector3(.65f,.7f,.65f),timber);
            }
            depth.Refresh();PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("MARKET_DEPTH_BUILD_PASSED: six saved houses, physical roofs and awnings; no colliders.");
    }
}

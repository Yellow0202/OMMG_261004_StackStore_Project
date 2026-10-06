using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Persist solid wall geometry, worker markers and semantic camera input in the editor.</summary>
public static class HexShopPolishAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    [MenuItem("Stack Store/Hex World/Upgrade Solid Walls And Tile Rewards")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");
        var mesh=CreateWallMesh();
        string materialPath=Root+"/Art/SolidWall.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(Shader.Find("StackStore/Solid Pixel Wall"));AssetDatabase.CreateAsset(material,materialPath);}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/BoundaryWall.png");EditorUtility.SetDirty(material);
        var prefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/HexTile.prefab");
        foreach(var wall in prefab.GetComponentsInChildren<HexWallView>(true))ConfigureWall(wall,mesh,material);
        PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/HexTile.prefab");PrefabUtility.UnloadPrefabContents(prefab);
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");
        var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        foreach(var facing in game.GetComponentsInChildren<HexCameraFacingSprite>(true))ConfigurePitch(facing);
        foreach(var name in new[]{"StartingShop","WorldCustomer","ServedMeal","FlyingFood"})
        {
            string path=Root+"/Prefabs/"+name+".prefab";
            var asset=PrefabUtility.LoadPrefabContents(path);
            foreach(var facing in asset.GetComponentsInChildren<HexCameraFacingSprite>(true))ConfigurePitch(facing);
            PrefabUtility.SaveAsPrefabAsset(asset,path);PrefabUtility.UnloadPrefabContents(asset);
        }
        foreach(var wall in game.GetComponentsInChildren<HexWallView>(true))ConfigureWall(wall,mesh,material);
        if(!game.service.worker.GetComponent<HexKitchenOccupant>())game.service.worker.gameObject.AddComponent<HexKitchenOccupant>();
        game.orbit.zoomSpeed=.009f;game.orbit.board=game.board;
        var input=AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root+"/Data/HexWorld.inputactions");
        var map=input.FindActionMap("World",true);
        if(map.FindAction("Pan")==null)map.AddAction("Pan",InputActionType.Button,"<Mouse>/middleButton");
        File.WriteAllText(Root+"/Data/HexWorld.inputactions",input.ToJson());AssetDatabase.ImportAsset(Root+"/Data/HexWorld.inputactions");
        var strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        Add(strings,"hex.tile.choice.title","TILE REWARD / CHOOSE ONE","타일 보상 / 하나 선택");
        Add(strings,"hex.tile.choice.help","Choose one of three random tiles. Only that tile is added to inventory.","무작위 타일 3종 중 하나를 선택하세요. 선택한 타일 1개만 보관됩니다.");
        Add(strings,"hex.choice.help","Gold is retained. A tile reward opens three random tile choices.","골드는 소비되지 않습니다. 타일 보상을 선택하면 무작위 타일 3종 중 하나를 고릅니다.");
        Add(strings,"hex.tile.reward.name","Tile reward","타일 보상");
        Add(strings,"hex.tile.reward.description","Choose one of three random tiles to expand your shop. The selected part's existing upgrade effect is also applied.","무작위 타일 3종 중 하나를 선택해 가게를 확장합니다. 선택한 부품의 기존 강화 효과도 적용됩니다.");
        Add(strings,"hex.camera.help","Wheel: zoom / Left drag: angle / Middle drag: pan / Click: tile","휠: 줌 · 왼쪽 드래그: 시선 높낮이 · 휠 버튼 드래그: 화면 이동 · 클릭: 타일");
        strings.Rebuild();EditorUtility.SetDirty(strings);
        var binding=game.GetComponent<PrototypeFontBinding>();if(binding)binding.Apply();
        EditorUtility.SetDirty(game.orbit);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("HEX_SHOP_POLISH_AUTHORED");
    }
    static void Add(LocalizationTable table,string key,string en,string ko)
    {
        var entry=table.entries.Find(e=>e.key==key);
        if(entry==null)table.entries.Add(new TranslationEntry{key=key,english=en,korean=ko});
        else {entry.english=en;entry.korean=ko;}
    }
    static void ConfigurePitch(HexCameraFacingSprite facing)
    {
        facing.pitchFollow=.35f;facing.maximumTilt=18;facing.maximumGroundLean=.3f;
        facing.FaceCamera();EditorUtility.SetDirty(facing);
    }
    static void ConfigureWall(HexWallView wall,Mesh mesh,Material material)
    {
        var child=wall.transform.Find("Solid Wall Volume");
        if(!child){var go=new GameObject("Solid Wall Volume");go.layer=2;go.transform.SetParent(wall.transform,false);child=go.transform;}
        var filter=child.GetComponent<MeshFilter>();if(!filter)filter=child.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        wall.volume=child.GetComponent<MeshRenderer>();if(!wall.volume)wall.volume=child.gameObject.AddComponent<MeshRenderer>();
        wall.volume.sharedMaterial=material;wall.volume.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wall.volume.receiveShadows=false;
        if(wall.image)wall.image.enabled=false;
        if(wall.obstacle)wall.obstacle.size=new Vector3(2,1.25f,.24f);
        EditorUtility.SetDirty(wall);
    }
    static Mesh CreateWallMesh()
    {
        string path=Root+"/Art/SolidWallMesh.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh)return mesh;
        mesh=new Mesh{name="Pixel wall / thickness 0.24"};
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint,bool texture)
        {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
            uv.AddRange(texture?new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right}:new[]{new Vector2(.1f,.5f),new Vector2(.1f,.5f),new Vector2(.1f,.5f),new Vector2(.1f,.5f)});
            for(int i=0;i<4;i++)colors.Add(tint);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        float x=1,y=.625f,z=.12f;
        Face(new Vector3(-x,-y,-z),new Vector3(-x,y,-z),new Vector3(x,y,-z),new Vector3(x,-y,-z),Color.white,true);
        Face(new Vector3(x,-y,z),new Vector3(x,y,z),new Vector3(-x,y,z),new Vector3(-x,-y,z),Color.white,true);
        Face(new Vector3(-x,-y,z),new Vector3(-x,y,z),new Vector3(-x,y,-z),new Vector3(-x,-y,-z),new Color(.72f,.72f,.72f),false);
        Face(new Vector3(x,-y,-z),new Vector3(x,y,-z),new Vector3(x,y,z),new Vector3(x,-y,z),new Color(.72f,.72f,.72f),false);
        Face(new Vector3(-x,y,-z),new Vector3(-x,y,z),new Vector3(x,y,z),new Vector3(x,y,-z),new Color(1.18f,1.18f,1.18f),false);
        Face(new Vector3(-x,-y,z),new Vector3(-x,-y,-z),new Vector3(x,-y,-z),new Vector3(x,-y,z),Color.gray,false);
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}

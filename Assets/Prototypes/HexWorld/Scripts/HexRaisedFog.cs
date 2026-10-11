using System.Collections.Generic;
using UnityEngine;

/// <summary>Layered vertical mist along exposed hex edges. Visual only; no collider or ownership.</summary>
public sealed class HexRaisedFog
{
    GameObject root;
    Mesh mesh;
    MeshRenderer renderer;
    MaterialPropertyBlock properties;
    readonly HashSet<Vector2Int> cells=new HashSet<Vector2Int>();
    float height,depth;int layers;
    public void Refresh(HashSet<Vector2Int> visible,HexTestSettings.ExteriorFogOptions options,Transform parent)
    {
        if(!options.enabled||!options.raisedFog||options.raisedOpacity<=0)
        {if(root)root.SetActive(false);return;}
        if(!root)
        {
            var material=Resources.Load<Material>("HexRaisedFog");if(!material)return;
            root=new GameObject("Raised exterior mist",typeof(MeshFilter),typeof(MeshRenderer));
            root.hideFlags=HideFlags.HideAndDontSave;root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
            renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            mesh=new Mesh{name="Exterior mist boundary",hideFlags=HideFlags.HideAndDontSave};root.GetComponent<MeshFilter>().sharedMesh=mesh;
            properties=new MaterialPropertyBlock();
        }
        root.SetActive(true);
        if(!cells.SetEquals(visible)||height!=options.height||depth!=options.depth||layers!=options.layers)
        {
            cells.Clear();cells.UnionWith(visible);height=options.height;depth=options.depth;layers=options.layers;Build();
        }
        properties.SetColor("_Color",options.color);
        properties.SetVector("_Cloud",new Vector4(options.raisedOpacity*options.opacity,options.cloudScale,options.cloudSpeed,height));
        properties.SetFloat("_NearOpacity",options.nearSideOpacity);
        renderer.SetPropertyBlock(properties);
    }
    void Build()
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        foreach(var cell in cells)for(int d=0;d<6;d++)
        {
            if(cells.Contains(cell+HexBoardModel.Directions[d]))continue;
            var center=HexBoardModel.World(cell);var normal=(HexBoardModel.World(cell+HexBoardModel.Directions[d])-center).normalized;
            var tangent=new Vector3(-normal.z,0,normal.x)*.7f;
            for(int layer=0;layer<layers;layer++)
            {
                var middle=center+normal*(1.212435565f+.12f+depth*layer/Mathf.Max(1,layers-1));
                var a=middle-tangent*1.12f;var b=middle+tangent*1.12f;float h=height*(1+layer*.12f);
                int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(b+Vector3.up*h);vertices.Add(a+Vector3.up*h);
                normals.Add(normal);normals.Add(normal);normals.Add(normal);normals.Add(normal);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));
                triangles.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
            }
        }
        mesh.Clear();mesh.indexFormat=vertices.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    }
    public void Dispose()
    {
        if(root)root.SetActive(false);Destroy(root);Destroy(mesh);root=null;mesh=null;renderer=null;cells.Clear();
    }
    static void Destroy(Object value){if(!value)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>Rounded cloud banks along exposed hex edges. Visual only; no collider or ownership.</summary>
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
            renderer.sortingOrder=-32700;
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
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
        var centerSum=Vector3.zero;foreach(var cell in cells)centerSum+=HexBoardModel.World(cell);
        var shopCenter=centerSum/Mathf.Max(1,cells.Count);properties.SetVector("_ShopCenter",new Vector4(shopCenter.x,shopCenter.z,0,0));
        foreach(var cell in cells)for(int d=0;d<6;d++)
        {
            if(cells.Contains(cell+HexBoardModel.Directions[d]))continue;
            var center=HexBoardModel.World(cell);var outward=(HexBoardModel.World(cell+HexBoardModel.Directions[d])-center).normalized;
            var tangent=new Vector3(-outward.z,0,outward.x);
            for(int layer=0;layer<layers;layer++)for(int lobe=0;lobe<2;lobe++)
            {
                float seed=Mathf.Sin(cell.x*17.13f+cell.y*41.71f+d*9.3f+layer*13.7f+lobe*3.1f)*.5f+.5f;
                float size=.85f+seed*.3f;
                var point=center+outward*(1.212435565f+.48f+depth*layer/Mathf.Max(1,layers-1))+tangent*((lobe-.5f)*1.05f);
                point.y=height*.38f*size;
                var radius=new Vector3(1.12f*size,height*.64f*size,1.12f*size);
                int start=vertices.Count;const int columns=24,rows=16;
                for(int row=0;row<=rows;row++)for(int column=0;column<=columns;column++)
                {
                    float latitude=Mathf.PI*row/rows,longitude=Mathf.PI*2*column/columns;
                    var normal=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude));
                    vertices.Add(point+Vector3.Scale(normal,radius));
                    normals.Add(new Vector3(normal.x/radius.x,normal.y/radius.y,normal.z/radius.z).normalized);
                }
                for(int row=0;row<rows;row++)for(int column=0;column<columns;column++)
                {
                    int at=start+row*(columns+1)+column;
                    triangles.AddRange(new[]{at,at+1,at+columns+1,at+1,at+columns+2,at+columns+1});
                }
            }
        }
        mesh.Clear();mesh.indexFormat=vertices.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    }
    public void Dispose()
    {
        if(root)root.SetActive(false);Destroy(root);Destroy(mesh);root=null;mesh=null;renderer=null;cells.Clear();
    }
    static void Destroy(Object value){if(!value)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
}

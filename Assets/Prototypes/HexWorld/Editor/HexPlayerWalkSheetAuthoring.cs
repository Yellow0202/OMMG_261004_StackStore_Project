using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Slice authored walk art without synthesizing poses. Preserve frame asset GUIDs and clip timing.</summary>
public static class HexPlayerWalkSheetAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Directions={"North","North-East","East","South-East","South","South-West","West","North-West"};
    [MenuItem("Stack Store/Hex World/Import Player Walk Source Sheets")]
    public static void Build()
    {
        var options=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset").playerAnimation;
        foreach(string direction in Directions)Slice(direction,options);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        HexPlayerAnimationAuthoring.Build();
        Validate();
    }
    static void Slice(string direction,HexTestSettings.PlayerAnimationOptions options)
    {
        string path=Root+"/Art/PlayerAnimation/WalkSources/"+direction+".png";
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            if(!texture.LoadImage(File.ReadAllBytes(path)))throw new Exception("Cannot decode "+path);
            var pixels=texture.GetPixels32();int width=texture.width,height=texture.height;
            var labels=new int[pixels.Length];var figures=new List<List<int>>();var queue=new Queue<int>();
            for(int start=0;start<pixels.Length;start++)
            {
                if(labels[start]!=0||pixels[start].a<options.alphaCutoff)continue;
                int label=figures.Count+1;var figure=new List<int>();labels[start]=label;queue.Enqueue(start);
                while(queue.Count>0)
                {
                    int index=queue.Dequeue();figure.Add(index);int x=index%width,y=index/width;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;
                        int next=ny*width+nx;
                        if(labels[next]==0&&pixels[next].a>=options.alphaCutoff){labels[next]=label;queue.Enqueue(next);}
                    }
                }
                figures.Add(figure);
            }
            var selected=figures.OrderByDescending(x=>x.Count).Take(8).ToArray();
            if(selected.Length!=8||selected.Any(x=>x.Count<100))throw new Exception("Expected eight complete figures: "+path);
            selected=selected.OrderByDescending(x=>x.Average(i=>i/width)).ToArray();
            selected=Enumerable.Range(0,2).SelectMany(row=>selected.Skip(row*4).Take(4).OrderBy(x=>x.Average(i=>i%width))).ToArray();
            int size=options.frameCanvasSize,padding=options.framePadding,bodyHeight=options.frameBodyHeight;
            if(size<128||bodyHeight<64||bodyHeight>size-padding*2)throw new Exception("Invalid player sheet canvas settings");
            // The first pose is the existing idle authoring height reference. Use one ratio for the
            // entire cycle so its vertical motion survives and idle keeps the original world height.
            float ratio=bodyHeight/(float)(selected[0].Max(i=>i/width)-selected[0].Min(i=>i/width)+1);
            string folder=Root+"/Art/PlayerAnimation/Walk/"+direction;Directory.CreateDirectory(folder);
            for(int frame=0;frame<8;frame++)
            {
                var figure=selected[frame];int left=figure.Min(i=>i%width),right=figure.Max(i=>i%width);
                int bottom=figure.Min(i=>i/width),top=figure.Max(i=>i/width),label=labels[figure[0]];
                int dw=Mathf.RoundToInt((right-left+1)*ratio),dh=Mathf.RoundToInt((top-bottom+1)*ratio);
                var head=figure.Where(i=>i/width>=top-Mathf.RoundToInt((top-bottom+1)*.25f)).ToArray();
                float anchor=(head.Min(i=>i%width)+head.Max(i=>i%width))*.5f;
                int offset=Mathf.RoundToInt(size*.5f+(left-anchor)*ratio);
                if(offset<padding||offset+dw>size-padding||dh>size-padding*2)throw new Exception("Player frame exceeds canvas "+direction+"/"+frame);
                var output=new Color32[size*size];
                for(int y=0;y<dh;y++)for(int x=0;x<dw;x++)
                {
                    int sx=Mathf.Min(right,left+Mathf.FloorToInt(x/ratio)),sy=Mathf.Min(top,bottom+Mathf.FloorToInt(y/ratio));
                    int source=sy*width+sx;if(labels[source]==label)output[(padding+y)*size+offset+x]=pixels[source];
                }
                var cutout=new Texture2D(size,size,TextureFormat.RGBA32,false);
                try{cutout.SetPixels32(output);cutout.Apply();File.WriteAllBytes(folder+"/"+direction+"_"+(frame+1).ToString("D4")+".png",cutout.EncodeToPNG());}
                finally{UnityEngine.Object.DestroyImmediate(cutout);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static void Validate()
    {
        var options=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset").playerAnimation;
        var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/PlayerAnimationSet.asset");
        for(int d=0;d<8;d++)for(int f=0;f<8;f++)
        {
            var sprite=set.frames[d].sprites[f];string path=AssetDatabase.GetAssetPath(sprite);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(sprite.rect.size!=Vector2.one*options.frameCanvasSize||Mathf.Abs(sprite.pivot.y-options.framePadding)>.01f||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled)
                throw new Exception("Player canvas/pivot/import mismatch "+d+"/"+f);
            if(!path.EndsWith("/"+Directions[d]+"_"+(f+1).ToString("D4")+".png",StringComparison.Ordinal))throw new Exception("Player frame order mismatch");
        }
        Debug.Log("PLAYER_WALK_SHEETS_PASSED: 64 uniform transparent cutouts, original asset paths/frame order, registered feet and Point filtering.");
    }
}

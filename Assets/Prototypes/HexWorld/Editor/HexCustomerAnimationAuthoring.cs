using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Import authored 4x2 sheets, align feet, and persist sprites/clips/catalog in Assets.</summary>
public static class HexCustomerAnimationAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Names={"Male-01","Male-02","Male-03","Female-01","Female-02","Female-03"};
    static readonly string[] Directions={"North","North-East","East","South-East","South","South-West","West","North-West"};
    [MenuItem("Stack Store/Hex World/Rebuild Customer Walk Animations")]
    public static void Build()
    {
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        if(!settings)throw new Exception("Missing test settings");
        var options=settings.customerAnimation;
        string dataFolder=Root+"/Data/CustomerAnimation";
        Directory.CreateDirectory(dataFolder);AssetDatabase.Refresh();
        var catalog=LoadOrCreate<HexCustomerAnimationCatalog>(dataFolder+"/CustomerAnimationCatalog.asset");
        catalog.characters=new HexPlayerAnimationSet[Names.Length];
        for(int c=0;c<Names.Length;c++)
        {
            string folder=Root+"/Art/CustomerAnimation/"+Names[c];
            var set=LoadOrCreate<HexPlayerAnimationSet>(dataFolder+"/"+Names[c]+".asset");
            set.walk=new AnimationClip[8];set.idle=new Sprite[8];set.frames=new HexPlayerAnimationSet.Frames[8];
            for(int d=0;d<8;d++)
            {
                var sprites=Import(folder+"/"+Directions[d]+".png",4,2,options);
                if(sprites.Length!=8)throw new Exception("Missing frames "+Names[c]+"/"+Directions[d]);
                set.frames[d]=new HexPlayerAnimationSet.Frames{sprites=sprites};set.idle[d]=sprites[2];
                var clip=LoadOrCreateClip(folder+"/Walk_"+Directions[d]+".anim");
                var keys=new ObjectReferenceKeyframe[9];
                for(int f=0;f<9;f++)keys[f]=new ObjectReferenceKeyframe{time=f/HexPlayerAnimationSet.ClipFramesPerSecond,value=sprites[f%8]};
                clip.frameRate=HexPlayerAnimationSet.ClipFramesPerSecond;
                AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
                var loop=AnimationUtility.GetAnimationClipSettings(clip);loop.loopTime=true;loop.stopTime=.8f;
                AnimationUtility.SetAnimationClipSettings(clip,loop);EditorUtility.SetDirty(clip);set.walk[d]=clip;
            }
            set.idle[4]=Import(folder+"/Idle-Reference.png",1,1,options)[0];
            EditorUtility.SetDirty(set);catalog.characters[c]=set;
        }
        EditorUtility.SetDirty(catalog);
        foreach(string name in new[]{"PrototypeTestSettings","PrototypeTestSettings_Baseline"})
        {
            var shared=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/"+name+".asset");
            if(shared){shared.customerAnimation.catalog=catalog;EditorUtility.SetDirty(shared);}
        }
        string prefabPath=Root+"/Prefabs/WorldCustomer.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if(!prefab.GetComponent<HexCustomerAnimation>())prefab.AddComponent<HexCustomerAnimation>();
            var actor=prefab.GetComponent<HexWorldActor>();actor.body.sprite=catalog.characters[0].idle[4];
            actor.body.spriteSortPoint=SpriteSortPoint.Pivot;
            PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();Validate();
    }
    static T LoadOrCreate<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}
        return asset;
    }
    static AnimationClip LoadOrCreateClip(string path)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}return clip;
    }
    static Sprite[] Import(string path,int columns,int rows,HexTestSettings.CustomerAnimationOptions options)
    {
        // Read original pixels for rect detection only; source PNG art remains untouched.
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();
        int width=texture.width,height=texture.height;
        var rects=new Rect[columns*rows];float maxHeight=1;
        for(int f=0;f<rects.Length;f++)
        {
            int x0=f%columns*width/columns,x1=(f%columns+1)*width/columns;
            int y0=(rows-1-f/columns)*height/rows,y1=(rows-f/columns)*height/rows;
            int left=x1,right=x0,bottom=y1,top=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                if(pixels[y*width+x].a>=options.alphaCutoff){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            if(right<=left||top<=bottom)throw new Exception("Empty cell "+path+"/"+f);
            left=Math.Max(x0,left-options.framePadding);right=Math.Min(x1-1,right+options.framePadding);
            bottom=Math.Max(y0,bottom-options.framePadding);top=Math.Min(y1-1,top+options.framePadding);
            rects[f]=new Rect(left,bottom,right-left+1,top-bottom+1);maxHeight=Mathf.Max(maxHeight,rects[f].height);
        }
        UnityEngine.Object.DestroyImmediate(texture);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=maxHeight/Mathf.Max(.1f,options.authoredHeight);
        var importSettings=new TextureImporterSettings();importer.ReadTextureSettings(importSettings);
        importSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(importSettings);
        var metadata=new SpriteMetaData[rects.Length];
        for(int f=0;f<rects.Length;f++)metadata[f]=new SpriteMetaData{name="Frame_"+f.ToString("D2"),rect=rects[f],alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(.5f,options.framePadding/rects[f].height)};
        #pragma warning disable 0618
        importer.spritesheet=metadata;
        #pragma warning restore 0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(x=>x.name,StringComparer.Ordinal).ToArray();
    }
    public static void Validate()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<HexCustomerAnimationCatalog>(Root+"/Data/CustomerAnimation/CustomerAnimationCatalog.asset");
        if(!catalog||!catalog.IsValid||catalog.characters.Length!=6)throw new Exception("Expected six valid characters");
        foreach(var set in catalog.characters)for(int d=0;d<8;d++)
        {
            var clip=set.walk[d];var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            if(keys.Length!=9||!AnimationUtility.GetAnimationClipSettings(clip).loopTime)throw new Exception("Invalid loop "+clip.name);
            for(int f=0;f<8;f++)
                if(set.Sample(d,(f+.25f)/HexPlayerAnimationSet.ClipFramesPerSecond)!=keys[f].value)throw new Exception("Frame mismatch");
        }
        var shared=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        var actorObject=new GameObject("Customer animation verification");
        try
        {
            var actor=actorObject.AddComponent<HexWorldActor>();actor.body=actorObject.AddComponent<SpriteRenderer>();
            var animation=actorObject.AddComponent<HexCustomerAnimation>();
            var randomState=UnityEngine.Random.state;UnityEngine.Random.InitState(71026);
            var chosen=new System.Collections.Generic.HashSet<HexPlayerAnimationSet>();
            for(int i=0;i<120;i++)
            {
                if(!animation.Configure(actor,shared)||!actor.externalAnimation)throw new Exception("Configure failed");
                chosen.Add(animation.Character);
                var tint=new Color(0,0,0,.4f);actor.body.color=tint;
                typeof(HexCustomerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
                if(actor.body.color!=tint)throw new Exception("Animation overwrote tint");
                actor.seated=true;typeof(HexCustomerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
                if(actor.body.sprite!=animation.Character.idle[animation.Direction])throw new Exception("Seated identity changed");
                actor.seated=false;
            }
            UnityEngine.Random.state=randomState;
            if(chosen.Count!=6)throw new Exception("Not all characters selected");
        }
        finally{UnityEngine.Object.DestroyImmediate(actorObject);}
        Debug.Log("CUSTOMER_ANIMATION_VALIDATED: 6 characters, 384 walk sprites, 48 looping clips; samples, spawn pool, seated identity and tint passed.");
    }
}

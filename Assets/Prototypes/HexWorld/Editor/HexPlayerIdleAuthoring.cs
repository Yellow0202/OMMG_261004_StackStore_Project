using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Import full-resolution idle art; match walk body height and register feet without editing PNG pixels.</summary>
public static class HexPlayerIdleAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Directions={"North","North-East","East","South-East","South","South-West","West","North-West"};
    // Ignore translucent cutout fringe when measuring the visible body; not a gameplay tuning value.
    static RectInt Body(string path)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            if(!texture.LoadImage(File.ReadAllBytes(path)))throw new Exception("Cannot decode "+path);
            var pixels=texture.GetPixels32();int left=texture.width,bottom=texture.height,right=-1,top=-1;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=128)
            {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            if(right<left||top<bottom)throw new Exception("Empty sprite "+path);
            return new RectInt(left,bottom,right-left+1,top-bottom+1);
        }
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    [MenuItem("Stack Store/Hex World/Apply Player Eight Direction Idle")]
    public static void Build()
    {
        var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/PlayerAnimationSet.asset");
        if(!set||!set.IsValid)throw new Exception("Player walk animation set must be authored first");
        var reference=set.frames[4].sprites[0];
        float height=Body(AssetDatabase.GetAssetPath(reference)).height/reference.pixelsPerUnit;
        var idle=new Sprite[8];
        for(int d=0;d<8;d++)
        {
            string path=Root+"/Art/PlayerAnimation/Idle/"+Directions[d]+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var body=Body(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));int width=source.width,canvasHeight=source.height;UnityEngine.Object.DestroyImmediate(source);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=body.height/height;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=new Vector2(body.center.x/width,(float)body.yMin/canvasHeight);
            settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
            idle[d]=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!idle[d])throw new Exception("Missing idle "+Directions[d]);
            string clipPath=Root+"/Art/PlayerAnimation/Idle_"+Directions[d]+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
            clip.frameRate=HexPlayerAnimationSet.ClipFramesPerSecond;
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},new[]{new ObjectReferenceKeyframe{time=0,value=idle[d]},new ObjectReferenceKeyframe{time=1/HexPlayerAnimationSet.ClipFramesPerSecond,value=idle[d]}});
            var clipSettings=AnimationUtility.GetAnimationClipSettings(clip);clipSettings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,clipSettings);EditorUtility.SetDirty(clip);
        }
        set.idle=idle;EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();Validate();
    }
    public static void Validate()
    {
        var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/PlayerAnimationSet.asset");
        if(!set||!set.IsValid)throw new Exception("Invalid animation data after idle import");
        float expected=Body(AssetDatabase.GetAssetPath(set.frames[4].sprites[0])).height/set.frames[4].sprites[0].pixelsPerUnit;
        for(int d=0;d<8;d++)
        {
            string path=AssetDatabase.GetAssetPath(set.idle[d]);
            if(!path.EndsWith("/Idle/"+Directions[d]+".png",StringComparison.Ordinal))throw new Exception("Wrong idle direction "+d);
            var body=Body(path);var sprite=set.idle[d];
            if(Mathf.Abs(body.height/sprite.pixelsPerUnit-expected)>.002f||Mathf.Abs(body.yMin-sprite.pivot.y)>1)throw new Exception("Idle height/feet mismatch "+d);
        }
        Debug.Log("PLAYER_IDLE_ASSETS_PASSED: eight dedicated transparent direction sprites and idle clips, visible height matched to walk and feet registered.");
    }
}

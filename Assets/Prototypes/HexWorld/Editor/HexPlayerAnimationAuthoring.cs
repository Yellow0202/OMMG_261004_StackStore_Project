using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HexPlayerAnimationAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    [MenuItem("Stack Store/Hex World/Rebuild Player Walk Animations")]
    public static void Build()
    {
        string folder=Root+"/Art/PlayerAnimation";
        string[] directions={"North","North-East","East","South-East","South","South-West","West","North-West"};
        string path=Root+"/Data/PlayerAnimationSet.asset";
        var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(path);
        if(!set){set=ScriptableObject.CreateInstance<HexPlayerAnimationSet>();AssetDatabase.CreateAsset(set,path);}
        set.walk=new AnimationClip[8];set.idle=new Sprite[8];set.frames=new HexPlayerAnimationSet.Frames[8];
        for(int d=0;d<8;d++)
        {
            var files=Directory.GetFiles(folder+"/Walk/"+directions[d],"*.png").OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(files.Length!=8)throw new Exception("Expected 8 walk frames: "+directions[d]);
            var keys=new ObjectReferenceKeyframe[9];set.frames[d]=new HexPlayerAnimationSet.Frames{sprites=new Sprite[8]};
            for(int f=0;f<8;f++)
            {
                string source=files[f].Replace('\\','/');
                var importer=(TextureImporter)AssetImporter.GetAtPath(source);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
                importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=48;
                var importSettings=new TextureImporterSettings();importer.ReadTextureSettings(importSettings);
                importSettings.spriteAlignment=(int)SpriteAlignment.Custom;importSettings.spritePivot=new Vector2(.5f,11f/84f);
                importSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(importSettings);importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(source);
                if(!sprite)throw new Exception("Missing sprite: "+source);
                set.frames[d].sprites[f]=sprite;keys[f]=new ObjectReferenceKeyframe{time=f/HexPlayerAnimationSet.ClipFramesPerSecond,value=sprite};
                if(f==0)set.idle[d]=sprite;
            }
            keys[8]=new ObjectReferenceKeyframe{time=8/HexPlayerAnimationSet.ClipFramesPerSecond,value=set.idle[d]};
            string clipPath=folder+"/Walk_"+directions[d]+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
            clip.frameRate=HexPlayerAnimationSet.ClipFramesPerSecond;
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
            var clipSettings=AnimationUtility.GetAnimationClipSettings(clip);clipSettings.loopTime=true;clipSettings.stopTime=.8f;AnimationUtility.SetAnimationClipSettings(clip,clipSettings);
            set.walk[d]=clip;EditorUtility.SetDirty(clip);
        }
        EditorUtility.SetDirty(set);
        foreach(string name in new[]{"PrototypeTestSettings","PrototypeTestSettings_Baseline"})
        {
            var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/"+name+".asset");
            settings.playerAnimation.clips=set;EditorUtility.SetDirty(settings);
        }
        AssetDatabase.SaveAssets();
        if(Directory.Exists(folder+"/Idle"))HexPlayerIdleAuthoring.Build();
        Validate();
    }
    public static void Validate()
    {
        var set=AssetDatabase.LoadAssetAtPath<HexPlayerAnimationSet>(Root+"/Data/PlayerAnimationSet.asset");
        if(!set||!set.IsValid)throw new Exception("Invalid player animation set");
        var actorObject=new GameObject("Animation verification");
        try
        {
            var renderer=actorObject.AddComponent<SpriteRenderer>();
            for(int d=0;d<8;d++)
            {
                var clip=set.walk[d];var binding=AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
                if(keys.Length!=9||!AnimationUtility.GetAnimationClipSettings(clip).loopTime)throw new Exception("Invalid loop");
                for(int f=0;f<8;f++)
                {
                    renderer.sprite=set.Sample(d,(f+.25f)/HexPlayerAnimationSet.ClipFramesPerSecond);
                    if(renderer.sprite!=keys[f].value)throw new Exception("Frame sample mismatch direction="+d+" frame="+f+" actual="+(renderer.sprite?renderer.sprite.name:"null")+" expected="+keys[f].value.name);
                }
                var delta=Quaternion.Euler(0,d*45,0)*Vector3.forward;
                if(HexPlayerAnimation.DirectionFor(delta,0)!=d||HexPlayerAnimation.DirectionFor(Quaternion.Euler(0,90,0)*delta,90)!=d)
                    throw new Exception("Direction mismatch "+d);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(actorObject);}
        Debug.Log("PLAYER_ANIMATION_VALIDATED: 64 imported sprites, 8 looping clips, all frame samples and camera-relative directions passed.");
    }
}

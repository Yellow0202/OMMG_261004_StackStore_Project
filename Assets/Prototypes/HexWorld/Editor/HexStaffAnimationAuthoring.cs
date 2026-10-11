using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexStaffAnimationAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Directions={"North","North-East","East","South-East","South","South-West","West","North-West"};
    public static readonly string[] Employees={"RookieEmployee","PromoterEmployee","VeteranEmployee"};
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var value=AssetDatabase.LoadAssetAtPath<T>(path);if(!value){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;
    }
    [MenuItem("Stack Store/Hex World/Build Staff Walk And Idle")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before importing staff art");
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/PrototypeTestSettings.asset");
        float height=settings.staffAnimation.authoredHeight;
        Directory.CreateDirectory(Root+"/Data/StaffAnimation");AssetDatabase.Refresh();
        for(int c=0;c<3;c++)
        {
            string name="Staff-0"+(c+1),data=Root+"/Data/StaffAnimation/"+name;
            var set=Asset<HexStaffAnimationSet>(data+".asset");var motion=Asset<HexPlayerAnimationSet>(data+"-Motion.asset");
            motion.mirrorWest=false;motion.walk=new AnimationClip[8];motion.idle=new Sprite[8];motion.frames=new HexPlayerAnimationSet.Frames[8];
            set.motion=motion;set.idleFrames=new HexPlayerAnimationSet.Frames[8];set.idleClips=new AnimationClip[8];
            for(int d=0;d<8;d++)
            {
                string folder=Root+"/Art/StaffAnimation/"+name+"/"+Directions[d];
                var walk=new Sprite[8];var idle=new Sprite[4];
                for(int f=0;f<8;f++)walk[f]=Import(folder+"/Walk-"+f.ToString("00")+".png",height);
                for(int f=0;f<4;f++)idle[f]=Import(folder+"/Idle-"+f.ToString("00")+".png",height);
                motion.frames[d]=new HexPlayerAnimationSet.Frames{sprites=walk};motion.idle[d]=idle[0];
                set.idleFrames[d]=new HexPlayerAnimationSet.Frames{sprites=idle};
                motion.walk[d]=Clip(folder+"/Walk.anim",walk,settings.staffAnimation.walkFramesPerSecond);
                set.idleClips[d]=Clip(folder+"/Idle.anim",idle,settings.staffAnimation.idleFramesPerSecond);
            }
            EditorUtility.SetDirty(motion);EditorUtility.SetDirty(set);
            var definition=AssetDatabase.LoadAssetAtPath<HexStaffDefinition>(Root+"/Data/"+Employees[c]+".asset");
            definition.animation=set;definition.picture=motion.idle[4];EditorUtility.SetDirty(definition);
        }
        // Persist the new tuning category without rewriting unrelated baseline categories.
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();Validate();
    }
    static Sprite Import(string path,float height)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit=192/Mathf.Max(.01f,height);importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
        var config=new TextureImporterSettings();importer.ReadTextureSettings(config);config.spriteAlignment=(int)SpriteAlignment.Custom;
        config.spritePivot=new Vector2(.5f,.125f);config.spriteMeshType=SpriteMeshType.FullRect;config.spriteGenerateFallbackPhysicsShape=false;
        importer.SetTextureSettings(config);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static AnimationClip Clip(string path,Sprite[] frames,float fps)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
        fps=Mathf.Max(.1f,fps);clip.frameRate=fps;
        var keys=new ObjectReferenceKeyframe[frames.Length+1];for(int f=0;f<keys.Length;f++)keys[f]=new ObjectReferenceKeyframe{time=f/fps,value=frames[f%frames.Length]};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var config=AnimationUtility.GetAnimationClipSettings(clip);config.loopTime=true;config.stopTime=frames.Length/fps;AnimationUtility.SetAnimationClipSettings(clip,config);EditorUtility.SetDirty(clip);return clip;
    }
    public static void Validate()
    {
        for(int c=0;c<3;c++)
        {
            var definition=AssetDatabase.LoadAssetAtPath<HexStaffDefinition>(Root+"/Data/"+Employees[c]+".asset");var set=definition.animation;
            if(!set||!set.IsValid||definition.picture!=set.motion.idle[4])throw new Exception("Invalid staff animation "+Employees[c]);
            for(int d=0;d<8;d++)
            {
                foreach(var frames in new[]{set.motion.frames[d].sprites,set.idleFrames[d].sprites})foreach(var sprite in frames)
                    if(sprite.texture.width!=256||sprite.texture.height!=256||sprite.pivot!=new Vector2(128,32))throw new Exception("Staff frame slicing / pivot mismatch");
                if(AnimationUtility.GetObjectReferenceCurve(set.motion.walk[d],new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"}).Length!=9
                    ||AnimationUtility.GetObjectReferenceCurve(set.idleClips[d],new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"}).Length!=5)throw new Exception("Staff clip key count mismatch");
            }
        }
        HexStaffChecks.Rules();Debug.Log("HEX_STAFF_ANIMATION_VALIDATED: 3 employees, 8 directions, 8 walk/4 idle frames, clips, pivots, portraits and existing employee rules.");
    }
}

using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HexPlayerAnimationPlayChecks
{
    const string Pending="HexPlayerAnimation.PlayChecks";
    static HexPlayerAnimationPlayChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run()
    {
        SessionState.SetBool(Pending,true);
        EditorApplication.update+=Check;
        EditorApplication.EnterPlaymode();
    }
    static void Check()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying||Time.deltaTime<=0)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);
        var go=new GameObject("Player animation play checks");
        int result=0;
        try
        {
            var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>("Assets/Prototypes/HexWorld/Data/PrototypeTestSettings.asset");
            var actor=go.AddComponent<HexWorldActor>();actor.enabled=false;
            actor.body=go.AddComponent<SpriteRenderer>();
            var animation=go.AddComponent<HexPlayerAnimation>();animation.Configure(actor,settings);
            HexPlayerIdleAuthoring.Validate();
            if(!actor.externalAnimation||actor.body.color!=Color.white)throw new Exception("Player configuration failed");
            for(int d=0;d<8;d++)
            {
                go.transform.position+=Quaternion.Euler(0,(Camera.main?Camera.main.transform.eulerAngles.y:0)+d*45,0)*Vector3.forward*.02f;
                typeof(HexPlayerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
                if(!animation.Moving||animation.Direction!=d)throw new Exception("Walk state failed "+d);
                typeof(HexPlayerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
                if(animation.Moving||actor.body.sprite!=settings.playerAnimation.clips.idle[d])throw new Exception("Idle state failed "+d);
            }
            go.transform.position+=Vector3.right*settings.playerAnimation.teleportDistance*2;
            typeof(HexPlayerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
            if(animation.Moving)throw new Exception("Teleport animated as walking");
            animation.enabled=false;if(actor.externalAnimation)throw new Exception("Animation ownership not released");
            animation.enabled=true;typeof(HexPlayerAnimation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(animation,null);
            if(animation.Moving||!actor.externalAnimation)throw new Exception("Re-enable failed");
            Debug.Log("PLAYER_ANIMATION_PLAY_CHECKS_PASSED: eight walk/idle directions, teleport, disable/re-enable and sprite ownership.");
        }
        catch(Exception exception){Debug.LogException(exception);result=1;}
        finally{UnityEngine.Object.DestroyImmediate(go);EditorApplication.Exit(result);}
    }
}

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HexCustomerAnimationPlayChecks
{
    const string Pending="HexCustomerAnimation.PlayChecks";
    static int updates;
    static HexCustomerAnimationPlayChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");
        SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();
    }
    static void Check()
    {
        if(!EditorApplication.isPlaying||Time.deltaTime<=0||++updates<10)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);
        int result=0;GameObject go=null;HexTestSettings settings=null;HexCustomerAnimationCatalog pool=null;
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<HexTestSettings>("Assets/Prototypes/HexWorld/Data/PrototypeTestSettings.asset");
            settings=UnityEngine.Object.Instantiate(source);pool=ScriptableObject.CreateInstance<HexCustomerAnimationCatalog>();
            settings.customerAnimation.catalog=pool;
            var tick=typeof(HexCustomerAnimation).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
            go=new GameObject("Customer animation checks");var actor=go.AddComponent<HexWorldActor>();actor.enabled=false;
            actor.body=go.AddComponent<SpriteRenderer>();var animation=go.AddComponent<HexCustomerAnimation>();
            foreach(var character in source.customerAnimation.catalog.characters)
            {
                pool.characters=new[]{character};animation.Configure(actor,settings);
                for(int d=0;d<8;d++)
                {
                    go.transform.position+=Quaternion.Euler(0,(Camera.main?Camera.main.transform.eulerAngles.y:0)+d*45,0)*Vector3.forward*.02f;
                    tick.Invoke(animation,null);
                    if(!animation.Moving||animation.Direction!=d)throw new Exception("Walk direction "+character.name+"/"+d);
                    tick.Invoke(animation,null);
                    if(animation.Moving||actor.body.sprite!=character.idle[d])throw new Exception("Idle direction "+d);
                }
                actor.seated=true;tick.Invoke(animation,null);
                if(animation.Moving||actor.body.sprite!=character.idle[animation.Direction])throw new Exception("Seated identity");
                actor.seated=false;
                var tint=new Color(0,0,0,.4f);actor.body.color=tint;tick.Invoke(animation,null);
                if(actor.body.color!=tint)throw new Exception("Tint changed");
                go.transform.position+=Vector3.right*settings.customerAnimation.teleportDistance*2;tick.Invoke(animation,null);
                if(animation.Moving)throw new Exception("Teleport animated");
                animation.enabled=false;if(actor.externalAnimation)throw new Exception("Ownership release");
                animation.enabled=true;tick.Invoke(animation,null);
                if(!actor.externalAnimation||animation.Moving)throw new Exception("Re-enable");
            }
            var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
            if(!game||game.CustomerCount==0)throw new Exception("No actual scene customers spawned");
            int spawned=0;
            foreach(var customer in game.customerRoot.GetComponentsInChildren<HexCustomerAnimation>(true))
            {
                if(!customer.Character||!Array.Exists(source.customerAnimation.catalog.characters,x=>x==customer.Character))throw new Exception("Invalid spawned appearance");
                spawned++;
            }
            if(spawned!=game.CustomerCount)throw new Exception("Spawn connection mismatch");
            Debug.Log("CUSTOMER_ANIMATION_PLAY_CHECKS_PASSED: all six characters/eight directions, idle, seating, tint, teleport, re-enable, actual scene spawn.");
        }
        catch(Exception exception){Debug.LogException(exception);result=1;}
        finally
        {
            if(go)UnityEngine.Object.DestroyImmediate(go);
            if(settings)UnityEngine.Object.DestroyImmediate(settings);
            if(pool)UnityEngine.Object.DestroyImmediate(pool);
            EditorApplication.Exit(result);
        }
    }
}

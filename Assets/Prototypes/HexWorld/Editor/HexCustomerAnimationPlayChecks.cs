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
            var advance=typeof(HexCustomerAnimation).GetMethod("AdvanceMotion",BindingFlags.NonPublic|BindingFlags.Instance);
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
                    if(actor.body.flipX!=(character.mirrorWest&&d==6))throw new Exception("Wrong horizontal mirroring");
                    advance.Invoke(animation,new object[]{Vector3.zero,settings.customerAnimation.stopGraceSeconds+.01f,0f});
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
            // A one-frame route gap must not restart the clock or switch to idle.
            pool.characters=new[]{source.customerAnimation.catalog.characters[0]};
            animation.Configure(actor,settings);
            advance.Invoke(animation,new object[]{Vector3.forward*.02f,1f/60,0f});
            var phaseField=typeof(HexCustomerAnimation).GetField("phase",BindingFlags.NonPublic|BindingFlags.Instance);
            float before=(float)phaseField.GetValue(animation);
            advance.Invoke(animation,new object[]{Vector3.zero,1f/60,0f});
            if(!animation.Moving||(float)phaseField.GetValue(animation)<=before)throw new Exception("Short gap reset animation");
            foreach(float angle in new[]{21f,24f,21f,24f})
            {
                advance.Invoke(animation,new object[]{Quaternion.Euler(0,angle,0)*Vector3.forward*.02f,1f/60,0f});
                if(animation.Direction!=0)throw new Exception("Direction boundary flicker");
            }
            advance.Invoke(animation,new object[]{Quaternion.Euler(0,40,0)*Vector3.forward*.02f,1f/60,0f});
            if(animation.Direction!=1)throw new Exception("Intentional direction change ignored");
            advance.Invoke(animation,new object[]{Vector3.zero,settings.customerAnimation.stopGraceSeconds+.01f,0f});
            if(animation.Moving)throw new Exception("Real stop ignored");
            before=(float)phaseField.GetValue(animation);
            advance.Invoke(animation,new object[]{Vector3.forward*.02f,1f/60,0f});
            if(!animation.Moving||(float)phaseField.GetValue(animation)<=before)throw new Exception("Resume reset animation");
            float referencePhase=-1;
            foreach(int rate in new[]{30,60,120})
            {
                animation.Configure(actor,settings);
                for(int i=0;i<rate;i++)advance.Invoke(animation,new object[]{Vector3.forward/rate,1f/rate,0f});
                float end=(float)phaseField.GetValue(animation);
                if(referencePhase>=0&&Mathf.Abs(Mathf.DeltaAngle(referencePhase*450,end*450))>.01f)throw new Exception("Frame-rate dependent walk clock");
                referencePhase=end;
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
            CheckWaypointBudget(game,settings);
            Debug.Log("CUSTOMER_ANIMATION_PLAY_CHECKS_PASSED: all six characters/eight directions, idle, seating, tint, teleport, re-enable, actual scene spawn; waypoint distance budget, short-gap clock continuity, direction hysteresis, real stop and resume.");
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
    static void CheckWaypointBudget(HexPrototype game,HexTestSettings settings)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
        var guestType=typeof(HexPrototype).GetNestedType("Guest",BindingFlags.NonPublic);
        var guest=Activator.CreateInstance(guestType,true);
        var obj=new GameObject("Waypoint budget check");
        try
        {
            var actor=obj.AddComponent<HexWorldActor>();actor.enabled=false;
            Vector3 a=HexBoardModel.World(Vector2Int.zero)+Vector3.up*settings.guests.groundHeight;
            Vector3 b=HexBoardModel.World(new Vector2Int(1,0))+Vector3.up*settings.guests.groundHeight;
            Vector3 axis=(b-a).normalized;obj.transform.position=a-axis*.01f;
            void Set(string name,object value)=>guestType.GetField(name,flags).SetValue(guest,value);
            Set("view",actor);Set("floor",0);Set("targetFloor",0);
            Set("navigationRevision",game.board.Layout.Revision);
            Set("navigationGoal",new HexNavNode(0,HexShopLayout.Cell(b)));
            Set("path",new System.Collections.Generic.List<HexNavNode>{new HexNavNode(0,Vector2Int.zero),new HexNavNode(0,new Vector2Int(1,0))});
            Set("waypoint",0);
            var stateField=guestType.GetField("state",flags);
            stateField.SetValue(guest,Enum.Parse(stateField.FieldType,"Browsing"));
            Vector3 start=obj.transform.position;
            typeof(HexPrototype).GetMethod("MoveGuest",flags).Invoke(game,new[]{guest,(object)b,(object)(.1f/settings.guests.walkSpeed)});
            if(Mathf.Abs(Vector3.Distance(start,obj.transform.position)-.1f)>.0001f)throw new Exception("Distance discarded at waypoint");
        }
        finally{UnityEngine.Object.DestroyImmediate(obj);}
    }

}

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HexStaffAnimationPlayChecks
{
    const string Pending="StaffAnimation.Play";static int frames;
    static HexStaffAnimationPlayChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();}
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Check()
    {
        if(!EditorApplication.isPlaying||++frames<10)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);int result=0;
        GameObject go=null;HexTestSettings shared=null;var original=HexTestSettings.Current;
        try
        {
            shared=UnityEngine.Object.Instantiate(original);typeof(HexTestSettings).GetProperty("Current").SetValue(null,shared);
            go=new GameObject("Staff animation checks");var actor=go.AddComponent<HexWorldActor>();actor.enabled=false;actor.body=go.AddComponent<SpriteRenderer>();
            var animation=go.AddComponent<HexStaffAnimation>();
            var advance=typeof(HexStaffAnimation).GetMethod("AdvanceMotion",BindingFlags.NonPublic|BindingFlags.Instance);
            Action<Vector3,float,float> tick=(delta,dt,yaw)=>advance.Invoke(animation,new object[]{delta,dt,yaw});
            var phase=typeof(HexStaffAnimation).GetField("walkFrame",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(string employee in HexStaffAnimationAuthoring.Employees)
            {
                var definition=AssetDatabase.LoadAssetAtPath<HexStaffDefinition>("Assets/Prototypes/HexWorld/Data/"+employee+".asset");var data=definition.animation;
                actor.body.transform.localScale=Vector3.one;animation.Configure(actor,data);
                for(int d=0;d<8;d++)
                {
                    for(int f=0;f<8;f++)
                    {
                        tick(Quaternion.Euler(0,d*45,0)*Vector3.forward*.02f,1/shared.staffAnimation.walkFramesPerSecond,0);
                        Assert(animation.Moving&&animation.Direction==d&&Array.IndexOf(data.motion.frames[d].sprites,actor.body.sprite)>=0,"Direction/frame identity");
                    }
                    tick(Vector3.zero,shared.staffAnimation.stopGraceSeconds+.01f,0);
                    Assert(!animation.Moving,"Failed to stop");
                    for(int f=0;f<4;f++){tick(Vector3.zero,1/shared.staffAnimation.idleFramesPerSecond,0);Assert(actor.body.sprite==data.Idle(d,f+1),"Breathing sequence");}
                }
                animation.Configure(actor,data);tick(Vector3.forward*.02f,.02f,0);float before=(float)phase.GetValue(animation);
                tick(Vector3.zero,.02f,0);Assert(animation.Moving&&(float)phase.GetValue(animation)>before,"Short navigation gap reset clock");
                before=(float)phase.GetValue(animation);tick(Vector3.right*.02f,.02f,0);Assert((float)phase.GetValue(animation)>before,"Direction change reset clock");
                tick(Vector3.right*.02f,.02f,90);Assert(animation.Direction==0,"Camera-relative direction");
                tick(Vector3.forward*100,.02f,0);Assert(!animation.Moving,"Teleport walking");
                shared.staffAnimation.scale=1.25f;tick(Vector3.zero,1,0);Assert(Mathf.Abs(actor.body.transform.localScale.x-1.25f)<.001f,"Live scale");shared.staffAnimation.scale=1;
                actor.body.color=new Color(.5f,.5f,.5f,.4f);tick(Vector3.zero,.02f,0);Assert(actor.body.color.a==.4f,"Transparency overwritten");
                animation.enabled=false;Assert(!actor.externalAnimation,"Disable ownership");animation.enabled=true;Assert(actor.externalAnimation,"Re-enable ownership");
            }
            var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();game.CloseChoice();game.AddGold(100);
            Assert(game.BeginShopping(),"Cannot open hiring");
            foreach(string name in HexStaffAnimationAuthoring.Employees)
            {
                var definition=AssetDatabase.LoadAssetAtPath<HexStaffDefinition>("Assets/Prototypes/HexWorld/Data/"+name+".asset");int before=game.Gold;
                Assert(game.staffSystem.Hire(definition),"Actual hiring failed");
                var hired=game.staffSystem.employees[game.staffSystem.employees.Count-1];var anim=hired.view.GetComponent<HexStaffAnimation>();
                Assert(anim&&anim.Character==definition.animation&&hired.view.body.color==Color.white&&hired.view.externalAnimation,"Actual employee appearance disconnected");
                Assert(game.Gold==before-definition.hirePrice,"Hire price changed");
            }
            game.EndShopping();HexStaffChecks.Rules();
            Debug.Log("HEX_STAFF_ANIMATION_PLAY_PASSED: three identities x eight directions, walk/idle cycles, camera yaw, short gaps, phase continuity, teleport, live scale, transparency, re-enable and actual paid hiring.");
        }
        catch(Exception error){Debug.LogException(error);result=1;}
        finally{typeof(HexTestSettings).GetProperty("Current").SetValue(null,original);if(go)UnityEngine.Object.DestroyImmediate(go);if(shared)UnityEngine.Object.DestroyImmediate(shared);EditorApplication.Exit(result);}
    }
}

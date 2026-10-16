using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HexReputationChecks
{
    const string Pending="HexReputationChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int frames;
    static HexReputationChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();}
    static void Require(bool condition,string detail){if(!condition)throw new Exception(detail);}
    static void Check()
    {
        if(!EditorApplication.isPlaying||++frames<20)return;
        try
        {
            var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
            game.enabled=false;
            var guests=(IList)typeof(HexPrototype).GetField("guests",Flags).GetValue(game);
            var waiting=(IList)typeof(HexPrototype).GetField("waitingGuests",Flags).GetValue(game);
            guests.Clear();waiting.Clear();game.ConfigureStart(0,100);
            Require(game.Reputation==0&&game.reputationLabel,"Initial reputation / saved UGUI reference");
            var type=typeof(HexPrototype).GetNestedType("Guest",BindingFlags.NonPublic);
            var state=typeof(HexPrototype).GetNestedType("GuestState",BindingFlags.NonPublic);
            int next=50000;
            object Guest(string status,bool seated=false)
            {
                var guest=Activator.CreateInstance(type);
                void Set(string key,object value)=>type.GetField(key).SetValue(guest,value);
                var view=UnityEngine.Object.Instantiate(game.customerPrefab,game.customerRoot);
                view.transform.position=game.service.Position;view.seated=seated;
                Set("view",view);Set("id",++next);Set("state",Enum.Parse(state,status));Set("floor",game.service.Floor);
                Set("originDirection",1);Set("patience",.01f);Set("initialPatience",10f);Set("reward",1);
                Set("hasFacility",seated);Set("facility",new HexNavNode(game.service.Floor,HexShopLayout.Cell(game.service.Position)));
                guests.Add(guest);if(status=="Waiting")waiting.Add(guest);return guest;
            }
            void Leave(object guest)=>typeof(HexPrototype).GetMethod("Leave",Flags).Invoke(game,new[]{guest,(object)1});
            int Id(object guest)=>(int)type.GetField("id").GetValue(guest);
            var passer=Guest("Crossing");Leave(passer);Require(game.Reputation==0,"Passer-by penalty");
            var standing=Guest("Waiting");Require(game.CompleteService(Id(standing),null)&&game.Reputation==1,"Standing +1");
            Require(!game.CompleteService(Id(standing),null)&&game.Reputation==1,"Duplicate service award");
            var table=Guest("Waiting",true);Require(game.CompleteService(Id(table),null)&&game.Reputation==3,"Table +2");
            Leave(table);Require(game.Reputation==3,"Eating guest departure penalty");
            var timeout=Guest("Waiting");typeof(HexPrototype).GetMethod("TickGuests",Flags).Invoke(game,new object[]{.02f});
            Require(game.Reputation==2,"Waiting timeout -1");Leave(timeout);Require(game.Reputation==2,"Duplicate departure penalty");
            var expelled=Guest("Waiting");Require(game.EjectWaitingGuest(game.service.Position,game.service.Floor,10)&&game.Reputation==1,"Employee ejection -1");
            game.ConfigureStart(0,100);var negative=Guest("Waiting");Leave(negative);Require(game.Reputation==-1,"Negative reputation allowed");
            var settings=HexTestSettings.Current;int saved=settings.reputation.standingServiceChange;
            try{settings.reputation.standingServiceChange=4;var custom=Guest("Waiting");Require(game.CompleteService(Id(custom),null)&&game.Reputation==3,"Live ScriptableObject tuning");}
            finally{settings.reputation.standingServiceChange=saved;}
            Require(game.reputationLabel.text==LocalizationService.Text("hud.reputation","value",game.Reputation),"Localized HUD refresh");
            Require(game.reputationLabel.font&&game.reputationLabel.rectTransform.rect.height>=30,"HUD font / layout");
            Debug.Log("HEX_REPUTATION_VALIDATED: saved localized UGUI, initial zero/negative values, standing +1/table +2, timeout/ejection -1, neutral passers/completed visits, no duplicate events, live ScriptableObject tuning.");
            Finish(0);
        }
        catch(Exception exception){Debug.LogException(exception);Finish(1);}
    }
    static void Finish(int code){SessionState.SetBool(Pending,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
}

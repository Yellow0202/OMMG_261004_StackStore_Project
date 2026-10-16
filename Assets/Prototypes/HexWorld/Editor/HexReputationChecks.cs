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
            Require(game.Reputation==50&&game.MaximumReputation==100&&game.reputationLabel&&game.gameOverPanel&&!game.gameOverPanel.activeSelf,"Initial reputation / saved UGUI reference");
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
            var settings=HexTestSettings.Current;
            void Change(int value)=>typeof(HexPrototype).GetMethod("ChangeReputation",Flags).Invoke(game,new object[]{value});
            void Tick(float dt)=>typeof(HexPrototype).GetMethod("TickReputation",Flags).Invoke(game,new object[]{dt});
            var passer=Guest("Crossing");Leave(passer);Require(game.Reputation==50,"Passer-by penalty");
            var standing=Guest("Waiting");Require(game.CompleteService(Id(standing),null)&&game.Reputation==51,"Standing +1");
            Require(!game.CompleteService(Id(standing),null)&&game.Reputation==51,"Duplicate service award");
            var table=Guest("Waiting",true);Require(game.CompleteService(Id(table),null)&&game.Reputation==53,"Table +2");
            Leave(table);Require(game.Reputation==53,"Completed guest departure penalty");
            var timeout=Guest("Waiting");typeof(HexPrototype).GetMethod("TickGuests",Flags).Invoke(game,new object[]{.02f});
            Require(game.Reputation==52,"Waiting timeout -1");Leave(timeout);Require(game.Reputation==52,"Duplicate departure penalty");
            Guest("Waiting");Require(game.EjectWaitingGuest(game.service.Position,game.service.Floor,10)&&game.Reputation==51,"Employee ejection -1");
            Change(10000);Require(game.Reputation==100,"Maximum clamp");
            Tick(19);Require(game.Reputation==100,"Grace period");Tick(6);Require(game.Reputation==99,"First idle decay");
            Tick(10);Require(game.Reputation==97,"Repeated idle decay");
            var reset=Guest("Waiting");Require(game.CompleteService(Id(reset),null)&&game.ReputationIdleSeconds==0,"Service reset");
            Tick(24);Require(game.Reputation==98,"Reset grace");var failed=Guest("Waiting");Leave(failed);
            Tick(1);Require(game.Reputation==96&&game.ReputationIdleSeconds==25,"Failure does not reset idle timer");
            var shop=UnityEngine.Object.FindFirstObjectByType<HexShopSystem>();shop.Open();
            double idle=game.ReputationIdleSeconds;Tick(100);Require(game.ReputationIdleSeconds==idle&&game.Reputation==96,"Shopping pause");shop.Close();
            game.BeginBuilding();Tick(100);Require(game.ReputationIdleSeconds==idle,"Building pause");game.EndBuilding();
            typeof(HexPrototype).GetProperty("IsChoosing").SetValue(game,true);Tick(100);Require(game.ReputationIdleSeconds==idle,"Reward selection pause");typeof(HexPrototype).GetProperty("IsChoosing").SetValue(game,false);
            int saved=settings.reputation.standingServiceChange;
            try{settings.reputation.standingServiceChange=3;var custom=Guest("Waiting");Require(game.CompleteService(Id(custom),null)&&game.Reputation==99,"Live tuning");}
            finally{settings.reputation.standingServiceChange=saved;}
            var atCap=Guest("Waiting",true);Require(game.CompleteService(Id(atCap),null)&&game.Reputation==100,"Table cap");
            Require(game.reputationLabel.text==LocalizationService.Text("hud.reputation","value",100,"max",100),"Localized bounded HUD");
            Change(-99);Guest("Waiting");typeof(HexPrototype).GetMethod("TickGuests",Flags).Invoke(game,new object[]{.02f});
            Require(game.Reputation==0&&game.IsGameOver&&Time.timeScale==0&&game.gameOverPanel.activeSelf,"Timeout game over");
            Require(game.gameOverLabel.font&&game.gameOverLabel.text==LocalizationService.Text("reputation.gameover"),"Game-over font/localization");
            Require(!game.BeginShopping(),"Shopping after game over");game.BeginBuilding();Require(!game.IsBuilding,"Building after game over");
            int gold=game.Gold;game.AddGold(10);Require(game.Gold==gold&&!game.TrySpendGold(1),"Wallet after game over");
            Require(!game.CompleteService(Id(Guest("Waiting")),null),"Service after game over");
            Change(10);Tick(100);game.EndShopping();Require(game.Reputation==0&&Time.timeScale==0,"Terminal state cannot resume");
            Require(!shop.CanUse(shop.quickConsumable),"Consumable after game over");
            game.ConfigureStart(0,100);typeof(HexPrototype).GetMethod("Resume",Flags).Invoke(game,null);Tick(270);
            Require(game.Reputation==0&&game.IsGameOver&&Time.timeScale==0&&game.gameOverPanel.activeSelf,"Inactivity game over");
            Debug.Log("HEX_REPUTATION_VALIDATED: initial 50/max 100, service +1/table +2, timeout/ejection -1, cap, repeated inactivity decay, grace reset only on success, menus pause decay, terminal zero freezes game and blocks interactions, saved localized UGUI.");
            Finish(0);
        }
        catch(Exception exception){Debug.LogException(exception);Finish(1);}
    }
    static void Finish(int code){SessionState.SetBool(Pending,false);EditorApplication.update-=Check;EditorApplication.Exit(code);}
}

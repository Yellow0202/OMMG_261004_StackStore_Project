using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HexMoreItemsPlayChecks
{
    const string Pending="HexMoreItems.Play";static int frames;
    static HexMoreItemsPlayChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();
    }
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Check()
    {
        if(!EditorApplication.isPlaying||++frames<10)return;
        EditorApplication.update-=Check;SessionState.SetBool(Pending,false);int result=0;
        try
        {
            var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();var localization=LocalizationService.Instance;var language=localization.language;
            var items=Array.ConvertAll(HexMoreItemsAuthoring.Keys,key=>AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Prototypes/HexWorld/Data/Items/"+key+".asset"));
            float originalInterval=game.ServiceInterval,originalSpeed=game.service.walkSpeed;int originalGold=game.Gold;
            foreach(var item in items)
            {
                Assert(game.CanGrantItem(item)&&game.GrantItem(item)&&game.ItemLevel(item)==1,"Failed grant "+item.key);
                foreach(var lang in new[]{DisplayLanguage.Korean,DisplayLanguage.English})
                {
                    localization.SetLanguage(lang);string description=item.DisplayDescription();
                    Assert(!description.Contains("{")&&!description.Contains("[")&&!item.DisplayName.StartsWith("["),"Unformatted translation "+item.key);
                }
            }
            localization.SetLanguage(language);
            Assert(game.ServiceReward(1,false)==2&&game.ServiceReward(2,true)==4,"Standing/table reward bonus routing");
            Assert(Mathf.Abs(game.OrderPatience(10,false)-11)<.001f&&Mathf.Abs(game.OrderPatience(20,true)-26)<.001f,"Table-only patience routing");
            Assert(Mathf.Abs(game.DeliverySpeedMultiplier-1.2f)<.001f&&Mathf.Abs(game.service.EffectiveWalkSpeed-originalSpeed*1.2f)<.001f,"Delivery stacking / integration");
            Assert(Mathf.Abs(game.ServiceInterval-originalInterval/1.08f)<.001f,"Cooking percent integration");
            Assert(game.Gold==originalGold,"Granting a reward spent gold");
            foreach(var item in items)
            {
                while(game.ItemLevel(item)<item.maxLevel)Assert(game.GrantItem(item),"Upgrade stacking failed");
                Assert(!game.CanGrantItem(item)&&!game.GrantItem(item),"Exceeded level limit "+item.key);
            }
            Assert(game.ServiceReward(1,false)==4&&game.ServiceReward(2,true)==8,"Maximum gold stacks");
            Assert(Mathf.Abs(game.DeliverySpeedMultiplier-2)<.001f,"Maximum delivery stacks");
            typeof(HexPrototype).GetMethod("OpenChoice",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
            var offered=(System.Collections.IEnumerable)typeof(HexPrototype).GetField("offered",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            foreach(ItemDefinition item in offered)Assert(Array.IndexOf(items,item)<0,"Max-level item remained in reward selection");
            game.CloseChoice();HexDirectServiceChecks.Run();
            Debug.Log("HEX_MORE_ITEMS_PLAY_PASSED: six grants/levels/caps, current-gold preservation, table versus standing rewards/patience, cooking and player movement stacking, bilingual formatted strings, capped-item exclusion, existing service/wall rules.");
        }
        catch(Exception error){Debug.LogException(error);result=1;}
        finally{EditorApplication.Exit(result);}
    }
}

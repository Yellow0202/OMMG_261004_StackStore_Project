using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
[InitializeOnLoad]
public static class HexCrisisShopChecks
{
 const string Pending="HexCrisisShopChecks";static int frames,stage;static float cook,delivery;
 static HexCrisisShopChecks(){if(SessionState.GetBool(Pending,false))EditorApplication.update+=Check;}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");SessionState.SetBool(Pending,true);EditorApplication.update+=Check;EditorApplication.EnterPlaymode();}
 static void Require(bool value,string error){if(!value)throw new Exception(error);}
 static void Check()
 {
  if(!Application.isPlaying||!EditorApplication.isPlaying||++frames<20)return;
  try
  {
   var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();var shop=UnityEngine.Object.FindFirstObjectByType<HexShopSystem>();
   if(stage==1){Require(game.BuffRemaining==cook&&game.DeliveryBuffRemaining==delivery,"Paused buff timer advanced");shop.Close();stage=2;frames=0;return;}
   if(stage==2){Require(game.BuffRemaining<cook&&game.DeliveryBuffRemaining<delivery,"Buff timers did not resume");Capture("QuickSlots.png");Finish(0);return;}
   Require(shop.rows.Length>=7&&shop.quickSlots.Length==5,"Saved shop/quick slot count");
   Require(!shop.quickUseButton||!shop.quickUseButton.gameObject.activeSelf,"Legacy potion button still active");
   foreach(var slot in shop.quickSlots){Require(slot.offer.picture&&slot.icon&&slot.title&&slot.count&&slot.status,"Missing quick slot references");Require(slot.offer.IsValid,"Invalid consumable");}
   Require(shop.rows.All(x=>!x.offer.key.ToLowerInvariant().Contains("admission")),"Admission restriction unexpectedly present");
   game.ConfigureStart(200,100);shop.Open();Require(shop.IsOpen&&Time.timeScale==0,"Shop pause failed");
   var snack=shop.rows.First(x=>x.offer.key=="SnackOffer").offer;
   var kitchen=shop.rows.First(x=>x.offer.key=="KitchenRushOffer").offer;
   var deliveryOffer=shop.rows.First(x=>x.offer.key=="DeliveryRushOffer").offer;
   var meals=shop.rows.First(x=>x.offer.key=="ReadyMealsOffer").offer;
   var potion=shop.quickConsumable;
   game.ConfigureStart(0,100);Require(!shop.Buy(snack)&&shop.Stock(snack)==0,"Insufficient gold transaction");game.ConfigureStart(200,100);
   int before=game.Gold;Require(shop.Buy(snack)&&game.Gold==before-snack.price&&shop.Stock(snack)==1,"Purchase transaction");
   Require(!shop.Use(snack)&&shop.Stock(snack)==1,"No-target snack consumed");
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(HexPrototype).GetNestedType("Guest",BindingFlags.NonPublic);var guest=Activator.CreateInstance(type);
   var state=typeof(HexPrototype).GetNestedType("GuestState",BindingFlags.NonPublic);
   var view=UnityEngine.Object.Instantiate(game.customerPrefab,game.customerRoot);view.transform.position=Vector3.zero;
   type.GetField("view").SetValue(guest,view);type.GetField("state").SetValue(guest,Enum.Parse(state,"Waiting"));type.GetField("floor").SetValue(guest,game.board.CurrentFloor);
   type.GetField("initialPatience").SetValue(guest,10f);type.GetField("patience").SetValue(guest,1f);
   ((IList)typeof(HexPrototype).GetField("waitingGuests",flags).GetValue(game)).Add(guest);
   Require(shop.Use(snack)&&shop.Stock(snack)==0&&(float)type.GetField("patience").GetValue(guest)==6,"Snack recovery");
   Require(shop.Buy(snack)&&shop.Use(snack)&&(float)type.GetField("patience").GetValue(guest)==8.5f,"Snack diminishing returns");
   Require(!shop.Use(snack),"Zero stock allowed use");
   Require(shop.Buy(kitchen)&&shop.Use(kitchen),"Kitchen buff failed");
   Require(shop.Buy(potion)&&!shop.Use(potion)&&shop.Stock(potion)>0,"Cooking overlap consumed potion");
   Require(shop.Buy(deliveryOffer)&&shop.Use(deliveryOffer)&&game.DeliverySpeedMultiplier>1,"Delivery buff failed");
   Require(shop.Buy(deliveryOffer)&&!shop.Use(deliveryOffer)&&shop.Stock(deliveryOffer)==1,"Delivery overlap consumed item");
   Require(shop.Buy(meals)&&shop.Use(meals),"Ready meals failed");
   Require(game.service.Food.ReadyCount+game.service.Food.ReservedMeals==meals.readyMeals,"Ready meal accounting");
   for(int i=0;i<meals.readyMeals;i++)Require(game.service.Food.Consume(game.service.Food.ReadySlot()),"Ready reserve refill failed");
   Require(game.service.Food.ReadyCount==0&&game.service.Food.ReservedMeals==0,"Extra meal duplication");
   ((IList)typeof(HexPrototype).GetField("waitingGuests",flags).GetValue(game)).Remove(guest);UnityEngine.Object.Destroy(view.gameObject);
   cook=game.BuffRemaining;delivery=game.DeliveryBuffRemaining;
   foreach(var label in UnityEngine.Object.FindObjectsByType<PrototypeFontBinding>(FindObjectsInactive.Include,FindObjectsSortMode.None))label.Apply();
   Capture("Shop-Top.png");var scroll=shop.panel.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Capture("Shop-Bottom.png");
   stage=1;frames=0;
  }
  catch(Exception exception){Debug.LogException(exception);Finish(1);}
 }
 static void Finish(int code){SessionState.SetBool(Pending,false);EditorApplication.update-=Check;if(code==0)Debug.Log("HEX_CRISIS_SHOP_VALIDATED: saved UGUI, five quick slots, gold/stock, no-target and diminishing snacks, cooking conflict, independent delivery buff, ready meal reserves, pause/resume timers; screenshots.");EditorApplication.Exit(code);}
 static void Capture(string name)
 {
  var camera=Camera.main;var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=false;cameraData.antialiasing=AntialiasingMode.None;var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x=>x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
  foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
  var target=new RenderTexture(1600,900,24);var picture=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
  try
  {
   target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
   RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,1600,900),0,0);picture.Apply();
   var directory=Path.Combine(Directory.GetParent(Application.dataPath).Parent.Parent.FullName,"ArtReferences","2026-10-12-Crisis-Items");Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name),picture.EncodeToPNG());
  }
  finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(picture);foreach(var canvas in canvases)canvas.renderMode=RenderMode.ScreenSpaceOverlay;}
 }
}

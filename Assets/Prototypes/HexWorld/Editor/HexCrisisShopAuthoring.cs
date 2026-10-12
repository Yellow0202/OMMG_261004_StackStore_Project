using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class HexCrisisShopAuthoring
{
 const string Root="Assets/Prototypes/HexWorld";
 static Font font;static LocalizationTable strings;
 public static void Run()
 {
  strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
  Add("shop.use.stock","USE ({count})","사용 ({count}개)");Add("shop.potion.short","Speed Potion","가속 물약");
  Add("shop.slot.no.target","NO WAITING GUESTS","대기 손님 없음");Add("shop.slot.count","{count} owned","보유 {count}개");Add("shop.slot.active","{seconds}s active","효과 {seconds}초");Add("shop.slot.ready","USE","사용 가능");Add("shop.slot.empty","BUY IN SHOP","상점에서 구매");
  Add("shop.help","Scroll for more products. Shopping pauses gameplay. Buy consumables for the quick slots.","스크롤하여 모든 상품을 확인하세요. 상점에서는 시간이 멈추며 소모품은 퀵슬롯에 보관됩니다.");
  Add("shop.used","Item used. Timed effects resume with gameplay.","아이템을 사용했습니다. 지속 효과는 영업 재개 후 감소합니다.");
  Add("shop.buff.all","Cook {seconds}s / Delivery {delivery}s / Meals {meals}","조리 {seconds}초 · 이동 {delivery}초 · 예비 음식 {meals}개");
  string[] names={"SnackOffer","KitchenRushOffer","DeliveryRushOffer","ReadyMealsOffer"};
  string[] icons={"snack","kitchen","delivery","meals"};
  string[] ko={"서비스 간식","주방 비상 가동","전력 배달","긴급 완제품"};
  string[] en={"Complimentary Snacks","Kitchen Overdrive","Express Delivery","Emergency Ready Meals"};
  string[] descriptions={"현재 층에서 가장 급한 대기 손님 {targets}명의 시간을 최대 {patience}초 회복. 원래 대기 한도까지 회복하며 같은 손님에게 반복 사용하면 효과 감소.","{seconds}초 동안 조리 속도 +{speed}%. 가속 물약과 동시 사용 불가.","{seconds}초 동안 플레이어와 직원 이동 속도 +{speed}%. 효과 중 재사용 불가.","준비된 음식 {meals}개 즉시 확보. 음식 칸을 먼저 채우며 남은 음식은 예비 보관해 다음 배달에 사용."};
  string[] english={"Restore up to {patience}s for the {targets} most urgent waiting guests on this floor. Capped at original patience; repeated snacks have diminishing returns.","Cooking speed +{speed}% for {seconds}s. Cannot overlap a speed potion.","Player and employee movement speed +{speed}% for {seconds}s. Cannot refresh while active.","Receive {meals} ready meals. Fill preparation slots first and reserve extras for subsequent deliveries."};
  var extra=new HexShopOffer[4];
  for(int i=0;i<4;i++)
  {
   string path=Root+"/Art/CrisisItems/"+icons[i]+".png";
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Missing icon "+path);
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
   string asset=Root+"/Data/"+names[i]+".asset";var offer=AssetDatabase.LoadAssetAtPath<HexShopOffer>(asset);
   if(!offer){offer=ScriptableObject.CreateInstance<HexShopOffer>();AssetDatabase.CreateAsset(offer,asset);}
   offer.key=names[i];offer.nameKey="shop.crisis."+icons[i]+".name";offer.descriptionKey="shop.crisis."+icons[i]+".desc";offer.kind=HexShopKind.Consumable;
   offer.effect=i==0?HexConsumableEffect.Patience:i==1?HexConsumableEffect.ServiceSpeed:i==2?HexConsumableEffect.DeliverySpeed:HexConsumableEffect.ReadyMeals;
   offer.price=new[]{3,4,3,5}[i];offer.quantity=1;offer.buffSeconds=i==1?8:10;offer.speedBonus=i==1?2:1;offer.picture=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   Add(offer.nameKey,en[i],ko[i]);Add(offer.descriptionKey,english[i],descriptions[i]);EditorUtility.SetDirty(offer);extra[i]=offer;
  }
  EditorUtility.SetDirty(strings);strings.Rebuild();
  var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var shop=UnityEngine.Object.FindFirstObjectByType<HexShopSystem>(FindObjectsInactive.Include);font=shop.balanceLabel.font;
  var offers=shop.rows.Select(x=>x.offer).Where(x=>x&&!names.Contains(x.name)).Concat(extra).ToArray();
  foreach(var row in shop.rows)if(row.buy)UnityEngine.Object.DestroyImmediate(row.buy.transform.parent.gameObject);
  var panel=shop.panel.transform;
  var modalCanvas=panel.GetComponent<Canvas>();if(!modalCanvas)modalCanvas=panel.gameObject.AddComponent<Canvas>();modalCanvas.overrideSorting=true;modalCanvas.sortingOrder=30000;
  if(!panel.GetComponent<GraphicRaycaster>())panel.gameObject.AddComponent<GraphicRaycaster>();
  panel.Find("Title").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,335);
  shop.balanceLabel.rectTransform.anchoredPosition=new Vector2(0,285);
  shop.messageLabel.rectTransform.anchoredPosition=new Vector2(0,-270);
  shop.closeButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-345);
  var oldViewport=panel.Find("Product Scroll");if(oldViewport)UnityEngine.Object.DestroyImmediate(oldViewport.gameObject);
  var viewport=Rect("Product Scroll",panel,new Vector2(0,10),new Vector2(1340,470));viewport.gameObject.AddComponent<Image>().color=new Color(.03f,.05f,.08f,1);viewport.gameObject.AddComponent<RectMask2D>();
  var content=Rect("Product Content",viewport,Vector2.zero,new Vector2(1340,Mathf.Ceil(offers.Length/2f)*210));content.anchorMin=content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,1);
  var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
  shop.rows=new HexShopRow[offers.Length];
  for(int i=0;i<offers.Length;i++)
  {
   var card=Rect("Product "+i,content,new Vector2(335+(i%2)*670,-105-(i/2)*210),new Vector2(640,195));card.anchorMin=card.anchorMax=new Vector2(0,1);card.gameObject.AddComponent<Image>().color=new Color(.06f,.12f,.17f,1);
   var row=new HexShopRow{offer=offers[i]};row.icon=Rect("Icon",card,new Vector2(-265,48),new Vector2(66,66)).gameObject.AddComponent<Image>();row.icon.sprite=offers[i].picture;row.icon.preserveAspect=true;row.icon.raycastTarget=false;
   row.title=Label("Name",card,new Vector2(35,65),new Vector2(510,38),20);row.title.text=strings.Resolve(offers[i].nameKey,DisplayLanguage.Korean);
   row.description=Label("Description",card,new Vector2(15,1),new Vector2(595,84),16);row.description.text=offers[i].Description;
   row.price=Label("Price",card,new Vector2(-220,-66),new Vector2(140,34),19);row.price.text=LocalizationService.Text("shop.price","price",offers[i].price);
   row.buy=Button("Buy",card,new Vector2(20,-66),new Vector2(170,38),"shop.buy");row.use=Button("Use",card,new Vector2(215,-66),new Vector2(170,38),"shop.use");row.use.gameObject.SetActive(offers[i].kind==HexShopKind.Consumable);shop.rows[i]=row;
  }
  if(shop.quickUseButton)UnityEngine.Object.DestroyImmediate(shop.quickUseButton.gameObject);shop.quickUseButton=null;shop.quickUseLabel=null;
  if(shop.quickSlotPanel)UnityEngine.Object.DestroyImmediate(shop.quickSlotPanel);
  var bar=Rect("Consumable Quick Slots",shop.transform,new Vector2(0,-245),new Vector2(920,120));shop.quickSlotPanel=bar.gameObject;
  var consumables=offers.Where(x=>x.kind==HexShopKind.Consumable).ToArray();shop.quickSlots=new HexQuickSlot[consumables.Length];
  for(int i=0;i<consumables.Length;i++)
  {
   var cell=Rect("Quick Slot "+i,bar,new Vector2((i-(consumables.Length-1)*.5f)*184,0),new Vector2(176,114));var bg=cell.gameObject.AddComponent<Image>();bg.color=new Color(.05f,.16f,.2f,.96f);
   var slot=new HexQuickSlot{offer=consumables[i],button=cell.gameObject.AddComponent<Button>()};slot.button.targetGraphic=bg;slot.button.transition=UnityEngine.UI.Selectable.Transition.None;
   slot.icon=Rect("Icon",cell,new Vector2(-54,22),new Vector2(48,48)).gameObject.AddComponent<Image>();slot.icon.sprite=slot.offer.picture;slot.icon.preserveAspect=true;slot.icon.raycastTarget=false;
   slot.title=Label("Title",cell,new Vector2(28,22),new Vector2(105,56),14);slot.title.text=strings.Resolve(slot.offer.key=="PotionOffer"?"shop.potion.short":slot.offer.nameKey,DisplayLanguage.Korean);
   slot.count=Label("Count",cell,new Vector2(0,-19),new Vector2(166,24),16);slot.count.text=strings.Resolve("shop.slot.count",DisplayLanguage.Korean).Replace("{count}","0");
   slot.status=Label("Status",cell,new Vector2(0,-43),new Vector2(166,22),12);slot.status.text=strings.Resolve("shop.slot.empty",DisplayLanguage.Korean);shop.quickSlots[i]=slot;
  }
  shop.buffLabel.rectTransform.anchoredPosition=new Vector2(600,151);shop.buffLabel.rectTransform.sizeDelta=new Vector2(360,35);shop.buffLabel.fontSize=13;
  shop.GetComponent<PrototypeFontBinding>().Apply();
  var game=shop.game;shop.game=null;PrefabUtility.SaveAsPrefabAsset(shop.gameObject,Root+"/Prefabs/ShopUI.prefab");shop.game=game;
  foreach(string name in new[]{"PrototypeTestSettings","PrototypeTestSettings_Baseline"}){var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(Root+"/Data/"+name+".asset");settings.shopOffers=offers;EditorUtility.SetDirty(settings);}
  EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_CRISIS_SHOP_AUTHORED: four crisis products, icons, saved scroll shop and five quick slots.");
 }
 static void Add(string key,string en,string ko){var entry=strings.entries.Find(x=>x.key==key);if(entry==null){entry=new TranslationEntry{key=key};strings.entries.Add(entry);}entry.english=en;entry.korean=ko;}
 static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;}
 static Text Label(string name,Transform parent,Vector2 position,Vector2 size,int pixels){var label=Rect(name,parent,position,size).gameObject.AddComponent<Text>();label.font=font;label.fontSize=pixels;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;}
 static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string key){var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.1f,.45f,.4f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var text=Label("Label",r,Vector2.zero,size,17);text.text=strings.Resolve(key,DisplayLanguage.Korean);text.gameObject.AddComponent<LocalizedLabel>().key=key;return b;}
}

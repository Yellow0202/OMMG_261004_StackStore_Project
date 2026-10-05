using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class HexShopExpansionAuthoring
{
 const string Root="Assets/Prototypes/HexWorld";static Font font;static LocalizationTable table;
 [MenuItem("Stack Store/Hex World/Upgrade Walls Facilities And Floors")]
 public static void Upgrade()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before authoring.");font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
  Add("hex.help.Walls","PAUSED: select an owned tile, then toggle one of its six sides.","일시정지 · 소유 타일을 선택하고 여섯 방향의 벽을 편집하세요.");
  Add("shop.wall.mode","EDIT WALLS","벽 편집");Add("shop.wall.select","Select an owned tile","소유 타일을 선택하세요");Add("shop.wall.selected","Selected tile ({x}, {y})","선택 타일 ({x}, {y})");Add("shop.wall.changed","Wall updated.","벽을 변경했습니다.");Add("shop.wall.blocked","Keep a route to the outside for every owned tile.","모든 소유 타일에 외부와 연결된 통로가 필요합니다.");
  string[] directions={"RIGHT","FRONT RIGHT","FRONT LEFT","LEFT","BACK LEFT","BACK RIGHT"};string[] directionNames={"오른쪽","앞 오른쪽","앞 왼쪽","왼쪽","뒤 왼쪽","뒤 오른쪽"};for(int d=0;d<6;d++)Add("shop.wall.side."+d,directions[d],directionNames[d]);Add("shop.wall.side.label","{direction} / {action}","{direction} / {action}");Add("shop.wall.demolish","DEMOLISH","허물기");Add("shop.wall.build","BUILD","짓기");
  Add("shop.floor","Floor {floor}","{floor}층");Add("shop.floor.previous","LOWER FLOOR","아래층");Add("shop.floor.next","UPPER FLOOR","위층");Add("shop.kitchen.empty","No employees assigned","배치된 직원이 없습니다");Add("shop.capacity","{count} / {capacity} occupied","이용 {count} / {capacity}명");Add("shop.tile.label","Tile information","타일 안내");
  var definitions=new System.Collections.Generic.List<HexTileDefinition>{AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/DisplayShelfTile.asset")};definitions[0].worldSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/ShelfFront.png");EditorUtility.SetDirty(definitions[0]);
  string[] names={"Table","Kitchen","Storage","Stairs","Lodging","Entrance"};string[] korean={"테이블 타일","주방 타일","창고 타일","계단 타일","숙박 타일","출입구 타일"};
  for(int i=0;i<names.Length;i++)
  {
   string key="shop.tile."+names[i].ToLowerInvariant();Add(key,names[i]+" tile",korean[i]);Add(key+".description",names[i]+" facility tile.",korean[i]+"을 배치합니다.");
   var color=Color.HSVToRGB((i+.6f)/7,.45f,.9f);string art="Facility"+names[i];Paint(art,32,32,(x,y)=>
   {
    Color c=Color.clear;if(x>=5&&x<27&&y>=4&&y<23)c=color;
    if(i==0){c=Color.clear;if(x>=4&&x<=28&&y>=12&&y<=20)c=new Color(.72f,.45f,.24f);if((x>=7&&x<=10||x>=22&&x<=25)&&y>=3&&y<12)c=new Color(.5f,.28f,.12f);}
    if(i==1&&x>=8&&x<=23&&y>=16&&y<=24)c=new Color(.75f,.78f,.8f);
    if(i==2&&(x==10||x==21||y==10||y==17)&&x>=5&&x<27&&y>=4&&y<23)c=new Color(.4f,.25f,.12f);
    if(i==3){c=Color.clear;for(int step=0;step<4;step++)if(x>=4+step*5&&x<29&&y>=3+step*5&&y<8+step*5)c=color;}
    if(i==4&&x>=6&&x<26&&y>=13&&y<20)c=Color.white;
    if(i==5){c=Color.clear;if((x>=4&&x<9||x>=23&&x<28)&&y>=3&&y<27||x>=4&&x<28&&y>=23&&y<28)c=color;}
    return c;
   },true);
   string path=Root+"/Data/"+names[i]+"Tile.asset";var definition=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(path);if(!definition){definition=ScriptableObject.CreateInstance<HexTileDefinition>();AssetDatabase.CreateAsset(definition,path);}definition.kind=(HexTileKind)(i+1);definition.key=names[i].ToLowerInvariant();definition.nameKey=key;definition.descriptionKey=key+".description";definition.color=color;definition.icon=definition.worldSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/"+art+".png");EditorUtility.SetDirty(definition);definitions.Add(definition);
  }
  Paint("SeatedCustomer",32,32,(x,y)=>{Color c=Color.clear;if(x>=11&&x<=21&&y>=22&&y<=31)c=new Color(1,.88f,.65f);if(x>=8&&x<=24&&y>=10&&y<22)c=Color.white;if(x>=7&&x<=25&&y>=5&&y<=10)c=new Color(.35f,.43f,.5f);if(x>=22&&x<=27&&y>=2&&y<=5)c=new Color(.27f,.30f,.35f);return c;},true,24);var actorPrefab=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/WorldCustomer.prefab");actorPrefab.GetComponent<HexWorldActor>().seatedFrame=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/SeatedCustomer.png");PrefabUtility.SaveAsPrefabAsset(actorPrefab,Root+"/Prefabs/WorldCustomer.prefab");PrefabUtility.UnloadPrefabContents(actorPrefab);
  Paint("BoundaryWall",32,16,(x,y)=>((y==4||y==10)||(x+(y<8?0:8))%16==0)?new Color(.30f,.40f,.46f):new Color(.64f,.73f,.75f),false);table.Rebuild();EditorUtility.SetDirty(table);
  var tile=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/HexTile.prefab");ConfigureTile(tile.GetComponent<HexTileView>());PrefabUtility.SaveAsPrefabAsset(tile,Root+"/Prefabs/HexTile.prefab");PrefabUtility.UnloadPrefabContents(tile);
  var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();var board=game.board;board.tileTypes=definitions.ToArray();board.startingShop=GameObject.Find("Starting Shop - fixed");
  foreach(var actor in UnityEngine.Object.FindObjectsByType<HexWorldActor>(FindObjectsSortMode.None))if(actor.transform.IsChildOf(game.transform)&&!actor.transform.IsChildOf(game.customerRoot))board.player=actor.gameObject;
  ConfigureTile(board.preview);foreach(var wall in board.preview.walls)wall.Show(false);board.preview.labelCanvas.gameObject.SetActive(false);
  game.tileDropdown.ClearOptions();var options=new System.Collections.Generic.List<string>();foreach(var type in definitions)options.Add(table.Resolve(type.nameKey,DisplayLanguage.Korean));game.tileDropdown.AddOptions(options);StyleDropdown(game.tileDropdown);
  var canvas=game.buildButton.GetComponentInParent<Canvas>();string uiPath=Root+"/Prefabs/ShopBuildingUI.prefab";var saved=AssetDatabase.LoadAssetAtPath<GameObject>(uiPath);
  if(!saved){var panel=BuildUI(canvas.transform);var component=panel.GetComponent<HexShopBuildingUI>();component.board=null;PrefabUtility.SaveAsPrefabAsset(panel,uiPath);UnityEngine.Object.DestroyImmediate(panel);saved=AssetDatabase.LoadAssetAtPath<GameObject>(uiPath);}
  var existing=UnityEngine.Object.FindFirstObjectByType<HexShopBuildingUI>();if(!existing){var ui=(GameObject)PrefabUtility.InstantiatePrefab(saved,canvas.transform);ui.GetComponent<HexShopBuildingUI>().board=board;ui.GetComponent<PrototypeFontBinding>().Apply();}
  var prefab=PrefabUtility.LoadPrefabContents(uiPath);StyleDropdown(prefab.GetComponent<HexShopBuildingUI>().floorDropdown);PrefabUtility.SaveAsPrefabAsset(prefab,uiPath);PrefabUtility.UnloadPrefabContents(prefab);
  var controls=UnityEngine.Object.FindFirstObjectByType<HexShopBuildingUI>();StyleDropdown(controls.floorDropdown);controls.transform.SetSiblingIndex(game.choicePanel.transform.GetSiblingIndex());
  var sceneFonts=game.GetComponent<PrototypeFontBinding>();if(!sceneFonts)sceneFonts=game.gameObject.AddComponent<PrototypeFontBinding>();sceneFonts.Apply();EditorUtility.SetDirty(board);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_SHOP_EXPANSION_AUTHORED");
 }
 static void Add(string key,string english,string korean){var entry=table.entries.Find(e=>e.key==key);if(entry==null)table.entries.Add(new TranslationEntry{key=key,english=english,korean=korean});else{entry.english=english;entry.korean=korean;}}
 static void StyleDropdown(Dropdown dropdown){dropdown.captionText.fontSize=18;dropdown.captionText.color=new Color(.12f,.18f,.2f);dropdown.captionText.horizontalOverflow=HorizontalWrapMode.Overflow;dropdown.captionText.verticalOverflow=VerticalWrapMode.Overflow;dropdown.itemText.fontSize=18;dropdown.itemText.color=dropdown.captionText.color;dropdown.RefreshShownValue();}
 static void Paint(string name,int width,int height,Func<int,int,Color> pixel,bool bottom,int ppu=16)
 {
  string path=Root+"/Art/"+name+".png";var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);for(int y=0;y<height;y++)for(int x=0;x<width;x++)texture.SetPixel(x,y,pixel(x,y));texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,bottom?0:.5f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
 }
 static void ConfigureTile(HexTileView tile)
 {
  if(tile.walls==null||tile.walls.Length!=6)
  {
   tile.walls=new HexWallView[6];for(int d=0;d<6;d++)
   {
    Vector3 normal=HexBoardModel.World(HexBoardModel.Directions[d]);var wall=new GameObject("Wall Side "+(d+1));wall.layer=2;wall.transform.SetParent(tile.transform,false);wall.transform.localPosition=normal*.5f+Vector3.up*.68f;wall.transform.localRotation=Quaternion.Euler(0,Mathf.Atan2(normal.x,normal.z)*Mathf.Rad2Deg,0);wall.transform.localScale=new Vector3(.70f,.9f,1);
    var image=wall.AddComponent<SpriteRenderer>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/BoundaryWall.png");image.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/CharacterSprite.mat");var collider=wall.AddComponent<BoxCollider>();collider.size=new Vector3(2,1,.12f);var view=wall.AddComponent<HexWallView>();view.image=image;view.obstacle=collider;tile.walls[d]=view;wall.SetActive(false);
   }
  }
  if(!tile.labelCanvas)
  {
   var label=new GameObject("Facility Label",typeof(RectTransform));label.transform.SetParent(tile.transform,false);label.transform.localPosition=new Vector3(0,2.3f,0);label.transform.localScale=Vector3.one*.012f;tile.labelCanvas=label.AddComponent<Canvas>();tile.labelCanvas.renderMode=RenderMode.WorldSpace;tile.labelCanvas.sortingOrder=10;
   label.GetComponent<RectTransform>().sizeDelta=new Vector2(240,90);tile.label=Text("Information",label.transform,Vector2.zero,new Vector2(240,90),"shop.tile.label",16);label.AddComponent<PrototypeFontBinding>().Apply();label.SetActive(false);
  }
 }
 static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size)
 {
  var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;return go;
 }
 static Text Text(string name,Transform parent,Vector2 pos,Vector2 size,string key,int fontSize=18)
 {
  var go=Rect(name,parent,pos,size);var text=go.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.text=table.Resolve(key,DisplayLanguage.Korean);return text;
 }
 static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string key)
 {
  var go=Rect(name,parent,pos,size);var image=go.AddComponent<Image>();image.color=new Color(.1f,.45f,.43f,.98f);var button=go.AddComponent<Button>();button.targetGraphic=image;var label=Text("Label",go.transform,Vector2.zero,size,key);label.gameObject.AddComponent<LocalizedLabel>().key=key;return button;
 }
 static GameObject BuildUI(Transform parent)
 {
  var root=Rect("Shop Building UGUI",parent,Vector2.zero,new Vector2(1600,900));var rootRect=root.GetComponent<RectTransform>();rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;var ui=root.AddComponent<HexShopBuildingUI>();
  var wall=Rect("Wall Controls",root.transform,new Vector2(20,-120),new Vector2(250,355));wall.AddComponent<Image>().color=new Color(.03f,.08f,.11f,.94f);ui.wallMode=Button("Wall Mode",wall.transform,new Vector2(10,-10),new Vector2(230,40),"shop.wall.mode");ui.selection=Text("Selected Tile",wall.transform,new Vector2(10,-55),new Vector2(230,45),"shop.wall.select");ui.sides=new Button[6];for(int d=0;d<6;d++)ui.sides[d]=Button("Toggle Side "+d,wall.transform,new Vector2(10,-105-d*39),new Vector2(230,35),"shop.wall.side."+d);
  var floor=Rect("Floor Controls",root.transform,new Vector2(1320,-340),new Vector2(250,180));floor.GetComponent<RectTransform>().anchorMin=floor.GetComponent<RectTransform>().anchorMax=new Vector2(1,1);floor.GetComponent<RectTransform>().anchoredPosition=new Vector2(-280,-340);floor.AddComponent<Image>().color=new Color(.03f,.08f,.11f,.94f);
  ui.previousFloor=Button("Previous Floor",floor.transform,new Vector2(10,-10),new Vector2(230,40),"shop.floor.previous");ui.nextFloor=Button("Next Floor",floor.transform,new Vector2(10,-120),new Vector2(230,40),"shop.floor.next");
  var original=UnityEngine.Object.FindFirstObjectByType<HexPrototype>().tileDropdown;ui.floorDropdown=UnityEngine.Object.Instantiate(original,floor.transform);ui.floorDropdown.onValueChanged=new Dropdown.DropdownEvent();var rect=ui.floorDropdown.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(10,-65);rect.sizeDelta=new Vector2(230,40);ui.floorDropdown.ClearOptions();ui.floorDropdown.AddOptions(new System.Collections.Generic.List<string>{table.Resolve("shop.floor",DisplayLanguage.Korean).Replace("{floor}","1")});
  root.AddComponent<PrototypeFontBinding>().Apply();return root;
 }
}

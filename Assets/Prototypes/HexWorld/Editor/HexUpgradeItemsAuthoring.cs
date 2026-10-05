using System.IO;
using UnityEditor;
using UnityEngine;
public static class HexUpgradeItemsAuthoring
{
 const string Root="Assets/Prototypes/HexWorld";
 [MenuItem("Stack Store/Hex World/Add Food And Attraction Upgrades")]
 public static void Upgrade()
 {
  var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
  Add(table,"item.ability_extra_servings.name","Generous Portions","푸짐한 한 상");
  Add(table,"item.ability_extra_servings.description","Throw +{FoodThrowCount} food per level to different guests in contact order. Max level 3 (4 food total).","레벨당 투척 음식 +{FoodThrowCount}개. 접촉 순서대로 서로 다른 손님에게 배부합니다. 최대 3레벨 (총 4개).");
  Add(table,"item.ability_attention_call.name","Street-Side Invitation","거리의 초대");
  Add(table,"item.ability_attention_call.description","Increase the chance of attracting passing guests by {VisitorChance} percentage points per level, up to 100%.","레벨당 외곽 손님의 관심을 끌 확률 +{VisitorChance}%p. 최대 확률은 100%입니다.");
  Make("ability_extra_servings",3,ItemEffectKind.FoodThrowCount,1,"ExtraServings",false);
  Make("ability_attention_call",0,ItemEffectKind.VisitorChance,.1f,"AttentionCall",true);
  var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root+"/Data/HexCatalog.asset");
  foreach(var key in new[]{"ability_extra_servings","ability_attention_call"}){var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/"+key+".asset");if(!catalog.items.Contains(item))catalog.items.Add(item);}
  table.Rebuild();EditorUtility.SetDirty(table);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("HEX_UPGRADE_ITEMS_AUTHORED");
 }
 static void Add(LocalizationTable table,string key,string english,string korean){var entry=table.entries.Find(e=>e.key==key);if(entry==null)table.entries.Add(new TranslationEntry{key=key,english=english,korean=korean});}
 static void Make(string key,int maximum,ItemEffectKind effect,float amount,string art,bool attraction)
 {
  string path=Root+"/Data/"+key+".asset";if(AssetDatabase.LoadAssetAtPath<ItemDefinition>(path))return;
  string image=Root+"/Art/"+art+".png";var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);
  for(int y=0;y<32;y++)for(int x=0;x<32;x++)
  {
   Color color=Color.clear;
   if(attraction){if(x>=6&&x<=22&&Mathf.Abs(y-18)<=2+(x-6)/3)color=new Color(1,.77f,.22f);if(x>=9&&x<=13&&y>=5&&y<=13)color=new Color(.25f,.72f,.65f);if(x>=25&&x<=27&&(y==12||y==18||y==24))color=Color.white;}
   else for(int bowl=0;bowl<3;bowl++){int cx=8+bowl*8,cy=12+(bowl%2)*7;if(Mathf.Abs(x-cx)<=5&&y>=cy-4&&y<=cy)color=new Color(.95f,.58f,.2f);if(Mathf.Abs(x-cx)<=5&&y==cy+1)color=new Color(1,.87f,.55f);if(x==cx&&y>=cy+4&&y<=cy+6)color=Color.white;}
   texture.SetPixel(x,y,color);
  }
  texture.Apply();File.WriteAllBytes(image,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(image);
  var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var item=ScriptableObject.CreateInstance<ItemDefinition>();item.key=key;item.nameKey="item."+key+".name";item.descriptionKey="item."+key+".description";item.kind=ItemKind.Ability;item.maxLevel=maximum;item.picture=AssetDatabase.LoadAssetAtPath<Sprite>(image);item.effects.Add(new ItemEffect{kind=effect,amountPerLevel=amount});AssetDatabase.CreateAsset(item,path);
 }
}

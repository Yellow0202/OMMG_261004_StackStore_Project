using System;
using UnityEditor;
using UnityEngine;

public static class HexMoreItemsAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static ItemEffect Effect(ItemEffectKind kind,float amount)=>new ItemEffect{kind=kind,amountPerLevel=amount};
    public static readonly string[] Keys={"food_golden_stew","food_herbal_tea","food_pepper_skewer","ability_courier_boots","ability_silver_cutlery","ability_guest_ledger"};
    [MenuItem("Stack Store/Hex World/Add Tavern Upgrade Items")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
        if(!AssetDatabase.IsValidFolder(Root+"/Data/Items"))AssetDatabase.CreateFolder(Root+"/Data","Items");
        Make(Keys[0],"GoldenStew",ItemKind.Food,3,"Golden Stew","황금 스튜","Earn +{ServiceGoldBonus} gold for each meal served per level. Applies to table and standing guests, excluding lodging. Max level {MaxLevel}.","레벨당 음식 접객 수익 +{ServiceGoldBonus}골드. 테이블·외곽 손님에게 적용되며 숙박 수익은 제외됩니다. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.ServiceGoldBonus,1));
        Make(Keys[1],"HerbalTea",ItemKind.Food,4,"Mint Herbal Tea","박하 허브차","Seated table guests wait {TablePatienceSeconds} more seconds per level. Applies to newly seated guests. Max level {MaxLevel}.","레벨당 테이블 손님의 주문 대기 시간 +{TablePatienceSeconds}초. 새로 착석한 손님부터 적용됩니다. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.TablePatienceSeconds,5));
        Make(Keys[2],"PepperSkewer",ItemKind.Food,5,"Pepper Skewers","매콤 꼬치","Per level: cooking speed +{ServiceSpeedPercent}% and player delivery movement speed +{DeliverySpeedPercent}%. Max level {MaxLevel}.","레벨당 조리 속도 +{ServiceSpeedPercent}%, 플레이어 배달 이동 속도 +{DeliverySpeedPercent}%. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.ServiceSpeedPercent,.08f),Effect(ItemEffectKind.DeliverySpeedPercent,.05f));
        Make(Keys[3],"CourierBoots",ItemKind.Ability,5,"Courier Boots","배달꾼의 장화","Player delivery and return movement speed +{DeliverySpeedPercent}% per level. Does not affect customers or employees. Max level {MaxLevel}.","레벨당 플레이어 배달·복귀 이동 속도 +{DeliverySpeedPercent}%. 손님과 직원의 이동 속도에는 영향을 주지 않습니다. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.DeliverySpeedPercent,.15f));
        Make(Keys[4],"SilverCutlery",ItemKind.Ability,3,"Silver Cutlery","은빛 식기","Earn +{TableGoldBonus} gold per seated table guest served, per level. Standing and lodging guests are excluded. Max level {MaxLevel}.","레벨당 착석한 테이블 손님 접객 수익 +{TableGoldBonus}골드. 외곽 손님과 숙박 수익은 제외됩니다. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.TableGoldBonus,1));
        Make(Keys[5],"GuestLedger",ItemKind.Ability,5,"Regulars' Ledger","단골 장부","Per level: visitor interest +{VisitorChance} percentage points and order patience +{CustomerPatienceSeconds} second for new guests. Max level {MaxLevel}.","레벨당 방문 관심 확률 +{VisitorChance}%p, 새 손님의 주문 대기 시간 +{CustomerPatienceSeconds}초. 최대 {MaxLevel}레벨.",Effect(ItemEffectKind.VisitorChance,.04f),Effect(ItemEffectKind.CustomerPatienceSeconds,1));
        AssetDatabase.SaveAssets();Validate();Debug.Log("HEX_MORE_ITEMS_AUTHORED");
    }
    static void Make(string key,string art,ItemKind kind,int maximum,string enName,string koName,string enDescription,string koDescription,params ItemEffect[] effects)
    {
        string image=Root+"/Art/Items/"+art+".png";AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        string path=Root+"/Data/Items/"+key+".asset";var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        if(!item)
        {
            item=ScriptableObject.CreateInstance<ItemDefinition>();item.key=key;item.kind=kind;item.maxLevel=maximum;item.effects.AddRange(effects);AssetDatabase.CreateAsset(item,path);
        }
        item.nameKey="item."+key+".name";item.descriptionKey="item."+key+".description";item.picture=AssetDatabase.LoadAssetAtPath<Sprite>(image);EditorUtility.SetDirty(item);
        var strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        Add(strings,item.nameKey,enName,koName);Add(strings,item.descriptionKey,enDescription,koDescription);strings.Rebuild();EditorUtility.SetDirty(strings);
        var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root+"/Data/HexCatalog.asset");if(!catalog.items.Contains(item))catalog.items.Add(item);EditorUtility.SetDirty(catalog);
    }
    static void Add(LocalizationTable table,string key,string en,string ko)
    {
        if(!table.entries.Exists(e=>e.key==key))table.entries.Add(new TranslationEntry{key=key,english=en,korean=ko});
    }
    public static void Validate()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root+"/Data/HexCatalog.asset");
        var strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");
        var unique=new System.Collections.Generic.HashSet<string>();foreach(var item in catalog.items)if(!unique.Add(item.key))throw new Exception("Duplicate item key "+item.key);
        foreach(string key in Keys)
        {
            var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/Items/"+key+".asset");
            if(!item||!item.picture||!catalog.items.Contains(item)||item.maxLevel<1||item.effects.Count<1)throw new Exception("Incomplete new item "+key);
            foreach(var language in new[]{DisplayLanguage.English,DisplayLanguage.Korean})
                if(strings.Resolve(item.nameKey,language).StartsWith("[")||strings.Resolve(item.descriptionKey,language).StartsWith("["))throw new Exception("Missing translation "+key);
        }
        Debug.Log("HEX_MORE_ITEMS_VALIDATED: six icons, unique catalog keys, effect data, level limits and English/Korean strings.");
    }
}

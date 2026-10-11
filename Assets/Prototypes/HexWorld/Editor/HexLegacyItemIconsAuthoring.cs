using System;
using UnityEditor;
using UnityEngine;

public static class HexLegacyItemIconsAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Names={"WarmSoup","QuickService","ExtraServings","AttentionCall","TileReward","ServicePotion"};
    static readonly string[] Keys={"food_warm_soup","ability_quick_service","ability_extra_servings","ability_attention_call","part_display_shelf"};
    [MenuItem("Stack Store/Hex World/Import Existing Item Icons")]
    public static void Run()
    {
        for(int i=0;i<Names.Length;i++)
        {
            string path=Root+"/Art/LegacyItemIcons/"+Names[i]+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(i<Keys.Length){var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/"+Keys[i]+".asset");item.picture=icon;EditorUtility.SetDirty(item);}
            else {var potion=AssetDatabase.LoadAssetAtPath<HexShopOffer>(Root+"/Data/PotionOffer.asset");potion.picture=icon;EditorUtility.SetDirty(potion);}
        }
        foreach(string guid in AssetDatabase.FindAssets("t:HexShopOffer",new[]{Root+"/Data"}))
        {
            var offer=AssetDatabase.LoadAssetAtPath<HexShopOffer>(AssetDatabase.GUIDToAssetPath(guid));
            if(offer.kind==HexShopKind.Upgrade&&offer.upgrade){offer.picture=offer.upgrade.picture;EditorUtility.SetDirty(offer);}
        }
        AssetDatabase.SaveAssets();Validate();
    }
    public static void Validate()
    {
        var unique=new System.Collections.Generic.HashSet<Sprite>();
        for(int i=0;i<Names.Length;i++)
        {
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/LegacyItemIcons/"+Names[i]+".png");
            if(!icon||!unique.Add(icon)||icon.texture.width!=64||icon.texture.height!=64)throw new Exception("Invalid existing item icon "+Names[i]);
            var actual=i<Keys.Length?AssetDatabase.LoadAssetAtPath<ItemDefinition>(Root+"/Data/"+Keys[i]+".asset").picture:AssetDatabase.LoadAssetAtPath<HexShopOffer>(Root+"/Data/PotionOffer.asset").picture;
            if(actual!=icon)throw new Exception("Disconnected icon "+Names[i]);
        }
        var catalog=AssetDatabase.LoadAssetAtPath<ItemCatalog>(Root+"/Data/HexCatalog.asset");
        foreach(var item in catalog.items)if(!item||!item.picture)throw new Exception("Missing catalog icon");
        var service=AssetDatabase.LoadAssetAtPath<HexShopOffer>(Root+"/Data/ServiceOffer.asset");
        if(service.picture!=service.upgrade.picture)throw new Exception("Shop upgrade icon mismatch");
        Debug.Log("HEX_LEGACY_ITEM_ICONS_VALIDATED: five original rewards, consumable and shop upgrade connected; entire item catalog has icons.");
    }
}

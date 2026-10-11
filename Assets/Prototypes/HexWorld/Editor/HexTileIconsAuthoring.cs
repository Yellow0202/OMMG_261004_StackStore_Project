using System;
using UnityEditor;
using UnityEngine;

public static class HexTileIconsAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static readonly string[] Names={"DisplayShelf","Table","Kitchen","Storage","Stairs","Lodging","Entrance"};
    [MenuItem("Stack Store/Hex World/Import Tile Icons")]
    public static void Run()
    {
        foreach(string name in Names)
        {
            string path=Root+"/Art/TileIcons/"+name+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit=64;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var tile=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/"+name+"Tile.asset");
            tile.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);EditorUtility.SetDirty(tile);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:HexShopOffer",new[]{Root+"/Data"}))
        {
            var offer=AssetDatabase.LoadAssetAtPath<HexShopOffer>(AssetDatabase.GUIDToAssetPath(guid));
            if(offer.kind==HexShopKind.Tile&&offer.tile){offer.picture=offer.tile.icon;EditorUtility.SetDirty(offer);}
        }
        AssetDatabase.SaveAssets();Validate();
    }
    public static void Validate()
    {
        var icons=new System.Collections.Generic.HashSet<Sprite>();
        foreach(string name in Names)
        {
            var tile=AssetDatabase.LoadAssetAtPath<HexTileDefinition>(Root+"/Data/"+name+"Tile.asset");
            if(!tile||!tile.icon||!icons.Add(tile.icon)||tile.icon.texture.width!=64||tile.icon.texture.height!=64)throw new Exception("Invalid tile icon: "+name);
            if(!tile.worldSprite||!tile.interiorPrefab)throw new Exception("Missing existing world art: "+name);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:HexShopOffer",new[]{Root+"/Data"}))
        {
            var offer=AssetDatabase.LoadAssetAtPath<HexShopOffer>(AssetDatabase.GUIDToAssetPath(guid));
            if(offer.kind==HexShopKind.Tile&&offer.tile&&offer.picture!=offer.tile.icon)throw new Exception("Shop icon mismatch");
        }
        Debug.Log("HEX_TILE_ICONS_VALIDATED: seven distinct 64x64 icons, tile reward references, shop references and preserved world art.");
    }
}

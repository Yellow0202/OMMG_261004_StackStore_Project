using UnityEngine;

public enum HexShopKind { Tile, Upgrade, Consumable }
[CreateAssetMenu(menuName="Stack Store/Shop Offer")]
public sealed class HexShopOffer : ScriptableObject
{
    public string key, nameKey, descriptionKey;
    public Sprite picture;
    public HexShopKind kind;
    [Min(1)] public int price = 3;
    public HexTileDefinition tile;
    public ItemDefinition upgrade;
    [Min(1)] public int quantity = 1;
    [Min(.1f)] public float buffSeconds = 20;
    [Min(.01f)] public float speedBonus = 1;
    public bool IsValid => price>0 && quantity>0 && (kind==HexShopKind.Tile ? tile!=null : kind==HexShopKind.Upgrade ? upgrade!=null : buffSeconds>0&&speedBonus>0);
    public string Description => LocalizationService.Text(descriptionKey,"count",quantity,"seconds",buffSeconds,"speed",speedBonus*100,"effect",upgrade?upgrade.DisplayDescription():"");
}

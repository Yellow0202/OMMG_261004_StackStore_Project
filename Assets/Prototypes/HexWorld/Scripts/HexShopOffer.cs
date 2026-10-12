using UnityEngine;

public enum HexConsumableEffect { ServiceSpeed, Patience, DeliverySpeed, ReadyMeals }
public enum HexShopKind { Tile, Upgrade, Consumable }
[CreateAssetMenu(menuName="Stack Store/Shop Offer")]
public sealed class HexShopOffer : ScriptableObject
{
    public string key, nameKey, descriptionKey;
    public Sprite picture;
    public HexShopKind kind;
    public HexConsumableEffect effect;
    [Min(1)] public int targetCount=4, readyMeals=2;
    [Min(.1f)] public float patienceRecovery=5;
    [Range(0,1)] public float repeatedSnackMultiplier=.5f;
    [Min(1)] public int price = 3;
    public HexTileDefinition tile;
    public ItemDefinition upgrade;
    [Min(1)] public int quantity = 1;
    [Min(.1f)] public float buffSeconds = 20;
    [Min(.01f)] public float speedBonus = 1;
    public bool IsValid => price>0 && quantity>0 && (kind==HexShopKind.Tile ? tile!=null : kind==HexShopKind.Upgrade ? upgrade!=null : effect==HexConsumableEffect.Patience ? patienceRecovery>0&&targetCount>0 : effect==HexConsumableEffect.ReadyMeals ? readyMeals>0 : buffSeconds>0&&speedBonus>0);
    public string Description => LocalizationService.Text(descriptionKey,"count",quantity,"seconds",buffSeconds,"speed",speedBonus*100,"targets",targetCount,"patience",patienceRecovery,"meals",readyMeals,"effect",upgrade?upgrade.DisplayDescription():"");
}

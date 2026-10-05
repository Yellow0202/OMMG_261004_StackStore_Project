using UnityEngine;
public enum HexTileKind { DisplayShelf, Table, Kitchen, Storage, Stairs, Lodging, Entrance }

[CreateAssetMenu(menuName = "Stack Store/Hex Tile Definition")]
public sealed class HexTileDefinition : ScriptableObject
{
    public string key = "display_shelf";
    public string nameKey = "hex.tile.shelf";
    public string descriptionKey = "hex.tile.description";
    public Sprite icon;
    public Color color = new Color(.85f, .55f, .23f);
    public HexTileKind kind;
    public Sprite worldSprite;
    public int minimumSeats=2, maximumSeats=4;
    public float waitingSeconds=20, lodgingSeconds=30;
    public int serviceGold=2, lodgingGold=6;
}

using UnityEngine;

[CreateAssetMenu(menuName = "Stack Store/Hex Tile Definition")]
public sealed class HexTileDefinition : ScriptableObject
{
    public string key = "display_shelf";
    public string nameKey = "hex.tile.shelf";
    public string descriptionKey = "hex.tile.description";
    public Sprite icon;
    public Color color = new Color(.85f, .55f, .23f);
}

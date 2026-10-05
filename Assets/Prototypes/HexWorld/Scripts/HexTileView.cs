using UnityEngine;

public sealed class HexTileView : MonoBehaviour
{
    public Vector2Int coordinate;
    public MeshRenderer surface;
    public GameObject furniture;
    public SpriteRenderer buildingSprite;
    public LineRenderer outline;
    public void Show(bool owned, HexTileDefinition definition, Color? highlight = null)
    {
        Color color = highlight ?? (owned ? (definition ? definition.color : new Color(.24f,.66f,.58f)) : new Color(.20f,.27f,.33f));
        surface.enabled = false;
        if (outline) { outline.sortingOrder = -32000; outline.startColor = outline.endColor = color; }
        furniture.SetActive(owned && coordinate != Vector2Int.zero);
        if (buildingSprite) buildingSprite.color = highlight.HasValue && highlight.Value.a < .99f ? highlight.Value : Color.white;
    }
}

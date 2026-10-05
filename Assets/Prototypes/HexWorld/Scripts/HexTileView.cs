using UnityEngine;

public sealed class HexTileView : MonoBehaviour
{
    public Vector2Int coordinate;
    public MeshRenderer surface;
    public GameObject furniture;
    MaterialPropertyBlock block;
    public void Show(bool owned, HexTileDefinition definition, Color? highlight = null)
    {
        if (block == null) block = new MaterialPropertyBlock();
        Color color = highlight ?? (owned ? (definition ? definition.color : new Color(.24f,.66f,.58f)) : new Color(.20f,.27f,.33f));
        block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
        surface.SetPropertyBlock(block);
        furniture.SetActive(owned && coordinate != Vector2Int.zero);
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class HexTileView : MonoBehaviour
{
    public Vector2Int coordinate;
    public MeshRenderer surface;
    public GameObject furniture;
    public SpriteRenderer buildingSprite;
    public LineRenderer outline;
    public HexWallView[] walls;
    public Canvas labelCanvas;
    public Text label;
    HexShopLayout layout;
    int floor;
    bool owned, kitchenOccupied;
    bool shownOwned,ghostFloor;
    HexTileDefinition shownDefinition;
    MaterialPropertyBlock floorProperties;
    public void ApplyFloor(HexEnvironmentTheme theme,bool isOwned,HexTileDefinition definition)
    {
        var material=theme?theme.Floor(definition?definition.kind:HexTileKind.DisplayShelf):null;
        surface.enabled=isOwned&&material&&theme.floorMesh;
        if(!surface.enabled)return;
        surface.sharedMaterial=material;
        surface.GetComponent<MeshFilter>().sharedMesh=theme.floorMesh;
        if(floorProperties==null)floorProperties=new MaterialPropertyBlock();
        floorProperties.Clear();floorProperties.SetFloat("_Height",theme.floorHeight);surface.SetPropertyBlock(floorProperties);
    }
    public void Show(bool owned, HexTileDefinition definition, Color? highlight = null)
    {
        Color color = highlight ?? (owned ? (definition ? definition.color : new Color(.24f,.66f,.58f)) : new Color(.20f,.27f,.33f));
        shownOwned=owned;shownDefinition=definition;ghostFloor=highlight.HasValue&&highlight.Value.a<.99f;
        ApplyFloor(HexTestSettings.Current?HexTestSettings.Current.environment:null,owned&&!ghostFloor,definition);
        if (outline) { outline.sortingOrder = -32000; outline.startColor = outline.endColor = color; }
        furniture.SetActive(owned && definition);
        if(buildingSprite&&definition)buildingSprite.sprite=definition.worldSprite?definition.worldSprite:definition.icon;
        if (buildingSprite) buildingSprite.color = highlight.HasValue && highlight.Value.a < .99f ? highlight.Value : Color.white;
    }
    public void ShowShop(HexShopLayout layout,int floor,bool owned)
    {
        this.layout=layout;this.floor=floor;this.owned=owned;
        for(int d=0;d<walls.Length;d++)
        {
            var edge=new HexWallEdge(coordinate,d);
            bool shared=layout.Floor(floor).model.IsOwned(coordinate+HexBoardModel.Directions[d]);
            walls[d].Show(owned&&(!shared||edge.a==coordinate)&&layout.HasWall(floor,coordinate,d));
        }
        if(!labelCanvas)return;var definition=layout.Floor(floor).model.Definition(coordinate);labelCanvas.gameObject.SetActive(owned&&definition);
        if(!owned||!definition)return;
        kitchenOccupied=HexKitchenOccupant.Present(floor,coordinate);
        string detail="";if(definition.kind==HexTileKind.Kitchen&&!kitchenOccupied)detail=LocalizationService.Text("shop.kitchen.empty");
        if(layout.Floor(floor).use.TryGetValue(coordinate,out var use))detail=LocalizationService.Text("shop.capacity","count",use.Count,"capacity",use.capacity);
        label.text=LocalizationService.Text(definition.nameKey)+(detail.Length>0?"\n"+detail:"");label.color=definition.kind==HexTileKind.Kitchen?new Color(1,.83f,.28f):Color.white;
    }
    void LateUpdate()
    {
        if(HexTestSettings.Current)ApplyFloor(HexTestSettings.Current.environment,shownOwned&&!ghostFloor,shownDefinition);
        if(outline&&HexTestSettings.Current)outline.widthMultiplier=HexTestSettings.Current.visual.outlineWidth;
        if(labelCanvas&&Camera.main)labelCanvas.transform.rotation=Camera.main.transform.rotation;
        if(layout!=null&&owned&&layout.Floor(floor).model.Definition(coordinate)?.kind==HexTileKind.Kitchen&&kitchenOccupied!=HexKitchenOccupant.Present(floor,coordinate))ShowShop(layout,floor,owned);
    }
}

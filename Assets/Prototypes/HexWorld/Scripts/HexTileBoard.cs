using System.Collections.Generic;
using UnityEngine;

public enum HexBuildMode { None, Place, Move, Recover, Walls }

[DefaultExecutionOrder(-200)]
public sealed class HexTileBoard : MonoBehaviour
{
    public HexTileView tilePrefab;
    public HexTileView[] authoredTiles;
    public HexTileView preview;
    public Transform tileRoot;
    public HexTileDefinition[] tileTypes;
    [HideInInspector] public int initialStock = 2;
    public HexPrototype game;
    public HexShopLayout Layout { get; private set; }
    public int CurrentFloor { get; private set; }
    public HexBoardModel Model => Layout==null?null:Layout.Floor(CurrentFloor).model;
    public Vector2Int? WallSelection { get; private set; }
    public GameObject startingShop, player;
    public HexBuildMode Mode { get; private set; }
    public int SelectedType { get; private set; }
    public HexTileDefinition Selected => tileTypes[SelectedType];
    public Vector2Int? MoveSource { get; private set; }
    readonly Dictionary<Vector2Int, HexTileView> tiles = new Dictionary<Vector2Int, HexTileView>();
    Vector2Int? hover;
    readonly HexPlacementVisibility placementVisibility = new HexPlacementVisibility();
    float labelTimer;
    void LateUpdate()
    {
        placementVisibility.Update(preview);labelTimer-=Time.unscaledDeltaTime;
        if(Layout!=null&&labelTimer<=0){labelTimer=.25f;foreach(var pair in tiles)if(pair.Value.gameObject.activeInHierarchy)pair.Value.ShowShop(Layout,CurrentFloor,Model.IsOwned(pair.Key));}
    }
    void OnDisable() { placementVisibility.Restore(); }

    void Awake()
    {
        Layout = new HexShopLayout();
        foreach (var tile in authoredTiles) tiles.Add(tile.coordinate, tile);
        foreach(var type in tileTypes)Layout.Floor(0).model.Grant(type,initialStock); Refresh();
    }
    public void SelectType(int index) { SelectedType = Mathf.Clamp(index, 0, tileTypes.Length - 1); Refresh(); }
    public void SetMode(HexBuildMode mode)
    {
        Mode = mode; MoveSource = null; hover = null; WallSelection=null; Refresh();
        game.SetMessage("hex.help." + mode);
    }
    public void Grant(HexTileDefinition item, int count) { Layout.Floor(0).model.Grant(item,count); Refresh(); }
    public void SetFloor(int floor)
    {
        if(Layout==null||!Layout.floors.ContainsKey(floor))return;
        CurrentFloor=floor;MoveSource=null;hover=null;WallSelection=null;
        game.orbit.floorOffset=floor==0?Vector3.zero:HexBoardModel.World(Model.Root);
        if(startingShop)startingShop.SetActive(floor==0&&!Layout.Floor(0).model.Definition(Layout.Floor(0).model.Root));
        EnsureFrontier();Refresh();game.UpdateFloorVisibility();
    }
    public void ToggleWall(int direction)
    {
        if(!WallSelection.HasValue||!game.IsBuilding)return;
        bool success=Layout.SetWall(CurrentFloor,WallSelection.Value,direction,!Layout.HasWall(CurrentFloor,WallSelection.Value,direction));
        game.SetMessage(success?"shop.wall.changed":"shop.wall.blocked");Refresh();
    }
    public void Hover(HexTileView tile)
    {
        Vector2Int? next = tile ? tile.coordinate : (Vector2Int?)null;
        if (hover == next) return;
        hover = next; Refresh();
    }
    public void Click(HexTileView tile)
    {
        if (!tile || Mode == HexBuildMode.None) return;
        var at = tile.coordinate; bool success = false;
        if((game.service&&game.service.Occupies(CurrentFloor,at)||game.staffSystem&&game.staffSystem.BlocksEdit(CurrentFloor,at))&&(Mode==HexBuildMode.Recover||Mode==HexBuildMode.Move&&!MoveSource.HasValue)){game.SetMessage(game.staffSystem&&game.staffSystem.BlocksEdit(CurrentFloor,at)?"staff.build.worker":"service.build.worker");return;}
        if(Mode==HexBuildMode.Walls){if(Model.IsOwned(at))WallSelection=at;Refresh();return;}
        if(Model.IsOwned(at)){WallSelection=at;if(Mode==HexBuildMode.Place){Refresh();return;}}
        if (Mode == HexBuildMode.Place) success = Layout.TryPlace(CurrentFloor,at,Selected);
        if (Mode == HexBuildMode.Recover) success = Layout.TryRecover(CurrentFloor,at);
        if (Mode == HexBuildMode.Move)
        {
            if (!MoveSource.HasValue)
            {
                if (at != Model.Root && Model.IsOwned(at)) { MoveSource = at; game.SetMessage("hex.move.destination"); Refresh(); return; }
            }
            else { success = Layout.TryMove(CurrentFloor,MoveSource.Value, at); if (success) MoveSource = null; }
        }
        game.SetMessage(success ? "hex.build.success" : "hex.build.invalid");
        EnsureFrontier(); Refresh();
    }
    bool Valid(Vector2Int at)
    {
        if (Mode == HexBuildMode.Place) return Model.Stock(Selected) > 0 && Model.CanPlace(at);
        if (Mode == HexBuildMode.Recover) return (!game.service||!game.service.Occupies(CurrentFloor,at))&&(!game.staffSystem||!game.staffSystem.BlocksEdit(CurrentFloor,at))&&Layout.CanRecover(CurrentFloor,at);
        if (Mode == HexBuildMode.Move) return MoveSource.HasValue ? Layout.CanMove(CurrentFloor,MoveSource.Value, at) : at != Model.Root && Model.IsOwned(at)&&(!game.service||!game.service.Occupies(CurrentFloor,at))&&(!game.staffSystem||!game.staffSystem.BlocksEdit(CurrentFloor,at));
        return false;
    }
    void EnsureFrontier()
    {
        bool added = false;
        foreach (var owned in Model.Owned) foreach (var direction in HexBoardModel.Directions)
        {
            var at = owned + direction;
            if (tiles.ContainsKey(at)) continue;
            var view = Instantiate(tilePrefab, tileRoot); view.coordinate = at;
            view.transform.position = HexBoardModel.World(at); tiles.Add(at, view); added = true;
        }
        // FixedUpdate is paused in build mode; make newly created cells raycastable immediately.
        if (added) Physics.SyncTransforms();
    }
    public void Refresh()
    {
        if (Model == null) return;
        var kitchen=System.Array.Find(tileTypes,t=>t.kind==HexTileKind.Kitchen);
        Layout.ConvertEnclosedStall(kitchen);
        if(startingShop)startingShop.SetActive(CurrentFloor==0&&!Layout.Floor(0).model.Definition(Layout.Floor(0).model.Root));
        foreach (var pair in tiles)
        {
            bool adjacent=Model.IsOwned(pair.Key);foreach(var direction in HexBoardModel.Directions)adjacent|=Model.IsOwned(pair.Key+direction);
            pair.Value.gameObject.SetActive(CurrentFloor==0||adjacent);
            Color? highlight = Mode != HexBuildMode.None && Valid(pair.Key) ? new Color(.32f,.57f,.43f) : (Color?)null;
            if (MoveSource == pair.Key || WallSelection == pair.Key) highlight = new Color(1,.72f,.25f);
            if (hover == pair.Key && Mode != HexBuildMode.None) highlight = (Mode==HexBuildMode.Walls?Model.IsOwned(pair.Key):Valid(pair.Key)) ? new Color(.33f,.90f,.58f) : new Color(.82f,.29f,.31f);
            pair.Value.Show(Model.IsOwned(pair.Key), Model.Definition(pair.Key), highlight);
            pair.Value.ShowShop(Layout,CurrentFloor,Model.IsOwned(pair.Key));
        }
        bool showPreview = hover.HasValue && (Mode == HexBuildMode.Place || Mode == HexBuildMode.Move && MoveSource.HasValue);
        preview.gameObject.SetActive(showPreview);
        if (showPreview)
        {
            preview.coordinate = hover.Value; preview.transform.position = HexBoardModel.World(hover.Value) + Vector3.up * .16f;
            preview.Show(true, Selected, Valid(hover.Value) ? new Color(.2f,1,.55f,.4f) : new Color(1,.15f,.2f,.4f));
        }
    }
}

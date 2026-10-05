using System.Collections.Generic;
using UnityEngine;

public enum HexBuildMode { None, Place, Move, Recover }

[DefaultExecutionOrder(-200)]
public sealed class HexTileBoard : MonoBehaviour
{
    public HexTileView tilePrefab;
    public HexTileView[] authoredTiles;
    public HexTileView preview;
    public Transform tileRoot;
    public HexTileDefinition[] tileTypes;
    public int initialStock = 2;
    public HexPrototype game;
    public HexBoardModel Model { get; private set; }
    public HexBuildMode Mode { get; private set; }
    public int SelectedType { get; private set; }
    public HexTileDefinition Selected => tileTypes[SelectedType];
    public Vector2Int? MoveSource { get; private set; }
    readonly Dictionary<Vector2Int, HexTileView> tiles = new Dictionary<Vector2Int, HexTileView>();
    Vector2Int? hover;

    void Awake()
    {
        Model = new HexBoardModel();
        foreach (var tile in authoredTiles) tiles.Add(tile.coordinate, tile);
        Model.Grant(Selected, initialStock); Refresh();
    }
    public void SelectType(int index) { SelectedType = Mathf.Clamp(index, 0, tileTypes.Length - 1); Refresh(); }
    public void SetMode(HexBuildMode mode)
    {
        Mode = mode; MoveSource = null; hover = null; Refresh();
        game.SetMessage("hex.help." + mode);
    }
    public void Grant(HexTileDefinition item, int count) { Model.Grant(item, count); Refresh(); }
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
        if (Mode == HexBuildMode.Place) success = Model.Place(at, Selected);
        if (Mode == HexBuildMode.Recover) success = Model.Recover(at);
        if (Mode == HexBuildMode.Move)
        {
            if (!MoveSource.HasValue)
            {
                if (at != Vector2Int.zero && Model.IsOwned(at)) { MoveSource = at; game.SetMessage("hex.move.destination"); Refresh(); return; }
            }
            else { success = Model.Move(MoveSource.Value, at); if (success) MoveSource = null; }
        }
        game.SetMessage(success ? "hex.build.success" : "hex.build.invalid");
        EnsureFrontier(); Refresh();
    }
    bool Valid(Vector2Int at)
    {
        if (Mode == HexBuildMode.Place) return Model.Stock(Selected) > 0 && Model.CanPlace(at);
        if (Mode == HexBuildMode.Recover) return Model.CanRemove(at);
        if (Mode == HexBuildMode.Move) return MoveSource.HasValue ? Model.CanMove(MoveSource.Value, at) : at != Vector2Int.zero && Model.IsOwned(at);
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
        foreach (var pair in tiles)
        {
            Color? highlight = Mode != HexBuildMode.None && Valid(pair.Key) ? new Color(.32f,.57f,.43f) : (Color?)null;
            if (MoveSource == pair.Key) highlight = new Color(1,.72f,.25f);
            if (hover == pair.Key && Mode != HexBuildMode.None) highlight = Valid(pair.Key) ? new Color(.33f,.90f,.58f) : new Color(.82f,.29f,.31f);
            pair.Value.Show(Model.IsOwned(pair.Key), Model.Definition(pair.Key), highlight);
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

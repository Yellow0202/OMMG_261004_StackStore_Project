using System.Collections.Generic;
using UnityEngine;

/// <summary>Axial coordinates; only installed tiles belong to the player.</summary>
public sealed class HexBoardModel
{
    public static readonly Vector2Int[] Directions = {
        new Vector2Int(1,0), new Vector2Int(1,-1), new Vector2Int(0,-1),
        new Vector2Int(-1,0), new Vector2Int(-1,1), new Vector2Int(0,1)
    };
    readonly Dictionary<Vector2Int, HexTileDefinition> owned = new Dictionary<Vector2Int, HexTileDefinition>();
    readonly Dictionary<HexTileDefinition, int> stock = new Dictionary<HexTileDefinition, int>();
    public IEnumerable<Vector2Int> Owned => owned.Keys;
    public int OwnedCount => owned.Count;
    public HexBoardModel() { owned.Add(Vector2Int.zero, null); }
    public bool IsOwned(Vector2Int at) => owned.ContainsKey(at);
    public HexTileDefinition Definition(Vector2Int at) => owned.TryGetValue(at, out var item) ? item : null;
    public int Stock(HexTileDefinition item) => item && stock.TryGetValue(item, out int n) ? n : 0;
    public void Grant(HexTileDefinition item, int count) { if (item && count > 0) stock[item] = Stock(item) + count; }
    public static Vector3 World(Vector2Int at, float radius = 1.4f) => new Vector3(Mathf.Sqrt(3) * radius * (at.x + at.y * .5f), 0, 1.5f * radius * at.y);

    public bool CanPlace(Vector2Int at)
    {
        if (IsOwned(at)) return false;
        foreach (var direction in Directions) if (IsOwned(at + direction)) return true;
        return false;
    }
    public bool Place(Vector2Int at, HexTileDefinition item)
    {
        if (!CanPlace(at) || Stock(item) <= 0) return false;
        stock[item]--; owned.Add(at, item); return true;
    }
    public bool CanRemove(Vector2Int at)
    {
        if (at == Vector2Int.zero || !IsOwned(at)) return false;
        return Connected(at, null);
    }
    public bool CanMove(Vector2Int from, Vector2Int to)
    {
        if (from == to || from == Vector2Int.zero || !IsOwned(from) || IsOwned(to)) return false;
        // Validate the final layout, allowing a bridge to move only if no region is detached.
        return Connected(from, to);
    }
    bool Connected(Vector2Int removed, Vector2Int? added)
    {
        var remaining = new HashSet<Vector2Int>(owned.Keys); remaining.Remove(removed);
        if (added.HasValue) remaining.Add(added.Value);
        var visited = new HashSet<Vector2Int> { Vector2Int.zero };
        var pending = new Queue<Vector2Int>(); pending.Enqueue(Vector2Int.zero);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var d in Directions) if (remaining.Contains(current + d) && visited.Add(current + d)) pending.Enqueue(current + d);
        }
        return visited.Count == remaining.Count;
    }
    public bool Move(Vector2Int from, Vector2Int to)
    {
        if (!CanMove(from, to)) return false;
        var item = owned[from]; owned.Remove(from); owned.Add(to, item); return true;
    }
    public bool Recover(Vector2Int at)
    {
        if (!CanRemove(at)) return false;
        var item = owned[at]; owned.Remove(at); Grant(item, 1); return true;
    }
}

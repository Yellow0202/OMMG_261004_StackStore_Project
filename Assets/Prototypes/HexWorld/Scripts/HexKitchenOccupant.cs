using System.Collections.Generic;
using UnityEngine;

/// <summary>Attach to a player or assigned employee to suppress the empty-kitchen notice.</summary>
public sealed class HexKitchenOccupant : MonoBehaviour
{
    public int floor;
    public bool assigned = true;
    public Vector2Int kitchenCell;
    static readonly HashSet<HexKitchenOccupant> active = new HashSet<HexKitchenOccupant>();
    void Awake() { active.Add(this); }
    void OnDestroy() { active.Remove(this); }
    public void Assign(int kitchenFloor, Vector2Int cell) { floor=kitchenFloor;kitchenCell=cell;assigned=true; }
    public void Unassign() { assigned=false; }
    public static bool Present(int floor, Vector2Int cell)
    {
        foreach (var worker in active)
            if (worker && worker.assigned && worker.floor == floor && worker.kitchenCell == cell) return true;
        return false;
    }
}

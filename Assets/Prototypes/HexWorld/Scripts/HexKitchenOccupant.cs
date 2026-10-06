using System.Collections.Generic;
using UnityEngine;

/// <summary>Attach to a player or assigned employee to suppress the empty-kitchen notice.</summary>
public sealed class HexKitchenOccupant : MonoBehaviour
{
    public int floor;
    public bool assigned = true;
    static readonly HashSet<HexKitchenOccupant> active = new HashSet<HexKitchenOccupant>();
    void OnEnable() { active.Add(this); }
    void OnDisable() { active.Remove(this); }
    public static bool Present(int floor, Vector2Int cell)
    {
        foreach (var worker in active)
            if (worker && worker.assigned && worker.floor == floor && HexShopLayout.Cell(worker.transform.position) == cell) return true;
        return false;
    }
}

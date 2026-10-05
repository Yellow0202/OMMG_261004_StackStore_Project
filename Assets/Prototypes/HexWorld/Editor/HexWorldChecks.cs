using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Regression checks for adjacency, inventory conservation and connected ownership.</summary>
public static class HexWorldChecks
{
    static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
    [MenuItem("Stack Store/Hex World/Validate Tile Rules")]
    public static void Run()
    {
        var item=ScriptableObject.CreateInstance<HexTileDefinition>();var model=new HexBoardModel();model.Grant(item,20);
        Check(!model.Place(new Vector2Int(2,0),item),"Gap placement must fail");
        Check(!model.Place(new Vector2Int(1,1),item),"Corner or distant placement must fail");
        foreach(var d in HexBoardModel.Directions)Check(model.CanPlace(d),"All six edge neighbors must be placeable");
        Check(model.Place(new Vector2Int(1,0),item)&&model.Place(new Vector2Int(2,0),item),"Expansion from installed parts failed");
        Check(!model.Recover(new Vector2Int(1,0)),"Bridge recovery detached an island");
        Check(!model.Move(new Vector2Int(1,0),new Vector2Int(0,1)),"Bridge relocation detached an island");
        Check(!model.Recover(Vector2Int.zero)&&!model.Move(Vector2Int.zero,new Vector2Int(-1,0)),"Starting tile must be fixed");
        Check(model.Move(new Vector2Int(2,0),new Vector2Int(0,1)),"Valid connected relocation failed");
        Check(model.Recover(new Vector2Int(1,0))&&model.Stock(item)==19,"Recovery must return exactly one tile");
        var random=new System.Random(51005);
        for(int i=0;i<2000;i++)
        {
            var a=new Vector2Int(random.Next(-5,6),random.Next(-5,6));var b=new Vector2Int(random.Next(-5,6),random.Next(-5,6));
            switch(i%3){case 0:model.Place(a,item);break;case 1:model.Move(a,b);break;default:model.Recover(a);break;}
            Check(model.Stock(item)+model.OwnedCount-1==20,"Inventory conservation failed");
            var owned=new HashSet<Vector2Int>(model.Owned);var visited=new HashSet<Vector2Int>{Vector2Int.zero};var pending=new Queue<Vector2Int>();pending.Enqueue(Vector2Int.zero);
            while(pending.Count>0){var at=pending.Dequeue();foreach(var d in HexBoardModel.Directions)if(owned.Contains(at+d)&&visited.Add(at+d))pending.Enqueue(at+d);}
            Check(owned.Count==visited.Count,"Random operation disconnected ownership");
        }
        var empty=new HexBoardModel();Check(!empty.Place(new Vector2Int(1,0),item),"Zero-stock placement must fail");
        UnityEngine.Object.DestroyImmediate(item);
        Debug.Log("HEX_RULES_PASS: six neighbors, gaps, fixed origin, bridge rejection, move/recovery and 2000 stock/connectivity checks.");
    }
}

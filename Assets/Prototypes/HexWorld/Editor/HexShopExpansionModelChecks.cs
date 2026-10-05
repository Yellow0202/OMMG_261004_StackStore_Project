using System;
using UnityEditor;
using UnityEngine;
public static class HexShopExpansionModelChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static HexTileDefinition Load(string name)=>AssetDatabase.LoadAssetAtPath<HexTileDefinition>("Assets/Prototypes/HexWorld/Data/"+name+"Tile.asset");
 [MenuItem("Stack Store/Hex World/Validate Walls Facilities And Floors")]
 public static void Run()
 {
  UnityEngine.Random.InitState(6057);var table=Load("Table");var stairs=Load("Stairs");var entry=Load("Entrance");Check(table&&stairs&&entry,"Facility data missing");var layout=new HexShopLayout();foreach(var def in new[]{table,stairs,entry,Load("Kitchen"),Load("Storage"),Load("Lodging")})layout.Floor(0).model.Grant(def,30);
  for(int d=0;d<6;d++)Check(!layout.HasWall(0,Vector2Int.zero,d),"Starting stall has default walls");
  for(int d=0;d<5;d++)Check(layout.SetWall(0,Vector2Int.zero,d,true),"Legal wall rejected");Check(!layout.SetWall(0,Vector2Int.zero,5,true)&&layout.AllOwnedReachOutside(),"Last outside passage closed");
  var at=new Vector2Int(1,0);Check(layout.TryPlace(0,at,entry)&&!layout.HasWall(0,Vector2Int.zero,0),"Interior wall not removed on placement");Check(layout.SetWall(0,Vector2Int.zero,5,true),"Entrance not serving as outside route");Check(layout.AllOwnedReachOutside(),"Entrance external path missing");
  Check(layout.TryPlace(0,new Vector2Int(0,1),table),"Table placement failed");var use=layout.Floor(0).use[new Vector2Int(0,1)];Check(use.capacity>=2&&use.capacity<=4,"Table capacity outside 2-4");int capacity=use.capacity;
  for(int i=1;i<=capacity;i++)Check(layout.Reserve(i,HexTileKind.Table,new HexNavNode(0,new Vector2Int(-5,0)),out var target,out var seat)&&target.cell==new Vector2Int(0,1),"Seat reservation failed");Check(!layout.Reserve(99,HexTileKind.Table,new HexNavNode(0,new Vector2Int(-5,0)),out _,out _),"Table overbooked");Check(!layout.TryRecover(0,new Vector2Int(0,1)),"Occupied table recovered");for(int i=1;i<=capacity;i++)layout.Release(i);
  Check(layout.TryMove(0,new Vector2Int(0,1),new Vector2Int(-1,1))&&layout.Floor(0).use[new Vector2Int(-1,1)].capacity==capacity,"Table capacity rerolled during move");
  Check(layout.TryPlace(0,new Vector2Int(0,1),stairs)&&layout.floors.Count==2&&layout.Floor(1).model.OwnedCount==1,"Stairs did not create exactly one upper landing");int stock=layout.Floor(0).model.Stock(table);Check(layout.TryPlace(1,new Vector2Int(1,1),table)&&layout.Floor(0).model.Stock(table)==stock-1,"Upper expansion did not use inventory");
  var path=layout.Path(new HexNavNode(0,new Vector2Int(-5,0)),new HexNavNode(1,new Vector2Int(1,1)));Check(path!=null,"Upper floor unreachable");bool climbed=false;for(int i=1;i<path.Count;i++)if(path[i].floor!=path[i-1].floor){climbed=true;Check(layout.IsStair(path[i-1])&&layout.IsStair(path[i]),"Floor changed outside stairs");}Check(climbed,"Path did not use stairs");Check(!layout.TryRecover(0,new Vector2Int(0,1)),"Developed upper floor orphaned");
  Check(layout.TryPlace(1,new Vector2Int(2,1),stairs)&&layout.floors.Count==3&&layout.Floor(2).model.OwnedCount==1,"Next storey did not require another staircase");Check(layout.AllOwnedReachOutside(),"Upper levels not connected outside");
  var detour=new HexShopLayout();detour.Floor(0).model.Grant(table,10);Check(detour.TryPlace(0,at,table),"Detour fixture failed");Check(detour.TryPlace(0,new Vector2Int(0,1),table),"Alternate route fixture failed");Check(detour.SetWall(0,Vector2Int.zero,0,true),"Alternate-route wall rejected");path=detour.Path(new HexNavNode(0,Vector2Int.zero),new HexNavNode(0,at));Check(path!=null&&path.Count>2,"Path walked through closed wall");for(int i=1;i<path.Count;i++)if(path[i].floor==path[i-1].floor){int d=Array.IndexOf(HexBoardModel.Directions,path[i].cell-path[i-1].cell);Check(d>=0&&!detour.HasWall(path[i-1].floor,path[i-1].cell,d),"Navigation crossed wall");}
  var ring=new HexShopLayout();ring.Floor(0).model.Grant(table,300);foreach(var d in HexBoardModel.Directions)Check(ring.TryPlace(0,d,table),"Ring placement failed");Check(ring.AllOwnedReachOutside(),"Ring sealed customers inside");
  for(int i=0;i<100;i++){var owned=new System.Collections.Generic.List<Vector2Int>(ring.Floor(0).model.Owned);var cell=owned[UnityEngine.Random.Range(0,owned.Count)];int d=UnityEngine.Random.Range(0,6);ring.SetWall(0,cell,d,!ring.HasWall(0,cell,d));Check(ring.AllOwnedReachOutside(),"Random wall edit disconnected outside");}
  HexWorldChecks.Run();Debug.Log("HEX_SHOP_MODEL_PASS: defaults/interiors, last passage rejection, entrance route, seats2-4/reservation/occupied protection/preserved capacity, stair-only paths/one landing/inventory/protected upper floors, wall detours, enclosing ring and random edits, original connectivity checks.");
 }
}

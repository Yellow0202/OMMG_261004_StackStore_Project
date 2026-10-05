using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct HexNavNode : IEquatable<HexNavNode>
{
 public readonly int floor;public readonly Vector2Int cell;
 public HexNavNode(int floor,Vector2Int cell){this.floor=floor;this.cell=cell;}
 public bool Equals(HexNavNode other)=>floor==other.floor&&cell==other.cell;
 public override bool Equals(object obj)=>obj is HexNavNode other&&Equals(other);
 public override int GetHashCode()=>floor*397^cell.GetHashCode();
}
public readonly struct HexWallEdge : IEquatable<HexWallEdge>
{
 public readonly Vector2Int a,b;
 public HexWallEdge(Vector2Int cell,int direction){var other=cell+HexBoardModel.Directions[direction];bool first=cell.x<other.x||cell.x==other.x&&cell.y<other.y;a=first?cell:other;b=first?other:cell;}
 public bool Equals(HexWallEdge other)=>a==other.a&&b==other.b;
 public override bool Equals(object obj)=>obj is HexWallEdge other&&Equals(other);
 public override int GetHashCode()=>a.GetHashCode()*397^b.GetHashCode();
}
public sealed class HexTileUse
{
 public int capacity;public int[] occupants;
 public HexTileUse(int capacity){this.capacity=capacity;occupants=new int[capacity];}
 public HexTileUse Clone(){var copy=new HexTileUse(capacity);Array.Copy(occupants,copy.occupants,capacity);return copy;}
 public int Count{get{int n=0;foreach(int id in occupants)if(id!=0)n++;return n;}}
}
public sealed class HexShopFloor
{
 public HexBoardModel model;
 public readonly Dictionary<HexWallEdge,bool> walls=new Dictionary<HexWallEdge,bool>();
 public readonly Dictionary<Vector2Int,HexTileUse> use=new Dictionary<Vector2Int,HexTileUse>();
 public HexShopFloor(HexBoardModel model){this.model=model;}
 public HexShopFloor Clone(){var copy=new HexShopFloor(model.Clone());foreach(var p in walls)copy.walls.Add(p.Key,p.Value);foreach(var p in use)copy.use.Add(p.Key,p.Value.Clone());return copy;}
}
public sealed class HexShopLayout
{
 public readonly SortedDictionary<int,HexShopFloor> floors=new SortedDictionary<int,HexShopFloor>();
 readonly Dictionary<HexNavNode,HexNavNode> stairs=new Dictionary<HexNavNode,HexNavNode>();
 public int Revision{get;private set;}
 public HexShopLayout(){floors.Add(0,new HexShopFloor(new HexBoardModel()));}
 public HexShopFloor Floor(int floor)=>floors[floor];
 public bool IsStair(HexNavNode node)=>stairs.ContainsKey(node)||HasLanding(node);
 bool HasLanding(HexNavNode node){foreach(var p in stairs)if(p.Value.Equals(node))return true;return false;}
 public static Vector2Int Cell(Vector3 world)
 {
  float r=world.z/2.1f,q=world.x/(Mathf.Sqrt(3)*1.4f)-r*.5f;
  float x=q,z=r,y=-x-z;int rx=Mathf.RoundToInt(x),ry=Mathf.RoundToInt(y),rz=Mathf.RoundToInt(z);
  float dx=Mathf.Abs(rx-x),dy=Mathf.Abs(ry-y),dz=Mathf.Abs(rz-z);if(dx>dy&&dx>dz)rx=-ry-rz;else if(dz>dy)rz=-rx-ry;
  return new Vector2Int(rx,rz);
 }
 public bool HasWall(int floor,Vector2Int at,int direction)
 {
  var f=Floor(floor);var edge=new HexWallEdge(at,direction);
  if(f.walls.TryGetValue(edge,out bool manual))return manual;
  bool a=f.model.IsOwned(at),b=f.model.IsOwned(at+HexBoardModel.Directions[direction]);
  if(a==b)return false;var owned=a?at:at+HexBoardModel.Directions[direction];var definition=f.model.Definition(owned);
  return !(floor==0&&owned==f.model.Root||definition&&definition.kind==HexTileKind.Entrance);
 }
 public bool SetWall(int floor,Vector2Int at,int direction,bool present)
 {
  if(direction<0||direction>=6||!floors.ContainsKey(floor)||!Floor(floor).model.IsOwned(at))return false;
  var edge=new HexWallEdge(at,direction);var f=Floor(floor);bool existed=f.walls.TryGetValue(edge,out bool old);f.walls[edge]=present;
  if(!AllOwnedReachOutside()){if(existed)f.walls[edge]=old;else f.walls.Remove(edge);return false;}
  Revision++;return true;
 }
 public bool TryPlace(int floor,Vector2Int at,HexTileDefinition definition)=>Edit(()=>
 {
  var f=Floor(floor);if(!f.model.Place(at,definition))return false;
  InitialiseUse(f,at,definition);OpenInteriorEdges(f,at);
  if(definition.kind==HexTileKind.Stairs)ConnectStair(floor,at,definition);
  return true;
 });
 public bool CanRecover(int floor,Vector2Int at)
 {
  var f=Floor(floor);if(!f.model.CanRemove(at)||f.use.TryGetValue(at,out var use)&&use.Count>0)return false;
  var node=new HexNavNode(floor,at);if(stairs.TryGetValue(node,out var landing))
  {
   int links=0;foreach(var p in stairs)if(p.Value.floor==landing.floor)links++;
   if(links==1&&(Floor(landing.floor).model.OwnedCount>1||floors.ContainsKey(landing.floor+1)))return false;
  }
  return true;
 }
 public bool TryRecover(int floor,Vector2Int at)=>Edit(()=>
 {
  if(!CanRecover(floor,at))return false;RemoveStair(new HexNavNode(floor,at));var f=Floor(floor);f.use.Remove(at);RemoveEdges(f,at);return f.model.Recover(at);
 });
 public bool CanMove(int floor,Vector2Int from,Vector2Int to)=>!stairs.ContainsKey(new HexNavNode(floor,from))&&Floor(floor).model.CanMove(from,to)&&(!Floor(floor).use.TryGetValue(from,out var use)||use.Count==0);
 public bool TryMove(int floor,Vector2Int from,Vector2Int to)=>Edit(()=>
 {
  if(!CanMove(floor,from,to))return false;var f=Floor(floor);var definition=f.model.Definition(from);var node=new HexNavNode(floor,from);bool stair=stairs.TryGetValue(node,out var landing);
  if(!f.model.Move(from,to))return false;if(f.use.TryGetValue(from,out var use)){f.use.Remove(from);f.use.Add(to,use);}RemoveEdges(f,from);OpenInteriorEdges(f,to);
  if(stair){stairs.Remove(node);stairs.Add(new HexNavNode(floor,to),landing);}return true;
 });
 void InitialiseUse(HexShopFloor f,Vector2Int at,HexTileDefinition definition)
 {
  if(definition.kind==HexTileKind.Table)f.use[at]=new HexTileUse(UnityEngine.Random.Range(Mathf.Clamp(definition.minimumSeats,2,4),Mathf.Clamp(definition.maximumSeats,2,4)+1));
  if(definition.kind==HexTileKind.Lodging)f.use[at]=new HexTileUse(1);
 }
 void OpenInteriorEdges(HexShopFloor f,Vector2Int at){for(int d=0;d<6;d++)if(f.model.IsOwned(at+HexBoardModel.Directions[d]))f.walls.Remove(new HexWallEdge(at,d));}
 void RemoveEdges(HexShopFloor f,Vector2Int at){for(int d=0;d<6;d++)f.walls.Remove(new HexWallEdge(at,d));}
 void ConnectStair(int floor,Vector2Int at,HexTileDefinition definition)
 {
  if(!floors.ContainsKey(floor+1))floors.Add(floor+1,new HexShopFloor(new HexBoardModel(at,definition)));
  Floor(floor+1).model.ShareStock(Floor(0).model);
  stairs[new HexNavNode(floor,at)]=new HexNavNode(floor+1,Floor(floor+1).model.Root);
 }
 void RemoveStair(HexNavNode node)
 {
  if(!stairs.TryGetValue(node,out var landing))return;stairs.Remove(node);foreach(var p in stairs)if(p.Value.floor==landing.floor)return;floors.Remove(landing.floor);
 }
 bool Edit(Func<bool> operation)
 {
  var old=new SortedDictionary<int,HexShopFloor>();foreach(var p in floors)old.Add(p.Key,p.Value.Clone());var links=new Dictionary<HexNavNode,HexNavNode>(stairs);
  if(operation()&&RepairGroundExit()&&AllOwnedReachOutside()){Revision++;return true;}
  floors.Clear();foreach(var p in old)floors.Add(p.Key,p.Value);foreach(var f in floors.Values)f.model.ShareStock(Floor(0).model);stairs.Clear();foreach(var p in links)stairs.Add(p.Key,p.Value);return false;
 }
 public IEnumerable<HexNavNode> OwnedNodes(){foreach(var f in floors)foreach(var cell in f.Value.model.Owned)yield return new HexNavNode(f.Key,cell);}
 void Bounds(out Vector2Int min,out Vector2Int max)
 {
  min=new Vector2Int(-8,-8);max=new Vector2Int(8,8);foreach(var c in Floor(0).model.Owned){min=Vector2Int.Min(min,c-new Vector2Int(4,4));max=Vector2Int.Max(max,c+new Vector2Int(4,4));}
 }
 bool Passable(HexNavNode node,Vector2Int min,Vector2Int max)=>floors.ContainsKey(node.floor)&&(node.floor==0?node.cell.x>=min.x&&node.cell.y>=min.y&&node.cell.x<=max.x&&node.cell.y<=max.y:Floor(node.floor).model.IsOwned(node.cell));
 IEnumerable<HexNavNode> Neighbours(HexNavNode node,Vector2Int min,Vector2Int max,bool ignoreWalls=false)
 {
  for(int d=0;d<6;d++){var next=new HexNavNode(node.floor,node.cell+HexBoardModel.Directions[d]);if(Passable(next,min,max)&&(ignoreWalls||!HasWall(node.floor,node.cell,d)))yield return next;}
  if(stairs.TryGetValue(node,out var up))yield return up;foreach(var p in stairs)if(p.Value.Equals(node))yield return p.Key;
 }
 HashSet<HexNavNode> ExteriorReach(bool ignoreWalls=false)
 {
  Bounds(out var min,out var max);var visited=new HashSet<HexNavNode>();var pending=new Queue<HexNavNode>();
  for(int q=min.x;q<=max.x;q++)for(int r=min.y;r<=max.y;r++)if(q==min.x||q==max.x||r==min.y||r==max.y){var n=new HexNavNode(0,new Vector2Int(q,r));if(visited.Add(n))pending.Enqueue(n);}
  while(pending.Count>0){var at=pending.Dequeue();foreach(var next in Neighbours(at,min,max,ignoreWalls))if(visited.Add(next))pending.Enqueue(next);}return visited;
 }
 public bool AllOwnedReachOutside(){var reached=ExteriorReach();foreach(var node in OwnedNodes())if(!reached.Contains(node))return false;return true;}
 bool RepairGroundExit()
 {
  // Tile changes preserve a real route, not merely an opening into a sealed courtyard.
  for(int attempt=0;attempt<Floor(0).model.OwnedCount*6+6;attempt++)
  {
   var reached=ExteriorReach();HexNavNode? blocked=null;foreach(var node in OwnedNodes())if(!reached.Contains(node)){blocked=node;break;}if(!blocked.HasValue)return true;
   Bounds(out var outsideMin,out var outsideMax);var route=Path(blocked.Value,new HexNavNode(0,outsideMin),true);if(route==null)return false;bool opened=false;
   for(int i=1;i<route.Count;i++)if(route[i-1].floor==0&&route[i].floor==0){Vector2Int direction=route[i].cell-route[i-1].cell;int d=Array.IndexOf(HexBoardModel.Directions,direction);if(d>=0&&HasWall(0,route[i-1].cell,d)){Floor(0).walls[new HexWallEdge(route[i-1].cell,d)]=false;opened=true;break;}}
   if(!opened)return false;
  }
  return false;
 }
 public List<HexNavNode> Path(HexNavNode start,HexNavNode goal,bool ignoreWalls=false)
 {
  Bounds(out var min,out var max);if(!Passable(start,min,max)||!Passable(goal,min,max))return null;
  var from=new Dictionary<HexNavNode,HexNavNode>();var seen=new HashSet<HexNavNode>{start};var pending=new Queue<HexNavNode>();pending.Enqueue(start);
  while(pending.Count>0){var node=pending.Dequeue();if(node.Equals(goal)){var path=new List<HexNavNode>{node};while(!node.Equals(start)){node=from[node];path.Add(node);}path.Reverse();return path;}foreach(var next in Neighbours(node,min,max,ignoreWalls))if(seen.Add(next)){from.Add(next,node);pending.Enqueue(next);}}
  return null;
 }
 public bool Reserve(int guest,HexTileKind kind,HexNavNode start,out HexNavNode target,out int seat)
 {
  target=default;seat=-1;float distance=float.PositiveInfinity;
  foreach(var floor in floors)foreach(var entry in floor.Value.use)
  {
   var definition=floor.Value.model.Definition(entry.Key);if(!definition||definition.kind!=kind)continue;
   var node=new HexNavNode(floor.Key,entry.Key);float score=(HexBoardModel.World(entry.Key)-HexBoardModel.World(start.cell)).sqrMagnitude+floor.Key*8;
   if(score>=distance)continue;for(int i=0;i<entry.Value.capacity;i++)if(entry.Value.occupants[i]==0&&Path(start,node)!=null){target=node;seat=i;distance=score;break;}
  }
  if(seat<0)return false;Floor(target.floor).use[target.cell].occupants[seat]=guest;return true;
 }
 public void Release(int guest){foreach(var floor in floors)foreach(var use in floor.Value.use.Values)for(int i=0;i<use.capacity;i++)if(use.occupants[i]==guest)use.occupants[i]=0;}
 public Vector3 SeatPosition(HexNavNode node,int seat){var use=Floor(node.floor).use[node.cell];float a=seat*Mathf.PI*2/use.capacity;return HexBoardModel.World(node.cell)+new Vector3(Mathf.Cos(a)*.65f,.22f,Mathf.Sin(a)*.65f);}
}

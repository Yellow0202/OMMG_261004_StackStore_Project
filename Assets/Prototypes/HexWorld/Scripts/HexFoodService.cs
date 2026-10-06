using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Walk to a cooking station, prepare individual dishes and serve through owned open cells.</summary>
public sealed class HexFoodService : MonoBehaviour
{
    public HexPrototype game;
    public HexWorldActor worker;
    public SpriteRenderer servedFoodPrefab;
    public SpriteRenderer carriedFood;
    public Image[] gauges;
    public Text[] labels;
    public GameObject[] slotViews;
    public float walkSpeed=3.8f;
    public int Floor { get; private set; }
    public readonly HexFoodPreparation Food=new HexFoodPreparation();
    int targetGuest,revision=-1,waypoint;
    HexNavNode goal;
    List<HexNavNode> path;
    bool returning=true;
    public bool AtStation { get; private set; }
    public bool RouteBlocked { get; private set; }
    public Vector3 Position=>worker.transform.position;
    public bool Occupies(int floor,Vector2Int cell)=>floor==Floor&&HexShopLayout.Cell(Position)==cell;

    public void Tick(float dt)
    {
        Food.SetCount(game.FoodPerThrow);
        var layout=game.board.Layout;
        var current=new HexNavNode(Floor,HexShopLayout.Cell(Position));
        if(!returning&&!game.ServiceTarget(targetGuest,out var target,out var position))
        {targetGuest=0;returning=true;path=null;}
        if(returning)
        {
            if(!FindStation(current,out var station)){AtStation=false;RouteBlocked=true;RefreshGauges();return;}
            RouteBlocked=false;
            Move(station,StationPosition(station),dt);
            AtStation=Floor==station.floor&&Vector3.Distance(Position,StationPosition(station))<.1f;
            Food.Tick(dt,game.ServiceInterval,AtStation);
            if(AtStation&&Food.ReadySlot()>=0&&game.NextService(current,out targetGuest,out goal))
            {returning=false;path=null;AtStation=false;}
        }
        else if(game.ServiceTarget(targetGuest,out var destination,out var spot))
        {
            AtStation=false;
            if(game.ServicePath(current,destination)==null){returning=true;targetGuest=0;path=null;return;}
            Move(destination,spot,dt);
            if(Floor==destination.floor&&Vector3.Distance(Position,spot)<.18f)
            {
                int slot=Food.ReadySlot();
                if(slot>=0&&game.CompleteService(targetGuest,servedFoodPrefab))Food.Consume(slot);
                targetGuest=0;path=null;
                current=new HexNavNode(Floor,HexShopLayout.Cell(Position));
                if(Food.ReadySlot()<0||!game.NextService(current,out targetGuest,out goal))returning=true;
            }
        }
        RefreshGauges();
    }
    public void RefreshGauges()
    {
        if(carriedFood)carriedFood.gameObject.SetActive(Food.ReadySlot()>=0);
        for(int i=0;i<gauges.Length;i++)
        {
            slotViews[i].SetActive(i<Food.Count);
            if(i>=Food.Count)continue;
            slotViews[i].GetComponent<RectTransform>().anchoredPosition=new Vector2((i-(Food.Count-1)*.5f)*130,-6);
            gauges[i].fillAmount=Food[i];
            labels[i].text=LocalizationService.Text("service.food.slot","number",i+1,"state",LocalizationService.Text(RouteBlocked?"service.blocked":Food[i]>=1?"service.ready":AtStation?"service.cooking":"service.paused"));
        }
    }
    bool FindStation(HexNavNode start,out HexNavNode station)
    {
        station=default;int distance=int.MaxValue;
        foreach(var node in game.board.Layout.OwnedNodes())
        {
            var definition=game.board.Layout.Floor(node.floor).model.Definition(node.cell);
            bool initial=node.floor==0&&node.cell==game.board.Layout.Floor(0).model.Root;
            if(!initial&&(!definition||definition.kind!=HexTileKind.Kitchen))continue;
            var route=game.ServicePath(start,node);
            if(route!=null&&route.Count<distance){distance=route.Count;station=node;}
        }
        return distance<int.MaxValue;
    }
    Vector3 StationPosition(HexNavNode station)=>HexBoardModel.World(station.cell)+new Vector3(0,.22f,-.65f);
    void Move(HexNavNode destination,Vector3 spot,float dt)
    {
        var layout=game.board.Layout;
        if(path==null||revision!=layout.Revision||!goal.Equals(destination))
        {
            goal=destination;revision=layout.Revision;waypoint=0;
            path=game.ServicePath(new HexNavNode(Floor,HexShopLayout.Cell(Position)),destination);
        }
        if(path==null)return;
        if(waypoint<path.Count)
        {
            var node=path[waypoint];
            if(node.floor!=Floor){Floor=node.floor;worker.transform.position=HexBoardModel.World(node.cell)+Vector3.up*.22f;UpdateVisibility();waypoint++;return;}
            var center=HexBoardModel.World(node.cell)+Vector3.up*.22f;
            worker.transform.position=Vector3.MoveTowards(Position,center,dt*walkSpeed);
            if(Vector3.Distance(Position,center)<.05f)waypoint++;
        }
        else worker.transform.position=Vector3.MoveTowards(Position,spot,dt*walkSpeed);
    }
    public void UpdateVisibility(){if(worker){var occupant=worker.GetComponent<HexKitchenOccupant>();if(occupant)occupant.floor=Floor;worker.gameObject.SetActive(Floor==game.board.CurrentFloor);}}
}

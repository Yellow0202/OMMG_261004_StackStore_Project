using System;
using UnityEditor;
using UnityEngine;

public static class HexDirectServiceChecks
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static HexTileDefinition Load(string name)=>AssetDatabase.LoadAssetAtPath<HexTileDefinition>("Assets/Prototypes/HexWorld/Data/"+name+"Tile.asset");
    [MenuItem("Stack Store/Hex World/Validate Direct Service Rules")]
    public static void Run()
    {
        var food=new HexFoodPreparation();food.SetCount(1);food.Tick(2,5,true);Check(Mathf.Abs(food[0]-.4f)<.001f,"Preparation duration incorrect");food.Tick(20,5,false);Check(Mathf.Abs(food[0]-.4f)<.001f,"Preparation progressed away from station");
        food.SetCount(3);Check(food[1]==0&&food[0]>.39f,"New slot changed existing dish");food.Tick(3,5,true);Check(food[0]==1&&Mathf.Abs(food[1]-.6f)<.001f,"Dish timers coupled");Check(food.Consume(0)&&food[0]==0&&food[1]>.59f,"Consumption reset other dishes");food.Tick(2,5,true);Check(food.ReadySlot()==1&&food.ReadyCount==2&&food[0]<.41f,"Independent refill failed");Check(!food.Consume(0),"Unprepared food consumed");
        var gesture=new HexPointerGesture();gesture.Begin(Vector2.zero,false);Check(gesture.Click(new Vector2(4,4)),"Small click classified as drag");gesture.Begin(Vector2.zero,false);Check(gesture.Move(new Vector2(0,25))==25&&gesture.Dragging&&!gesture.Click(Vector2.zero),"Drag released as tile click");gesture.Begin(Vector2.zero,true);Check(gesture.Move(new Vector2(0,100))==0&&!gesture.Click(Vector2.zero),"UI interaction moved camera or clicked tile");
        var table=Load("Table");var entry=Load("Entrance");var kitchen=Load("Kitchen");var layout=new HexShopLayout();layout.Floor(0).model.Grant(table,20);layout.Floor(0).model.Grant(entry,2);layout.Floor(0).model.Grant(kitchen,2);
        Check(!layout.ConvertEnclosedStall(kitchen),"Stall converted before building formed");foreach(var direction in HexBoardModel.Directions)Check(layout.TryPlace(0,direction,table),"Envelope placement failed");Check(!layout.ConvertEnclosedStall(kitchen),"Incomplete outer envelope converted");
        Check(layout.TryRecover(0,new Vector2Int(1,0))&&layout.TryPlace(0,new Vector2Int(1,0),entry),"Entrance fixture failed");
        var model=layout.Floor(0).model;foreach(var cell in model.Owned)for(int d=0;d<6;d++)if(!model.IsOwned(cell+HexBoardModel.Directions[d])&&model.Definition(cell)!=entry)Check(layout.SetWall(0,cell,d,true),"Valid exterior wall rejected");
        int stock=model.Stock(kitchen);Check(layout.ConvertEnclosedStall(kitchen)&&model.Definition(model.Root)==kitchen&&model.Stock(kitchen)==stock,"Conversion lost stock or did not create kitchen");Check(!layout.TryRecover(0,model.Root),"Converted initial kitchen became removable");Check(!layout.ConvertEnclosedStall(kitchen),"Conversion repeated");
        var split=new HexShopLayout();split.Floor(0).model.Grant(table,4);split.Floor(0).model.Grant(entry,4);split.TryPlace(0,new Vector2Int(1,0),table);split.TryPlace(0,new Vector2Int(2,0),entry);Check(split.SetWall(0,Vector2Int.zero,0,true),"External-route fixture rejected");
        Check(split.Path(new HexNavNode(0,Vector2Int.zero),new HexNavNode(0,new Vector2Int(1,0)))!=null,"Existing outside navigation regressed");Check(split.Path(new HexNavNode(0,Vector2Int.zero),new HexNavNode(0,new Vector2Int(1,0)),false,true)==null,"Service path left owned area to cross sealed wall");
        HexShopExpansionModelChecks.Run();Debug.Log("HEX_DIRECT_RULES_PASS: separate preparation/progress/consumption, station-only ticking, click/drag/UI arbitration, closed envelope plus entrance conversion without stock consumption, protected root, owned service routes and original walls/facilities/floors checks.");
    }
}

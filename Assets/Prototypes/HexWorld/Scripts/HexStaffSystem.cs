using System.Collections.Generic;
using UnityEngine;

public sealed class HexEmployee
{
    public HexEmployeeState state;
    public HexWorldActor view;
    public HexKitchenOccupant assignment;
    public int floor,waypoint,revision=-1,cookSlot=-1;
    public List<HexNavNode> path;
    public HexNavNode goal;
    public HexStaffWorkTarget task;
    public float recruitTimer,ejectionTimer,patrolTimer;
    public Vector3 patrolPoint;
    public string statusKey="staff.status.standby";
    public int cooked,recruited,cleaned,removed,ejected;
}
public sealed class HexStaffSystem : MonoBehaviour
{
    public HexPrototype game;
    public HexWorldActor employeePrefab;
    public Transform employeeRoot;
    public readonly List<HexEmployee> employees=new List<HexEmployee>();
    static readonly HexTestSettings.StaffOptions Defaults=new HexTestSettings.StaffOptions();
    public HexTestSettings.StaffOptions Options=>HexTestSettings.Current?HexTestSettings.Current.staff:Defaults;
    int nextId=1;
    public string MessageKey { get; private set; }="staff.help";
    public bool CanHire(HexStaffDefinition definition)=>definition&&!string.IsNullOrWhiteSpace(definition.key)&&definition.hirePrice>=0&&definition.wage>=0&&employees.Count<Options.maximumEmployees&&game.Gold>=definition.hirePrice;
    public bool Hire(HexStaffDefinition definition)
    {
        if(!game.IsShopping||!CanHire(definition)){MessageKey="staff.hire.failed";return false;}
        if(definition.hirePrice>0&&!game.TrySpendGold(definition.hirePrice))return false;
        var actor=Instantiate(employeePrefab,employeeRoot);actor.transform.position=game.service.Position;actor.body.color=definition.color;actor.ShowPatience(false,1);
        var marker=actor.GetComponent<HexKitchenOccupant>();marker.Unassign();
        var employee=new HexEmployee{state=new HexEmployeeState(nextId++,definition,Options),view=actor,assignment=marker,floor=game.service.Floor};
        var identity=actor.GetComponentInChildren<HexActorIdentity>(true);if(identity)identity.SetEmployee(employee.state.id);
        employees.Add(employee);MessageKey="staff.hired";UpdateVisibility();return true;
    }
    public bool IsKitchen(HexNavNode node)
    {
        if(!game.board.Layout.floors.ContainsKey(node.floor))return false;
        var model=game.board.Layout.Floor(node.floor).model;
        return model.IsOwned(node.cell)&&(model.Definition(node.cell)?.kind==HexTileKind.Kitchen||node.floor==0&&node.cell==model.Root&&model.Definition(node.cell)==null);
    }
    public List<HexNavNode> Kitchens()
    {
        var list=new List<HexNavNode>();foreach(var node in game.board.Layout.OwnedNodes())if(IsKitchen(node))list.Add(node);
        list.Sort((a,b)=>a.floor!=b.floor?a.floor.CompareTo(b.floor):a.cell.x!=b.cell.x?a.cell.x.CompareTo(b.cell.x):a.cell.y.CompareTo(b.cell.y));return list;
    }
    public bool Assign(int index,HexStaffJob job,HexNavNode kitchen)
    {
        if(index<0||index>=employees.Count||!System.Enum.IsDefined(typeof(HexStaffJob),job))return false;
        var employee=employees[index];
        if(job==HexStaffJob.Kitchen)
        {
            if(!IsKitchen(kitchen)||game.board.Layout.Path(new HexNavNode(employee.floor,HexShopLayout.Cell(employee.view.transform.position)),kitchen)==null){MessageKey="staff.assign.route";return false;}
            foreach(var other in employees)if(other!=employee&&other.state.hasKitchen&&other.state.kitchen.Equals(kitchen)){MessageKey="staff.assign.occupied";return false;}
        }
        if(employee.task)employee.task.Release(employee.state.id);employee.task=null;employee.path=null;employee.cookSlot=-1;
        employee.state.job=job;employee.state.hasKitchen=job==HexStaffJob.Kitchen;employee.state.kitchen=kitchen;
        if(job==HexStaffJob.Kitchen)employee.assignment.Assign(kitchen.floor,kitchen.cell);else employee.assignment.Unassign();
        employee.recruitTimer=employee.ejectionTimer=employee.patrolTimer=0;MessageKey="staff.assigned";return true;
    }
    public bool BlocksEdit(int floor,Vector2Int cell)
    {
        foreach(var employee in employees)if(employee.view&&(employee.floor==floor&&HexShopLayout.Cell(employee.view.transform.position)==cell||employee.state.hasKitchen&&employee.state.kitchen.Equals(new HexNavNode(floor,cell))))return true;
        return false;
    }
    public void Tick(float seconds)
    {
        if(seconds<=0)return;
        game.service.EmployeeCookingSlots.Clear();game.service.Food.SetCount(game.FoodPerThrow);
        foreach(var employee in employees)
        {
            float worked=employee.state.Advance(seconds,Options,game.TrySpendGold);
            if(worked<=0){employee.statusKey="staff.status.unpaid";employee.cookSlot=-1;if(employee.task)employee.task.Release(employee.state.id);employee.task=null;continue;}
            if(employee.state.hasKitchen&&!IsKitchen(employee.state.kitchen))
            {employee.state.hasKitchen=false;employee.assignment.Unassign();employee.statusKey="staff.status.kitchen.missing";continue;}
            employee.statusKey="staff.status.standby";
            if(employee.state.job==HexStaffJob.Kitchen)Cook(employee,worked);
            else if(employee.state.job==HexStaffJob.Recruiting)Recruit(employee,worked);
            else if(employee.state.job==HexStaffJob.Cleaning||employee.state.job==HexStaffJob.Security)WorkTask(employee,worked);
            employee.ejectionTimer-=worked;
            if(employee.ejectionTimer<=0)
            {
                employee.ejectionTimer=Mathf.Max(.1f,Options.ejectionSeconds);
                if(Random.value<employee.state.Flaw(HexStaffFlawKind.WaitingGuestEjection)&&game.EjectWaitingGuest(employee.view.transform.position,employee.floor,Options.recruitRadius))employee.ejected++;
            }
            if(employee.state.Suspended)employee.statusKey="staff.status.unpaid";
        }
        UpdateVisibility();
    }
    void Cook(HexEmployee employee,float seconds)
    {
        if(!employee.state.hasKitchen){employee.statusKey="staff.status.kitchen.missing";return;}
        var spot=HexBoardModel.World(employee.state.kitchen.cell)+Options.kitchenOffset;
        if(!Move(employee,employee.state.kitchen,spot,seconds)){employee.cookSlot=-1;return;}
        employee.statusKey="staff.status.cooking";
        var food=game.service.Food;
        if(employee.cookSlot<0||employee.cookSlot>=food.Count||food[employee.cookSlot]>=1)employee.cookSlot=FreeCookSlot(employee);
        if(employee.cookSlot<0){employee.statusKey=food.ReadyCount==food.Count?"staff.status.food.ready":"staff.status.station.wait";return;}
        float before=food[employee.cookSlot];food.TickSlot(employee.cookSlot,seconds,game.ServiceInterval/employee.state.Efficiency(HexStaffAbility.Cooking,Options));
        game.service.EmployeeCookingSlots.Add(employee.cookSlot);if(before<1&&food[employee.cookSlot]>=1)employee.cooked++;
    }
    int FreeCookSlot(HexEmployee employee)
    {
        for(int i=0;i<game.service.Food.Count;i++)
        {
            if(game.service.Food[i]>=1)continue;bool taken=false;
            foreach(var other in employees)if(other!=employee&&other.state.job==HexStaffJob.Kitchen&&other.state.hasKitchen&&!other.state.Suspended&&other.cookSlot==i){taken=true;break;}
            if(!taken)return i;
        }
        return -1;
    }
    void Recruit(HexEmployee employee,float seconds)
    {
        employee.statusKey="staff.status.recruiting";employee.recruitTimer-=seconds*employee.state.Efficiency(HexStaffAbility.Persuasion,Options);
        if(game.FindRecruitGuest(employee.view.transform.position,employee.floor,Options.recruitRadius,out int id,out var node,out var spot))
        {
            if(Move(employee,node,spot,seconds)&&employee.recruitTimer<=0)
            {
                employee.recruitTimer=Mathf.Max(.1f,Options.recruitSeconds);
                if(Random.value<employee.state.PersuasionChance(Options)&&game.PersuadeGuest(id))employee.recruited++;
            }
        }
        else Patrol(employee,seconds);
    }
    void WorkTask(HexEmployee employee,float seconds)
    {
        bool security=employee.state.job==HexStaffJob.Security;
        if(employee.task&&(employee.task.Completed||!employee.task.gameObject.activeInHierarchy||employee.task.definition.kind!=(security?HexStaffTargetKind.Disruption:HexStaffTargetKind.Trash)||employee.task.ClaimedBy!=employee.state.id)){employee.task.Release(employee.state.id);employee.task=null;}
        if(!employee.task)
        {
            float nearest=float.MaxValue;bool weak=false;
            foreach(var target in HexStaffWorkTarget.Active)
            {
                if(!target||!target.definition||target.Completed||target.ClaimedBy!=0&&target.ClaimedBy!=employee.state.id||target.definition.kind!=(security?HexStaffTargetKind.Disruption:HexStaffTargetKind.Trash))continue;
                if(security&&!employee.state.CanRemove(target.definition.requiredStrength,Options)){weak=true;continue;}
                float distance=Vector3.Distance(employee.view.transform.position,target.transform.position)+Mathf.Abs(employee.floor-target.floor)*10;
                if(distance<nearest&&game.board.Layout.Path(new HexNavNode(employee.floor,HexShopLayout.Cell(employee.view.transform.position)),new HexNavNode(target.floor,HexShopLayout.Cell(target.transform.position)))!=null){employee.task=target;nearest=distance;}
            }
            if(employee.task)employee.task.Claim(employee.state.id);
            else{employee.statusKey=weak?"staff.status.weak":security?"staff.status.security.wait":"staff.status.clean.wait";return;}
        }
        var task=employee.task;
        if(security&&!employee.state.CanRemove(task.definition.requiredStrength,Options)){task.Release(employee.state.id);employee.task=null;employee.statusKey="staff.status.weak";return;}
        if(!Move(employee,new HexNavNode(task.floor,HexShopLayout.Cell(task.transform.position)),task.transform.position,seconds))return;
        employee.statusKey=security?"staff.status.security":"staff.status.cleaning";
        if(task.Work(employee.state.id,seconds,employee.state.Efficiency(security?HexStaffAbility.Strength:HexStaffAbility.Cleaning,Options)))
        {if(security)employee.removed++;else employee.cleaned++;employee.task=null;}
    }
    void Patrol(HexEmployee employee,float seconds)
    {
        employee.patrolTimer-=seconds;
        if(employee.patrolTimer<=0)
        {
            employee.patrolTimer=Mathf.Max(.1f,Options.patrolSeconds);
            var center=HexBoardModel.World(game.board.Layout.Floor(employee.floor).model.Root);float angle=Random.value*Mathf.PI*2;
            employee.patrolPoint=center+new Vector3(Mathf.Cos(angle)*Options.patrolRadius,Options.kitchenOffset.y,Mathf.Sin(angle)*Options.patrolRadius);
        }
        Move(employee,new HexNavNode(employee.floor,HexShopLayout.Cell(employee.patrolPoint)),employee.patrolPoint,seconds);
    }
    bool Move(HexEmployee employee,HexNavNode goal,Vector3 spot,float seconds)
    {
        if(employee.path==null||employee.revision!=game.board.Layout.Revision||!employee.goal.Equals(goal))
        {employee.path=game.board.Layout.Path(new HexNavNode(employee.floor,HexShopLayout.Cell(employee.view.transform.position)),goal);employee.revision=game.board.Layout.Revision;employee.goal=goal;employee.waypoint=0;}
        if(employee.path==null){employee.statusKey="staff.status.blocked";return false;}
        if(employee.waypoint<employee.path.Count)
        {
            var node=employee.path[employee.waypoint];var center=HexBoardModel.World(node.cell)+Vector3.up*Options.kitchenOffset.y;
            if(node.floor!=employee.floor){employee.floor=node.floor;employee.view.transform.position=center;employee.waypoint++;}
            else{employee.view.transform.position=Vector3.MoveTowards(employee.view.transform.position,center,seconds*Options.walkSpeed);if(Vector3.Distance(employee.view.transform.position,center)<(HexTestSettings.Current?HexTestSettings.Current.guests.waypointDistance:.05f))employee.waypoint++;}
            employee.statusKey="staff.status.moving";return false;
        }
        employee.view.transform.position=Vector3.MoveTowards(employee.view.transform.position,spot,seconds*Options.walkSpeed);
        bool arrived=Vector3.Distance(employee.view.transform.position,spot)<Options.workDistance;
        if(!arrived)employee.statusKey="staff.status.moving";return arrived;
    }
    public void UpdateVisibility()
    {
        foreach(var employee in employees)if(employee.view)employee.view.gameObject.SetActive(employee.floor==game.board.CurrentFloor);
        // Keep targets registered on other floors so stair routes can still discover them.
        foreach(var target in HexStaffWorkTarget.Active)if(target&&target.view){target.view.body.gameObject.SetActive(target.floor==game.board.CurrentFloor);target.view.patienceCanvas.gameObject.SetActive(target.floor==game.board.CurrentFloor);}
    }
    public HexStaffWorkTarget SpawnTestTarget(bool disruption)
    {
        var prefab=disruption?Options.disruptionTestPrefab:Options.trashTestPrefab;if(!prefab)return null;
        var target=Instantiate(prefab,employeeRoot);target.floor=game.board.CurrentFloor;target.board=game.board;
        target.transform.position=HexBoardModel.World(game.board.Model.Root)+(disruption?Options.disruptionTestOffset:Options.trashTestOffset);if(target.view)target.view.ShowPatience(true,1);return target;
    }
    void OnDestroy(){foreach(var employee in employees)if(employee.task)employee.task.Release(employee.state.id);}
}

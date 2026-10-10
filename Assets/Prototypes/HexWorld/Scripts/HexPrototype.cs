using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class HexOwnedItem { public ItemDefinition definition; public int level; }
[Serializable] public sealed class HexTileReward { public ItemDefinition item; public HexTileDefinition tile; public int count = 1; }
[Serializable] public sealed class HexChoiceCard { public Image icon; public Text title, description; public Button button; }

public sealed class HexPrototype : MonoBehaviour
{
    public HexTileBoard board;
    public HexOrbitCamera orbit;
    public HexWorldActor customerPrefab;
    public Transform customerRoot;
    public ItemCatalog catalog;
    public GoldLevelCurve curve;
    public HexTileReward[] tileRewards;
    public Text goldLabel, levelLabel, serviceLabel, inventoryLabel, tileLabel, statusLabel;
    public Image cooldownBar, levelBar;
    public GameObject choicePanel, buildingPanel;
    public HexChoiceCard[] cards;
    public Button buildButton, endBuildButton, placeButton, moveButton, recoverButton, testGoldButton, resetCameraButton, continueButton;
    public Dropdown tileDropdown;
    public Slider pitchSlider, zoomSlider;
    [HideInInspector] public float spawnSeconds = 1.155f, serviceSeconds = 5, patienceSeconds = 10, visitorChance = .4f;
    [HideInInspector,Range(0, 1)] public float directQueueChance = .5f, browsingQueueChance = .35f;
    [HideInInspector,Min(.1f)] public float browseDecisionSeconds = 3;
    [HideInInspector] public int maxCustomers = 32;
    [Min(2)] public float waitingRadius = 2.3f;
    public HexFoodProjectile foodPrefab;
    public Transform foodOrigin;
    public HexFoodService service;
    public HexStaffSystem staffSystem;
    [SerializeField] int gold, level = 1;
    [SerializeField] long earnedGold;
    [SerializeField] List<HexOwnedItem> ownedItems = new List<HexOwnedItem>();
    public int Gold => gold;
    public long EarnedGold => earnedGold;
    public int Level => level;
    public bool IsBuilding { get; private set; }
    public bool IsChoosing { get; private set; }
    public bool IsShopping { get; private set; }
    public float BuffRemaining { get; private set; }
    public int CustomerCount => guests.Count;
    public long NextThreshold => curve.ThresholdForNextLevel(level);
    readonly List<ItemDefinition> offered = new List<ItemDefinition>();
    readonly List<HexTileDefinition> offeredTiles = new List<HexTileDefinition>();
    ItemDefinition pendingTileItem;
    public bool IsChoosingTile => pendingTileItem != null;
    public IReadOnlyList<HexTileDefinition> OfferedTiles => offeredTiles;
    readonly List<Guest> guests = new List<Guest>();
    readonly List<Guest> waitingGuests = new List<Guest>();

    long contactSequence;
    float spawnTimer, cooldown, speedBonus, patienceBonus, attractionBonus, beforePause = 1;
    bool paused;
    float temporarySpeedBonus;
    int additionalFoodCount;
    public int FoodPerThrow => Mathf.Clamp((HexTestSettings.Current?HexTestSettings.Current.service.baseFoodCount:1)+additionalFoodCount,1,4);
    public void ConfigureStart(int startingGold,int startingLevel){gold=Mathf.Max(0,startingGold);level=Mathf.Max(1,startingLevel);earnedGold=0;}
    static readonly HexTestSettings.GuestOptions DefaultGuests=new HexTestSettings.GuestOptions();
    static readonly HexTestSettings.ServiceOptions DefaultService=new HexTestSettings.ServiceOptions();
    HexTestSettings.GuestOptions GuestOptions => HexTestSettings.Current?HexTestSettings.Current.guests:DefaultGuests;
    HexTestSettings.ServiceOptions ServiceOptions => HexTestSettings.Current?HexTestSettings.Current.service:DefaultService;
    public float ReactiveVisitorChance => Mathf.Clamp01(visitorChance+attractionBonus);
    string messageKey = "hex.help.None";
    enum GuestState { Crossing, Browsing, Approaching, Waiting, Receiving, Lodging, Leaving, Eating }
    sealed class Guest
    {
        public HexWorldActor view; public GuestState state; public Vector3 destination;
        public float browse, patience, initialPatience, turnTime, targetZ;
        public int direction, originDirection; public bool interested;
        public int waitingSlot = -1; public long contactOrder;
        public int id, floor, targetFloor, seat=-1, reward=1, navigationRevision=-1, waypoint;
        public HexNavNode facility, navigationGoal;
        public bool hasFacility, room;
        public List<HexNavNode> path;
        public float stay; public GameObject meal;
    }
    public float ServiceInterval => serviceSeconds / Mathf.Max(.1f, 1 + speedBonus + temporarySpeedBonus);
    public int ItemLevel(ItemDefinition item) => ownedItems.Find(x => x.definition == item)?.level ?? 0;

    void Awake()
    {
        buildButton.onClick.AddListener(BeginBuilding); endBuildButton.onClick.AddListener(EndBuilding);
        placeButton.onClick.AddListener(() => board.SetMode(HexBuildMode.Place));
        moveButton.onClick.AddListener(() => board.SetMode(HexBuildMode.Move));
        recoverButton.onClick.AddListener(() => board.SetMode(HexBuildMode.Recover));
        testGoldButton.onClick.AddListener(() => AddGold(1)); resetCameraButton.onClick.AddListener(orbit.ResetView);
        continueButton.onClick.AddListener(CloseChoice);
        for (int i=0;i<cards.Length;i++) { int index=i; cards[i].button.onClick.AddListener(() => Choose(index)); }
        tileDropdown.onValueChanged.AddListener(board.SelectType);
        pitchSlider.onValueChanged.AddListener(orbit.SetPitch); zoomSlider.onValueChanged.AddListener(orbit.SetZoom);
        choicePanel.SetActive(false); buildingPanel.SetActive(false); cooldown = ServiceInterval;
        RefreshLanguage();
    }
    void OnEnable() { LocalizationService.LanguageChanged += RefreshLanguage; }
    void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLanguage;
        if (paused) { Time.timeScale = beforePause; paused = false; }
    }
    void OnDestroy() { foreach(var guest in guests)if(guest.meal)Destroy(guest.meal); }
    void Pause()
    {
        if (!paused) { beforePause = Time.timeScale; paused = true; Time.timeScale = 0; }
    }
    void Resume()
    {
        if (paused && !IsBuilding && !IsChoosing && !IsShopping) { Time.timeScale = beforePause; paused = false; }
    }
    public void SetMessage(string key) { messageKey = key; RefreshHUD(); }
    public void BeginBuilding()
    {
        if (IsChoosing || IsBuilding || IsShopping) return;
        IsBuilding = true; Pause(); buildingPanel.SetActive(true); board.SetMode(HexBuildMode.Place); RefreshHUD();
    }
    public void EndBuilding()
    {
        IsBuilding = false; buildingPanel.SetActive(false); board.SetMode(HexBuildMode.None); Resume(); RefreshHUD();
    }
    public void AddGold(int count) { if (count > 0) { gold = (int)Math.Min(int.MaxValue,(long)gold+count); earnedGold += count; } RefreshHUD(); }
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount; RefreshHUD(); return true;
    }
    public bool BeginShopping()
    {
        if (IsBuilding || IsChoosing || IsShopping) return false;
        IsShopping = true; Pause(); return true;
    }
    public void EndShopping() { IsShopping = false; Resume(); }
    public bool ActivateServiceBuff(float seconds, float bonus)
    {
        if (BuffRemaining > 0 || seconds <= 0 || bonus <= 0) return false;
        float previous = ServiceInterval; temporarySpeedBonus = bonus; BuffRemaining = seconds;
        cooldown *= ServiceInterval / previous; return true;
    }
    void Update()
    {
        RefreshHUD();
        pitchSlider.SetValueWithoutNotify(orbit.pitch); zoomSlider.SetValueWithoutNotify(orbit.zoom);
        if (!IsBuilding && !IsChoosing && !IsShopping && gold >= NextThreshold) OpenChoice();
        if (IsBuilding || IsChoosing || IsShopping || Time.deltaTime <= 0) return;
        float dt = Time.deltaTime; spawnTimer -= dt;
        if (BuffRemaining > 0)
        {
            BuffRemaining = Mathf.Max(0,BuffRemaining-dt);
            if (BuffRemaining == 0) { float previous=ServiceInterval; temporarySpeedBonus=0; cooldown*=ServiceInterval/previous; }
        }
        if (spawnTimer <= 0 && guests.Count < maxCustomers) { Spawn(); spawnTimer = spawnSeconds; }
        TickGuests(dt); if(staffSystem)staffSystem.Tick(dt); if(service)service.Tick(dt);
    }
    void Spawn()
    {
        int direction=UnityEngine.Random.value<.5f?1:-1;
        var view=Instantiate(customerPrefab,customerRoot);
        view.transform.position=new Vector3(-direction*GuestOptions.spawnX,GuestOptions.groundHeight,UnityEngine.Random.Range(-GuestOptions.spawnZ,GuestOptions.spawnZ));
        bool interested=UnityEngine.Random.value<ReactiveVisitorChance;
        var animation=view.GetComponent<HexCustomerAnimation>();
        if(!animation)animation=view.gameObject.AddComponent<HexCustomerAnimation>();
        bool animated=animation.Configure(view,HexTestSettings.Current);
        view.body.color=interested?(animated?Color.white:Color.HSVToRGB(UnityEngine.Random.value,HexTestSettings.Current?HexTestSettings.Current.visual.customerSaturation:.36f,HexTestSettings.Current?HexTestSettings.Current.visual.customerBrightness:.96f)):(HexTestSettings.Current?HexTestSettings.Current.visual.unresponsiveColor:new Color(0,0,0,.4f));
        view.waiting=false;view.patienceCanvas.gameObject.SetActive(false);
        view.gameObject.SetActive(board.CurrentFloor==0);
        bool browsing=interested&&UnityEngine.Random.value>=directQueueChance;
        guests.Add(new Guest{id=view.GetInstanceID(),view=view,direction=direction,originDirection=direction,interested=interested,
            state=!interested?GuestState.Crossing:browsing?GuestState.Browsing:GuestState.Approaching,
            browse=Mathf.Max(.1f,browseDecisionSeconds),
            destination=browsing?new Vector3(-direction*UnityEngine.Random.Range(GuestOptions.browseX.x,GuestOptions.browseX.y),GuestOptions.groundHeight,UnityEngine.Random.Range(-GuestOptions.browseZ,GuestOptions.browseZ)):new Vector3(direction*GuestOptions.destinationX,GuestOptions.groundHeight,view.transform.position.z),targetZ=view.transform.position.z});
    }
    Vector3 WaitingPosition(int slot)
    {
        var cells=StandingCells();if(cells.Count==0)return Vector3.zero;
        var cell=cells[Mathf.Clamp(slot/GuestOptions.standingSlotsPerCell,0,cells.Count-1)];float angle=(slot%GuestOptions.standingSlotsPerCell)*Mathf.PI*2/GuestOptions.standingSlotsPerCell;
        return HexBoardModel.World(cell)+new Vector3(Mathf.Cos(angle)*GuestOptions.standingRadius,GuestOptions.groundHeight,Mathf.Sin(angle)*GuestOptions.standingRadius);
    }
    List<Vector2Int> StandingCells()
    {
        var cells=new List<Vector2Int>();var model=board.Layout.Floor(0).model;
        foreach(var cell in model.Owned){var definition=model.Definition(cell);if(!definition||definition.kind==HexTileKind.DisplayShelf||definition.kind==HexTileKind.Entrance||definition.kind==HexTileKind.Storage)cells.Add(cell);}
        // Outdoor service spots must touch an open shop boundary, never an arbitrary exterior cell.
        var outside=new HashSet<Vector2Int>();
        foreach(var cell in model.Owned)for(int d=0;d<6;d++)
        {
            var neighbour=cell+HexBoardModel.Directions[d];
            if(!model.IsOwned(neighbour)&&!board.Layout.HasWall(0,cell,d))outside.Add(neighbour);
        }
        cells.AddRange(outside);
        cells.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));return cells;
    }
    bool HasAvailableTable(HexNavNode start)
    {
        foreach(var node in board.Layout.OwnedNodes())
        {
            var f=board.Layout.Floor(node.floor);
            if(f.model.Definition(node.cell)?.kind==HexTileKind.Table&&f.use.TryGetValue(node.cell,out var use)&&use.Count<use.capacity&&board.Layout.Path(start,node)!=null)return true;
        }
        return false;
    }
    void ReserveWaitingPosition(Guest guest)
    {
        if(guest.waitingSlot>=0)return;
        var used=new HashSet<int>();
        foreach(var other in guests)if(other!=guest&&other.waitingSlot>=0&&other.state!=GuestState.Leaving)used.Add(other.waitingSlot);
        float distance=float.PositiveInfinity;
        var start=new HexNavNode(guest.floor,HexShopLayout.Cell(guest.view.transform.position));
        bool tableAvailable=HasAvailableTable(start);
        for(int slot=0;slot<StandingCells().Count*GuestOptions.standingSlotsPerCell;slot++)
        {
            if(used.Contains(slot))continue;
            bool occupied=false;Vector3 spot=WaitingPosition(slot);
            if(tableAvailable&&!board.Layout.Floor(0).model.IsOwned(HexShopLayout.Cell(spot)))continue;
            foreach(var other in guests)if(other!=guest&&other.view&&(other.state==GuestState.Waiting||other.state==GuestState.Eating)&&Vector3.Distance(other.view.transform.position,spot)<GuestOptions.separation){occupied=true;break;}
            if(occupied)continue;
            float candidate=(WaitingPosition(slot)-guest.view.transform.position).sqrMagnitude;
            if(candidate<distance&&board.Layout.Path(new HexNavNode(guest.floor,HexShopLayout.Cell(guest.view.transform.position)),new HexNavNode(0,HexShopLayout.Cell(WaitingPosition(slot))))!=null){distance=candidate;guest.waitingSlot=slot;}
        }
    }
    void JoinWaiting(Guest guest)
    {
        guest.view.seated=guest.hasFacility&&!guest.room;
        var definition=guest.hasFacility?board.Layout.Floor(guest.facility.floor).model.Definition(guest.facility.cell):null;
        if(guest.room){guest.state=GuestState.Lodging;guest.stay=guest.initialPatience=definition.lodgingSeconds;guest.view.ShowPatience(true,1);return;}
        guest.state=GuestState.Waiting;guest.reward=definition?definition.serviceGold:ServiceOptions.standingGold;
        guest.patience=guest.initialPatience=Mathf.Max(.1f,(definition?definition.waitingSeconds:patienceSeconds)+patienceBonus);
        guest.view.patienceFraction=1;
        guest.view.ShowPatience(true,1);
        guest.contactOrder=++contactSequence;
        waitingGuests.Add(guest);
    }
    void TickGuests(float dt)
    {
        for (int i=guests.Count-1;i>=0;i--)
        {
            var guest=guests[i];
            if(!guest.view){if(guest.meal)Destroy(guest.meal);board.Layout.Release(guest.id);waitingGuests.Remove(guest);guests.RemoveAt(i);continue;}
            if(guest.state==GuestState.Eating){guest.stay-=dt;if(guest.stay<=0)Leave(guest,guest.originDirection);}
            if(guest.state==GuestState.Lodging)
            {
                guest.stay-=dt;guest.view.ShowPatience(true,guest.stay/guest.initialPatience);
                if(guest.stay<=0){int reward=board.Layout.Floor(guest.facility.floor).model.Definition(guest.facility.cell).lodgingGold;Leave(guest,guest.originDirection);AddGold(reward);}
            }
            if(guest.state==GuestState.Waiting)
            {
                if(guest.hasFacility)guest.destination=board.Layout.SeatPosition(guest.facility,guest.seat);
                guest.patience-=dt; guest.view.patienceFraction=guest.patience/guest.initialPatience;
                if(guest.patience<=0){waitingGuests.Remove(guest);Leave(guest,guest.originDirection);}
            }
            else if(guest.state==GuestState.Browsing)
            {
                if(Vector3.Distance(guest.view.transform.position,guest.destination)<.25f)
                {
                    guest.browse-=dt;
                    if(guest.browse<=0)
                    {
                        if(UnityEngine.Random.value<Mathf.Clamp01(browsingQueueChance)) guest.state=GuestState.Approaching;
                        else
                        {
                            guest.destination=new Vector3(UnityEngine.Random.value<.5f?-UnityEngine.Random.Range(GuestOptions.browseX.x,GuestOptions.browseX.y):UnityEngine.Random.Range(GuestOptions.browseX.x,GuestOptions.browseX.y),GuestOptions.groundHeight,UnityEngine.Random.Range(-GuestOptions.browseZ,GuestOptions.browseZ));
                            guest.browse=Mathf.Max(.1f,browseDecisionSeconds);
                        }
                    }
                }
            }
            if(guest.state==GuestState.Approaching)
            {
                if(!guest.hasFacility&&guest.waitingSlot<0)
                {
                    var start=new HexNavNode(guest.floor,HexShopLayout.Cell(guest.view.transform.position));HexNavNode facility=default;int seat=-1;
                    guest.room=UnityEngine.Random.value<GuestOptions.lodgingChance&&board.Layout.Reserve(guest.view.GetInstanceID(),HexTileKind.Lodging,start,out facility,out seat);
                    if(guest.room||board.Layout.Reserve(guest.view.GetInstanceID(),HexTileKind.Table,start,out facility,out seat)){guest.hasFacility=true;guest.facility=facility;guest.seat=seat;guest.targetFloor=facility.floor;}
                    else ReserveWaitingPosition(guest);
                }
                if(!guest.hasFacility&&guest.waitingSlot<0)
                {
                    guest.state=GuestState.Browsing;guest.browse=GuestOptions.fullAreaRetrySeconds;
                    guest.destination=new Vector3(-guest.originDirection*UnityEngine.Random.Range(GuestOptions.browseX.x,GuestOptions.browseX.y),GuestOptions.groundHeight,UnityEngine.Random.Range(-GuestOptions.browseZ,GuestOptions.browseZ));
                    continue;
                }
                guest.destination=guest.hasFacility?board.Layout.SeatPosition(guest.facility,guest.seat):WaitingPosition(Mathf.Max(0,guest.waitingSlot));
                // Contact begins after arriving inside an owned tile.
                if((guest.hasFacility||guest.waitingSlot>=0)&&guest.floor==guest.targetFloor&&Vector3.Distance(guest.view.transform.position,guest.destination)<GuestOptions.contactDistance) JoinWaiting(guest);
            }
            else if(guest.state==GuestState.Crossing||guest.state==GuestState.Leaving)
            {
                guest.turnTime-=dt;
                if(guest.turnTime<=0){guest.turnTime=UnityEngine.Random.Range(GuestOptions.turnSeconds.x,GuestOptions.turnSeconds.y);guest.targetZ=Mathf.Clamp(guest.destination.z+UnityEngine.Random.Range(-GuestOptions.verticalDeviation,GuestOptions.verticalDeviation),-GuestOptions.spawnZ,GuestOptions.spawnZ);}
                guest.destination.z=Mathf.MoveTowards(guest.destination.z,guest.targetZ,dt*GuestOptions.verticalSpeed);
                if(Mathf.Abs(guest.destination.z)<GuestOptions.stallClearance) guest.destination.z=guest.destination.z<0?-GuestOptions.stallClearance:GuestOptions.stallClearance;
            }
            MoveGuest(guest,guest.destination,dt);
            if(guest.state!=GuestState.Lodging)guest.view.ShowPatience(guest.state==GuestState.Waiting||guest.state==GuestState.Receiving,
                guest.initialPatience>0?guest.patience/guest.initialPatience:1);
            if(guest.floor==0&&(guest.state==GuestState.Leaving||guest.state==GuestState.Crossing))
                if(Mathf.Abs(guest.view.transform.position.x)>GuestOptions.despawnX){Destroy(guest.view.gameObject);guests.RemoveAt(i);}
        }
    }
    void Leave(Guest guest,int direction)
    {
        if(guest.meal){Destroy(guest.meal);guest.meal=null;}
        board.Layout.Release(guest.view.GetInstanceID());guest.hasFacility=false;guest.targetFloor=0;
        guest.view.seated=false;
        guest.state=GuestState.Leaving;guest.view.waiting=false;guest.waitingSlot=-1;
        guest.view.ShowPatience(false,guest.view.patienceFraction);
        guest.destination=new Vector3(direction*GuestOptions.destinationX,GuestOptions.groundHeight,guest.view.transform.position.z);guest.direction=direction;
    }
    void MoveGuest(Guest guest,Vector3 target,float dt)
    {
        if(guest.state==GuestState.Waiting||guest.state==GuestState.Receiving||guest.state==GuestState.Lodging||guest.state==GuestState.Eating)return;
        var goal=new HexNavNode(guest.targetFloor,HexShopLayout.Cell(target));
        if(guest.navigationRevision!=board.Layout.Revision||!guest.navigationGoal.Equals(goal)||guest.path==null)
        {
            guest.navigationGoal=goal;guest.navigationRevision=board.Layout.Revision;guest.waypoint=0;
            guest.path=board.Layout.Path(new HexNavNode(guest.floor,HexShopLayout.Cell(guest.view.transform.position)),goal);
        }
        if(guest.path==null)
        {
            // A wandering destination may lie inside an enclosed, unowned courtyard.
            // Decide again from the reachable current position rather than stalling there.
            if(guest.state==GuestState.Browsing){guest.destination=guest.view.transform.position;guest.browse=Mathf.Min(guest.browse,1f);}
            return;
        }
        // Spend this frame's complete distance budget, even when crossing a waypoint.
        // Discarding the remainder caused periodic slow frames and idle-animation resets.
        float remaining=Mathf.Max(0,dt*GuestOptions.walkSpeed);
        while(guest.waypoint<guest.path.Count)
        {
            var next=guest.path[guest.waypoint];
            if(next.floor!=guest.floor){guest.floor=next.floor;guest.view.transform.position=HexBoardModel.World(next.cell)+Vector3.up*GuestOptions.groundHeight;guest.view.gameObject.SetActive(guest.floor==board.CurrentFloor);guest.waypoint++;return;}
            Vector3 point=HexBoardModel.World(next.cell)+Vector3.up*GuestOptions.groundHeight;
            float distance=Vector3.Distance(guest.view.transform.position,point);
            if(distance<=Mathf.Max(.00001f,GuestOptions.waypointDistance)){guest.waypoint++;continue;}
            if(remaining<=0)return;
            float travel=Mathf.Min(distance,remaining);
            guest.view.transform.position=Vector3.MoveTowards(guest.view.transform.position,point,travel);
            remaining-=travel;
            if(travel<distance)return;
            guest.waypoint++;
        }
        if(remaining>0)guest.view.transform.position=Vector3.MoveTowards(guest.view.transform.position,target,remaining);
    }

    public void UpdateFloorVisibility()
    {
        foreach(var guest in guests)if(guest.view)guest.view.gameObject.SetActive(guest.floor==board.CurrentFloor);
        foreach(var guest in guests)if(guest.meal)guest.meal.SetActive(guest.floor==board.CurrentFloor);
        if(service)service.UpdateVisibility();
        if(staffSystem)staffSystem.UpdateVisibility();
    }
    public bool FindRecruitGuest(Vector3 from,int floor,float radius,out int id,out HexNavNode node,out Vector3 spot)
    {
        id=0;node=default;spot=default;float nearest=radius;
        foreach(var guest in guests)
        {
            if(!guest.view||guest.floor!=floor||!guest.interested||guest.state!=GuestState.Browsing)continue;
            float distance=Vector3.Distance(from,guest.view.transform.position);
            var target=new HexNavNode(floor,HexShopLayout.Cell(guest.view.transform.position));
            if(distance<nearest&&board.Layout.Path(new HexNavNode(floor,HexShopLayout.Cell(from)),target)!=null)
            {nearest=distance;id=guest.id;node=target;spot=guest.view.transform.position;}
        }
        return id!=0;
    }
    public bool PersuadeGuest(int id)
    {
        var guest=guests.Find(g=>g.id==id);
        if(guest==null||!guest.view||!guest.interested||guest.state!=GuestState.Browsing)return false;
        guest.state=GuestState.Approaching;guest.path=null;guest.browse=0;return true;
    }
    public bool EjectWaitingGuest(Vector3 from,int floor,float radius)
    {
        Guest selected=null;float nearest=radius;
        foreach(var guest in waitingGuests)
        {
            if(!guest.view||guest.floor!=floor||guest.state!=GuestState.Waiting)continue;
            float distance=Vector3.Distance(from,guest.view.transform.position);
            if(distance<nearest&&board.Layout.Path(new HexNavNode(floor,HexShopLayout.Cell(from)),new HexNavNode(floor,HexShopLayout.Cell(guest.view.transform.position)))!=null){selected=guest;nearest=distance;}
        }
        if(selected==null)return false;waitingGuests.Remove(selected);Leave(selected,selected.originDirection);return true;
    }
    public bool NextService(HexNavNode start,out int id,out HexNavNode node)
    {
        id=0;node=default;Guest oldest=null;
        foreach(var guest in waitingGuests)
        {
            if(!ServiceTarget(guest.id,out var candidate,out var spot)||ServicePath(start,candidate)==null)continue;
            if(oldest==null||guest.contactOrder<oldest.contactOrder){oldest=guest;node=candidate;}
        }
        if(oldest==null)return false;id=oldest.id;return true;
    }
    public bool ServiceTarget(int id,out HexNavNode node,out Vector3 position)
    {
        node=default;position=default;var guest=guests.Find(g=>g.id==id);
        if(guest==null||!guest.view||guest.state!=GuestState.Waiting)return false;
        position=guest.view.transform.position;node=new HexNavNode(guest.floor,HexShopLayout.Cell(position));
        if(!board.Layout.floors.ContainsKey(node.floor))return false;
        if(board.Layout.Floor(node.floor).model.IsOwned(node.cell))return true;
        return node.floor==0&&!guest.hasFacility&&guest.waitingSlot>=0&&StandingCells().Contains(node.cell);
    }
    public List<HexNavNode> ServicePath(HexNavNode start,HexNavNode destination)
    {
        if(!board.Layout.floors.ContainsKey(start.floor)||!board.Layout.floors.ContainsKey(destination.floor))return null;
        bool interior=board.Layout.Floor(start.floor).model.IsOwned(start.cell)&&board.Layout.Floor(destination.floor).model.IsOwned(destination.cell);
        return board.Layout.Path(start,destination,false,interior);
    }
    public bool CompleteService(int id,SpriteRenderer mealPrefab)
    {
        var guest=guests.Find(g=>g.id==id);
        if(!ServiceTarget(id,out var node,out var spot)||!service||service.Floor!=node.floor||Vector3.Distance(service.Position,spot)>ServiceOptions.deliveryDistance)return false;
        waitingGuests.Remove(guest);AddGold(guest.reward);
        if(guest.hasFacility)
        {
            guest.state=GuestState.Eating;guest.stay=ServiceOptions.eatingSeconds;guest.view.ShowPatience(false,1);
            if(mealPrefab){var meal=Instantiate(mealPrefab);float angle=guest.seat*Mathf.PI*2/board.Layout.Floor(node.floor).use[node.cell].capacity;meal.transform.position=HexBoardModel.World(node.cell)+new Vector3(Mathf.Cos(angle)*ServiceOptions.mealRadius,ServiceOptions.mealHeight,Mathf.Sin(angle)*ServiceOptions.mealRadius);guest.meal=meal.gameObject;guest.meal.SetActive(node.floor==board.CurrentFloor);}
        }
        else Leave(guest,guest.originDirection);
        return true;
    }
    void OpenChoice()
    {
        pendingTileItem=null;offeredTiles.Clear();
        offered.Clear(); var eligible=new List<ItemDefinition>(); var keys=new HashSet<string>();
        foreach(var item in catalog.items)if(item&&keys.Add(item.key)&&(item.maxLevel<=0||ItemLevel(item)<item.maxLevel))eligible.Add(item);
        while(offered.Count<(HexTestSettings.Current?HexTestSettings.Current.start.rewardChoices:3)&&eligible.Count>0){int index=UnityEngine.Random.Range(0,eligible.Count);offered.Add(eligible[index]);eligible.RemoveAt(index);}
        level++;IsChoosing=true;Pause();choicePanel.SetActive(true); RefreshCards();
    }
    public void Choose(int index)
    {
        if(IsChoosing&&IsChoosingTile)
        {
            if(index<0||index>=offeredTiles.Count)return;
            board.Grant(offeredTiles[index],1);
            GrantItemInternal(pendingTileItem,false);
            pendingTileItem=null;offeredTiles.Clear();CloseChoice();return;
        }
        if(!IsChoosing||index<0||index>=offered.Count)return;
        if(offered[index].kind==ItemKind.ShopPart)
        {
            var unique=new HashSet<string>();var pool=new List<HexTileDefinition>();
            foreach(var tile in board.tileTypes)if(tile&&unique.Add(tile.key))pool.Add(tile);
            if(pool.Count>0)
            {
                pendingTileItem=offered[index];offeredTiles.Clear();
                while(offeredTiles.Count<(HexTestSettings.Current?HexTestSettings.Current.start.rewardChoices:3)&&pool.Count>0){int selected=UnityEngine.Random.Range(0,pool.Count);offeredTiles.Add(pool[selected]);pool.RemoveAt(selected);}
                RefreshCards();return;
            }
        }
        GrantItem(offered[index]);CloseChoice();
    }
    public bool CanGrantItem(ItemDefinition item) => item && (item.maxLevel<=0 || ItemLevel(item)<item.maxLevel);
    public bool GrantItem(ItemDefinition item) => GrantItemInternal(item,true);
    bool GrantItemInternal(ItemDefinition item,bool grantMappedTile)
    {
        if(!CanGrantItem(item))return false;
        var owned=ownedItems.Find(x=>x.definition==item);
        if(owned==null){owned=new HexOwnedItem{definition=item};ownedItems.Add(owned);}owned.level++;
        if(grantMappedTile&&item.kind==ItemKind.ShopPart)foreach(var reward in tileRewards)if(reward.item==item)board.Grant(reward.tile,reward.count);
        ApplyEffects();RefreshHUD();return true;
    }
    void ApplyEffects()
    {
        float oldInterval=ServiceInterval;speedBonus=patienceBonus=attractionBonus=0;additionalFoodCount=0;
        foreach(var owned in ownedItems)foreach(var effect in owned.definition.effects)
        {
            float amount=effect.amountPerLevel*owned.level;
            if(effect.kind==ItemEffectKind.ServiceSpeedPercent)speedBonus+=amount;
            if(effect.kind==ItemEffectKind.CustomerPatienceSeconds)patienceBonus+=amount;
            if(effect.kind==ItemEffectKind.VisitorChance)attractionBonus+=amount;
            if(effect.kind==ItemEffectKind.FoodThrowCount)additionalFoodCount+=Mathf.Max(0,Mathf.RoundToInt(amount));
        }
        cooldown*=ServiceInterval/oldInterval;
    }
    public void CloseChoice(){if(IsChoosingTile)return;IsChoosing=false;choicePanel.SetActive(false);Resume();RefreshHUD();}
    void RefreshCards()
    {
        foreach(var label in choicePanel.GetComponentsInChildren<LocalizedLabel>(true))
        {
            if(label.gameObject.name=="Title")label.key=IsChoosingTile?"hex.tile.choice.title":"hex.choice.title";
            if(label.gameObject.name=="Explanation")label.key=IsChoosingTile?"hex.tile.choice.help":"hex.choice.help";
            label.GetComponent<Text>().text=LocalizationService.Text(label.key);
        }
        for(int i=0;i<cards.Length;i++)
        {
            int count=IsChoosingTile?offeredTiles.Count:offered.Count;
            cards[i].button.gameObject.SetActive(i<count);
            if(IsChoosingTile)
            {
                if(i>=count)continue;
                var tile=offeredTiles[i];cards[i].icon.sprite=tile.icon;
                cards[i].title.text=LocalizationService.Text(tile.nameKey);
                cards[i].description.text=LocalizationService.Text(tile.descriptionKey);continue;
            }
            if(i>=offered.Count)continue;
            var item=offered[i];cards[i].icon.sprite=item.picture;
            cards[i].title.text=LocalizationService.Text("hex.item.title","name",item.kind==ItemKind.ShopPart?LocalizationService.Text("hex.tile.reward.name"):item.DisplayName,"kind",LocalizationService.Text("item.kind."+item.kind),"level",ItemLevel(item)+1);
            cards[i].description.text=item.kind==ItemKind.ShopPart?LocalizationService.Text("hex.tile.reward.description"):item.DisplayDescription();
        }
        continueButton.gameObject.SetActive(!IsChoosingTile&&offered.Count==0);
    }
    void RefreshLanguage()
    {
        if(!board||!tileDropdown)return;
        var names=new List<string>();foreach(var type in board.tileTypes)names.Add(LocalizationService.Text(type.nameKey));
        tileDropdown.ClearOptions();tileDropdown.AddOptions(names);tileDropdown.SetValueWithoutNotify(board.SelectedType);
        if(IsChoosing)RefreshCards();RefreshHUD();
    }
    void RefreshHUD()
    {
        if(!goldLabel||board.Model==null)return;
        goldLabel.text=LocalizationService.Text("hud.gold","gold",gold);
        levelLabel.text=LocalizationService.Text("hex.level","level",level,"next",NextThreshold,"gold",gold);
        levelBar.fillAmount=Mathf.Clamp01((float)gold/NextThreshold);
        serviceLabel.text=LocalizationService.Text("service.summary","queue",waitingGuests.Count,"ready",service?service.Food.ReadyCount:0,"count",FoodPerThrow);
        if(service){service.RefreshGauges();cooldownBar.fillAmount=service.Food.Count>0?service.Food[0]:0;}
        tileLabel.text=LocalizationService.Text("hex.stock","name",LocalizationService.Text(board.Selected.nameKey),"stock",board.Model.Stock(board.Selected),"owned",board.Model.OwnedCount);
        statusLabel.text=LocalizationService.Text(messageKey);
        var inventory=new StringBuilder();
        foreach(var item in ownedItems)inventory.AppendLine(LocalizationService.Text("inventory.row","name",item.definition.DisplayName,"level",item.level));
        inventoryLabel.text=inventory.Length==0?LocalizationService.Text("inventory.empty","gold",NextThreshold):inventory.ToString();
        buildButton.interactable=!IsBuilding&&!IsChoosing&&!IsShopping;testGoldButton.interactable=!IsBuilding&&!IsChoosing&&!IsShopping;
    }
}

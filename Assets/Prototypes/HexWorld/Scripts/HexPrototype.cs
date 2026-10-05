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
    public float spawnSeconds = 1.155f, serviceSeconds = 5, patienceSeconds = 10, visitorChance = .4f;
    [Range(0, 1)] public float directQueueChance = .5f, browsingQueueChance = .35f;
    [Min(.1f)] public float browseDecisionSeconds = 3;
    public int maxCustomers = 32;
    [Min(2)] public float waitingRadius = 2.3f;
    public HexFoodProjectile foodPrefab;
    public Transform foodOrigin;
    sealed class FoodDelivery { public Guest recipient; public HexFoodProjectile projectile; }
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
    readonly List<Guest> guests = new List<Guest>();
    readonly List<Guest> waitingGuests = new List<Guest>();
    readonly List<FoodDelivery> deliveries = new List<FoodDelivery>();
    long contactSequence;
    float spawnTimer, cooldown, speedBonus, patienceBonus, attractionBonus, beforePause = 1;
    bool paused;
    float temporarySpeedBonus;
    int additionalFoodCount;
    public int FoodPerThrow => 1+additionalFoodCount;
    public float ReactiveVisitorChance => Mathf.Clamp01(visitorChance+attractionBonus);
    string messageKey = "hex.help.None";
    enum GuestState { Crossing, Browsing, Approaching, Waiting, Receiving, Lodging, Leaving }
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
        public float stay;
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
    void OnDestroy()
    {
        foreach(var delivery in deliveries)if(delivery.projectile)Destroy(delivery.projectile.gameObject);
    }
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
        TickGuests(dt); cooldown = Mathf.Max(0, cooldown - dt);
        TickDeliveries(dt);
        if (cooldown <= 0 && waitingGuests.Count > 0 && foodPrefab && foodOrigin)
            ThrowFood();
    }
    void Spawn()
    {
        int direction=UnityEngine.Random.value<.5f?1:-1;
        var view=Instantiate(customerPrefab,customerRoot);
        view.transform.position=new Vector3(-direction*11,.22f,UnityEngine.Random.Range(-6f,6f));
        bool interested=UnityEngine.Random.value<ReactiveVisitorChance;
        view.body.color=interested?Color.HSVToRGB(UnityEngine.Random.value,.36f,.96f):new Color(0,0,0,.4f);
        view.waiting=false;view.patienceCanvas.gameObject.SetActive(false);
        view.gameObject.SetActive(board.CurrentFloor==0);
        bool browsing=interested&&UnityEngine.Random.value>=directQueueChance;
        guests.Add(new Guest{id=view.GetInstanceID(),view=view,direction=direction,originDirection=direction,interested=interested,
            state=!interested?GuestState.Crossing:browsing?GuestState.Browsing:GuestState.Approaching,
            browse=Mathf.Max(.1f,browseDecisionSeconds),
            destination=browsing?new Vector3(-direction*UnityEngine.Random.Range(2.7f,5f),.22f,UnityEngine.Random.Range(-3f,3f)):new Vector3(direction*12,.22f,view.transform.position.z),targetZ=view.transform.position.z});
    }
    void ThrowFood()
    {
        for(int count=0;count<FoodPerThrow&&waitingGuests.Count>0;count++) ThrowOneFood();
    }
    void ThrowOneFood()
    {
        Guest recipient=null;
        foreach(var guest in waitingGuests)
            if(guest.view&&(recipient==null||guest.contactOrder<recipient.contactOrder))recipient=guest;
        if(recipient==null)return;
        waitingGuests.Remove(recipient);recipient.state=GuestState.Receiving;
        recipient.view.ShowPatience(true,recipient.patience/recipient.initialPatience);
        Vector3 origin=recipient.floor==0?foodOrigin.position:HexBoardModel.World(board.Layout.Floor(recipient.floor).model.Root)+Vector3.up*1.4f;
        var food=Instantiate(foodPrefab,origin,Quaternion.identity);
        food.Launch(origin,recipient.view.body.transform);food.gameObject.SetActive(board.CurrentFloor==recipient.floor);
        deliveries.Add(new FoodDelivery{recipient=recipient,projectile=food});cooldown=ServiceInterval;
    }
    void TickDeliveries(float dt)
    {
        for(int i=deliveries.Count-1;i>=0;i--)
        {
            var delivery=deliveries[i];
            if(!delivery.recipient.view||!delivery.projectile)
            {
                if(delivery.projectile)Destroy(delivery.projectile.gameObject);
                board.Layout.Release(delivery.recipient.id);waitingGuests.Remove(delivery.recipient);guests.Remove(delivery.recipient);
                deliveries.RemoveAt(i);continue;
            }
            if(!delivery.projectile.Advance(dt))continue;
            Leave(delivery.recipient,delivery.recipient.originDirection);
            AddGold(delivery.recipient.reward);Destroy(delivery.projectile.gameObject);deliveries.RemoveAt(i);
        }
    }
    Vector3 WaitingPosition(int slot)
    {
        float angle=(slot%12)*Mathf.PI/6;float radius=waitingRadius+(slot/12)*.95f;
        return new Vector3(Mathf.Cos(angle)*radius,.22f,Mathf.Sin(angle)*radius);
    }
    void ReserveWaitingPosition(Guest guest)
    {
        if(guest.waitingSlot>=0)return;
        var used=new HashSet<int>();
        foreach(var other in guests)if(other!=guest&&other.waitingSlot>=0&&other.state!=GuestState.Leaving)used.Add(other.waitingSlot);
        float distance=float.PositiveInfinity;
        for(int slot=0;slot<(guests.Count/12+1)*12;slot++)
        {
            if(used.Contains(slot))continue;
            float candidate=(WaitingPosition(slot)-guest.view.transform.position).sqrMagnitude;
            if(candidate<distance&&board.Layout.Path(new HexNavNode(guest.floor,HexShopLayout.Cell(guest.view.transform.position)),new HexNavNode(0,HexShopLayout.Cell(WaitingPosition(slot))))!=null){distance=candidate;guest.waitingSlot=slot;}
        }
    }
    void JoinWaiting(Guest guest)
    {
        guest.view.seated=guest.hasFacility&&!guest.room;
        var definition=guest.hasFacility?board.Layout.Floor(guest.facility.floor).model.Definition(guest.facility.cell):null;
        if(guest.room){guest.state=GuestState.Lodging;guest.stay=guest.initialPatience=definition.lodgingSeconds;guest.view.ShowPatience(true,1);return;}
        guest.state=GuestState.Waiting;guest.reward=definition?definition.serviceGold:1;
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
            if(!guest.view){board.Layout.Release(guest.id);waitingGuests.Remove(guest);guests.RemoveAt(i);continue;}
            if(guest.state==GuestState.Lodging)
            {
                guest.stay-=dt;guest.view.ShowPatience(true,guest.stay/guest.initialPatience);
                if(guest.stay<=0){int reward=board.Layout.Floor(guest.facility.floor).model.Definition(guest.facility.cell).lodgingGold;Leave(guest,guest.originDirection);AddGold(reward);}
            }
            if(guest.state==GuestState.Waiting)
            {
                guest.destination=guest.hasFacility?board.Layout.SeatPosition(guest.facility,guest.seat):WaitingPosition(guest.waitingSlot);
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
                            guest.destination=new Vector3(UnityEngine.Random.value<.5f?-UnityEngine.Random.Range(2.7f,5f):UnityEngine.Random.Range(2.7f,5f),.22f,UnityEngine.Random.Range(-3f,3f));
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
                    guest.room=UnityEngine.Random.value<.25f&&board.Layout.Reserve(guest.view.GetInstanceID(),HexTileKind.Lodging,start,out facility,out seat);
                    if(guest.room||board.Layout.Reserve(guest.view.GetInstanceID(),HexTileKind.Table,start,out facility,out seat)){guest.hasFacility=true;guest.facility=facility;guest.seat=seat;guest.targetFloor=facility.floor;}
                    else ReserveWaitingPosition(guest);
                }
                guest.destination=guest.hasFacility?board.Layout.SeatPosition(guest.facility,guest.seat):WaitingPosition(Mathf.Max(0,guest.waitingSlot));
                // First contact is arrival at the reserved position around the shop.
                if(guest.floor==guest.targetFloor&&Vector3.Distance(guest.view.transform.position,guest.destination)<.2f) JoinWaiting(guest);
            }
            else if(guest.state==GuestState.Crossing||guest.state==GuestState.Leaving)
            {
                guest.turnTime-=dt;
                if(guest.turnTime<=0){guest.turnTime=UnityEngine.Random.Range(1f,3f);guest.targetZ=Mathf.Clamp(guest.destination.z+UnityEngine.Random.Range(-1f,1f),-6,6);}
                guest.destination.z=Mathf.MoveTowards(guest.destination.z,guest.targetZ,dt*.5f);
                if(Mathf.Abs(guest.destination.z)<1.6f) guest.destination.z=guest.destination.z<0?-1.6f:1.6f;
            }
            MoveGuest(guest,guest.destination,dt);
            if(guest.state!=GuestState.Lodging)guest.view.ShowPatience(guest.state==GuestState.Waiting||guest.state==GuestState.Receiving,
                guest.initialPatience>0?guest.patience/guest.initialPatience:1);
            if(guest.floor==0&&(guest.state==GuestState.Leaving||guest.state==GuestState.Crossing))
                if(Mathf.Abs(guest.view.transform.position.x)>11.8f){Destroy(guest.view.gameObject);guests.RemoveAt(i);}
        }
    }
    void Leave(Guest guest,int direction)
    {
        board.Layout.Release(guest.view.GetInstanceID());guest.hasFacility=false;guest.targetFloor=0;
        guest.view.seated=false;
        guest.state=GuestState.Leaving;guest.view.waiting=false;guest.waitingSlot=-1;
        guest.view.ShowPatience(false,guest.view.patienceFraction);
        guest.destination=new Vector3(direction*12,.22f,guest.view.transform.position.z);guest.direction=direction;
    }
    void MoveGuest(Guest guest,Vector3 target,float dt)
    {
        if(guest.state==GuestState.Waiting||guest.state==GuestState.Receiving||guest.state==GuestState.Lodging)return;
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
        if(guest.waypoint<guest.path.Count)
        {
            var next=guest.path[guest.waypoint];
            if(next.floor!=guest.floor){guest.floor=next.floor;guest.view.transform.position=HexBoardModel.World(next.cell)+Vector3.up*.22f;guest.view.gameObject.SetActive(guest.floor==board.CurrentFloor);guest.waypoint++;return;}
            Vector3 point=HexBoardModel.World(next.cell)+Vector3.up*.22f;
            guest.view.transform.position=Vector3.MoveTowards(guest.view.transform.position,point,dt*1.9f);
            if(Vector3.Distance(guest.view.transform.position,point)<.05f)guest.waypoint++;
        }
        else guest.view.transform.position=Vector3.MoveTowards(guest.view.transform.position,target,dt*1.9f);
    }
    public void UpdateFloorVisibility()
    {
        foreach(var guest in guests)if(guest.view)guest.view.gameObject.SetActive(guest.floor==board.CurrentFloor);
        foreach(var delivery in deliveries)if(delivery.projectile)delivery.projectile.gameObject.SetActive(delivery.recipient.floor==board.CurrentFloor);
    }
    void OpenChoice()
    {
        offered.Clear(); var eligible=new List<ItemDefinition>(); var keys=new HashSet<string>();
        foreach(var item in catalog.items)if(item&&keys.Add(item.key)&&(item.maxLevel<=0||ItemLevel(item)<item.maxLevel))eligible.Add(item);
        while(offered.Count<3&&eligible.Count>0){int index=UnityEngine.Random.Range(0,eligible.Count);offered.Add(eligible[index]);eligible.RemoveAt(index);}
        level++;IsChoosing=true;Pause();choicePanel.SetActive(true); RefreshCards();
    }
    public void Choose(int index)
    {
        if(!IsChoosing||index<0||index>=offered.Count)return;
        GrantItem(offered[index]);CloseChoice();
    }
    public bool CanGrantItem(ItemDefinition item) => item && (item.maxLevel<=0 || ItemLevel(item)<item.maxLevel);
    public bool GrantItem(ItemDefinition item)
    {
        if(!CanGrantItem(item))return false;
        var owned=ownedItems.Find(x=>x.definition==item);
        if(owned==null){owned=new HexOwnedItem{definition=item};ownedItems.Add(owned);}owned.level++;
        if(item.kind==ItemKind.ShopPart)foreach(var reward in tileRewards)if(reward.item==item)board.Grant(reward.tile,reward.count);
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
    public void CloseChoice(){IsChoosing=false;choicePanel.SetActive(false);Resume();RefreshHUD();}
    void RefreshCards()
    {
        for(int i=0;i<cards.Length;i++)
        {
            cards[i].button.gameObject.SetActive(i<offered.Count);
            if(i>=offered.Count)continue;
            var item=offered[i];cards[i].icon.sprite=item.picture;
            cards[i].title.text=LocalizationService.Text("hex.item.title","name",item.DisplayName,"kind",LocalizationService.Text("item.kind."+item.kind),"level",ItemLevel(item)+1);
            cards[i].description.text=item.DisplayDescription();
        }
        continueButton.gameObject.SetActive(offered.Count==0);
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
        serviceLabel.text=LocalizationService.Text("hex.service","seconds",cooldown.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture),"queue",waitingGuests.Count);
        cooldownBar.fillAmount=1-Mathf.Clamp01(cooldown/ServiceInterval);
        tileLabel.text=LocalizationService.Text("hex.stock","name",LocalizationService.Text(board.Selected.nameKey),"stock",board.Model.Stock(board.Selected),"owned",board.Model.OwnedCount);
        statusLabel.text=LocalizationService.Text(messageKey);
        var inventory=new StringBuilder();
        foreach(var item in ownedItems)inventory.AppendLine(LocalizationService.Text("inventory.row","name",item.definition.DisplayName,"level",item.level));
        inventoryLabel.text=inventory.Length==0?LocalizationService.Text("inventory.empty","gold",NextThreshold):inventory.ToString();
        buildButton.interactable=!IsBuilding&&!IsChoosing&&!IsShopping;testGoldButton.interactable=!IsBuilding&&!IsChoosing&&!IsShopping;
    }
}

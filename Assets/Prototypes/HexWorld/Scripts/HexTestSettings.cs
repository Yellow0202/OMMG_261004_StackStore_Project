using System;
using UnityEngine;

/// <summary>Authoring data only. Wallets, guests, inventory and cooking progress remain runtime state.</summary>
[CreateAssetMenu(menuName="Stack Store/Hex World/Test Settings")]
public sealed class HexTestSettings : ScriptableObject
{
    public static HexTestSettings Current { get; internal set; }
    [Header("손님 이동 애니메이션 / 목록은 다음 생성, 속도·크기는 즉시 반영")]
    public CustomerAnimationOptions customerAnimation = new CustomerAnimationOptions();
    [Serializable] public sealed class CustomerAnimationOptions
    {
        public HexCustomerAnimationCatalog catalog;
        [Min(0)] public float framesPerSecond=8;
        [Min(0)] public float minimumMovement=.0001f;
        [Min(.01f)] public float teleportDistance=2;
        [Min(.01f)] public float scale=1;
        [Header("시트 재분할 메뉴 실행 시 반영")]
        [Min(.1f)] public float authoredHeight=1.333333f;
        [Range(1,254)] public int alphaCutoff=128;
        [Range(0,8)] public int framePadding=2;
        [Range(128,512)] public int frameCanvasSize=256;
        [Range(64,400)] public int frameBodyHeight=192;
    }
    [Header("플레이어 이동 애니메이션 / 즉시 반영")] public PlayerAnimationOptions playerAnimation = new PlayerAnimationOptions();
    [Serializable] public sealed class PlayerAnimationOptions
    {
        public HexPlayerAnimationSet clips;
        [Min(0)] public float framesPerSecond=10;
        [Min(0)] public float minimumMovement=.0001f;
        [Min(.01f)] public float teleportDistance=2;
        [Min(.01f)] public float scale=1;
    }
    [Header("카메라 / 즉시 반영")] public CameraOptions camera = new CameraOptions();
    [Header("2D 표시 / 즉시 반영")] public VisualOptions visual = new VisualOptions();
    [Header("손님 / 속도는 즉시, 생성·예약 값은 다음 대상부터")] public GuestOptions guests = new GuestOptions();
    [Header("접객 / 준비 속도 즉시, 대기 시간은 다음 손님부터")] public ServiceOptions service = new ServiceOptions();
    [Header("시작 조건 / 다음 Play부터")] public StartOptions start = new StartOptions();
    [Header("아르바이트 / 고용·급여·성장·업무")] public StaffOptions staff = new StaffOptions();
    [Header("캐릭터 역할 표시 / 즉시 반영")] public IdentityOptions identity = new IdentityOptions();
    [Header("데이터 원본 / 아래 Inspector에서 펼쳐 편집")]
    public GoldLevelCurve levelCurve;
    public ItemCatalog itemCatalog;
    public HexTileDefinition[] tileTypes;
    public HexShopOffer[] shopOffers;

    [Serializable] public sealed class CameraOptions
    {
        [Range(0,89)] public float minimumPitch=4,maximumPitch=50,initialPitch=30;
        [Min(.1f)] public float baseDistance=18,minimumZoom=.6f,maximumZoom=1.8f,initialZoom=1;
        [Range(-180,180)] public float yaw;
        [Range(10,100)] public float fieldOfView=60;
        [Min(0)] public float zoomSpeed=.009f,orbitSensitivity=.15f,panMargin=3,overscroll=2;
        [Min(.1f)] public float returnSpeed=5;
        [Min(0)] public float focusHeight=.3f;
        [Min(1)] public float dragThresholdPixels=8;
    }
    [Serializable] public sealed class VisualOptions
    {
        [Range(0,1)] public float pitchFollow=.35f;
        [Range(0,30)] public float maximumTilt=18;
        [Min(0)] public float maximumGroundLean=.3f,gaugeHeight=1.5f,walkFramesPerSecond=7;
        [Min(.001f)] public float outlineWidth=.035f,wallThickness=.24f,wallHeight=1.125f;
        public Color unresponsiveColor=new Color(0,0,0,.4f);
        [Range(0,1)] public float customerSaturation=.36f,customerBrightness=.96f;
    }
    [Serializable] public sealed class GuestOptions
    {
        [Min(.01f)] public float spawnSeconds=1.155f;
        [Min(0)] public int maxCustomers=32;
        [Range(0,1)] public float visitorChance=.4f,directQueueChance=.5f,browsingQueueChance=.35f,lodgingChance=.25f;
        [Min(.1f)] public float browseDecisionSeconds=3,fullAreaRetrySeconds=.5f;
        [Min(.01f)] public float walkSpeed=1.9f;
        [Tooltip("생성·퇴장 경계 / 월드 단위")][Min(1)] public float spawnX=11,destinationX=12,despawnX=11.8f;
        [Min(0)] public float spawnZ=6,browseZ=3;
        public Vector2 browseX=new Vector2(2.7f,5),turnSeconds=new Vector2(1,3);
        [Min(0)] public float verticalDeviation=1,verticalSpeed=.5f,stallClearance=1.6f;
        [Range(1,12)] public int standingSlotsPerCell=6;
        [Min(0)] public float standingRadius=.8f,separation=.65f;
        [Min(.001f)] public float contactDistance=.2f,waypointDistance=.05f;
        public float groundHeight=.22f;
    }
    [Serializable] public sealed class ServiceOptions
    {
        [Min(.1f)] public float preparationSeconds=5,patienceSeconds=10,walkSpeed=3.8f,eatingSeconds=3.5f;
        [Range(1,4)] public int baseFoodCount=1;
        [Min(0)] public int standingGold=1;
        public Vector3 stationOffset=new Vector3(0,.22f,-.65f);
        [Min(.001f)] public float stationDistance=.1f,arrivalDistance=.18f,deliveryDistance=.25f;
        [Min(0)] public float mealRadius=.4f,mealHeight=1.4f;
    }
    [Serializable] public sealed class StartOptions
    {
        [Min(0)] public int gold,tilesPerType=2;
        [Min(1)] public int level=1;
        [Range(1,3)] public int rewardChoices=3;
    }
    [Serializable] public sealed class StaffOptions
    {
        [Min(1)] public int maximumEmployees=6;
        [Min(.1f)] public float wageSeconds=30,passiveExperiencePerSecond=.5f,jobExperiencePerSecond=1;
        public float[] gradeExperience={0,60,180,360,600,900,1260};
        public float[] gradeEfficiency={.6f,.75f,.9f,1,1.2f,1.4f,1.7f};
        [Min(.01f)] public float walkSpeed=2.4f;
        [Min(.1f)] public float workDistance=.25f,recruitRadius=5,recruitSeconds=4,ejectionSeconds=10;
        [Range(0,1)] public float persuasionBaseChance=.2f,persuasionPerGrade=.08f;
        public Vector3 kitchenOffset=new Vector3(.55f,.22f,0);
        [Min(0)] public float patrolRadius=2.8f;
        [Min(.1f)] public float patrolSeconds=4;
        public HexStaffDefinition[] candidates=Array.Empty<HexStaffDefinition>();
        public HexStaffSkill[] skills=Array.Empty<HexStaffSkill>();
        [Header("테스트 대상 / prototype")] public HexStaffWorkTarget trashTestPrefab,disruptionTestPrefab;
        [Min(0)] public int testGold=20;
        [Min(0)] public float testExperience=180;
        public Vector3 trashTestOffset=new Vector3(-.5f,.22f,.35f),disruptionTestOffset=new Vector3(.6f,.22f,.35f);
    }
    [Serializable] public sealed class IdentityOptions
    {
        public bool showLabels=true,showGroundMarkers=true;
        public Color playerColor=new Color(1,.72f,.12f,1),employeeColor=new Color(.15f,.85f,1,1),textColor=Color.white;
        [Min(0)] public float labelHeight=1.85f,groundHeight=.025f;
        [Min(.001f)] public float labelScale=.009f,groundRadius=.45f,groundWidth=.06f;
        public Vector2 labelSize=new Vector2(160,38);
        [Range(12,40)] public int fontSize=24;
        [Range(0,1)] public float backgroundOpacity=.92f;
    }
    void OnValidate()
    {
        camera.maximumPitch=Mathf.Max(camera.minimumPitch,camera.maximumPitch);
        camera.maximumZoom=Mathf.Max(camera.minimumZoom,camera.maximumZoom);
        camera.initialPitch=Mathf.Clamp(camera.initialPitch,camera.minimumPitch,camera.maximumPitch);
        camera.initialZoom=Mathf.Clamp(camera.initialZoom,camera.minimumZoom,camera.maximumZoom);
        guests.browseX=new Vector2(Mathf.Max(0,guests.browseX.x),Mathf.Max(guests.browseX.x,guests.browseX.y));
        guests.turnSeconds=new Vector2(Mathf.Max(.01f,guests.turnSeconds.x),Mathf.Max(.01f,Mathf.Max(guests.turnSeconds.x,guests.turnSeconds.y)));
        guests.despawnX=Mathf.Max(guests.spawnX+.1f,guests.despawnX);
        guests.destinationX=Mathf.Max(guests.despawnX+.1f,guests.destinationX);
        service.deliveryDistance=Mathf.Max(service.arrivalDistance,service.deliveryDistance);
    }
}

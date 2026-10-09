using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public sealed class HexSettingsBinding : MonoBehaviour
{
    public HexTestSettings settings;
    public HexPrototype game;
    public Text pitchLabel,zoomLabel;
    void Awake()
    {
        if(!settings)return;
        HexTestSettings.Current=settings;
        if(game.service&&game.service.worker&&settings.playerAnimation.clips)
        {
            var actor=game.service.worker;
            var animation=actor.GetComponent<HexPlayerAnimation>();
            if(!animation)animation=actor.gameObject.AddComponent<HexPlayerAnimation>();
            animation.Configure(actor,settings);
        }
        game.board.initialStock=settings.start.tilesPerType;
        if(settings.tileTypes!=null&&settings.tileTypes.Length>0)game.board.tileTypes=settings.tileTypes;
        game.ConfigureStart(settings.start.gold,settings.start.level);
        Apply();game.orbit.pitch=settings.camera.initialPitch;game.orbit.zoom=settings.camera.initialZoom;
    }
    void OnEnable(){if(settings)HexTestSettings.Current=settings;}
    void OnDisable(){if(HexTestSettings.Current==settings)HexTestSettings.Current=null;}
    void Update(){Apply();}
    public void Apply()
    {
        if(!settings||!game)return;
        HexTestSettings.Current=settings;
        var c=settings.camera;var orbit=game.orbit;
        orbit.minimumPitch=c.minimumPitch;orbit.maximumPitch=Mathf.Max(c.minimumPitch,c.maximumPitch);
        orbit.minimumZoom=c.minimumZoom;orbit.maximumZoom=Mathf.Max(c.minimumZoom,c.maximumZoom);
        orbit.baseDistance=c.baseDistance;orbit.yaw=c.yaw;orbit.zoomSpeed=c.zoomSpeed;
        orbit.panMargin=c.panMargin;orbit.overscroll=c.overscroll;orbit.returnSpeed=c.returnSpeed;
        orbit.GetComponent<Camera>().fieldOfView=c.fieldOfView;
        orbit.SetPitch(orbit.pitch);orbit.SetZoom(orbit.zoom);
        game.pitchSlider.minValue=orbit.minimumPitch;game.pitchSlider.maxValue=orbit.maximumPitch;
        game.zoomSlider.minValue=orbit.minimumZoom;game.zoomSlider.maxValue=orbit.maximumZoom;
        if(pitchLabel)pitchLabel.text=LocalizationService.Text("hex.camera.pitch","min",c.minimumPitch,"max",c.maximumPitch);
        if(zoomLabel)zoomLabel.text=LocalizationService.Text("hex.camera.zoom","min",c.minimumZoom,"max",c.maximumZoom);
        var g=settings.guests;game.spawnSeconds=Mathf.Max(.01f,g.spawnSeconds);game.maxCustomers=Mathf.Max(0,g.maxCustomers);
        game.visitorChance=g.visitorChance;game.directQueueChance=g.directQueueChance;
        game.browsingQueueChance=g.browsingQueueChance;game.browseDecisionSeconds=g.browseDecisionSeconds;
        game.serviceSeconds=settings.service.preparationSeconds;game.patienceSeconds=settings.service.patienceSeconds;
        if(game.service)game.service.walkSpeed=settings.service.walkSpeed;
        if(settings.levelCurve)game.curve=settings.levelCurve;
        if(settings.itemCatalog)game.catalog=settings.itemCatalog;
    }
}

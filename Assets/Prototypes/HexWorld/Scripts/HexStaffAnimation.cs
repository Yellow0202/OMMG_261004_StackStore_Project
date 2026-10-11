using UnityEngine;

/// <summary>Staff motion uses its own authored identity, navigation displacement and breathing cycle.</summary>
[DefaultExecutionOrder(200)]
public sealed class HexStaffAnimation : MonoBehaviour
{
    static readonly HexTestSettings.StaffAnimationOptions Defaults=new HexTestSettings.StaffAnimationOptions();
    HexWorldActor actor;
    HexStaffAnimationSet character;
    Vector3 previous,originalScale;
    float walkFrame,idleFrame,stationarySeconds;
    int direction=4;
    bool wasMoving;
    public bool Moving{get;private set;}
    public int Direction=>direction;
    public HexStaffAnimationSet Character=>character;
    public void Configure(HexWorldActor target,HexStaffAnimationSet data)
    {
        actor=target;character=data;originalScale=actor.body.transform.localScale;
        previous=transform.position;walkFrame=idleFrame=stationarySeconds=0;direction=4;wasMoving=false;
        actor.externalAnimation=true;actor.body.flipX=false;actor.body.sprite=data.Idle(direction,0);
    }
    void OnEnable(){previous=transform.position;walkFrame=idleFrame=stationarySeconds=0;wasMoving=false;if(actor)actor.externalAnimation=true;}
    void OnDisable(){if(actor)actor.externalAnimation=false;}
    void LateUpdate()
    {
        if(!actor||!character||!character.IsValid)return;
        var delta=transform.position-previous;previous=transform.position;
        AdvanceMotion(delta,Time.deltaTime,Camera.main?Camera.main.transform.eulerAngles.y:0);
    }
    void AdvanceMotion(Vector3 delta,float dt,float yaw)
    {
        var options=HexTestSettings.Current?HexTestSettings.Current.staffAnimation:Defaults;
        delta.y=0;bool teleport=delta.sqrMagnitude>=options.teleportDistance*options.teleportDistance;
        bool displaced=dt>0&&!teleport&&delta.sqrMagnitude>options.minimumMovement*options.minimumMovement;
        if(displaced)stationarySeconds=0;else stationarySeconds+=Mathf.Max(0,dt);
        Moving=!teleport&&dt>0&&(displaced||(wasMoving&&stationarySeconds<options.stopGraceSeconds));
        if(Moving)
        {
            if(displaced)
            {
                var local=Quaternion.Euler(0,-yaw,0)*delta;float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
                if(!wasMoving||Mathf.Abs(Mathf.DeltaAngle(direction*45,angle))>22.5f+options.directionHysteresisDegrees)
                    direction=HexPlayerAnimation.DirectionFor(delta,yaw);
            }
            if(!wasMoving)walkFrame=0;
            walkFrame=Mathf.Repeat(walkFrame+dt*Mathf.Max(0,options.walkFramesPerSecond),8);
            actor.body.sprite=character.motion.frames[direction].sprites[Mathf.FloorToInt(walkFrame)];idleFrame=0;
        }
        else
        {
            if(wasMoving||teleport)idleFrame=0;
            idleFrame=Mathf.Repeat(idleFrame+dt*Mathf.Max(0,options.idleFramesPerSecond),4);
            actor.body.sprite=character.Idle(direction,idleFrame);
        }
        actor.body.flipX=false;actor.body.transform.localScale=originalScale*Mathf.Max(.01f,options.scale);
        if(teleport){walkFrame=0;stationarySeconds=options.stopGraceSeconds;}
        wasMoving=Moving;
    }
}

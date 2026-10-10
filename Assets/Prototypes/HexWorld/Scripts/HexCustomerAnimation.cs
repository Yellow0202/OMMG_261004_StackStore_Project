using UnityEngine;

/// <summary>Chooses appearance once at spawn; animation never overrides tint, navigation or billboard rotation.</summary>
[DefaultExecutionOrder(200)]
public sealed class HexCustomerAnimation : MonoBehaviour
{
    HexWorldActor actor;
    HexTestSettings settings;
    HexPlayerAnimationSet character;
    Vector3 previous,originalScale;
    float phase;
    int direction=4;
    bool wasMoving;
    public HexPlayerAnimationSet Character=>character;
    public int Direction=>direction;
    public bool Moving { get; private set; }
    public bool Configure(HexWorldActor target,HexTestSettings shared)
    {
        actor=target;settings=shared;
        character=shared && shared.customerAnimation.catalog ? shared.customerAnimation.catalog.Pick() : null;
        if(!character){actor.externalAnimation=false;return false;}
        originalScale=actor.body.transform.localScale;
        previous=transform.position;phase=0;stationarySeconds=0;direction=4;wasMoving=false;
        actor.externalAnimation=true;actor.body.flipX=false;
        actor.body.sprite=character.idle[direction];
        return true;
    }
    void OnEnable(){previous=transform.position;stationarySeconds=0;wasMoving=false;if(actor&&character)actor.externalAnimation=true;}
    void OnDisable(){if(actor)actor.externalAnimation=false;}
    float stationarySeconds;
    void LateUpdate()
    {
        if(!actor||!settings||!character)return;
        var delta=transform.position-previous;previous=transform.position;
        AdvanceMotion(delta,Time.deltaTime,Camera.main?Camera.main.transform.eulerAngles.y:0);
    }
    // One clock survives direction changes and brief navigation gaps.
    void AdvanceMotion(Vector3 delta,float dt,float cameraYaw)
    {
        var options=settings.customerAnimation;delta.y=0;
        bool teleported=delta.sqrMagnitude>=options.teleportDistance*options.teleportDistance;
        bool displaced=dt>0&&delta.sqrMagnitude>options.minimumMovement*options.minimumMovement&&!teleported;
        if(displaced)stationarySeconds=0;else stationarySeconds+=Mathf.Max(0,dt);
        Moving=!actor.seated&&!teleported&&dt>0&&(displaced||(wasMoving&&stationarySeconds<options.stopGraceSeconds));
        if(Moving)
        {
            if(displaced)
            {
                var local=Quaternion.Euler(0,-cameraYaw,0)*delta;
                float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
                if(!wasMoving||Mathf.Abs(Mathf.DeltaAngle(direction*45,angle))>22.5f+options.directionHysteresisDegrees)
                    direction=HexPlayerAnimation.DirectionFor(delta,cameraYaw);
            }
            phase=Mathf.Repeat(phase+dt*Mathf.Max(0,options.framesPerSecond)/HexPlayerAnimationSet.ClipFramesPerSecond,.8f);
            actor.body.sprite=character.Sample(direction,phase);
        }
        else actor.body.sprite=character.idle[direction];
        if(teleported){phase=0;stationarySeconds=options.stopGraceSeconds;}
        actor.body.flipX=character.mirrorWest&&direction==6;
        actor.body.transform.localScale=originalScale*Mathf.Max(.01f,options.scale);
        wasMoving=Moving;
    }
}

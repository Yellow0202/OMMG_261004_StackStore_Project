using UnityEngine;

/// <summary>Samples authored sprite clips after navigation, keeping billboard rotation untouched.</summary>
[DefaultExecutionOrder(200)]
public sealed class HexPlayerAnimation : MonoBehaviour
{
    HexWorldActor actor;
    HexTestSettings settings;
    Vector3 previous,originalScale;
    float clock;
    int direction=4;
    bool wasMoving;
    public int Direction=>direction;
    public bool Moving {get;private set;}
    public void Configure(HexWorldActor target,HexTestSettings shared)
    {
        actor=target;settings=shared;originalScale=actor.body.transform.localScale;
        previous=transform.position;actor.externalAnimation=true;
        actor.body.color=Color.white;actor.body.flipX=false;
        actor.body.sprite=shared.playerAnimation.clips.idle[direction];
    }
    void OnEnable(){previous=transform.position;clock=0;wasMoving=false;if(actor)actor.externalAnimation=true;}
    void OnDisable(){if(actor)actor.externalAnimation=false;}
    public static int DirectionFor(Vector3 delta,float cameraYaw)
    {
        var local=Quaternion.Euler(0,-cameraYaw,0)*delta;
        return (Mathf.RoundToInt(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/45)+8)%8;
    }
    void LateUpdate()
    {
        if(!actor||!settings||!settings.playerAnimation.clips||!settings.playerAnimation.clips.IsValid)return;
        var options=settings.playerAnimation;var data=options.clips;
        var delta=transform.position-previous;previous=transform.position;delta.y=0;
        Moving=Time.deltaTime>0&&delta.sqrMagnitude>options.minimumMovement*options.minimumMovement
            &&delta.sqrMagnitude<options.teleportDistance*options.teleportDistance;
        if(Moving)
        {
            direction=DirectionFor(delta,Camera.main?Camera.main.transform.eulerAngles.y:0);
            if(!wasMoving)clock=0;
            clock+=Time.deltaTime*Mathf.Max(0,options.framesPerSecond)/HexPlayerAnimationSet.ClipFramesPerSecond;
            actor.body.sprite=data.Sample(direction,clock);
        }
        else {clock=0;actor.body.sprite=data.idle[direction];}
        actor.body.transform.localScale=originalScale*Mathf.Max(.01f,options.scale);
        wasMoving=Moving;
    }
}

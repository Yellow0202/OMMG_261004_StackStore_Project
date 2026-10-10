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
        previous=transform.position;phase=0;direction=4;wasMoving=false;
        actor.externalAnimation=true;actor.body.flipX=false;
        actor.body.sprite=character.idle[direction];
        return true;
    }
    void OnEnable(){previous=transform.position;wasMoving=false;if(actor&&character)actor.externalAnimation=true;}
    void OnDisable(){if(actor)actor.externalAnimation=false;}
    void LateUpdate()
    {
        if(!actor||!settings||!character)return;
        var options=settings.customerAnimation;
        var delta=transform.position-previous;previous=transform.position;delta.y=0;
        Moving=!actor.seated && Time.deltaTime>0
            && delta.sqrMagnitude>options.minimumMovement*options.minimumMovement
            && delta.sqrMagnitude<options.teleportDistance*options.teleportDistance;
        if(Moving)
        {
            direction=HexPlayerAnimation.DirectionFor(delta,Camera.main?Camera.main.transform.eulerAngles.y:0);
            if(!wasMoving)phase=0;
            phase+=Time.deltaTime*Mathf.Max(0,options.framesPerSecond)/HexPlayerAnimationSet.ClipFramesPerSecond;
            actor.body.sprite=character.Sample(direction,phase);
        }
        else {phase=0;actor.body.sprite=character.idle[direction];}
        actor.body.flipX=character.mirrorWest&&direction==6;
        actor.body.transform.localScale=originalScale*Mathf.Max(.01f,options.scale);
        wasMoving=Moving;
    }
}

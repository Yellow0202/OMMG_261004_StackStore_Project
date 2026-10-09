using UnityEngine;

/// <summary>Clockwise camera-relative directions: N, NE, E, SE, S, SW, W, NW.</summary>
[CreateAssetMenu(menuName="Stack Store/Hex World/Player Animation Set")]
public sealed class HexPlayerAnimationSet : ScriptableObject
{
    public AnimationClip[] walk = new AnimationClip[8];
    public Sprite[] idle = new Sprite[8];
    [System.Serializable] public sealed class Frames { public Sprite[] sprites; }
    public Frames[] frames=new Frames[8];
    public const float ClipFramesPerSecond=10;
    public bool IsValid => walk!=null&&walk.Length==8&&idle!=null&&idle.Length==8
        &&System.Array.TrueForAll(walk,x=>x)&&System.Array.TrueForAll(idle,x=>x)
        &&frames!=null&&frames.Length==8&&System.Array.TrueForAll(frames,x=>x!=null&&x.sprites!=null&&x.sprites.Length==8&&System.Array.TrueForAll(x.sprites,s=>s));
    public Sprite Sample(int direction,float seconds)=>frames[direction].sprites[Mathf.FloorToInt(seconds*ClipFramesPerSecond)%frames[direction].sprites.Length];
}

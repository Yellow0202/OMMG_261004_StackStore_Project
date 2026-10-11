using UnityEngine;

[CreateAssetMenu(menuName="Stack Store/Hex World/Staff Animation Set")]
public sealed class HexStaffAnimationSet : ScriptableObject
{
    public HexPlayerAnimationSet motion;
    public HexPlayerAnimationSet.Frames[] idleFrames=new HexPlayerAnimationSet.Frames[8];
    public AnimationClip[] idleClips=new AnimationClip[8];
    public bool IsValid=>motion&&motion.IsValid&&idleFrames!=null&&idleFrames.Length==8
        &&System.Array.TrueForAll(idleFrames,x=>x!=null&&x.sprites!=null&&x.sprites.Length==4&&System.Array.TrueForAll(x.sprites,s=>s))
        &&idleClips!=null&&idleClips.Length==8&&System.Array.TrueForAll(idleClips,c=>c);
    public Sprite Idle(int direction,float frame)=>idleFrames[direction].sprites[Mathf.FloorToInt(frame)%4];
}

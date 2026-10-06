using UnityEngine;

/// <summary>Keep the authored front illustration readable, with its bottom pivot on the ground.</summary>
[ExecuteAlways]
public sealed class HexCameraFacingSprite : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Transform groundAnchor;
    [Range(0,1)] public float pitchFollow = .35f;
    [Range(0,30)] public float maximumTilt = 18;
    [Min(0)] public float maximumGroundLean = .3f;
    public float TiltFor(float cameraPitch)
    {
        float limit=maximumTilt;
        if(sprite&&sprite.sprite)
        {
            var bounds=sprite.sprite.bounds;
            float reach=Mathf.Max(Mathf.Abs(bounds.min.y),Mathf.Abs(bounds.max.y))*Mathf.Abs(sprite.transform.lossyScale.y);
            if(reach>.0001f)limit=Mathf.Min(limit,Mathf.Asin(Mathf.Clamp01(maximumGroundLean/reach))*Mathf.Rad2Deg);
        }
        return Mathf.Clamp(cameraPitch*pitchFollow,-limit,limit);
    }
    public void FaceCamera()
    {
        var camera = Camera.main;
        if (!camera || !sprite) return;
        // Keep the bottom pivot fixed and bound the upper pixels' movement on the ground.
        float pitch=Mathf.DeltaAngle(0,camera.transform.eulerAngles.x);
        sprite.transform.rotation = Quaternion.Euler(TiltFor(pitch), camera.transform.eulerAngles.y, 0);
        Vector3 anchor = groundAnchor ? groundAnchor.position : transform.position;
        sprite.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(camera.transform.forward, anchor - camera.transform.position) * 100), -20000, 20000);
    }
    void OnEnable() { FaceCamera(); }
    void LateUpdate() { FaceCamera(); }
}

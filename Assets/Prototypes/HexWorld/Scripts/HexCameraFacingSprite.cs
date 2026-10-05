using UnityEngine;

/// <summary>Keep the authored front illustration readable, with its bottom pivot on the ground.</summary>
[ExecuteAlways]
public sealed class HexCameraFacingSprite : MonoBehaviour
{
    public SpriteRenderer sprite;
    public Transform groundAnchor;
    public void FaceCamera()
    {
        var camera = Camera.main;
        if (!camera || !sprite) return;
        sprite.transform.rotation = camera.transform.rotation;
        Vector3 anchor = groundAnchor ? groundAnchor.position : transform.position;
        sprite.sortingOrder = Mathf.RoundToInt(-Vector3.Dot(camera.transform.forward, anchor - camera.transform.position) * 100);
    }
    void OnEnable() { FaceCamera(); }
    void LateUpdate() { FaceCamera(); }
}

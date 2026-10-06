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
        // Rotate around the vertical axis only: pitching a bottom-pivot billboard
        // pushes its upper pixels across the tile boundary and through nearby walls.
        sprite.transform.rotation = Quaternion.Euler(0, camera.transform.eulerAngles.y, 0);
        Vector3 anchor = groundAnchor ? groundAnchor.position : transform.position;
        sprite.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-Vector3.Dot(camera.transform.forward, anchor - camera.transform.position) * 100), -20000, 20000);
    }
    void OnEnable() { FaceCamera(); }
    void LateUpdate() { FaceCamera(); }
}

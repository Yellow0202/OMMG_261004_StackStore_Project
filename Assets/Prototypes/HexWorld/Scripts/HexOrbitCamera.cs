using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class HexOrbitCamera : MonoBehaviour
{
    public Transform target;
    public float pitch = 30, minimumPitch = 4, maximumPitch = 65;
    public float baseDistance = 18, zoom = 1, minimumZoom = .6f, maximumZoom = 1.8f;
    public float yaw = 0;
    public Vector3 floorOffset;
    public Vector3 panOffset;
    public float zoomSpeed = .0045f;
    public void SetPitch(float value) { pitch = Mathf.Clamp(value, minimumPitch, maximumPitch); }
    public void SetZoom(float value) { zoom = Mathf.Clamp(value, minimumZoom, maximumZoom); }
    public void Orbit(float delta) { SetPitch(pitch + delta * .15f); }
    public void Scroll(float delta) { SetZoom(zoom - delta * zoomSpeed); }
    public void Pan(Vector2 pixels)
    {
        var camera = GetComponent<Camera>();
        float scale = camera ? 2 * baseDistance * zoom * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Max(1, camera.pixelHeight) : .02f;
        Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        panOffset -= (right * pixels.x + forward * pixels.y) * scale;
    }
    public void ResetView() { pitch = 30; yaw = 0; zoom = 1; panOffset = Vector3.zero; }
    void LateUpdate()
    {
        if (!target) return;
        SetPitch(pitch); SetZoom(zoom);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.SetPositionAndRotation(target.position + floorOffset + panOffset + Vector3.up * .3f - rotation * Vector3.forward * baseDistance * zoom, rotation);
    }
}

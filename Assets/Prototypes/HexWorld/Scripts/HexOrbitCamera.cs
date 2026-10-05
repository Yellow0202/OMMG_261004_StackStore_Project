using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class HexOrbitCamera : MonoBehaviour
{
    public Transform target;
    public float pitch = 30, minimumPitch = 25, maximumPitch = 65;
    public float baseDistance = 18, zoom = 1, minimumZoom = .6f, maximumZoom = 1.8f;
    public float yaw = 0;
    public void SetPitch(float value) { pitch = Mathf.Clamp(value, minimumPitch, maximumPitch); }
    public void SetZoom(float value) { zoom = Mathf.Clamp(value, minimumZoom, maximumZoom); }
    public void Orbit(float delta) { SetPitch(pitch + delta * .15f); }
    public void Scroll(float delta) { SetZoom(zoom - delta * .0015f); }
    public void ResetView() { pitch = 30; yaw = 0; zoom = 1; }
    void LateUpdate()
    {
        if (!target) return;
        SetPitch(pitch); SetZoom(zoom);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.SetPositionAndRotation(target.position + Vector3.up * .3f - rotation * Vector3.forward * baseDistance * zoom, rotation);
    }
}

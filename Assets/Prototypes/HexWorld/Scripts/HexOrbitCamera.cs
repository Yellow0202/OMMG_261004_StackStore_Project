using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class HexOrbitCamera : MonoBehaviour
{
    public Transform target;
    public float pitch = 30, minimumPitch = 4, maximumPitch = 50;
    public float baseDistance = 18, zoom = 1, minimumZoom = .6f, maximumZoom = 1.8f;
    public float yaw = 0;
    public Vector3 floorOffset;
    public Vector3 panOffset;
    public float zoomSpeed = .009f;
    public HexTileBoard board;
    public float panMargin=3, overscroll=2, returnSpeed=5;
    public bool IsPanning { get; set; }
    public Bounds PanBounds
    {
        get
        {
            var bounds=new Bounds(target?target.position+floorOffset:Vector3.zero,Vector3.zero);
            if(board&&board.Model!=null)
            {
                bool first=true;
                foreach(var cell in board.Model.Owned)
                {
                    var point=HexBoardModel.World(cell);
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                }
            }
            bounds.Expand(new Vector3(panMargin*2,0,panMargin*2));return bounds;
        }
    }
    public void ConstrainPan(float dt)
    {
        if(!target)return;
        var origin=target.position+floorOffset;
        var focus=origin+panOffset;var bounds=PanBounds;
        Vector3 limited=new Vector3(Mathf.Clamp(focus.x,bounds.min.x,bounds.max.x),focus.y,Mathf.Clamp(focus.z,bounds.min.z,bounds.max.z));
        if(IsPanning)
        {
            focus.x=Mathf.Clamp(focus.x,bounds.min.x-overscroll,bounds.max.x+overscroll);
            focus.z=Mathf.Clamp(focus.z,bounds.min.z-overscroll,bounds.max.z+overscroll);
        }
        else
        {
            // Exponential recovery is fast far away and slows near the boundary.
            focus=Vector3.Lerp(focus,limited,1-Mathf.Exp(-returnSpeed*Mathf.Max(0,dt)));
            if((focus-limited).sqrMagnitude<.0001f)focus=limited;
        }
        panOffset=focus-origin;panOffset.y=0;
    }
    public void SetPitch(float value) { pitch = Mathf.Clamp(value, minimumPitch, maximumPitch); }
    public void SetZoom(float value) { zoom = Mathf.Clamp(value, minimumZoom, maximumZoom); }
    public void Orbit(float delta) { SetPitch(pitch + delta * (HexTestSettings.Current?HexTestSettings.Current.camera.orbitSensitivity:.15f)); }
    public void Scroll(float delta) { SetZoom(zoom - delta * zoomSpeed); }
    public void Pan(Vector2 pixels)
    {
        var camera = GetComponent<Camera>();
        float scale = camera ? 2 * baseDistance * zoom * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f) / Mathf.Max(1, camera.pixelHeight) : .02f;
        Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        panOffset -= (right * pixels.x + forward * pixels.y) * scale;
        bool held=IsPanning;IsPanning=true;ConstrainPan(0);IsPanning=held;
    }
    public void ResetView() { var c=HexTestSettings.Current?HexTestSettings.Current.camera:null;pitch=c!=null?c.initialPitch:30;yaw=c!=null?c.yaw:0;zoom=c!=null?c.initialZoom:1;panOffset = Vector3.zero; }
    void LateUpdate()
    {
        if (!target) return;
        SetPitch(pitch); SetZoom(zoom);
        ConstrainPan(Time.unscaledDeltaTime);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        transform.SetPositionAndRotation(target.position + floorOffset + panOffset + Vector3.up * (HexTestSettings.Current?HexTestSettings.Current.camera.focusHeight:.3f) - rotation * Vector3.forward * baseDistance * zoom, rotation);
    }
}

using UnityEngine;

/// <summary>A saved 2D food prefab follows a 3D parabola, driven by simulation time.</summary>
public sealed class HexFoodProjectile : MonoBehaviour
{
    [Min(.05f)] public float flightSeconds = .65f;
    [Min(0)] public float arcHeight = 1.6f;
    Vector3 start;
    Transform recipient;
    float elapsed;
    public void Launch(Vector3 origin, Transform target) { start=origin;recipient=target;elapsed=0;transform.position=start; }
    public bool Advance(float dt)
    {
        if(!recipient)return false;
        elapsed+=Mathf.Max(0,dt);
        float t=Mathf.Clamp01(elapsed/Mathf.Max(.05f,flightSeconds));
        Vector3 end=recipient.position+Vector3.up*.7f;
        transform.position=Vector3.Lerp(start,end,t)+Vector3.up*(4*arcHeight*t*(1-t));
        return t>=1;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>Reusable hook for future real litter and disruptive customers; test prefabs use it now.</summary>
[DefaultExecutionOrder(100)]
public sealed class HexStaffWorkTarget : MonoBehaviour
{
    public HexStaffTargetDefinition definition;
    public HexWorldActor view;
    public int floor;
    public HexTileBoard board;
    public float Progress { get; private set; }
    public bool Completed { get; private set; }
    public int ClaimedBy { get; private set; }
    public static readonly HashSet<HexStaffWorkTarget> Active=new HashSet<HexStaffWorkTarget>();
    void OnEnable(){Active.Add(this);}
    void OnDisable(){Active.Remove(this);ClaimedBy=0;}
    void LateUpdate()
    {
        if(!board||!view)return;bool visible=floor==board.CurrentFloor;
        view.body.gameObject.SetActive(visible);view.patienceCanvas.gameObject.SetActive(visible);if(view.shadow)view.shadow.gameObject.SetActive(visible);
    }
    public bool Claim(int id){if(Completed||ClaimedBy!=0&&ClaimedBy!=id)return false;ClaimedBy=id;return true;}
    public void Release(int id){if(ClaimedBy==id)ClaimedBy=0;}
    public bool Work(int id,float seconds,float efficiency)
    {
        if(!definition||Completed||ClaimedBy!=id)return false;
        Progress=Mathf.Clamp01(Progress+Mathf.Max(0,seconds)*Mathf.Max(0,efficiency)/Mathf.Max(.1f,definition.workSeconds));
        if(view)view.ShowPatience(true,1-Progress);
        if(Progress<1)return false;Completed=true;Active.Remove(this);gameObject.SetActive(false);Destroy(gameObject);return true;
    }
}

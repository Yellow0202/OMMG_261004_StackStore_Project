using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HexCameraAnchorChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Stack Store/Hex World/Validate Camera Anchors In Play")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Run this check in Play mode.");
        Validate(UnityEngine.Object.FindFirstObjectByType<HexPrototype>());
    }
    public static void Validate(HexPrototype game)
    {
        var camera=game.orbit;var originalRotation=camera.transform.rotation;
        var originalPan=camera.panOffset;bool originalHeld=camera.IsPanning;
        try
        {
            foreach(float pitch in new[]{4f,30f,65f})
            {
                camera.transform.rotation=Quaternion.Euler(pitch,0,0);
                foreach(var facing in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))
                {
                    facing.FaceCamera();Check(Vector3.Dot(facing.sprite.transform.up,Vector3.up)>.999f,"Billboard leans across a tile at pitch "+pitch);
                }
                foreach(var actor in UnityEngine.Object.FindObjectsByType<HexWorldActor>(FindObjectsSortMode.None))
                {
                    typeof(HexWorldActor).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actor,null);
                    var offset=actor.patienceCanvas.position-actor.transform.position;
                    Check(Mathf.Abs(offset.x)<.001f&&Mathf.Abs(offset.z)<.001f&&Mathf.Abs(offset.y-1.5f)<.001f,"Guest gauge moved across the ground");
                }
            }
            camera.IsPanning=true;camera.Pan(new Vector2(-100000,-100000));
            var focus=camera.target.position+camera.floorOffset+camera.panOffset;var bounds=camera.PanBounds;
            Check(focus.x<=bounds.max.x+camera.overscroll+.001f&&focus.z<=bounds.max.z+camera.overscroll+.001f,"Pan exceeded hard boundary");
            camera.IsPanning=false;var before=camera.panOffset;camera.ConstrainPan(.05f);float first=Vector3.Distance(before,camera.panOffset);
            float last=first;for(int i=0;i<80;i++){before=camera.panOffset;camera.ConstrainPan(.05f);last=Vector3.Distance(before,camera.panOffset);}
            focus=camera.target.position+camera.floorOffset+camera.panOffset;
            Check(first>last&&focus.x<=bounds.max.x+.001f&&focus.z<=bounds.max.z+.001f,"Recovery did not ease into the allowed area");
            Check(Mathf.Abs(camera.zoomSpeed-.009f)<.00001f&&camera.board==game.board,"Camera settings not saved");
            var record=new GameObject("Assignment check");var marker=record.AddComponent<HexKitchenOccupant>();
            try
            {
                var cell=new Vector2Int(77,77);marker.Assign(4,cell);record.transform.position=Vector3.one*100;record.SetActive(false);
                Check(HexKitchenOccupant.Present(4,cell),"Assignment lost when worker left or became hidden");
                marker.Unassign();Check(!HexKitchenOccupant.Present(4,cell),"Unassigned kitchen still staffed");
            }
            finally{UnityEngine.Object.DestroyImmediate(record);}
            Debug.Log("HEX_CAMERA_ANCHOR_PASS: upright billboards and gauge anchors at4/30/65deg, hard pan cap, fast-to-slow return, zoom settings and persistent assignment independent of position/visibility.");
        }
        finally
        {
            camera.transform.rotation=originalRotation;camera.panOffset=originalPan;camera.IsPanning=originalHeld;
            foreach(var facing in UnityEngine.Object.FindObjectsByType<HexCameraFacingSprite>(FindObjectsSortMode.None))facing.FaceCamera();
        }
    }
}

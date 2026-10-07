using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HexActorIdentityChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Assets()
    {
        EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        var player=game.service.worker.GetComponentInChildren<HexActorIdentity>(true);var staff=game.staffSystem.employeePrefab.GetComponentInChildren<HexActorIdentity>(true);
        Check(player&&staff&&player.role==HexActorRole.Player&&staff.role==HexActorRole.Employee,"Player/employee identity bindings missing");
        foreach(var identity in new[]{player,staff})Check(identity.labelCanvas&&identity.label&&identity.background&&identity.groundMarker&&identity.settings&&!identity.label.raycastTarget&&!identity.background.raycastTarget,"Saved noninteractive UGUI role marker incomplete");
        Check(player.groundMarker.positionCount==4&&staff.groundMarker.positionCount==32,"Distinct diamond/circle shapes missing");
        Check(!game.customerPrefab.GetComponentInChildren<HexActorIdentity>(true),"Customer unexpectedly has staff/player identity");
        Debug.Log("HEX_IDENTITY_ASSET_PASS: saved role plates, nonblocking UGUI, diamond/circle distinction, scene player and dynamic employee references; customers unchanged.");
    }
    public static void ValidatePlay(HexPrototype game)
    {
        var player=game.service.worker.GetComponentInChildren<HexActorIdentity>();var settings=HexTestSettings.Current;
        foreach(float pitch in new[]{4f,50f})
        {
            game.orbit.transform.rotation=Quaternion.Euler(pitch,0,0);
            foreach(var identity in UnityEngine.Object.FindObjectsByType<HexActorIdentity>(FindObjectsSortMode.None))
            {
                identity.Refresh();var actor=identity.GetComponentInParent<HexWorldActor>();
                Check(Vector3.Distance(identity.labelCanvas.transform.position,actor.transform.position+Vector3.up*settings.identity.labelHeight)<.001f,"Role plate moved across ground");
                Check(Quaternion.Angle(identity.labelCanvas.transform.rotation,Camera.main.transform.rotation)<.01f,"Plate does not face camera");
                Check(identity.label.font&&identity.label.text.Length>0&&!identity.label.text.Contains("{"),"Font or localization missing");
            }
        }
        Check(player.label.text==LocalizationService.Text("actor.role.player"),"Player role not localized");
        foreach(var employee in game.staffSystem.employees)
        {
            var identity=employee.view.GetComponentInChildren<HexActorIdentity>();Check(identity.employeeId==employee.state.id&&identity.label.text==LocalizationService.Text("actor.role.employee.id","id",employee.state.id),"Employee number or role missing");
        }
        Debug.Log("HEX_IDENTITY_PLAY_PASS: fixed anchors and facing at 4/50deg, readable font/localization and individually numbered employee labels.");
    }
}

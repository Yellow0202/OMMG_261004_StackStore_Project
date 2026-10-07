using UnityEngine;
using UnityEngine.UI;

public enum HexActorRole { Player, Employee }

/// <summary>Saved UGUI role plate and a floor marker. Customers carry neither.</summary>
[ExecuteAlways,DefaultExecutionOrder(150)]
public sealed class HexActorIdentity : MonoBehaviour
{
    public HexActorRole role;
    public HexTestSettings settings;
    public Canvas labelCanvas;
    public Image background;
    public Text label;
    public LineRenderer groundMarker;
    public int employeeId;
    LocalizationTable editorTable;
    static readonly HexTestSettings.IdentityOptions Defaults=new HexTestSettings.IdentityOptions();
    public void SetEmployee(int id){role=HexActorRole.Employee;employeeId=id;Refresh();}
    void OnEnable(){Refresh();}
    void LateUpdate(){Refresh();}
    public void Refresh()
    {
        if(!labelCanvas||!label||!groundMarker)return;
        var common=HexTestSettings.Current?HexTestSettings.Current:settings;
        var options=common?common.identity:Defaults;
        var actor=GetComponentInParent<HexWorldActor>();if(!actor)return;
        Color color=role==HexActorRole.Player?options.playerColor:options.employeeColor;
        labelCanvas.gameObject.SetActive(options.showLabels);groundMarker.gameObject.SetActive(options.showGroundMarkers);
        labelCanvas.transform.position=actor.transform.position+Vector3.up*options.labelHeight;
        if(Camera.main)labelCanvas.transform.rotation=Camera.main.transform.rotation;
        labelCanvas.transform.localScale=Vector3.one*options.labelScale;
        ((RectTransform)labelCanvas.transform).sizeDelta=options.labelSize;
        label.fontSize=options.fontSize;label.color=options.textColor;color.a=options.backgroundOpacity;background.color=color;
        var table=Application.isPlaying?null:UnityEditorTable();
        string key=role==HexActorRole.Player?"actor.role.player":employeeId>0?"actor.role.employee.id":"actor.role.employee";
        label.text=table?table.Resolve(key,DisplayLanguage.Korean).Replace("{id}",employeeId.ToString()):LocalizationService.Text(key,"id",employeeId);
        groundMarker.transform.position=new Vector3(actor.transform.position.x,options.groundHeight,actor.transform.position.z);
        groundMarker.transform.rotation=Quaternion.identity;groundMarker.transform.localScale=Vector3.one*options.groundRadius;
        color.a=1;groundMarker.startColor=groundMarker.endColor=color;groundMarker.widthMultiplier=options.groundWidth;
        // A fixed floor marker must remain below bodies and furniture, like tile outlines.
        groundMarker.sortingOrder=-31000;labelCanvas.overrideSorting=true;labelCanvas.sortingOrder=actor.body?actor.body.sortingOrder+1:0;
    }
    LocalizationTable UnityEditorTable()
    {
        #if UNITY_EDITOR
        if(!editorTable)editorTable=UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prototypes/HexWorld/Data/HexWorldStrings.asset");
        return editorTable;
        #else
        return null;
        #endif
    }
}

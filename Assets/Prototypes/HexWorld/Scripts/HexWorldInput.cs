using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class HexWorldInput : MonoBehaviour
{
    public InputActionAsset actions;
    public HexTileBoard board;
    public HexPrototype game;
    public HexOrbitCamera orbit;
    InputAction pointer, confirm, cancel, rotate, delta, zoom, pan;
    Vector2 panPrevious;
    bool panBlocked;
    InputActionAsset runtimeActions;
    const string Preferences = "StackStore.HexWorld.Bindings";
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    readonly HexPointerGesture gesture=new HexPointerGesture();
    bool confirming;
    void OnEnable()
    {
        if (!actions) return;
        runtimeActions = Instantiate(actions);
        if (PlayerPrefs.HasKey(Preferences)) runtimeActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(Preferences));
        pointer = runtimeActions.FindAction("World/Point", true); confirm = runtimeActions.FindAction("World/Confirm", true);
        cancel = runtimeActions.FindAction("World/Cancel", true); rotate = runtimeActions.FindAction("World/Orbit", true);
        delta = runtimeActions.FindAction("World/Look", true); zoom = runtimeActions.FindAction("World/Zoom", true);
        pan = runtimeActions.FindAction("World/Pan", true); pan.started += BeginPan;
        confirm.started+=BeginConfirm;confirm.canceled+=Confirm;rotate.started+=BeginOrbit;cancel.performed += Cancel; runtimeActions.Enable();
    }
    void OnDisable()
    {
        if (!runtimeActions || confirm == null) return;
        confirm.started-=BeginConfirm;confirm.canceled-=Confirm;rotate.started-=BeginOrbit;cancel.performed -= Cancel;pan.started-=BeginPan; runtimeActions.Disable(); Destroy(runtimeActions);confirming=false;
        pointer = confirm = cancel = rotate = delta = zoom = pan = null;
    }
    public void Rebind(string actionName, int binding, string path)
    {
        var action = runtimeActions.FindAction(actionName, true); bool enabled = action.enabled;
        action.Disable(); action.ApplyBindingOverride(binding, path); if (enabled) action.Enable();
        PlayerPrefs.SetString(Preferences, runtimeActions.SaveBindingOverridesAsJson()); PlayerPrefs.Save();
    }
    bool OverUI()
    {
        if (!EventSystem.current) return false;
        hits.Clear(); var data = new PointerEventData(EventSystem.current) { position = pointer.ReadValue<Vector2>() };
        EventSystem.current.RaycastAll(data, hits); return hits.Count > 0;
    }
    HexTileView Tile()
    {
        if (OverUI()) return null;
        Ray ray = orbit.GetComponent<Camera>().ScreenPointToRay(pointer.ReadValue<Vector2>());
        return Physics.Raycast(ray, out var hit, 1000) ? hit.collider.GetComponentInParent<HexTileView>() : null;
    }
    void BeginConfirm(InputAction.CallbackContext context){confirming=true;gesture.Begin(pointer.ReadValue<Vector2>(),OverUI());}
    void BeginOrbit(InputAction.CallbackContext context){if(!confirming)gesture.Begin(pointer.ReadValue<Vector2>(),OverUI());}
    void BeginPan(InputAction.CallbackContext context){panPrevious=pointer.ReadValue<Vector2>();panBlocked=OverUI();}
    void Confirm(InputAction.CallbackContext context)
    {
        bool click=confirming&&gesture.Click(pointer.ReadValue<Vector2>());confirming=false;
        if(click&&!OverUI())
        {
            var tile=Tile();
            if(!game.IsBuilding&&tile&&board.Model.IsOwned(tile.coordinate))game.BeginBuilding();
            if(game.IsBuilding)board.Click(tile);
        }
    }
    void Cancel(InputAction.CallbackContext context) { if (game.IsBuilding) game.EndBuilding(); }
    void Update()
    {
        if (pointer == null) return;
        if(pan.IsPressed())
        {
            Vector2 position=pointer.ReadValue<Vector2>();
            if(!panBlocked)orbit.Pan(position-panPrevious);
            panPrevious=position;
        }
        if(confirming||rotate.IsPressed())
        {
            float movement=gesture.Move(pointer.ReadValue<Vector2>());
            if(rotate.IsPressed())orbit.Orbit(movement);
        }
        if (!OverUI())
        {
            orbit.Scroll(zoom.ReadValue<Vector2>().y);
        }
        board.Hover(game.IsBuilding ? Tile() : null);
    }
}

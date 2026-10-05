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
    InputAction pointer, confirm, cancel, rotate, delta, zoom;
    InputActionAsset runtimeActions;
    const string Preferences = "StackStore.HexWorld.Bindings";
    readonly List<RaycastResult> hits = new List<RaycastResult>();
    void OnEnable()
    {
        if (!actions) return;
        runtimeActions = Instantiate(actions);
        if (PlayerPrefs.HasKey(Preferences)) runtimeActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(Preferences));
        pointer = runtimeActions.FindAction("World/Point", true); confirm = runtimeActions.FindAction("World/Confirm", true);
        cancel = runtimeActions.FindAction("World/Cancel", true); rotate = runtimeActions.FindAction("World/Orbit", true);
        delta = runtimeActions.FindAction("World/Look", true); zoom = runtimeActions.FindAction("World/Zoom", true);
        confirm.performed += Confirm; cancel.performed += Cancel; runtimeActions.Enable();
    }
    void OnDisable()
    {
        if (!runtimeActions || confirm == null) return;
        confirm.performed -= Confirm; cancel.performed -= Cancel; runtimeActions.Disable(); Destroy(runtimeActions);
        pointer = confirm = cancel = rotate = delta = zoom = null;
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
    void Confirm(InputAction.CallbackContext context) { if (game.IsBuilding) board.Click(Tile()); }
    void Cancel(InputAction.CallbackContext context) { if (game.IsBuilding) game.EndBuilding(); }
    void Update()
    {
        if (pointer == null) return;
        if (!OverUI())
        {
            if (rotate.IsPressed()) orbit.Orbit(delta.ReadValue<Vector2>().y);
            orbit.Scroll(zoom.ReadValue<Vector2>().y);
        }
        board.Hover(game.IsBuilding ? Tile() : null);
    }
}

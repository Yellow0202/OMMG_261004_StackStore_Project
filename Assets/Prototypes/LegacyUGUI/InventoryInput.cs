using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Uses a semantic action. Binding overrides can be edited and saved by future settings UI.</summary>
public sealed class InventoryInput : MonoBehaviour
{
    public InputActionReference openInventory;
    public GameObject inventoryPanel;
    public GoldLevelSystem levelSystem;
    bool wasEnabled;
    const string OverridesKey = "StackStore.InputBindingOverrides";

    void OnEnable()
    {
        if (!openInventory || openInventory.action == null) return;
        var action = openInventory.action;
        string saved = PlayerPrefs.GetString(OverridesKey, "");
        if (!string.IsNullOrEmpty(saved)) action.actionMap.asset.LoadBindingOverridesFromJson(saved);
        wasEnabled = action.enabled;
        action.performed += Toggle;
        action.Enable();
    }
    void OnDisable()
    {
        if (!openInventory || openInventory.action == null) return;
        openInventory.action.performed -= Toggle;
        if (!wasEnabled) openInventory.action.Disable();
    }
    void Toggle(InputAction.CallbackContext context)
    {
        if (inventoryPanel && (!levelSystem || !levelSystem.IsChoosing)) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
    public void SetInventoryBinding(string controlPath)
    {
        if (!openInventory || string.IsNullOrWhiteSpace(controlPath)) return;
        bool enabledBefore = openInventory.action.enabled;
        if (enabledBefore) openInventory.action.Disable();
        openInventory.action.ApplyBindingOverride(0, controlPath);
        if (enabledBefore) openInventory.action.Enable();
        PlayerPrefs.SetString(OverridesKey, openInventory.action.actionMap.asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }
}

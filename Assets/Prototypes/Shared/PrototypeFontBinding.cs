using UnityEngine;
using UnityEngine.UI;

/// <summary>Bind a live OS font; a serialized OS font has no usable glyph atlas.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PrototypeFontBinding : MonoBehaviour
{
    static Font liveFont;
    static bool warned;

    void OnEnable() { Apply(); }

    public void Apply()
    {
        if (!liveFont)
        {
            string[] installed = Font.GetOSInstalledFontNames();
            if (System.Array.IndexOf(installed, "Malgun Gothic") < 0)
            {
                if (!warned)
                    Debug.LogWarning("Prototype requires the Windows Malgun Gothic font. Add a distributable Korean font for alpha development.", this);
                warned = true;
                return;
            }
            liveFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 20);
            liveFont.hideFlags = HideFlags.HideAndDontSave;
        }

        foreach (Text label in GetComponentsInChildren<Text>(true))
        {
            label.font = liveFont;
            label.SetAllDirty();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class LocalizationService : MonoBehaviour
{
    public LocalizationTable table;
    public DisplayLanguage language = DisplayLanguage.Korean;
    public static LocalizationService Instance { get; private set; }
    public static event Action LanguageChanged;
    static readonly Regex Variable = new Regex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}");
    readonly HashSet<string> reported = new HashSet<string>();
    DisplayLanguage appliedLanguage;

    void Awake() { Instance = this; appliedLanguage = language; }
    void Update() { if (language != appliedLanguage) { appliedLanguage = language; LanguageChanged?.Invoke(); } }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void SetLanguage(DisplayLanguage value) { language = value; appliedLanguage = value; LanguageChanged?.Invoke(); }

    public static string Text(string key, params object[] nameValuePairs)
    {
        if (!Instance || !Instance.table) return "[" + key + "]";
        var data = new Dictionary<string, object>();
        if (nameValuePairs.Length % 2 != 0) throw new ArgumentException("Localization data must be name/value pairs.");
        for (int i = 0; i < nameValuePairs.Length; i += 2) data.Add((string)nameValuePairs[i], nameValuePairs[i + 1]);
        return Instance.Format(key, data);
    }

    public string Format(string key, IReadOnlyDictionary<string, object> data)
    {
        string value = table.Resolve(key, language);
        return Variable.Replace(value, match =>
        {
            string name = match.Groups[1].Value;
            if (data != null && data.TryGetValue(name, out var item))
                return item is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : item?.ToString() ?? "";
            if (reported.Add(key + "/" + name)) Debug.LogWarning("Missing localization variable: " + key + " / " + name, this);
            return match.Value;
        });
    }
}

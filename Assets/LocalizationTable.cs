using System;
using System.Collections.Generic;
using UnityEngine;

public enum DisplayLanguage { English, Korean }

[Serializable]
public class TranslationEntry
{
    public string key;
    [TextArea] public string english;
    [TextArea] public string korean;
}

[CreateAssetMenu(menuName = "Stack Store/Localization Table")]
public sealed class LocalizationTable : ScriptableObject
{
    public List<TranslationEntry> entries = new List<TranslationEntry>();
    Dictionary<string, TranslationEntry> index;
    readonly HashSet<string> missing = new HashSet<string>();

    public void Rebuild()
    {
        index = new Dictionary<string, TranslationEntry>();
        missing.Clear();
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
            if (index.ContainsKey(entry.key)) Debug.LogWarning("Duplicate localization key: " + entry.key, this);
            else index.Add(entry.key, entry);
        }
    }

    public string Resolve(string key, DisplayLanguage language)
    {
        if (index == null) Rebuild();
        if (!index.TryGetValue(key, out var entry)) { if (missing.Add(key)) Debug.LogWarning("Missing localization key: " + key, this); return "[" + key + "]"; }
        return language == DisplayLanguage.Korean && !string.IsNullOrEmpty(entry.korean) ? entry.korean : entry.english;
    }

    void OnEnable() { index = null; }
    void OnValidate() { index = null; }
}

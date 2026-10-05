using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public sealed class LocalizedLabel : MonoBehaviour
{
    public string key;
    void OnEnable() { LocalizationService.LanguageChanged += Refresh; Refresh(); }
    void OnDisable() { LocalizationService.LanguageChanged -= Refresh; }
    void Refresh() { GetComponent<Text>().text = LocalizationService.Text(key); }
}

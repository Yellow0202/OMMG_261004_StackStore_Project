using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemKind { Food, Ability, ShopPart }
public enum ItemEffectKind { ServiceSpeedPercent, CustomerPatienceSeconds, VisitorChance }

[Serializable]
public struct ItemEffect
{
    public ItemEffectKind kind;
    public float amountPerLevel;
}

[CreateAssetMenu(menuName = "Stack Store/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    public Sprite picture;
    public string nameKey;
    public string descriptionKey;
    public string key;
    public ItemKind kind;
    [Tooltip("0 = unlimited upgrades")]
    [Min(0)] public int maxLevel;
    public List<ItemEffect> effects = new List<ItemEffect>();

    public string DisplayName => LocalizationService.Text(nameKey);
    public string DisplayDescription()
    {
        var totals = new Dictionary<string, float>();
        foreach (var effect in effects)
        {
            string name = effect.kind.ToString();
            totals.TryGetValue(name, out float current);
            totals[name] = current + (effect.kind == ItemEffectKind.CustomerPatienceSeconds ? effect.amountPerLevel : effect.amountPerLevel * 100f);
        }
        var values = new List<object>();
        foreach (var pair in totals) { values.Add(pair.Key); values.Add(pair.Value); }
        return LocalizationService.Text(descriptionKey, values.ToArray());
    }
}

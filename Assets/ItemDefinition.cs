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
    public string itemName;
    [TextArea] public string description;
    public string key;
    public ItemKind kind;
    [Tooltip("0 = unlimited upgrades")]
    [Min(0)] public int maxLevel;
    public List<ItemEffect> effects = new List<ItemEffect>();
}

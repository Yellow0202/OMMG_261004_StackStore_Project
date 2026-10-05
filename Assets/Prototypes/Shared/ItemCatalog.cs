using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stack Store/Item Catalog")]
public sealed class ItemCatalog : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();
}

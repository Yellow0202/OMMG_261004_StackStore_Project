using UnityEngine;

/// <summary>Spawn pool. Each entry owns eight camera-relative directions and eight walk frames.</summary>
[CreateAssetMenu(menuName="Stack Store/Hex World/Customer Animation Catalog")]
public sealed class HexCustomerAnimationCatalog : ScriptableObject
{
    public HexPlayerAnimationSet[] characters = new HexPlayerAnimationSet[0];
    public bool IsValid => characters!=null && characters.Length>0
        && System.Array.TrueForAll(characters,x=>x && x.IsValid);
    public HexPlayerAnimationSet Pick()
        => IsValid ? characters[Random.Range(0,characters.Length)] : null;
}

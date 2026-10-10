using UnityEngine;

/// <summary>Feed the six actual neighbours to the floor shader without changing ownership.</summary>
public static class HexFloorBlend
{
    static readonly int[] Maps=new int[6],Tints=new int[6],Contrasts=new int[6];
    static HexFloorBlend()
    {
        for(int d=0;d<6;d++){Maps[d]=Shader.PropertyToID("_Neighbor"+d);Tints[d]=Shader.PropertyToID("_NeighborTint"+d);Contrasts[d]=Shader.PropertyToID("_NeighborContrast"+d);}
    }
    public static void Apply(MaterialPropertyBlock properties,HexEnvironmentTheme theme,HexShopLayout layout,int floor,Vector2Int at)
    {
        properties.SetFloat("_BlendWidth",theme.floorBlendWidth);
        properties.SetTexture("_GroundMap",theme.groundMaterial?theme.groundMaterial.GetTexture("_BaseMap"):Texture2D.whiteTexture);
        properties.SetColor("_GroundTint",theme.groundTint);properties.SetFloat("_GroundContrast",theme.groundContrast);
        properties.SetFloat("_GroundMeters",Mathf.Max(.1f,theme.pavingRepeatMeters));
        var flagsA=Vector4.zero;var flagsB=Vector4.zero;
        var model=layout!=null?layout.Floor(floor).model:null;
        for(int d=0;d<6;d++)
        {
            var neighbor=at+HexBoardModel.Directions[d];bool owned=model!=null&&model.IsOwned(neighbor);
            var definition=owned?model.Definition(neighbor):null;
            var material=owned?theme.Floor(definition?definition.kind:HexTileKind.DisplayShelf):null;
            owned=owned&&material;
            if(d<4)flagsA[d]=owned?1:0;else flagsB[d-4]=owned?1:0;
            properties.SetTexture(Maps[d],owned?material.GetTexture("_BaseMap"):Texture2D.whiteTexture);
            properties.SetColor(Tints[d],owned?material.GetColor("_BaseColor"):Color.white);
            properties.SetFloat(Contrasts[d],owned?material.GetFloat("_Contrast"):1);
        }
        properties.SetVector("_OwnedA",flagsA);properties.SetVector("_OwnedB",flagsB);
    }
}

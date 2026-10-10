Shader "StackStore/HexFloorBlend"
{
    Properties
    {
        _BaseMap("Role floor",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Height("Local floor height",Float)=.215
        _Contrast("Contrast",Float)=1
        _GroundMap("Exterior ground",2D)="white"{}
        _GroundTint("Exterior tint",Color)=(1,1,1,1)
        _GroundContrast("Exterior contrast",Float)=.5
        _GroundMeters("Exterior repeat",Float)=2.8
        _BlendWidth("Transition width",Float)=.45
        _OwnedA("Owned neighbours 0-3",Vector)=(0,0,0,0)
        _OwnedB("Owned neighbours 4-5",Vector)=(0,0,0,0)
        _Neighbor0("Neighbour 0",2D)="white"{}
        _NeighborTint0("Neighbour tint 0",Color)=(1,1,1,1)
        _NeighborContrast0("Neighbour contrast 0",Float)=1
        _Neighbor1("Neighbour 1",2D)="white"{}
        _NeighborTint1("Neighbour tint 1",Color)=(1,1,1,1)
        _NeighborContrast1("Neighbour contrast 1",Float)=1
        _Neighbor2("Neighbour 2",2D)="white"{}
        _NeighborTint2("Neighbour tint 2",Color)=(1,1,1,1)
        _NeighborContrast2("Neighbour contrast 2",Float)=1
        _Neighbor3("Neighbour 3",2D)="white"{}
        _NeighborTint3("Neighbour tint 3",Color)=(1,1,1,1)
        _NeighborContrast3("Neighbour contrast 3",Float)=1
        _Neighbor4("Neighbour 4",2D)="white"{}
        _NeighborTint4("Neighbour tint 4",Color)=(1,1,1,1)
        _NeighborContrast4("Neighbour contrast 4",Float)=1
        _Neighbor5("Neighbour 5",2D)="white"{}
        _NeighborTint5("Neighbour tint 5",Color)=(1,1,1,1)
        _NeighborContrast5("Neighbour contrast 5",Float)=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GroundMap);SAMPLER(sampler_GroundMap);
            TEXTURE2D(_Neighbor0);
            TEXTURE2D(_Neighbor1);
            TEXTURE2D(_Neighbor2);
            TEXTURE2D(_Neighbor3);
            TEXTURE2D(_Neighbor4);
            TEXTURE2D(_Neighbor5);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor,_GroundTint;float4 _OwnedA,_OwnedB;
            float _Height,_GroundMeters,_BlendWidth;half _Contrast,_GroundContrast;
            half4 _NeighborTint0;half _NeighborContrast0;
            half4 _NeighborTint1;half _NeighborContrast1;
            half4 _NeighborTint2;half _NeighborContrast2;
            half4 _NeighborTint3;half _NeighborContrast3;
            half4 _NeighborTint4;half _NeighborContrast4;
            half4 _NeighborTint5;half _NeighborContrast5;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 localXZ:TEXCOORD0;float2 worldXZ:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.localXZ=input.positionOS.xz;input.positionOS.y+=_Height;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.worldXZ=TransformObjectToWorld(input.positionOS.xyz).xz;return output;
            }
            half4 Tone(half4 color,half4 tint,half contrast)
            {color.rgb=(color.rgb-.5h)*contrast+.5h;return color*tint;}
            half4 Frag(Varyings input):SV_Target
            {
                half4 own=Tone(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.localXZ/2.8+.5),_BaseColor,_Contrast);
                if(_BlendWidth<=.0001)return own;
                half4 total=own;float sum=1,coverage=1,distance,weight;
                float width=max(.0001,_BlendWidth);
                distance=1.212435565-dot(input.localXZ,float2(1,0));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedA[0]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor0,sampler_BaseMap,(input.localXZ-float2(2.42487113,0.0))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint0,_NeighborContrast0)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                distance=1.212435565-dot(input.localXZ,float2(0.5,-0.866025404));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedA[1]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor1,sampler_BaseMap,(input.localXZ-float2(1.212435565,-2.1000000000061867))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint1,_NeighborContrast1)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                distance=1.212435565-dot(input.localXZ,float2(-0.5,-0.866025404));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedA[2]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor2,sampler_BaseMap,(input.localXZ-float2(-1.212435565,-2.1000000000061867))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint2,_NeighborContrast2)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                distance=1.212435565-dot(input.localXZ,float2(-1,0));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedA[3]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor3,sampler_BaseMap,(input.localXZ-float2(-2.42487113,0.0))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint3,_NeighborContrast3)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                distance=1.212435565-dot(input.localXZ,float2(-0.5,0.866025404));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedB[0]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor4,sampler_BaseMap,(input.localXZ-float2(-1.212435565,2.1000000000061867))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint4,_NeighborContrast4)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                distance=1.212435565-dot(input.localXZ,float2(0.5,0.866025404));
                weight=1-smoothstep(0,width,max(0,distance));
                if(_OwnedB[1]>.5)
                {
                    half4 neighbor=SAMPLE_TEXTURE2D(_Neighbor5,sampler_BaseMap,(input.localXZ-float2(1.212435565,2.1000000000061867))/2.8+.5);
                    total+=Tone(neighbor,_NeighborTint5,_NeighborContrast5)*weight;sum+=weight;
                }
                else coverage=min(coverage,1-weight);
                half4 ground=Tone(SAMPLE_TEXTURE2D(_GroundMap,sampler_GroundMap,input.worldXZ/max(.1,_GroundMeters)),_GroundTint,_GroundContrast);
                return lerp(ground,total/sum,coverage);
            }
            ENDHLSL
        }
    }
}

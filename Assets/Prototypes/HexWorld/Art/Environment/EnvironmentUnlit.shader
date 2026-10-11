Shader "StackStore/EnvironmentUnlit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Height("Local floor height",Float)=0
        _Contrast("Contrast",Range(0,1))=1
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
            #include "HexExteriorFog.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; half4 _BaseColor; float _Height; half _Contrast;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 worldXZ:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;input.positionOS.y+=_Height;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.worldXZ=TransformObjectToWorld(input.positionOS.xyz).xz;
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                color.rgb=(color.rgb-.5h)*_Contrast+.5h;color*=_BaseColor;color.rgb=HexFogTint(color.rgb,input.worldXZ);return color;
            }
            ENDHLSL
        }
    }
}

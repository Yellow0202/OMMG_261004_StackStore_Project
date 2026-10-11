Shader "StackStore/Fog Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _FogPerVertex("Fog per vertex",Float)=0
        [MaterialToggle] PixelSnap("Pixel snap",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" "DisableBatching"="True"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "HexExteriorFog.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;float _FogPerVertex;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float2 anchor:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS.xyz=UnityFlipSprite(input.positionOS.xyz,unity_SpriteProps.xy);
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.uv=input.uv;o.color=input.color*_Color*unity_SpriteColor;
                o.anchor=lerp(TransformObjectToWorld(float3(0,0,0)).xz,TransformObjectToWorld(input.positionOS.xyz).xz,_FogPerVertex);return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color;
                color.rgb=HexFogTint(color.rgb,input.anchor);return color;
            }
            ENDHLSL
        }
    }
}

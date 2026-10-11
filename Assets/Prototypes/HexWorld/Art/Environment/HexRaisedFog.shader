Shader "StackStore/Raised Exterior Fog"
{
    Properties { _NearOpacity("Near side opacity",Range(0,1))=.25 _Color("Fog color",Color)=(1,1,1,1) _Cloud("Opacity, scale, speed, height",Vector)=(.48,1.1,.18,2.6) }
    SubShader
    {
        Tags {"Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HexExteriorFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;float4 _Cloud;float _NearOpacity;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float3 normal:TEXCOORD2;};
            float Hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float Noise(float3 p)
            {
                float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),
                  lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+1),f.x),f.y),f.z);
            }
            V Vert(A input)
            {
                V o;o.world=TransformObjectToWorld(input.positionOS.xyz);
                o.world.y*=1+.13*sin(o.world.x*1.7+o.world.z*2.3);
                o.normal=TransformObjectToWorldNormal(input.normal);o.positionCS=TransformWorldToHClip(o.world);o.uv=input.uv;return o;
            }
            half4 Frag(V input):SV_Target
            {
                float3 p=input.world*_Cloud.y+float3(_Time.y*_Cloud.z,0,_Time.y*_Cloud.z*.6);
                float n=Noise(p)*.7+Noise(p*2.1)*.3;
                float top=1-smoothstep(.15+n*.5,.7+n*.3,input.uv.y);
                float side=smoothstep(0,.03,input.uv.x)*(1-smoothstep(.97,1,input.uv.x));
                float outside=smoothstep(0,.3,HexFogAmount(input.world.xz));
                float nearSide=saturate(dot(normalize(GetCameraPositionWS().xz-input.world.xz),input.normal.xz));
                float alpha=top*side*(.25+smoothstep(.2,.8,n)*.9)*_Cloud.x*outside*lerp(1,_NearOpacity,nearSide);
                // Broad shaded lobes convey height and depth without fine surface detail.
                half3 color=_Color.rgb*lerp(.82,1.0,n*.6+input.uv.y*.4);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}

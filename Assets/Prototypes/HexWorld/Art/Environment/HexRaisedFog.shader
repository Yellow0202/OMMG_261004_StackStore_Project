Shader "StackStore/Raised Exterior Fog"
{
    Properties { _NearOpacity("Near side opacity",Range(0,1))=.12 _Color("Fog color",Color)=(1,1,1,1) _Cloud("Opacity, scale, speed, height",Vector)=(.95,1.1,.08,1.5) }
    SubShader
    {
        Tags {"Queue"="Transparent-50" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Back ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HexExteriorFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;float4 _Cloud;float _NearOpacity;float4 _ShopCenter;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normal:NORMAL;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            V Vert(A input)
            {
                V o;o.world=TransformObjectToWorld(input.positionOS.xyz);
                float wave=sin(o.world.x*_Cloud.y+_Time.y*_Cloud.z)*sin(o.world.z*_Cloud.y*.8-_Time.y*_Cloud.z*.7);
                o.world+=input.normal*(wave*.1);
                o.normal=TransformObjectToWorldNormal(input.normal);o.positionCS=TransformWorldToHClip(o.world);return o;
            }
            half4 Frag(V input):SV_Target
            {
                float outside=HexFogAmount(input.world.xz);clip(outside-.015);
                float3 normal=normalize(input.normal);
                float light=saturate(dot(normal,normalize(float3(-.4,.85,-.3)))*.5+.5);
                float broad=sin(input.world.x*_Cloud.y*.7+input.world.z*_Cloud.y*.9+_Time.y*_Cloud.z)*.015;
                half3 color=_Color.rgb*(lerp(.72,1,light)+broad);
                float2 outward=normalize(input.world.xz-_ShopCenter.xy+float2(.001,.001));
                float nearSide=smoothstep(.05,.35,dot(normalize(GetCameraPositionWS().xz-_ShopCenter.xy),outward));
                float alpha=smoothstep(0,.3,outside)*_Cloud.x*lerp(1,_NearOpacity,nearSide);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}

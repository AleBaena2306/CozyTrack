// Sombreado de Cozy Track: «Normal» (luz suave + cielo/suelo) o «Cel shading» (dos tonos, sombra teñida,
// brillo de borde y contorno de casco invertido), según la global _CozyToon.
Shader "CozyTrack/Toon"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Textura", 2D) = "white" {}
        _EmissionColor ("Emisión", Color) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras", Float) = 2
        _Outline ("Contorno (0/1)", Float) = 1
        _Rim ("Brillo de borde (0/1)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "CozyCommon.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CozyLight.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 cs : SV_POSITION; float fog : TEXCOORD0; };
            V vert (A a)
            {
                V o;
                float w = _CozyOutlineWidth * _Outline * step(0.5, _CozyToon);
                float3 ws = TransformObjectToWorld(a.pos.xyz) + TransformObjectToWorldNormal(a.n) * w;
                o.cs = TransformWorldToHClip(ws);
                if (w <= 0) o.cs = float4(0, 0, -10, 1);   // sin contorno: fuera de la vista
                o.fog = ComputeFogFactor(o.cs.z);
                return o;
            }
            half4 frag (V i) : SV_Target { return half4(MixFog(float3(0.026, 0.023, 0.047), i.fog), 1); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CozyShadow.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; };
            struct V { float4 cs : SV_POSITION; };
            V vert (A a) { V o; o.cs = TransformObjectToHClip(a.pos.xyz); return o; }
            half frag (V i) : SV_Target { return i.cs.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 cs : SV_POSITION; float3 n : TEXCOORD0; };
            V vert (A a) { V o; o.cs = TransformObjectToHClip(a.pos.xyz); o.n = TransformObjectToWorldNormal(a.n); return o; }
            half4 frag (V i) : SV_Target { return half4(normalize(i.n), 0); }
            ENDHLSL
        }
    }
}

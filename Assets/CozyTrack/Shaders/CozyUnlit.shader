// Sin luz, con color de vértice y transparencia: partículas, línea de frenada, confeti.
Shader "CozyTrack/Unlit"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Textura", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Mezcla destino", Float) = 10
        _OffsetF ("Desplazamiento de profundidad (factor)", Float) = 0
        _OffsetU ("Desplazamiento de profundidad (unidades)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha [_DstBlend]
            ZWrite Off Cull Off
            Offset [_OffsetF], [_OffsetU]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _BaseMap_ST; float _DstBlend; float _OffsetF; float _OffsetU;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; };
            struct V { float4 cs : SV_POSITION; float2 uv : TEXCOORD0; float4 col : COLOR; float fog : TEXCOORD1; };
            V vert (A a) { V o; o.cs = TransformObjectToHClip(a.pos.xyz); o.uv = TRANSFORM_TEX(a.uv, _BaseMap); o.col = a.col; o.fog = ComputeFogFactor(o.cs.z); return o; }
            half4 frag (V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * i.col;
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}

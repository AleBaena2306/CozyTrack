// Detección de bordes (estilo «Cómic»): Sobel sobre profundidad y normales de la cámara.
// Lo usa un Full Screen Pass Renderer Feature (requisitos: Depth y Normal) que añade COZY TRACK > Configurar proyecto.
Shader "CozyTrack/Edges"
{
    Properties { _Ink ("Tinta", Color) = (0.026, 0.023, 0.047, 1) _Strength ("Intensidad", Float) = 0.92 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "Edges"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            float4 _Ink; float _Strength;
            half4 Frag (Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 px = 1.4 / _ScreenParams.xy;
                float dc = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float3 nc = SampleSceneNormals(uv);
                float de = 0, ne = 0;
                const float2 offs[8] = { float2(1,0), float2(-1,0), float2(0,1), float2(0,-1), float2(1,1), float2(-1,1), float2(1,-1), float2(-1,-1) };
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float2 q = uv + offs[k] * px;
                    de = max(de, abs(LinearEyeDepth(SampleSceneDepth(q), _ZBufferParams) - dc) / max(dc, 0.001));
                    ne = max(ne, distance(SampleSceneNormals(q), nc));
                }
                float edge = max(smoothstep(0.035, 0.07, de), smoothstep(0.45, 0.7, ne));
                col.rgb = lerp(col.rgb, _Ink.rgb, edge * _Strength);
                return col;
            }
            ENDHLSL
        }
    }
}

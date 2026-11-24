#ifndef COZY_COMMON_INCLUDED
#define COZY_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseMap_ST;
    float4 _EmissionColor;
    float _Cull;
    float _Outline;
    float _Rim;
CBUFFER_END
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

// globales que fija el juego (estilo y hora del día)
float _CozyToon;          // 0 = normal, 1 = cel shading
float _CozyOutlineWidth;  // grosor del contorno de casco invertido
float4 _CozySky, _CozyGround, _CozyShadowTint;
#endif

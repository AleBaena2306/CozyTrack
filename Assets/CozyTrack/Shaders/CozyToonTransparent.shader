// Igual que CozyTrack/Toon pero translúcido (cristal del rizo, charcos, fantasma, superficies). Sin contorno.
Shader "CozyTrack/Toon Transparent"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,0.5)
        _BaseMap ("Textura", 2D) = "white" {}
        _EmissionColor ("Emisión", Color) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras", Float) = 0
        _Outline ("Contorno", Float) = 0
        _Rim ("Brillo de borde", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        HLSLINCLUDE
        #include "CozyCommon.hlsl"
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
    }
}

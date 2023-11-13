Shader "Custom RP/Unlit Transparent"
{

    Properties
    {
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Intensity("Intensity", Float) = 1.0
    }

    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
}
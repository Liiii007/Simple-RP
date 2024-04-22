Shader "Custom RP/Unlit Particle"
{
    Properties
    {
        _Intensity("Intensity", Float) = 1.0
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass_Particle.hlsl"
            ENDHLSL
        }
    }
}
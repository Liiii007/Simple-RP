Shader "Hidden/Custom RP/Blur FX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "Blur.hlsl"
        ENDHLSL

        Pass
        {
            Name "Copy"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Horizontal"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment BlurHorizontalPassFragment
            ENDHLSL
        }
        Pass
        {
            Name "Bloom Vertical"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment BlurVerticalPassFragment
            ENDHLSL
        }
    }
}
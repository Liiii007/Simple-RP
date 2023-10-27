Shader "Simple/CustomLineShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _BoundColor ("BoundColor", Color) = (1,1,1,1)
        _Width ("Width", float) = 0.1
        _AARate ("AA Rate", float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
        }

        HLSLINCLUDE
        #include "LineRendererShader.hlsl"
        ENDHLSL

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {

            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment OuterPassFragment
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment InnerPassFragment
            ENDHLSL
        }
    }
}
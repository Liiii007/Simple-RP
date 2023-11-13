Shader "Custom RP/ColorDraw"
{
    Properties
    {
        [HDR] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
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
            #include "../../ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 baseUV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenUV = input.baseUV;
                return output;
            }

            half4 UnlitPassFragment(Varyings input) : SV_TARGET
            {
                float4 color = _BaseColor;
                color *= _Intensity;
                color.a = _BaseColor.a;
                return color;
            }
            ENDHLSL
        }
    }
}
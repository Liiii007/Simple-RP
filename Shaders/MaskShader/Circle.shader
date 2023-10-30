Shader "Custom RP/RingMask"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Radius("Radius", float) = 0.5
        _FillRate("FillRate", float) = 0.05
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
            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/SDF.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Radius;
                float _FillRate;
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
                float4 color = _Color;
                color.a *= RingSDF(input.screenUV, _Radius, _FillRate);
                return color;
            }
            ENDHLSL
        }
    }
}
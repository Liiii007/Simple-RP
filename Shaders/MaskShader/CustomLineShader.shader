Shader "Simple/CustomLineShader"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _BoundColor ("BoundColor", Color) = (0,0,0,1)
        _Intensity ("Intensity", float) = 1
        _Radius ("Radius", float) = 0
        _BoundWidth ("BoundWidth", float) = 0
        _AARate ("AA Rate", float) = 1
    }

    HLSLINCLUDE
    #include "../../ShaderLibrary/Common.hlsl"
    #include "../../ShaderLibrary/SDF.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "InnerPass"
            Tags
            {
                "LightMode" = "SRPPass2"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct AttributesDown
            {
                float3 positionOS : POSITION;
                float3 a : TEXCOORD0;
                float3 b : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 a : TEXCOORD0;
                float3 b : TEXCOORD1;
                float3 pos : TEXCOORD2;
            };

            float4 _Color;
            float4 _BoundColor;
            float _Intensity;
            float _Radius;
            float _BoundWidth;
            float _AARate;

            Varyings UnlitVertex(AttributesDown v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.pos = v.positionOS;
                o.a = v.a;
                o.b = v.b;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float _Delta = 0.01f * _AARate;
                float sdf = LineSegmentSDF(i.pos, i.a, i.b);
                float inner = sdf;
                inner = smoothstep(inner - _Delta, inner + _Delta, _Radius * (1 - _BoundWidth) - 0.1);
                float4 color = _Color;
                color *= _Intensity;
                return half4(color.rgb, inner * _Color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outer"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct AttributesDown
            {
                float3 positionOS : POSITION;
                float3 a : TEXCOORD0;
                float3 b : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 a : TEXCOORD0;
                float3 b : TEXCOORD1;
                float3 pos : TEXCOORD2;
            };

            float4 _Color;
            float4 _BoundColor;
            float _Intensity;
            float _Radius;
            float _BoundWidth;
            float _AARate;

            Varyings UnlitVertex(AttributesDown v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.pos = v.positionOS;
                o.a = v.a;
                o.b = v.b;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float _Delta = 0.01f * _AARate;
                float sdf = LineSegmentSDF(i.pos, i.a, i.b);
                float inner = sdf;
                inner = smoothstep(inner - _Delta, inner + _Delta, _Radius);
                return half4(_BoundColor.xyz, inner * _Color.a);
            }
            ENDHLSL
        }
    }
}
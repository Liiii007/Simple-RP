Shader "Simple/RectMask"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BoundColor ("BoundColor", Color) = (1,1,1,1)
        _Radius ("Radius", float) = 10
        _Ratio ("Ratio", float) = 1
        _Width ("Width", float) = 0.1
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

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct AttributesDown
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _BoundColor;
                float _Radius;
                float _Ratio;
                float _Width;
            CBUFFER_END

            Varyings UnlitVertex(AttributesDown v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float _Delta = 0.0015f;
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float2 uv_flipped = abs(step(0.5, i.uv) - i.uv);
                float2 uv_normalize = float2(uv_flipped.x, uv_flipped.y * _Ratio);
                float2 uv_vector = uv_normalize - _Radius + _Delta / 2;
                float len = length(uv_vector);
                float pass_big = smoothstep(_Radius - _Delta, _Radius + _Delta, uv_normalize.x) +
                    smoothstep(_Radius - _Delta, _Radius + _Delta, uv_normalize.y);

                pass_big += smoothstep(len - _Delta, len + _Delta, _Radius);
                pass_big = clamp(pass_big, 0, 1);

                float n_radius = _Radius - _Width;
                float uv_vector_small = uv_normalize - n_radius;
                float pass_small = smoothstep(length(uv_vector) - _Delta, length(uv_vector) + _Delta, n_radius);
                pass_small = pass_small + (step(_Radius, uv_normalize.x) && step(_Width, uv_normalize.y)) + (
                    step(_Radius, uv_normalize.y) && step(_Width, uv_normalize.x));
                pass_small = clamp(pass_small, 0, 1);

                float bound = pass_big - pass_small;
                float4 boundColor = _BoundColor;
                boundColor *= (1 - pass_small);
                boundColor.a = bound;

                mainTex *= 1 - bound;

                float4 color = mainTex + boundColor;
                color.a = pass_big;
                color *= i.color;
                return color;
            }
            ENDHLSL
        }
    }
}
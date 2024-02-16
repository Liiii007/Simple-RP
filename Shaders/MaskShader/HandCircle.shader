Shader "Simple/CircleMask"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BoundColor ("BoundColor", Color) = (1,1,1,1)
        _Radius ("Radius", float) = 0.5
        _Ratio ("Ratio", float) = 1
        _Width ("Width", float) = 0.1
        _AARate ("AA Rate", float) = 1
        _TextureOffset ("Texture Modify", Vector) = (0,0,0,0)
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

            struct Attributes
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
                float4 _Color;
                float4 _BoundColor;
                float4 _TextureOffset;
                float _Radius;
                float _Ratio;
                float _Width;
                float _AARate;
            CBUFFER_END

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                float _Delta = 0.001f * _AARate;
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                                                       (i.uv + _TextureOffset.xy) * (_TextureOffset.zw + 1));

                i.uv -= 0.5f;
                float2 uv_n = float2(i.uv.x, i.uv.y * _Ratio);

                float len = length(uv_n);
                float pass_big = smoothstep(len - _Delta, len + _Delta, _Radius);
                float pass_small = smoothstep(len - _Delta, len + _Delta, _Radius - _Width);

                float4 color = mainTexColor * _Color;
                color *= float4(1, 1, 1, pass_big);
                color = pass_small * color + (1 - pass_small) * _BoundColor;

                color.a = pass_big * _Color.a;
                color *= i.color;
                return color;
            }
            ENDHLSL
        }
    }
}
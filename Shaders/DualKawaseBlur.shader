Shader "Hidden/Simple RP/DualKawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        struct Varying
        {
            float3 position : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct AttributesDown
        {
            float4 positionCS : SV_POSITION;
            float2 uv : VAR_BASE_UV0;
            float2 uv1 : VAR_BASE_UV1;
            float2 uv2 : VAR_BASE_UV2;
            float2 uv3 : VAR_BASE_UV3;
            float2 uv4 : VAR_BASE_UV4;
        };

        struct AttributesUp
        {
            float4 positionCS : SV_POSITION;
            float2 uv0 : VAR_BASE_UV0;
            float2 uv1 : VAR_BASE_UV1;
            float2 uv2 : VAR_BASE_UV2;
            float2 uv3 : VAR_BASE_UV3;
            float2 uv4 : VAR_BASE_UV4;
            float2 uv5 : VAR_BASE_UV5;
            float2 uv6 : VAR_BASE_UV6;
            float2 uv7 : VAR_BASE_UV7;
        };

        struct AttributesSimple
        {
            float4 positionCS : SV_POSITION;
            float2 uv : VAR_BASE_UV0;
        };

        Texture2D<float3> _MainTex;
        float4 _MainTex_TexelSize;
        SamplerState sampler_linear_clamp;

        float ClampMax;
        float Threshold;
        float ThresholdKnee;

        AttributesSimple PrefilterPassVertex(Varying input)
        {
            AttributesSimple output;
            output.positionCS = TransformObjectToHClip(input.position);
            output.uv = input.uv;
            return output;
        }

        AttributesDown DownsamplePassVertex(Varying input)
        {
            AttributesDown output;
            output.positionCS = TransformObjectToHClip(input.position);
            output.uv = input.uv;
            output.uv1 = input.uv + float2(-1, -1) * _MainTex_TexelSize.xy * 0.5; //↖
            output.uv2 = input.uv + float2(-1, 1) * _MainTex_TexelSize.xy * 0.5; //↙
            output.uv3 = input.uv + float2(1, -1) * _MainTex_TexelSize.xy * 0.5; //↗
            output.uv4 = input.uv + float2(1, 1) * _MainTex_TexelSize.xy * 0.5; //↘
            return output;
        }

        AttributesUp UpsamplePassVertex(Varying input)
        {
            AttributesUp output;
            output.positionCS = TransformObjectToHClip(input.position);
            output.uv0 = input.uv + float2(-1, -1) * _MainTex_TexelSize.xy * 0.5;
            output.uv1 = input.uv + float2(-1, 1) * _MainTex_TexelSize.xy * 0.5;
            output.uv2 = input.uv + float2(1, -1) * _MainTex_TexelSize.xy * 0.5;
            output.uv3 = input.uv + float2(1, 1) * _MainTex_TexelSize.xy * 0.5;
            output.uv4 = input.uv + float2(-2, 0) * _MainTex_TexelSize.xy * 0.5;
            output.uv5 = input.uv + float2(0, -2) * _MainTex_TexelSize.xy * 0.5;
            output.uv6 = input.uv + float2(2, 0) * _MainTex_TexelSize.xy * 0.5;
            output.uv7 = input.uv + float2(0, 2) * _MainTex_TexelSize.xy * 0.5;
            return output;
        }
        ENDHLSL

        Pass // 0
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DownsamplePassVertex
            #pragma fragment BloomPrefilterPassFragment

            half3 Prefilter(half3 color)
            {
                // User controlled clamp to limit crazy high broken spec
                color = min(ClampMax, color);

                // Thresholding
                half brightness = Max3(color.r, color.g, color.b);
                half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
                softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
                half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
                color *= multiplier;

                // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
                color = max(color, 0);
                return color;
            }

            half4 BloomPrefilterPassFragment(AttributesDown input) : SV_TARGET
            {
                half3 color = Prefilter(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv, 0)) * 2;
                half3 color1 = Prefilter(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv1, 0));
                half3 color2 = Prefilter(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv2, 0));
                half3 color3 = Prefilter(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv3, 0));
                half3 color4 = Prefilter(SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv4, 0));

                return half4(color / 8, 1.0);
            }
            ENDHLSL
        }

        Pass // 1
        {
            Name "Bloom Downsample"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DownsamplePassVertex
            #pragma fragment BloomDownsamplePassFragment

            float3 BloomDownsamplePassFragment(AttributesDown input) : SV_TARGET
            {
                float3 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv, 0) * 4;
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv1, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv2, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv3, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv4, 0);

                return color / 8;
            }
            ENDHLSL
        }

        Pass // 2
        {
            Name "Bloom Upsample"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex UpsamplePassVertex
            #pragma fragment BloomUpsamplePassFragment

            float3 BloomUpsamplePassFragment(AttributesUp input) : SV_TARGET
            {
                float3 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv0, 0) * 2;
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv1, 0) * 2;
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv2, 0) * 2;
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv3, 0) * 2;
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv4, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv5, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv6, 0);
                color += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, input.uv7, 0);

                return color / 12;
            }
            ENDHLSL
        }

    }
}
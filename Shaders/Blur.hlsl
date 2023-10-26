#ifndef BLUR_EFFECT
#define BLUR_EFFECT

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

float4 _MainTex_TexelSize;

TEXTURE2D(_MainTex);
SAMPLER(sampler_linear_clamp);

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex(Attributes input)
{
    Varyings output;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.screenUV = input.uv;

    return output;
}

float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, screenUV, 0);
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    float4 screen = GetSource(input.screenUV);
    return screen;
}

float4 BlurHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] = {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };

    for (int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * 2.0 * _MainTex_TexelSize.x;
        color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 BlurVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] = {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };

    for (int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * _MainTex_TexelSize.y;
        color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 TestPassFragment(Varyings input) : SV_TARGET
{
    float3 color = GetSource(input.screenUV);
    color.xyz = color.x;

    return float4(color, 1);
}

#endif

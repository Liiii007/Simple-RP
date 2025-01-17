#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerMaterial)
    float _Intensity;
    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
CBUFFER_END

struct AttributesDown
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float4 color : COLOR;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : TEXCOORD0;
    float4 color : COLOR;
};

Varyings UnlitPassVertex(AttributesDown input)
{
    Varyings output;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    output.baseUV = input.baseUV;
    output.color = input.color;

    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    half4 tex = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.baseUV, 0);
    clip(tex.a);
    float4 color = input.color * _Intensity;
    color.a = input.color.a;
    return color * tex;
}

#endif

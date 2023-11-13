#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

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
};

Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    float4 color = _BaseColor * _Intensity;
    color.a = _BaseColor.a;
    return color;
}

#endif

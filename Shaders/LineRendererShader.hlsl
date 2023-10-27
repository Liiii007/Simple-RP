#ifndef LINE_RENDERER_SHADER
#define LINE_RENDERER_SHADER

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/SDF.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _BoundColor;
    float _Width;
    float _BoundWidth;
    float _AARate;
CBUFFER_END

struct LineVertex
{
    float3 positionOS : POSITION;
    float3 a : TEXCOORD0;
    float3 b : TEXCOORD1;
};

struct LineFragment
{
    float4 positionCS : SV_POSITION;
    float3 a : TEXCOORD0;
    float3 b : TEXCOORD1;
    float3 pos : TEXCOORD2;
};

LineFragment DefaultPassVertex(LineVertex v)
{
    LineFragment o = (LineFragment)0;
    o.positionCS = TransformObjectToHClip(v.positionOS);
    o.pos = v.positionOS;
    o.a = v.a;
    o.b = v.b;
    return o;
}

half4 OuterPassFragment(LineFragment i) : SV_Target
{
    float aaDelta = 0.01f * _AARate;
    float sdf = LineSegmentSDF(i.pos, i.a, i.b);
    float inner = sdf;
    inner = smoothstep(inner - aaDelta, inner + aaDelta, _Width - _BoundWidth);
    return half4(_BoundColor.xyz, inner);
}

half4 InnerPassFragment(LineFragment i) : SV_Target
{
    float aaDelta = 0.01f * _AARate;
    float sdf = LineSegmentSDF(i.pos, i.a, i.b);
    float inner = sdf;
    inner = smoothstep(inner - aaDelta, inner + aaDelta, _Width);
    return half4(_Color.xyz, inner);
}

#endif

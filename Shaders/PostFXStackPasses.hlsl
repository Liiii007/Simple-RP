#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "CustomACES.hlsl"

float4 _ProjectionParams;
float4 _PostFXSource_TexelSize;
float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

#define Scatter             _Params.x
#define ClampMax            _Params.y
#define Threshold           _Params.z
#define ThresholdKnee       _Params.w
float _BloomIntensity;
float _Brightness;
float _Saturation;
float _Contrast;

TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_TexelSize;

struct Attributes
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_WORLD_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 uv : VAR_BASE_UV;
};

struct Varying
{
    float3 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, screenUV, 0);
}

float4 GetSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(
        TEXTURE2D_ARGS(_MainTex, sampler_MainTex), screenUV,
        _PostFXSource_TexelSize.zwxy, 1.0, 0.0
    );
}

float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetMainTexTexelSize()
{
    return _MainTex_TexelSize;
}

//Generate triangle which cover whole screen
Attributes DefaultPassVertex(Varying input)
{
    Attributes output;
    output.positionWS = TransformObjectToWorld(input.position);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normal);
    output.uv = input.uv;
    return output;
}

float4 CopyPassFragment(Attributes input) : SV_TARGET
{
    float4 screen = GetSource(input.uv);
    return screen;
}

float _BlurRandom;

half4 BloomHorizontalPassFragment(Attributes input) : SV_TARGET
{
    float texelSize = _MainTex_TexelSize.x;
    float2 uv = input.uv;

    // 9-tap gaussian blur on the downsampled source
    half3 c0 = GetSource(uv - float2(texelSize * 4.0, 0.0));
    half3 c1 = GetSource(uv - float2(texelSize * 3.0, 0.0));
    half3 c2 = GetSource(uv - float2(texelSize * 2.0, 0.0));
    half3 c3 = GetSource(uv - float2(texelSize * 1.0, 0.0));
    half3 c4 = GetSource(uv);
    half3 c5 = GetSource(uv + float2(texelSize * 1.0, 0.0));
    half3 c6 = GetSource(uv + float2(texelSize * 2.0, 0.0));
    half3 c7 = GetSource(uv + float2(texelSize * 3.0, 0.0));
    half3 c8 = GetSource(uv + float2(texelSize * 4.0, 0.0));

    half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
        + c4 * 0.22702703
        + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

    float v = 1 / 9.0;

    color = c0 * v + c1 * v + c2 * v + c3 * v + c4 * v + c5 * v + c6 * v + c7 * v + c8 * v;

    return half4(color, 1.0);
}

half4 BloomVerticalPassFragment(Attributes input) : SV_TARGET
{
    float texelSize = _MainTex_TexelSize.y;
    float2 uv = input.uv;

    // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
    half3 c0 = GetSource(uv - float2(0.0, texelSize * 3.23076923));
    half3 c1 = GetSource(uv - float2(0.0, texelSize * 1.38461538));
    half3 c2 = GetSource(uv);
    half3 c3 = GetSource(uv + float2(0.0, texelSize * 1.38461538));
    half3 c4 = GetSource(uv + float2(0.0, texelSize * 3.23076923));

    half3 color = c0 * 0.07027027 + c1 * 0.31621622
        + c2 * 0.22702703
        + c3 * 0.31621622 + c4 * 0.07027027;

    color = c0 * 0.2 + c1 * 0.2 + c2 * 0.2 + c3 * 0.2 + c4 * 0.2;

    return half4(color, 1);
}

half4 BloomCombinePassFragment(Attributes input) : SV_TARGET
{
    float3 lowRes = GetSource2(input.uv).rgb;
    float3 highRes = GetSource(input.uv).rgb;
    return half4(lerp(highRes, lowRes, 0.7), 1);
}

half4 BloomPrefilterPassFragment(Attributes input) : SV_TARGET
{
    half3 color = GetSource(input.uv).rgb;

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
    return half4(color, 1.0);
}

struct LumaNeighborhood
{
    float m, n, e, s, w, ne, se, sw, nw;
    float highest, lowest;
    float range;
};

float GetLuma(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
{
    float2 texelSize = GetMainTexTexelSize().xy;
    uv += float2(uOffset, vOffset) * texelSize;
    return sqrt(Luminance(GetSource(uv).rgb));
}

LumaNeighborhood GetLumaNeighborhood(float2 uv)
{
    LumaNeighborhood luma;
    luma.m = GetLuma(uv);
    luma.n = GetLuma(uv, 0, 1.0);
    luma.e = GetLuma(uv, 1.0, 0.0);
    luma.s = GetLuma(uv, 0.0, -1.0);
    luma.w = GetLuma(uv, -1.0, 0.0);
    luma.ne = GetLuma(uv, 1.0, 1.0);
    luma.se = GetLuma(uv, 1.0, -1.0);
    luma.sw = GetLuma(uv, -1.0, -1.0);
    luma.nw = GetLuma(uv, -1.0, 1.0);
    luma.highest = max(max(max(max(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.lowest = min(min(min(min(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.range = luma.highest - luma.lowest;
    return luma;
}

half4 ToneMappingACESPassFragment(Attributes input) : SV_TARGET
{
    half4 color = GetSource(input.uv);
    color += GetSource2(input.uv) * _BloomIntensity;

    color.rgb = clamp(color.rgb, 0, 60);
    color.rgb = ACESFitted(color.rgb);
    color.a = 1;
    //brigtness亮度直接乘以一个系数，也就是RGB整体缩放，调整亮度
    float3 finalColor = color * _Brightness;
    //saturation饱和度：首先根据公式计算同等亮度情况下饱和度最低的值：
    float gray = 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
    float3 grayColor = float3(gray, gray, gray);
    //根据Saturation在饱和度最低的图像和原图之间差值
    finalColor = lerp(grayColor, finalColor, _Saturation);
    //contrast对比度：首先计算对比度最低的值
    float3 avgColor = float3(0.5, 0.5, 0.5);
    //根据Contrast在对比度最低的图像和原图之间差值
    finalColor = lerp(avgColor, finalColor, _Contrast);

    color.rgb = finalColor;
    color.a = GetLuma(input.uv);

    return color;
}

half4 BW(Attributes input) : SV_TARGET
{
    half4 color = GetSource(input.uv);
    color.rgb = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
    color.a = 1;
    return color;
}

bool CanSkipFXAA(LumaNeighborhood luma)
{
    float fixedThreshold = 0.07f;
    float relativeThreshold = 0.166f;
    return luma.range < max(fixedThreshold, relativeThreshold * luma.highest);
}

float GetSubpixelBlendFactor(LumaNeighborhood luma)
{
    float filter = 2 * (luma.n + luma.e + luma.s + luma.w);
    filter += luma.ne + luma.se + luma.sw + luma.nw;
    filter *= 1.0 / 12.0;
    filter = abs(filter - luma.m);
    filter = saturate(filter / luma.range);
    filter = smoothstep(0.0, 1.0, filter);
    return filter * filter;
}

bool IsHorizontalEdge(LumaNeighborhood luma)
{
    float horizontal =
        2.0 * abs(luma.n + luma.s - 2.0 * luma.m) +
        abs(luma.ne + luma.se - 2.0 * luma.e) +
        abs(luma.nw + luma.sw - 2.0 * luma.w);
    float vertical =
        2.0 * abs(luma.e + luma.w - 2.0 * luma.m) +
        abs(luma.ne + luma.nw - 2.0 * luma.n) +
        abs(luma.se + luma.sw - 2.0 * luma.s);
    return horizontal >= vertical;
}

struct FXAAEdge
{
    bool isHorizontal;
    float pixelStep;
    float lumaGradient, otherLuma;
};

FXAAEdge GetFXAAEdge(LumaNeighborhood luma)
{
    FXAAEdge edge;
    edge.isHorizontal = IsHorizontalEdge(luma);
    float lumaP, lumaN;
    if (edge.isHorizontal)
    {
        edge.pixelStep = GetMainTexTexelSize().y;
        lumaP = luma.n;
        lumaN = luma.s;
    }
    else
    {
        edge.pixelStep = GetMainTexTexelSize().x;
        lumaP = luma.e;
        lumaN = luma.w;
    }

    float gradientP = abs(lumaP - luma.m);
    float gradientN = abs(lumaN - luma.m);

    if (gradientP < gradientN)
    {
        edge.pixelStep = -edge.pixelStep;
        edge.lumaGradient = gradientN;
        edge.otherLuma = lumaN;
    }
    else
    {
        edge.lumaGradient = gradientP;
        edge.otherLuma = lumaP;
    }

    return edge;
}

#define EXTRA_EDGE_STEPS 8
#define EDGE_STEP_SIZES 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
#define LAST_EDGE_STEP_GUESS 8.0

static const float edgeStepSizes[EXTRA_EDGE_STEPS] = {EDGE_STEP_SIZES};

float GetEdgeBlendFactor(LumaNeighborhood luma, FXAAEdge edge, float2 uv)
{
    float2 edgeUV = uv;
    float2 uvStep = 0.0;
    if (edge.isHorizontal)
    {
        edgeUV.y += 0.5f * edge.pixelStep;
        uvStep.x = GetMainTexTexelSize().x;
    }
    else
    {
        edgeUV.x += 0.5 * edge.pixelStep;
        uvStep.y = GetMainTexTexelSize().y;
    }

    float edgeLuma = 0.5 * (luma.m + edge.otherLuma);
    float gradientThreshold = 0.25 * edge.lumaGradient;

    float2 uvP = edgeUV + uvStep;
    float lumaDeltaP = GetLuma(uvP) - edgeLuma;
    float lumaGradientP = abs(lumaDeltaP);
    bool atEndP = lumaGradientP >= gradientThreshold;
    int i;
    UNITY_UNROLL
    for (i = 0; i < EXTRA_EDGE_STEPS && !atEndP; i++)
    {
        uvP += uvStep * edgeStepSizes[i];
        lumaDeltaP = GetLuma(uvP) - edgeLuma;
        lumaGradientP = abs(lumaDeltaP);
        atEndP = lumaGradientP >= gradientThreshold;
    }
    if (!atEndP)
    {
        uvP += uvStep * LAST_EDGE_STEP_GUESS;
    }

    float2 uvN = edgeUV - uvStep;
    float lumaDeltaN = GetLuma(uvN) - edgeLuma;
    float lumaGradientN = abs(lumaDeltaN);
    bool atEndN = lumaGradientN >= gradientThreshold;

    UNITY_UNROLL
    for (i = 0; i < EXTRA_EDGE_STEPS && !atEndN; i++)
    {
        uvN -= uvStep * edgeStepSizes[i];
        lumaDeltaN = GetLuma(uvN) - edgeLuma;
        lumaGradientN = abs(lumaDeltaN);
        atEndN = lumaGradientN >= gradientThreshold;
    }
    if (!atEndN)
    {
        uvN -= uvStep * LAST_EDGE_STEP_GUESS;
    }

    float distanceToEndP, distanceToEndN;
    if (edge.isHorizontal)
    {
        distanceToEndP = uvP.x - uv.x;
        distanceToEndN = uv.x - uvN.x;
    }
    else
    {
        distanceToEndP = uvP.y - uv.y;
        distanceToEndN = uv.y - uvN.y;
    }

    float distanceToNearestEnd;
    bool deltaSign;
    if (distanceToEndP <= distanceToEndN)
    {
        distanceToNearestEnd = distanceToEndP;
        deltaSign = lumaDeltaP >= 0;
    }
    else
    {
        distanceToNearestEnd = distanceToEndN;
        deltaSign = lumaDeltaN >= 0;
    }

    if (deltaSign == (luma.m - edgeLuma >= 0))
    {
        return 0;
    }

    return 0.5 - distanceToNearestEnd / (distanceToEndP + distanceToEndN);
}

half4 FXAAPassFragment(Attributes input) : SV_TARGET
{
    // return GetSource(input.uv);
    LumaNeighborhood luma = GetLumaNeighborhood(input.uv);
    if (CanSkipFXAA(luma))
    {
        // return 0;
        return GetSource(input.uv);
    }

    // float blendFactor = GetSubpixelBlendFactor(luma);

    FXAAEdge edge = GetFXAAEdge(luma);
    float2 blendUV = input.uv;

    float blendFactor = max(GetEdgeBlendFactor(luma, edge, input.uv), GetSubpixelBlendFactor(luma));
    // return blendFactor;

    if (edge.isHorizontal)
    {
        blendUV.y += blendFactor * edge.pixelStep;
    }
    else
    {
        blendUV.x += blendFactor * edge.pixelStep;
    }

    return GetSource(blendUV);
}

#endif

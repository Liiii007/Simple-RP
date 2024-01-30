#ifndef FAST_ACES_INCLUDE
#define FAST_ACES_INCLUDE

// The code in this file was originally written by Stephen Hill (@self_shadow), who deserves all
// credit for coming up with this fit and implementing it. Buy him a beer next time you see him. :)

// sRGB => XYZ => D65_2_D60 => AP1 => RRT_SAT
// static const float3x3 ACESInputMat =
// {
//     {0.59719, 0.35458, 0.04823},
//     {0.07600, 0.90834, 0.01566},
//     {0.02840, 0.13383, 0.83777}
// };

static const float3x3 ACESInputMat = float3x3(
    0.59719, 0.35458, 0.04823,
    0.07600, 0.90834, 0.01566,
    0.02840, 0.13383, 0.83777
);

static const float3x3 ACESOutputMat = float3x3(
    1.60475, -0.53108, -0.07367,
    -0.10208, 1.10813, -0.00605,
    -0.00327, -0.07276, 1.07602
);

static const float3x3 ACESInputMatInv = float3x3(
    1.76474097, -0.67577768, -0.08896329,
    -0.14702785, 1.16025151, -0.01322366,
    -0.03633683, -0.16243644, 1.19877327
);

static const float3x3 ACESOutputMatInv = float3x3(
    0.64303825, 0.31118675, 0.04577546,
    0.05926869, 0.93143649, 0.00929492,
    0.0059619, 0.06392902, 0.93011838
);

float3 RRTAndODTFit(float3 v)
{
    float3 a = v * (v + 0.0245786f) - 0.000090537f;
    float3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
    return a / b;
}

float3 ACESFitted(float3 color)
{
    color = mul(ACESInputMat, color);

    // Apply RRT and ODT
    color = RRTAndODTFit(color);

    color = mul(ACESOutputMat, color);

    // Clamp to [0, 1]
    color = saturate(color);

    return color;
}


float3 RRTInv(float3 r)
{
    float x1 = 0.0245786f;
    float x2 = 0.000090537f;
    float x3 = 0.983729f;
    float x4 = 0.4329510f;
    float x5 = 0.238081f;
    float3 b = x4 * r - x1;
    float3 a = x3 * r - 1;
    float3 c = x5 * r + x2;

    return (-b - sqrt(b * b - 4 * a * c)) / (2 * a);
}

float3 ACESInverse(float3 color)
{
    color = mul(ACESOutputMatInv, color);
    color = RRTInv(color);
    color = mul(ACESInputMatInv, color);

    return color;
}

#endif

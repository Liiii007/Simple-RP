#ifndef SIMPLE_SDF
#define SIMPLE_SDF

float LineSegmentSDF(float3 p, float3 a, float3 b)
{
    float3 ab = b - a;
    float3 ap = p - a;
    float k = saturate(dot(ab, ap) / dot(ab, ab));
    return length(ap - k * ab);
}

float CircleSDF(float2 uv, float range)
{
    uv -= 0.5f;
    float len = length(uv);
    return smoothstep(len - 0.002f, len + 0.002f, range / 2);
}

float RingSDF(float2 uv, float radius, float fillRate)
{
    float large = CircleSDF(uv, radius);
    float small = clamp(0, large, CircleSDF(uv, radius * fillRate));
    return large - small;
}

#endif

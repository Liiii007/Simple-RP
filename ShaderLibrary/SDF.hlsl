#ifndef SIMPLE_SDF
#define SIMPLE_SDF

float LineSegmentSDF(float3 p, float3 a, float3 b)
{
    float3 ab = b - a;
    float3 ap = p - a;
    float k = saturate(dot(ab, ap) / dot(ab, ab));
    return length(ap - k * ab);
}

#endif

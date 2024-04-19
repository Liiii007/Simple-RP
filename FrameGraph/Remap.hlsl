#ifndef CUSTOM_CLODU_LAYER
#define CUSTOM_CLODU_LAYER

float remap(float x, float s1, float s2, float t1, float t2)
{
    return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
}

float SmoothClamp(float x, float b, float t, float strength)
{
    float bottom = remap(x, 0, 1, b, 1);
    float top = remap(x, 1, 0, 0, t);
    return saturate(strength * top * bottom);
}

float StratusDensity(float x)
{
    return SmoothClamp(x, 0.1, 0.16, 256);
}

float Cumulus(float x)
{
    return SmoothClamp(x, 0.1, 0.6, 8);
}

float Cumulonumbis(float x)
{
    float bottom = remap(x, 0, 1, 0.03, 1) * 16;
    float top = SmoothClamp(x, 0, 0.8, 4);
    return saturate(top * bottom * 32);
}

#endif

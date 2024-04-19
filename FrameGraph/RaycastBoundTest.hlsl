#ifndef CUSTOM_RAYCAST_BOUND_TEST_HLSL
#define CUSTOM_RAYCAST_BOUND_TEST_HLSL

//边界框最小值, 边界框最大值, 射线起点, 射线方向
float2 RayToAABB(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
{
    float3 invRaydir = 1 / rayDir;
    float3 t0 = (boundsMin - rayOrigin) * invRaydir;
    float3 t1 = (boundsMax - rayOrigin) * invRaydir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    float dstA = max(max(tmin.x, tmin.y), tmin.z); //进入点
    float dstB = min(tmax.x, min(tmax.y, tmax.z)); //出去点

    float dstToBox = max(0, dstA);
    float dstInsideBox = max(0, dstB - dstToBox);
    return float2(dstToBox, dstInsideBox);
}

float GetDistanceToHeight(float r, float3 viewDir, float height, float3 camPos)
{
    float a = viewDir.x * viewDir.x + viewDir.y * viewDir.y + viewDir.z * viewDir.z;
    float b = 2 * (r + camPos.y) * viewDir.y;
    float c = pow(r + camPos.y, 2) - pow(r + height, 2);

    float delta = b * b - 4 * a * c;

    float root1 = (sqrt(delta) - b) / (2 * a);
    float root2 = (-sqrt(delta) - b) / (2 * a);

    if (camPos.y > height)
    {
        return max(0, root2);
    }
    else
    {
        return max(0, root1);
    }
}

float2 RaySphereDst(float3 sphereCenter, float sphereRadius, float3 pos, float3 rayDir)
{
    float3 oc = pos - sphereCenter;
    float b = dot(rayDir, oc);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    float t = b * b - c; //t > 0有两个交点, = 0 相切， < 0 不相交

    float delta = sqrt(max(t, 0));
    float dstToSphere = max(-b - delta, 0);
    float dstInSphere = max(-b + delta - dstToSphere, 0);
    return float2(dstToSphere, dstInSphere);
}

float2 RayCloudLayerDstShape(float3 sphereCenter, float earthRadius, float heightMin, float heightMax,
                             float3 pos, float3 rayDir)
{
    float2 cloudDstMin = RaySphereDst(sphereCenter, heightMin + earthRadius, pos, rayDir);
    float2 cloudDstMax = RaySphereDst(sphereCenter, heightMax + earthRadius, pos, rayDir);

    //在球壳内
    if (pos.y <= heightMin)
    {
        //开始位置在地平线以上时，设置距离
        float dstToCloudLayer = cloudDstMin.y;
        float dstInCloudLayer = cloudDstMax.y - cloudDstMin.y;
        return float2(dstToCloudLayer, dstInCloudLayer);
    }

    //在云层内
    if (pos.y > heightMin && pos.y <= heightMax)
    {
        float dstToCloudLayer = 0;
        float dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x : cloudDstMax.y;
        return float2(dstToCloudLayer, dstInCloudLayer);
    }

    //在云层外
    float dstToCloudLayer = cloudDstMax.x;
    float dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x - dstToCloudLayer : cloudDstMax.y;

    return float2(dstToCloudLayer, dstInCloudLayer);
}

float2 RayCloudLayerDst(float3 sphereCenter, float earthRadius, float heightMin, float heightMax,
                        float3 pos, float3 rayDir, bool isShape = true)
{
    float2 cloudDstMin = RaySphereDst(sphereCenter, heightMin + earthRadius, pos, rayDir);
    float2 cloudDstMax = RaySphereDst(sphereCenter, heightMax + earthRadius, pos, rayDir);

    //射线到云层的最近距离
    float dstToCloudLayer = 0;
    //射线穿过云层的距离
    float dstInCloudLayer = 0;

    //形状步进时计算相交
    if (isShape)
    {
        //在地表上
        if (pos.y <= heightMin)
        {
            float3 startPos = pos + rayDir * cloudDstMin.y;
            //开始位置在地平线以上时，设置距离
            if (startPos.y >= 0)
            {
                dstToCloudLayer = cloudDstMin.y;
                dstInCloudLayer = cloudDstMax.y - cloudDstMin.y;
            }
            return float2(dstToCloudLayer, dstInCloudLayer);
        }

        //在云层内
        if (pos.y > heightMin && pos.y <= heightMax)
        {
            dstToCloudLayer = 0;
            dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x : cloudDstMax.y;
            return float2(dstToCloudLayer, dstInCloudLayer);
        }

        //在云层外
        dstToCloudLayer = cloudDstMax.x;
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x - dstToCloudLayer : cloudDstMax.y;
    }
    else //光照步进时，步进开始点一定在云层内
    {
        dstToCloudLayer = 0;
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x : cloudDstMax.y;
    }

    return float2(dstToCloudLayer, dstInCloudLayer);
}

#endif

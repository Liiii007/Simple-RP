Shader "Custom RP/ColorDraw"
{
    Properties
    {
        [HDR] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Intensity("Intensity", Float) = 1.0
    }

    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex UnlitPassVertex
            #pragma enable_d3d11_debug_symbols
            #pragma fragment UnlitPassFragment
            #include "../../ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensity;
            CBUFFER_END

            struct AttributesDown
            {
                float3 positionOS : POSITION;
                float2 baseUV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            Varyings UnlitPassVertex(AttributesDown input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenUV = input.baseUV;
                return output;
            }

            float3 rgb_to_hsv_no_clip(float3 RGB)
            {
                float3 HSV;

                float minChannel, maxChannel;
                if (RGB.x > RGB.y)
                {
                    maxChannel = RGB.x;
                    minChannel = RGB.y;
                }
                else
                {
                    maxChannel = RGB.y;
                    minChannel = RGB.x;
                }

                if (RGB.z > maxChannel) maxChannel = RGB.z;
                if (RGB.z < minChannel) minChannel = RGB.z;

                HSV.xy = 0;
                HSV.z = maxChannel;
                float delta = maxChannel - minChannel; //Delta RGB value
                if (delta != 0)
                {
                    // If gray, leave H  S at zero
                    HSV.y = delta / HSV.z;
                    float3 delRGB;
                    delRGB = (HSV.zzz - RGB + 3 * delta) / (6.0 * delta);
                    if (RGB.x == HSV.z) HSV.x = delRGB.z - delRGB.y;
                    else if (RGB.y == HSV.z) HSV.x = (1.0 / 3.0) + delRGB.x - delRGB.z;
                    else if (RGB.z == HSV.z) HSV.x = (2.0 / 3.0) + delRGB.y - delRGB.x;
                }
                return (HSV);
            }

            float3 hsv_to_rgb(float3 HSV)
            {
                float3 RGB = HSV.z;

                float var_h = HSV.x * 6;
                float var_i = floor(var_h); // Or ... var_i = floor( var_h )
                float var_1 = HSV.z * (1.0 - HSV.y);
                float var_2 = HSV.z * (1.0 - HSV.y * (var_h - var_i));
                float var_3 = HSV.z * (1.0 - HSV.y * (1 - (var_h - var_i)));
                if (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                else { RGB = float3(HSV.z, var_1, var_2); }

                return (RGB);
            }

            half4 UnlitPassFragment(Varyings input) : SV_TARGET
            {
                float4 color = _BaseColor;
                
                // color *= _Intensity;
                // color.a = _BaseColor.a;

                float3 hsv = rgb_to_hsv_no_clip(_BaseColor);
                hsv.b *= _Intensity;
                color.rgb = hsv_to_rgb(hsv);
                
                return color;
            }
            ENDHLSL
        }
    }
}
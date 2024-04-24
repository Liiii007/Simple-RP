using SimpleRP.Runtime.PostProcessing;

namespace SimpleRP.Runtime
{
    public static class SimpleRenderPipelineParameter
    {
        public static bool           EnablePostFX = true;
        public static PostFXSettings PostFXSettings;
        public static bool           AllowHDR    = true;
        public static float          RenderScale = 1;

        public static float Brightness = 0;
        public static float Saturation = 0;
        public static float Contrast   = 0;
        public static float Hue        = 0;
    }
}
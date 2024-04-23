using SimpleRP.Runtime.PostProcessing;

namespace SimpleRP.Runtime
{
    public static class SimpleRenderPipelineParameter
    {
        public static bool           EnablePostFX = true;
        public static PostFXSettings PostFXSettings;
        public static bool           AllowHDR    = true;
        public static float          RenderScale = 1;
    }
}
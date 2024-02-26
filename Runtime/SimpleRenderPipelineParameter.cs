using SimpleRP.Runtime.PostProcessing;

namespace SimpleRP.Runtime
{
    public static class SimpleRenderPipelineParameter
    {
        public static bool           EnablePostFX;
        public static PostFXSettings PostFXSettings;
        public static bool           AllowHDR;
        public static float          RenderScale;
    }
}
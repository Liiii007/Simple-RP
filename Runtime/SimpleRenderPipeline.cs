using System.Collections.Generic;
using SimpleRP.Runtime.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleRP.Runtime
{
    public class SimpleRenderPipeline : RenderPipeline
    {
        private readonly CameraRenderer _renderer = new();

        public SimpleRenderPipeline()
        {
            //Enable SRP Batcher
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                _renderer.Render(context, cameras[i]);
            }
        }
    }
}
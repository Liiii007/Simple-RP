using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer _renderer = new CameraRenderer();
    private PostFXSettings _postFXSettings;
    private bool _allowHDR;
    private float _renderScale;

    public CustomRenderPipeline(PostFXSettings postFXSettings, bool allowHDR, float renderScale)
    {
        _postFXSettings = postFXSettings;
        _allowHDR = allowHDR;
        _renderScale = renderScale;

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
            _renderer.Render(context, cameras[i], _postFXSettings, _allowHDR, _renderScale);
        }
    }
}
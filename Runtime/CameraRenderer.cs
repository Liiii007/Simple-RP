using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private const string BufferName = "Render Camera";

    private Camera _camera;
    private ScriptableRenderContext _context;
    private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };
    private CullingResults _cullingResults;

    private PostFXStack _postFXStack = new PostFXStack();
    private static int _frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    private bool _useHDR;

    public void Render(ScriptableRenderContext context, Camera camera, PostFXSettings postFXSettings, bool allowHDR)
    {
        _context = context;
        _camera = camera;
        _useHDR = camera.allowHDR && allowHDR;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }

        _postFXStack.Setup(context, camera, postFXSettings, _useHDR);
        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();

        DrawGizmosBeforePostFX();

        if (_postFXStack.IsActive)
        {
            _postFXStack.Render(_frameBufferId);
        }

        DrawGizmosAfterPostFX();

        Cleanup();

        Submit();
    }

    private void Setup()
    {
        _context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;

        if (_postFXStack.IsActive)
        {
            //To prevent random result
            if (flags > CameraClearFlags.Color)
            {
                //Left skybox(1) or color(2) flag here
                flags = CameraClearFlags.Color;
            }

            _buffer.GetTemporaryRT(
                _frameBufferId, _camera.pixelWidth, _camera.pixelHeight, 32,
                FilterMode.Bilinear,
                _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

            _buffer.SetRenderTarget(_frameBufferId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        _buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        _buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry()
    {
        var sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        _context.DrawRenderers(
            _cullingResults, ref drawingSettings, ref filteringSettings
        );
        _context.DrawSkybox(_camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        _context.DrawRenderers(
            _cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    private void Submit()
    {
        _buffer.EndSample(SampleName);
        ExecuteBuffer();
        _context.Submit();
    }

    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private bool Cull()
    {
        if (_camera.TryGetCullingParameters(out var p))
        {
            _cullingResults = _context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Cleanup()
    {
        if (_postFXStack.IsActive)
        {
            _buffer.ReleaseTemporaryRT(_frameBufferId);
        }
    }
}
using Plugins.SimpleRP.RenderGraph;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleRP.Runtime
{
    public partial class CameraRenderer
    {
        private const string BufferName = "Render Camera";

        private static readonly ShaderTagId FirstPassShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static readonly ShaderTagId SecondPassShaderTagId = new ShaderTagId("SRPPass2");

        private Camera _camera;
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;

        private PostProcessing.PostFXStack _postFXStack = new PostProcessing.PostFXStack();
        private bool _useHDR;
        private bool _useRenderScale;
        private static bool AllowHDR => SimpleRenderPipelineParameter.AllowHDR;
        private static float RenderScale => Mathf.Clamp(SimpleRenderPipelineParameter.RenderScale * 0.8f, 0.1f, 2f);

        private Vector2Int ScreenRTSize =>
            new((int)(_camera.pixelWidth * RenderScale), (int)(_camera.pixelHeight * RenderScale));

        private GraphInstance _graph = new();

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;
            _useHDR = camera.allowHDR && AllowHDR;

            var cameraColorBuffer = new VirtualTexture(BuiltinRenderTextureType.CameraTarget);
            var cameraDepthBuffer = new VirtualTexture(BuiltinRenderTextureType.Depth);

            _graph.StartNewFrame(new GraphInstance.SetupContext()
            {
                CameraColorBuffer = cameraColorBuffer,
            }, new GraphInstance.RenderContext()
            {
                Context = _context,
                CameraColorBuffer = cameraColorBuffer,
            });

            _useRenderScale = RenderScale < 0.99f || RenderScale > 1.01f;

            PrepareBuffer();
            PrepareForSceneWindow();
            if (!Cull())
            {
                return;
            }

            _postFXStack.Setup(context, camera, SimpleRenderPipelineParameter.PostFXSettings, _useHDR, ScreenRTSize);
            Setup();
            DrawVisibleGeometry();
            //DrawUnsupportedShaders();

            DrawGizmosBeforePostFX();

            if (_postFXStack.IsActive)
            {
                _postFXStack.Render(_graph, _opaqueTarget);
            }

            DrawGizmosAfterPostFX();

            Cleanup();

            Submit();
        }

        private VirtualTexture _opaqueTarget;

        private void Setup()
        {
            _context.SetupCameraProperties(_camera);
            CameraClearFlags flags = _camera.clearFlags;

            //To prevent random result
            if (flags > CameraClearFlags.Color)
            {
                //Left skybox(1) or color(2) flag here
                flags = CameraClearFlags.Color;
            }

            var enablePostFX = _postFXStack.IsActive && SimpleRenderPipelineParameter.EnablePostFX;

            if (enablePostFX)
            {
                //Set render target here
                var desc = new RenderTextureDescriptor(ScreenRTSize.x, ScreenRTSize.y,
                    _useHDR
                        ? RenderTextureFormat.DefaultHDR
                        : RenderTextureFormat.Default, 0);
                _opaqueTarget = new(desc, name: "OpaqueRT");
            }
            else
            {
                _opaqueTarget = new(BuiltinRenderTextureType.CameraTarget);
            }

            _graph.AddPass((builder, context) => { builder.WriteTexture(_opaqueTarget); }, context =>
            {
                if (enablePostFX)
                {
                    context.cmd.SetRenderTarget(_opaqueTarget.id, RenderBufferLoadAction.DontCare,
                        RenderBufferStoreAction.Store);
                }

                context.cmd.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color,
                    flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
            }, name: "Clear RT");
        }

        private void DrawVisibleGeometry()
        {
            _graph.AddPass((builder, context) => { builder.WriteTexture(_opaqueTarget); }, (context) =>
            {
                var sortingSettings = new SortingSettings(_camera)
                {
                    criteria = SortingCriteria.CommonOpaque
                };
                var drawingSettings = new DrawingSettings(FirstPassShaderTagId, sortingSettings);
                drawingSettings.SetShaderPassName(1, SecondPassShaderTagId);
                var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

                context.Context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                drawingSettings.sortingSettings = sortingSettings;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;

                context.Context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
            }, name: "Draw Opaque Geometry");
        }

        private void Submit()
        {
            _graph.Build();
            _graph.Draw();
            _context.Submit();
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
        }
    }
}
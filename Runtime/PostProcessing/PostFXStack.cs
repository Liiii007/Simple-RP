using System;
using System.Collections.Generic;
using Plugins.SimpleRP.RenderGraph;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace SimpleRP.Runtime.PostProcessing
{
    public partial class PostFXStack
    {
        public const string BufferName = "Post FX";

        private ScriptableRenderContext _context;
        private Camera _camera;
        private PostFXSettings _settings;
        private bool _useHDR;

        private int _fxSourceId2 = Shader.PropertyToID("_PostFXSource2");
        private int _colorGradingResultId = Shader.PropertyToID("_ColorGradingResult");

        private VirtualTexture[] _bloomMipUp;
        private VirtualTexture[] _bloomMipDown;

        private Vector2Int _screenRTSize;

        public PostFXStack()
        {
            _bloomMipUp = new VirtualTexture[_maxBloomPyramidLevels];
            _bloomMipDown = new VirtualTexture[_maxBloomPyramidLevels];
        }

        public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool useHDR,
            Vector2Int screenRTSize)
        {
            _context = context;
            _camera = camera;
            _useHDR = useHDR;
            _screenRTSize = screenRTSize;

            //Only GameView(1) and SceneView(2) camera will apply post fx
            if (camera.cameraType <= CameraType.SceneView && camera.CompareTag("MainCamera"))
            {
                _settings = settings;
            }
            else
            {
                _settings = null;
            }

            CheckApplySceneViewState();
        }

        public bool IsActive => _settings != null;

        private const string ENABLE_POSTFX_KEYWORD = "SIMPLE_ENABLE_POSTFX";

        private VirtualTexture _currentTexture;
        private GraphInstance _graph;

        public void Render(GraphInstance graph, VirtualTexture sourceId)
        {
            _graph = graph;

            Shader.SetGlobalFloat("_Brightness", SimpleRenderPipelineParameter.Brightness * 0.01f + 1f);
            Shader.SetGlobalFloat("_Saturation", SimpleRenderPipelineParameter.Saturation * 0.01f + 1f);
            Shader.SetGlobalFloat("_Contrast", SimpleRenderPipelineParameter.Contrast * 0.01f + 1f);

            _currentTexture = sourceId;


            if (SimpleRenderPipelineParameter.EnablePostFX)
            {
                var doBloom = DoBloom(sourceId, out var bloomResult);
                // TODO:兼容没有bloom的情况
                if (doBloom)
                {
                    sourceId = DoToneMapping(sourceId, bloomResult);
                }

                DoFXAA(sourceId);
                Shader.EnableKeyword(ENABLE_POSTFX_KEYWORD);
            }
            else
            {
                Shader.DisableKeyword(ENABLE_POSTFX_KEYWORD);
            }

            foreach (var (from, to, iteration) in blurRTQueue)
            {
                DoBlur(from, to, iteration);
            }

            foreach (var (from, to, iteration) in blurTextureQueue)
            {
                DoBlur(from, to, iteration);
            }

            blurRTQueue.Clear();
            blurTextureQueue.Clear();
        }

        private static List<(RenderTargetIdentifier, RenderTargetIdentifier, int)> blurRTQueue = new();
        private static List<(Texture, RenderTargetIdentifier, int)> blurTextureQueue = new();
        public static Dictionary<string, RenderTexture> _blurTextures = new();

        public static void GetBlurTexture(Texture to, Texture from = null, int iteration = 5)
        {
            if (from == null)
            {
                blurRTQueue.Add((BuiltinRenderTextureType.CameraTarget, to, iteration));
            }
            else
            {
                blurTextureQueue.Add((from, to, iteration));
            }
        }

        public static void RegisterBlurTexture(string path, RenderTexture rt)
        {
            if (_blurTextures.TryGetValue(path, out _))
            {
                Debug.LogError($"Blur texture {path} already exists!");
            }

            _blurTextures[path] = rt;
        }

        public static void RemoveBlurTexture(string path)
        {
            if (_blurTextures.TryGetValue(path, out var rt))
            {
                _blurTextures.Remove(path);
                if (rt != null)
                {
                    rt.Release();
                }
            }
        }

        #region DoBloom

        private const int _maxBloomPyramidLevels = 16;
        private int _bloomPyramidId;
        private int _bloomPrefilterRT = Shader.PropertyToID("_BloomPrefilter");
        private int _bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        private int _bloomParamsId = Shader.PropertyToID("_Params");
        private int _bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private int _bloomResultRT = Shader.PropertyToID("_BloomResult");

        private bool DoBloom(VirtualTexture source, out VirtualTexture bloomResult)
        {
            var bloomSettings = _settings.Bloom;
            bloomResult = source;

            //Prefilter
            int width = _camera.pixelWidth >> 1;
            int height = _camera.pixelHeight >> 1;
            var format = _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            //Bypass bloom if no need
            if (bloomSettings.maxIterations == 0 ||
                bloomSettings.intensity <= 0 ||
                height < bloomSettings.downscaleLimit * 2 ||
                width < bloomSettings.downscaleLimit * 2)
            {
                return false;
            }

            // _buffer.BeginSample("Bloom");
            //Calu bloom parameters

            int mipCount = bloomSettings.maxIterations;

            float clamp = 65472f;
            float threshold = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            float thresholdKnee = threshold * 0.5f;
            float scatter = Mathf.Lerp(0.05f, 0.95f, 0.7f);
            Shader.SetGlobalVector(_bloomParamsId, new Vector4(scatter, clamp, threshold, thresholdKnee));
            Shader.SetGlobalFloat(_bloomIntensityId, bloomSettings.intensity);

            //Prefilter
            for (int i = 0; i < bloomSettings.maxIterations; i++)
            {
                int cw = width >> i;
                int ch = height >> i;
                _bloomMipUp[i] = new(new RenderTextureDescriptor(Mathf.Max(1, cw), Mathf.Max(1, ch), format)
                    {
                        depthBufferBits = 0,
                        depthStencilFormat = GraphicsFormat.None,
                        useMipMap = false,
                        enableRandomWrite = true
                    }, $"BloomMipUp{i}");
                _bloomMipDown[i] = new(new RenderTextureDescriptor(Mathf.Max(1, cw), Mathf.Max(1, ch), format)
                    {
                        depthBufferBits = 0,
                        depthStencilFormat = GraphicsFormat.None,
                        useMipMap = false,
                        enableRandomWrite = true
                    }, $"BloomMipDown{i}");
            }

            // Prefilter
            _graph.AddPass((builder, context) =>
                {
                    builder.ReadTexture(source);
                    builder.WriteTexture(_bloomMipDown[0]);
                },
                context =>
                {
                    context.cmd.Blit(source.id, _bloomMipDown[0].id, _settings.Material,
                        (int)PostFXSettings.FXPass.BloomPrefilterPassFragment);
                }, name: "Bloom Prefilter");

            //Downsample
            var lastDown = _bloomMipDown[0];
            for (int i = 1; i < bloomSettings.maxIterations; i++)
            {
                var last = lastDown;
                var index = i;

                _graph.AddPass((builder, context) =>
                    {
                        builder.ReadTexture(last);
                        builder.WriteTexture(_bloomMipUp[index]);
                        builder.ReadTexture(_bloomMipUp[index]);
                        builder.WriteTexture(_bloomMipDown[index]);
                    },
                    context =>
                    {
                        // var cs = _settings.cs;
                        // var cmd = context.cmd;
                        //
                        // var blurXKernel = _settings.cs.FindKernel("BlurX");
                        // var blurYKernel = _settings.cs.FindKernel("BlurY");
                        //
                        // cmd.SetComputeTextureParam(cs, blurXKernel, "_Source", last.id);
                        // cmd.SetComputeTextureParam(cs, blurXKernel, "_Target_RW", _bloomMipUp[index].id);
                        // cmd.SetComputeVectorParam(cs, "_TSize",
                        //     new Vector4(1f / _bloomMipUp[index].Size.x, 1f / _bloomMipUp[index].Size.y,
                        //         _bloomMipUp[index].Size.x, _bloomMipUp[index].Size.y));
                        // cmd.DispatchCompute(cs, blurXKernel, Mathf.CeilToInt(_bloomMipUp[index].Size.x / 8f),
                        //     Mathf.CeilToInt(_bloomMipUp[index].Size.y / 8f), 1);
                        //
                        // cmd.SetComputeTextureParam(cs, blurYKernel, "_Source", _bloomMipUp[index].id);
                        // cmd.SetComputeTextureParam(cs, blurYKernel, "_Target_RW", _bloomMipDown[index].id);
                        // cmd.SetComputeVectorParam(cs, "_TSize",
                        //     new Vector4(1f / _bloomMipDown[index].Size.x, 1f / _bloomMipDown[index].Size.y,
                        //         _bloomMipDown[index].Size.x, _bloomMipDown[index].Size.y));
                        // cmd.DispatchCompute(cs, blurYKernel, Mathf.CeilToInt(_bloomMipDown[index].Size.x / 8f),
                        //     Mathf.CeilToInt(_bloomMipDown[index].Size.y / 8f), 1);

                        context.cmd.Blit(last.id, _bloomMipUp[index].id, _settings.Material,
                            (int)PostFXSettings.FXPass.BloomHorizontal);
                        
                        context.cmd.Blit(_bloomMipUp[index].id, _bloomMipDown[index].id, _settings.Material,
                            (int)PostFXSettings.FXPass.BloomVertical);
                    }, name: $"Bloom DownSample {i}");

                lastDown = _bloomMipDown[index];
            }

            //Upsample
            for (int i = bloomSettings.maxIterations - 2; i >= 0; i--)
            {
                var lowMip = (i == mipCount - 2) ? _bloomMipDown[i + 1] : _bloomMipUp[i + 1];
                var highMip = _bloomMipDown[i];
                var dst = _bloomMipUp[i];

                _graph.AddPass((builder, context) =>
                    {
                        builder.ReadTexture(lowMip);
                        builder.ReadTexture(highMip);
                        builder.WriteTexture(dst);
                    },
                    context =>
                    {
                        context.cmd.SetGlobalTexture(_fxSourceId2, lowMip.id);
                        context.cmd.Blit(highMip.id, dst.id, _settings.Material,
                            (int)PostFXSettings.FXPass.BloomCombine);
                    }, name: $"Bloom UpSample {i}");
            }

            bloomResult = _bloomMipUp[0];
            return true;
        }

        private int[] blurMips;

        // TODO:Use RenderGraph to blur image
        private bool DoBlur(object sourceId, RenderTargetIdentifier targetId, int iterations)
        {
            // iterations = Mathf.Max(1, iterations);
            //
            // if (blurMips == null)
            // {
            //     blurMips = new int[32];
            //
            //     for (int i = 0; i < 32; i++)
            //     {
            //         blurMips[i] = Shader.PropertyToID("_BlurTexture_SimpleRP" + i);
            //     }
            // }
            //
            // int width = _camera.pixelWidth >> 1;
            // int height = _camera.pixelHeight >> 1;
            // var format = _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            //
            // //1/2 RT
            // _buffer.GetTemporaryRT(blurMips[0], width, height, 0, FilterMode.Bilinear, format);
            //
            // for (int i = 1; i < iterations; i++)
            // {
            //     int cw = width >> i;
            //     int ch = height >> i;
            //
            //     _buffer.GetTemporaryRT
            //     (
            //         blurMips[i * 2 - 1],
            //         Mathf.Max(1, cw), Mathf.Max(1, ch),
            //         0, FilterMode.Bilinear, format
            //     );
            //
            //     _buffer.GetTemporaryRT
            //     (
            //         blurMips[i * 2],
            //         Mathf.Max(1, cw), Mathf.Max(1, ch),
            //         0, FilterMode.Bilinear, format
            //     );
            // }
            //
            // _buffer.GetTemporaryRT
            // (
            //     blurMips[iterations * 2 - 1],
            //     Mathf.Max(1, width >> 4), Mathf.Max(1, height >> 4),
            //     0, FilterMode.Bilinear, format
            // );
            //
            // if (sourceId is RenderTargetIdentifier rti)
            // {
            //     Draw(rti, blurMips[0], PostFXSettings.FXPass.BloomHorizontal);
            // }
            // else if (sourceId is Texture texture)
            // {
            //     Draw(texture, blurMips[0], PostFXSettings.FXPass.BloomHorizontal);
            // }
            // else
            // {
            //     Debug.LogError("Unsupported type");
            //     return false;
            // }
            //
            // Draw(blurMips[0], blurMips[1], PostFXSettings.FXPass.BloomVertical);
            //
            // for (int i = 1; i < iterations; i++)
            // {
            //     Profiler.BeginSample("Blur" + i);
            //     Draw
            //     (
            //         blurMips[i * 2 - 1], blurMips[i * 2],
            //         PostFXSettings.FXPass.BloomHorizontal
            //     );
            //     Draw
            //     (
            //         blurMips[i * 2], blurMips[i * 2 + 1],
            //         PostFXSettings.FXPass.BloomVertical
            //     );
            //     Profiler.EndSample();
            // }
            //
            // Draw(blurMips[iterations * 2 - 1], targetId, PostFXSettings.FXPass.Copy);
            //
            // for (int i = 0; i < iterations * 2; i++)
            // {
            //     _buffer.ReleaseTemporaryRT(blurMips[i]);
            // }
            //
            // return true;

            return true;
        }

        #endregion

        #region DoToneMapping

        /// <summary>
        /// Draw to _colorGradingResultId
        /// </summary>
        /// <param name="sourceId"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private VirtualTexture DoToneMapping(VirtualTexture sourceId, VirtualTexture bloomResult)
        {
            var colorGradingResultDesc =
                new RenderTextureDescriptor(_screenRTSize.x, _screenRTSize.y, RenderTextureFormat.Default)
                {
                    depthBufferBits = 0,
                    depthStencilFormat = GraphicsFormat.None,
                    useMipMap = false
                };

            var colorGradientTexture = new VirtualTexture(colorGradingResultDesc, "ColorGrading");

            PostFXSettings.FXPass pass;
            switch (_settings.toneMappingMode)
            {
                case PostFXSettings.ToneMappingMode.None:
                    pass = PostFXSettings.FXPass.Copy;
                    break;
                case PostFXSettings.ToneMappingMode.ACES:
                    pass = PostFXSettings.FXPass.ToneMappingACES;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _graph.AddPass((builder, context) =>
            {
                builder.ReadTexture(sourceId);
                builder.ReadTexture(bloomResult);
                builder.WriteTexture(colorGradientTexture);
            }, context =>
            {
                context.cmd.SetGlobalTexture(_fxSourceId2, bloomResult.id);
                context.cmd.Blit(sourceId.id, colorGradientTexture.id, _settings.Material, (int)pass);
            }, name: "Tonemapping & Bloom Integration");

            return colorGradientTexture;
        }

        #endregion

        private void DoFXAA(VirtualTexture sourceId)
        {
            _graph.AddPass((builder, context) =>
                {
                    builder.ReadTexture(sourceId);
                    builder.WriteTexture(context.CameraColorBuffer);
                },
                context =>
                {
                    context.cmd.Blit(sourceId.id, context.CameraColorBuffer.id, _settings.Material,
                        (int)PostFXSettings.FXPass.FXAA);
                }, name: "FXAA");
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace SimpleRP.Runtime.PostProcessing
{
    public partial class PostFXStack
    {
        public const string BufferName = "Post FX";

        private CommandBuffer           _buffer = new CommandBuffer() { name = BufferName };
        private ScriptableRenderContext _context;
        private Camera                  _camera;
        private PostFXSettings          _settings;
        private bool                    _useHDR;

        private int _fxSourceId2 = Shader.PropertyToID("_PostFXSource2");

        private int[] _bloomMipUp;
        private int[] _bloomMipDown;

        private Vector2Int _screenRTSize;

        public PostFXStack()
        {
            _bloomMipUp   = new int[_maxBloomPyramidLevels];
            _bloomMipDown = new int[_maxBloomPyramidLevels];

            //Get sequential texture id 
            for (int i = 0; i < _maxBloomPyramidLevels; i++)
            {
                _bloomMipUp[i]   = Shader.PropertyToID("_BloomPyramidUp"   + i);
                _bloomMipDown[i] = Shader.PropertyToID("_BloomPyramidDown" + i);
            }
        }

        public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool useHDR,
                          Vector2Int              screenRTSize)
        {
            _context      = context;
            _camera       = camera;
            _useHDR       = useHDR;
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

        public void Render(int sourceId)
        {
            // DoBlur(sourceId, BuiltinRenderTextureType.CameraTarget);

            if (SimpleRenderPipelineParameter.EnablePostFX && DoBloom(sourceId))
            {
                _buffer.SetGlobalTexture(_fxSourceId2, _bloomResultRT);
                DoToneMapping(sourceId);
                _buffer.ReleaseTemporaryRT(_bloomResultRT);
            }
            else
            {
                DoToneMapping(sourceId);
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

            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private static List<(RenderTargetIdentifier, RenderTargetIdentifier, int)> blurRTQueue      = new();
        private static List<(Texture, RenderTargetIdentifier, int)>                blurTextureQueue = new();
        public static  Dictionary<string, RenderTexture>                           _blurTextures    = new();

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

        private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFXSettings.FXPass pass)
        {
            _buffer.Blit(from, to, _settings.Material, (int)pass);
        }

        private void Draw(Texture from, RenderTargetIdentifier to, PostFXSettings.FXPass pass)
        {
            _buffer.Blit(from, to, _settings.Material, (int)pass);
        }

        #region DoBloom

        private const int _maxBloomPyramidLevels = 16;
        private       int _bloomPyramidId;
        private       int _bloomPrefilterRT = Shader.PropertyToID("_BloomPrefilter");
        private       int _bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        private       int _bloomParamsId    = Shader.PropertyToID("_Params");
        private       int _bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private       int _bloomResultRT    = Shader.PropertyToID("_BloomResult");

        private bool DoBloom(int sourceId)
        {
            var bloomSettings = _settings.Bloom;

            //Prefilter
            int width  = _camera.pixelWidth  >> 1;
            int height = _camera.pixelHeight >> 1;
            var format = _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            //Bypass bloom if no need
            if (bloomSettings.maxIterations == 0                               || bloomSettings.intensity <= 0 ||
                height                      < bloomSettings.downscaleLimit * 2 ||
                width                       < bloomSettings.downscaleLimit * 2)
            {
                return false;
            }

            // _buffer.BeginSample("Bloom");
            //Calu bloom parameters

            int mipCount = bloomSettings.maxIterations;

            float clamp         = 65472f;
            float threshold     = Mathf.GammaToLinearSpace(bloomSettings.threshold);
            float thresholdKnee = threshold * 0.5f;
            float scatter       = Mathf.Lerp(0.05f, 0.95f, 0.7f);
            _buffer.SetGlobalVector(_bloomParamsId, new Vector4(scatter, clamp, threshold, thresholdKnee));
            _buffer.SetGlobalFloat(_bloomIntensityId, bloomSettings.intensity);

            //Prefilter
            for (int i = 0; i < bloomSettings.maxIterations; i++)
            {
                int cw = width  >> i;
                int ch = height >> i;
                _buffer.GetTemporaryRT(_bloomMipUp[i], Mathf.Max(1, cw), Mathf.Max(1, ch), 0, FilterMode.Bilinear,
                                       format);
                _buffer.GetTemporaryRT(_bloomMipDown[i], Mathf.Max(1, cw), Mathf.Max(1, ch), 0, FilterMode.Bilinear,
                                       format);
            }

            Draw(sourceId, _bloomMipDown[0], PostFXSettings.FXPass.BloomPrefilterPassFragment);

            //Downsample
            var lastDown = _bloomMipDown[0];
            for (int i = 1; i < bloomSettings.maxIterations; i++)
            {
                Draw(lastDown, _bloomMipUp[i], PostFXSettings.FXPass.BloomHorizontal);
                Draw(_bloomMipUp[i], _bloomMipDown[i], PostFXSettings.FXPass.BloomVertical);

                lastDown = _bloomMipDown[i];
            }

            //Upsample
            for (int i = bloomSettings.maxIterations - 2; i >= 0; i--)
            {
                var lowMip  = (i == mipCount - 2) ? _bloomMipDown[i + 1] : _bloomMipUp[i + 1];
                var highMip = _bloomMipDown[i];
                var dst     = _bloomMipUp[i];

                _buffer.SetGlobalTexture(_fxSourceId2, lowMip);
                Draw(highMip, dst, PostFXSettings.FXPass.BloomCombine);

                _bloomResultRT = dst;
            }

            for (int i = 0; i < bloomSettings.maxIterations; i++)
            {
                _buffer.ReleaseTemporaryRT(_bloomMipDown[i]);

                if (_bloomMipUp[i] != _bloomResultRT)
                {
                    _buffer.ReleaseTemporaryRT(_bloomMipUp[i]);
                }
            }

            return true;
        }

        private int[] blurMips;

        private bool DoBlur(object sourceId, RenderTargetIdentifier targetId, int iterations)
        {
            iterations = Mathf.Max(1, iterations);

            if (blurMips == null)
            {
                blurMips = new int[32];

                for (int i = 0; i < 32; i++)
                {
                    blurMips[i] = Shader.PropertyToID("_BlurTexture_SimpleRP" + i);
                }
            }

            int width  = _camera.pixelWidth  >> 1;
            int height = _camera.pixelHeight >> 1;
            var format = _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            //1/2 RT
            _buffer.GetTemporaryRT(blurMips[0], width, height, 0, FilterMode.Bilinear, format);

            for (int i = 1; i < iterations; i++)
            {
                int cw = width  >> i;
                int ch = height >> i;

                _buffer.GetTemporaryRT
                (
                    blurMips[i * 2 - 1],
                    Mathf.Max(1, cw), Mathf.Max(1, ch),
                    0, FilterMode.Bilinear, format
                );

                _buffer.GetTemporaryRT
                (
                    blurMips[i * 2],
                    Mathf.Max(1, cw), Mathf.Max(1, ch),
                    0, FilterMode.Bilinear, format
                );
            }

            _buffer.GetTemporaryRT
            (
                blurMips[iterations * 2 - 1],
                Mathf.Max(1, width >> 4), Mathf.Max(1, height >> 4),
                0, FilterMode.Bilinear, format
            );

            if (sourceId is RenderTargetIdentifier rti)
            {
                Draw(rti, blurMips[0], PostFXSettings.FXPass.BloomHorizontal);
            }
            else if (sourceId is Texture texture)
            {
                Draw(texture, blurMips[0], PostFXSettings.FXPass.BloomHorizontal);
            }
            else
            {
                Debug.LogError("Unsupported type");
                return false;
            }

            Draw(blurMips[0], blurMips[1], PostFXSettings.FXPass.BloomVertical);

            for (int i = 1; i < iterations; i++)
            {
                Profiler.BeginSample("Blur" + i);
                Draw
                (
                    blurMips[i * 2 - 1], blurMips[i * 2],
                    PostFXSettings.FXPass.BloomHorizontal
                );
                Draw
                (
                    blurMips[i * 2], blurMips[i * 2 + 1],
                    PostFXSettings.FXPass.BloomVertical
                );
                Profiler.EndSample();
            }

            Draw(blurMips[iterations * 2 - 1], targetId, PostFXSettings.FXPass.Copy);

            for (int i = 0; i < iterations * 2; i++)
            {
                _buffer.ReleaseTemporaryRT(blurMips[i]);
            }

            return true;
        }

        #endregion

        #region DoToneMapping

        private void DoToneMapping(int sourceId)
        {
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

            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, pass);
        }

        #endregion
    }
}
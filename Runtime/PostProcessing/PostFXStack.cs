using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    public const string BufferName = "Post FX";

    private CommandBuffer _buffer = new CommandBuffer() { name = BufferName };
    private ScriptableRenderContext _context;
    private Camera _camera;
    private PostFXSettings _settings;
    private bool _useHDR;

    private int _fxSourceId = Shader.PropertyToID("_PostFXSource");
    private int _fxSourceId2 = Shader.PropertyToID("_PostFXSource2");

    public PostFXStack()
    {
        //Get sequential texture id 
        _bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < _maxBloomPyramidLevels; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings, bool useHDR)
    {
        _context = context;
        _camera = camera;
        _useHDR = useHDR;

        //Only GameView(1) and SceneView(2) camera will apply post fx
        if (camera.cameraType <= CameraType.SceneView)
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
        if (DoBloom(sourceId))
        {
            DoToneMapping(_bloomResultRT);
            _buffer.ReleaseTemporaryRT(_bloomResultRT);
        }
        else
        {
            DoToneMapping(sourceId);
        }

        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFXSettings.FXPass pass)
    {
        //Set origin texture
        _buffer.SetGlobalTexture(_fxSourceId, from);
        //Then draw to render target
        _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.DrawProcedural(Matrix4x4.identity, _settings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    #region DoBloom

    private const int _maxBloomPyramidLevels = 16;
    private int _bloomPyramidId;
    private int _bloomPrefilterRT = Shader.PropertyToID("_BloomPrefilter");
    private int _bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    private int _bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private int _bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    private int _bloomResultRT = Shader.PropertyToID("_BloomResult");

    private bool DoBloom(int sourceId)
    {
        var bloomSettings = _settings.Bloom;

        //Prefilter
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;
        var format = _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

        //Bypass bloom if no need
        if (bloomSettings.maxIterations == 0 || bloomSettings.intensity <= 0 ||
            height < bloomSettings.downscaleLimit * 2 ||
            width < bloomSettings.downscaleLimit * 2)
        {
            return false;
        }

        _buffer.BeginSample("Bloom");
        //Calu bloom parameters
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        _buffer.SetGlobalVector(_bloomThresholdId, threshold);
        _buffer.SetGlobalFloat(_bloomIntensityId, 1f);

        _buffer.GetTemporaryRT(_bloomPrefilterRT, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, _bloomPrefilterRT, PostFXSettings.FXPass.BloomPrefilterPassFragment);
        width /= 2;
        height /= 2;

        int fromRT = _bloomPrefilterRT;
        int toRT = _bloomPyramidId + 1;

        int i;
        //Down sample
        for (i = 0; i < bloomSettings.maxIterations; i++)
        {
            if (height < bloomSettings.downscaleLimit || width < bloomSettings.downscaleLimit)
            {
                break;
            }

            int midId = toRT - 1;
            _buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            _buffer.GetTemporaryRT(toRT, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromRT, midId, PostFXSettings.FXPass.BloomHorizontal);
            Draw(midId, toRT, PostFXSettings.FXPass.BloomVertical);
            fromRT = toRT;
            toRT += 2;
            width /= 2;
            height /= 2;
        }

        //Set intensity when upsampleing when combine
        _buffer.SetGlobalFloat(_bloomIntensityId, bloomSettings.intensity);
        if (i > 1)
        {
            _buffer.ReleaseTemporaryRT(fromRT - 1); //Release mid RT(fromId points to last toId)
            toRT -= 5;

            //Up sample 
            for (i -= 1; i > 0; i--)
            {
                _buffer.SetGlobalTexture(_fxSourceId2, toRT + 1);
                Draw(fromRT, toRT, PostFXSettings.FXPass.BloomCombine);
                _buffer.ReleaseTemporaryRT(fromRT);
                _buffer.ReleaseTemporaryRT(toRT - 1);
                fromRT = toRT;
                toRT -= 2;
            }
        }
        else
        {
            _buffer.ReleaseTemporaryRT(_bloomPyramidId);
        }

        _buffer.SetGlobalTexture(_fxSourceId2, sourceId);
        _buffer.GetTemporaryRT(_bloomResultRT, _camera.pixelWidth, _camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromRT, _bloomResultRT, PostFXSettings.FXPass.BloomCombine);
        _buffer.ReleaseTemporaryRT(fromRT);
        _buffer.ReleaseTemporaryRT(_bloomPrefilterRT);
        _buffer.EndSample("Bloom");

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
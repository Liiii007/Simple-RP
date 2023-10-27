using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private PostFXSettings _postFXSettings = default;
    [SerializeField] private bool allowHDR = true;
    [SerializeField] [Range(0.1f, 2f)] private float renderScale = 1f;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(_postFXSettings, allowHDR, renderScale);
    }
}
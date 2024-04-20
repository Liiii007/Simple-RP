using FrameGraph.View;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Value", typeof(Texture))]
    public class GetTemporaryRT : ResourceNodeBase<VirtualRenderTarget>
    {
        public GetTemporaryRT()
        {
            ValueSlot.Bind(new VirtualRenderTarget
            {
                desc = Blackboard<RenderTextureDescriptor>.Get("CameraOpaqueTextureDescriptor")
            });
        }
    }
}
using FrameGraph.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(Direction.Output, Port.Capacity.Multi, "Value", typeof(Texture))]
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
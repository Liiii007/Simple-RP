using FrameGraph.View;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Value", typeof(Texture))]
    public class FrameBuffer : StaticResourceNodeBase<VirtualRenderTarget>
    {
        public FrameBuffer() : base()
        {
            ValueSlot.Key = "_FrameBuffer";
        }
    }
}
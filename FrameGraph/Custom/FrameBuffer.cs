using FrameGraph.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(Direction.Output, Port.Capacity.Multi, "Value", typeof(Texture))]
    public class FrameBuffer : StaticResourceNodeBase<VirtualRenderTarget>
    {
        public FrameBuffer() : base()
        {
            ValueSlot.Key = "_FrameBuffer";
        }
    }
}
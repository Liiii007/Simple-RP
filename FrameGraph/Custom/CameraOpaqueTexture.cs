using FrameGraph.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(Direction.Output, Port.Capacity.Multi, "Value", typeof(Texture))]
    public class CameraOpaqueTexture : StaticResourceNodeBase<VirtualRenderTarget>
    {
        public CameraOpaqueTexture() : base()
        {
            ValueSlot.Key = "_CameraOpaqueTexture";
        }
    }
}
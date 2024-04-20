using FrameGraph.View;
using UnityEngine;

namespace FrameGraph
{
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Value", typeof(Texture))]
    public class CameraOpaqueTexture : StaticResourceNodeBase<VirtualRenderTarget>
    {
        public CameraOpaqueTexture() : base()
        {
            ValueSlot.Key = "_CameraOpaqueTexture";
        }
    }
}
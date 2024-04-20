using FrameGraph.View;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(PortDirection.Output, PortCapacity.Single, "Next", typeof(ExecRoot))]
    public class ExecRoot : PassBase
    {
        public override void Setup() { }

        public override void Execute(ScriptableRenderContext context, CommandBuffer cmd) { }
    }
}
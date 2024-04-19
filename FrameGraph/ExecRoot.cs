using FrameGraph.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(Direction.Output, Port.Capacity.Single, "Next", typeof(ExecRoot))]
    public class ExecRoot : PassBase
    {
        public override void Setup() { }

        public override void Execute(ScriptableRenderContext context, CommandBuffer cmd) { }
    }
}
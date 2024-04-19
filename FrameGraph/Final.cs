using FrameGraph.View;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(Direction.Input, Port.Capacity.Multi, "Final", typeof(ExecRoot))]
    public class Final : PassBase
    {
        public override void Setup() { }

        public override void Execute(ScriptableRenderContext context, CommandBuffer cmd) { }
    }
}
using FrameGraph.Node;
using UnityEditor.Experimental.GraphView;

namespace FrameGraph.View
{
    public abstract class ProcessViewNode : ViewNodeBase
    {
        public Port outputPort;

        public ProcessViewNode()
        {
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(ExecRoot));
            inputPort.portName = "Execute";
            inputContainer.Add(inputPort);

            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                typeof(ExecRoot));
            outputPort.portName = "Execute";
            outputContainer.Add(outputPort);
        }

        public abstract void Execute();
    }
}
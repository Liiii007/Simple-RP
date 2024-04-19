using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FrameGraph.View
{
    public class LogViewNode : ProcessViewNode
    {
        private Port _inputPort;

        public LogViewNode() : base()
        {
            title = "Log";

            _inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(string));
            _inputPort.portName = "Message";
            inputContainer.Add(_inputPort);
        }

        public override void Execute()
        {
            var edge = _inputPort.connections.FirstOrDefault();

            if (edge == null)
            {
                Debug.LogWarning("Log nothing");
                return;
            }

            var node = edge.output.node as StringViewNode;

            if (edge.output.node is StringViewNode sNode)
            {
                Debug.Log(node.Text);
            }

            if (edge.output.node is StringCatViewNode catNode)
            {
                Debug.Log(catNode.Result);
            }
        }
    }
}
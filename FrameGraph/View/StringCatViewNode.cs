using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;

namespace FrameGraph.View
{
    public class StringCatViewNode : ViewNodeBase
    {
        private Port _inputPort1;
        private Port _inputPort2;
        private Port _outputPort;

        public StringCatViewNode()
        {
            title = "String Cat";

            _inputPort1 = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(string));
            _inputPort1.portName = "Input1";
            _inputPort2 = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(string));
            _inputPort2.portName = "Input2";
            _outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                typeof(string));
            _outputPort.portName = "Result";

            inputContainer.Add(_inputPort1);
            inputContainer.Add(_inputPort2);
            outputContainer.Add(_outputPort);
        }

        public string Result
        {
            get
            {
                StringBuilder builder = new StringBuilder();

                if (_inputPort1?.connections.FirstOrDefault()?.output.node is StringViewNode node1)
                {
                    builder.Append(node1.Text);
                }
                else if (_inputPort1?.connections.FirstOrDefault()?.output.node is StringCatViewNode catNode1)
                {
                    builder.Append(catNode1.Result);
                }

                if (_inputPort2?.connections.FirstOrDefault()?.output.node is StringViewNode node2)
                {
                    builder.Append(node2.Text);
                }
                else if (_inputPort2?.connections.FirstOrDefault()?.output.node is StringCatViewNode catNode2)
                {
                    builder.Append(catNode2.Result);
                }

                return builder.ToString();
            }
        }
    }
}
using FrameGraph.Serliazion;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace FrameGraph.View
{
    public class StringViewNode : ViewNodeBase
    {
        private TextField _textField;
        public string Text => _textField.value;

        public StringViewNode() : this("") { }

        public StringViewNode(string text)
        {
            title = "String";

            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                typeof(string));
            outputContainer.Add(outputPort);

            _textField = new TextField(text);
            mainContainer.Add(_textField);
        }

        public override string Serliaze()
        {
            return Text;
        }

        public override ViewNodeBase Deserlize(string jsonData, FrameGraphData _)
        {
            _textField.SetValueWithoutNotify(jsonData);
            return this;
        }
    }
}
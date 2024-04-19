using FrameGraph.Node;

namespace FrameGraph
{
    public enum ConnectionDirection
    {
        Both,
        Input,
        Output
    }

    public class Connection
    {
        public NodeBase RightViewNode;
        public NodeBase LeftViewNode;

        public Slot LeftSlot;
        public Slot RightSlot;
    }
}
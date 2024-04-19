using System.Collections.Generic;

namespace FrameGraph.Node
{
    public abstract class NodeBase
    {
        public int              id                = 0;
        public List<Connection> inputConnections  = new();
        public List<Connection> outputConnections = new();

        public List<Slot> inputSlots  = new();
        public List<Slot> outputSlots = new();

        public Slot QuerySlot(string slotName, ConnectionDirection direction)
        {
            switch (direction)
            {
                case ConnectionDirection.Input:
                    return inputSlots.Find(s => s.PortName == slotName);
                case ConnectionDirection.Output:
                    return outputSlots.Find(s => s.PortName == slotName);
                case ConnectionDirection.Both:
                    return inputSlots.Find(s => s.PortName  == slotName) ??
                           outputSlots.Find(s => s.PortName == slotName);
                default:
                    return null;
            }
        }
    }
}
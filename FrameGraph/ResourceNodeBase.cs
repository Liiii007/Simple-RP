using FrameGraph.Node;

namespace FrameGraph
{
    public abstract class ResourceNodeBase<T> : NodeBase
    {
        public Slot<T> ValueSlot;

        public ResourceNodeBase()
        {
            ValueSlot = new Slot<T>()
            {
                PortName = "Value",
                SlotType = SlotType.Output,
                ParentNode = this,
            };

            outputSlots.Add(ValueSlot);
        }
    }

    public abstract class StaticResourceNodeBase<T> : NodeBase
    {
        public StaticSlot<T> ValueSlot;

        public StaticResourceNodeBase()
        {
            ValueSlot = new StaticSlot<T>()
            {
                PortName = "Value",
                SlotType = SlotType.Output,
                ParentNode = this,
            };

            outputSlots.Add(ValueSlot);
        }
    }
}
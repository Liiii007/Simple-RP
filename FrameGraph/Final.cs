using FrameGraph.View;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(PortDirection.Input, PortCapacity.Multi, "Final", typeof(ExecRoot))]
    public class Final : PassBase
    {
        public Slot<PassBase> PassSlot;
        
        public Final()
        {
            PassSlot = new Slot<PassBase>
            {
                PortName   = "Final",
                SlotType   = SlotType.Input,
                ParentNode = this,
            };
            
            inputSlots.Add(PassSlot);
        }
        public override void Setup() { }

        public override void Execute(ScriptableRenderContext context, CommandBuffer cmd) { }
    }
}
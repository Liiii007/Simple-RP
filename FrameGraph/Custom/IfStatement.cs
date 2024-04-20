using FrameGraph.View;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(PortDirection.Input, PortCapacity.Single, "Execute", typeof(ExecRoot))]
    [GraphPort(PortDirection.Output, PortCapacity.Single, "True", typeof(ExecRoot))]
    [GraphPort(PortDirection.Output, PortCapacity.Single, "False", typeof(ExecRoot))]
    public class IfStatement : PassBase
    {
        public Slot<PassBase> InputExecute;
        public Slot<PassBase> TrueExecute;
        public Slot<PassBase> FalseExecute;
        
        public PassBase TruePass;
        public PassBase FalsePass;

        public IfStatement()
        {
            InputExecute = new Slot<PassBase>
            {
                PortName   = "Execute",
                SlotType   = SlotType.Input,
                ParentNode = this,
            };
            
            TrueExecute = new Slot<PassBase>
            {
                PortName   = "True",
                SlotType   = SlotType.Output,
                ParentNode = this,
            };
            
            FalseExecute = new Slot<PassBase>
            {
                PortName   = "False",
                SlotType   = SlotType.Output,
                ParentNode = this,
            };
            
            inputSlots.Add(InputExecute);
            outputSlots.Add(TrueExecute);
            outputSlots.Add(FalseExecute);
        }
        
        public override void Setup()
        {
            
        }

        public override void Execute(ScriptableRenderContext context, CommandBuffer cmd)
        {
        }

        public PassBase GetNextPass()
        {
            return TruePass;
        }
    }
}
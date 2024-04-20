using FrameGraph.View;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrameGraph
{
    [GraphPort(PortDirection.Input, PortCapacity.Single, "Execute", typeof(ExecRoot))]
    [GraphPort(PortDirection.Input, PortCapacity.Single, "Source", typeof(Texture))]
    [GraphPort(PortDirection.Input, PortCapacity.Single, "Target", typeof(Texture))]
    [GraphPort(PortDirection.Input, PortCapacity.Single, "Material", typeof(Material))]
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Next", typeof(ExecRoot))]
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Result", typeof(Texture))]
    public class BlitPass : PassBase
    {
        private VirtualRenderTarget SourceRT => SourceRTSlot.InputValue;

        private VirtualRenderTarget TargetRT
        {
            get
            {
                var result = TargetRTSlot.InputValue;
                if (result == null)
                {
                    result = Blackboard<VirtualRenderTarget>.Get("_CameraOpaqueTexture");
                }

                return result;
            }
        }

        private Material BlitMaterialResource
        {
            get
            {
                var result = MaterialSlot.InputValue;

                if (result == null)
                {
                    result = CopyMaterialResource;
                }

                return result;
            }
        }

        private Material CopyMaterialResource
        {
            get
            {
                if (_copyMaterialResource == null)
                {
                    _copyMaterialResource = Resources.Load<Material>("CopyOnlyMaterial");
                }

                return _copyMaterialResource;
            }
        }

        private static Material _copyMaterialResource;

        public Slot<VirtualRenderTarget> SourceRTSlot;
        public Slot<VirtualRenderTarget> TargetRTSlot;
        public Slot<Material>            MaterialSlot;
        public Slot<VirtualRenderTarget> ResultRTSlot;

        public BlitPass()
        {
            SourceRTSlot = new Slot<VirtualRenderTarget>
            {
                PortName   = "Source",
                SlotType   = SlotType.Input,
                ParentNode = this,
            };

            TargetRTSlot = new Slot<VirtualRenderTarget>
            {
                PortName   = "Target",
                SlotType   = SlotType.Input,
                ParentNode = this,
            };

            MaterialSlot = new Slot<Material>
            {
                PortName   = "Material",
                SlotType   = SlotType.Input,
                ParentNode = this,
            };

            ResultRTSlot = new Slot<VirtualRenderTarget>
            {
                PortName   = "Result",
                SlotType   = SlotType.Output,
                ParentNode = this,
            };

            inputSlots.Add(SourceRTSlot);
            inputSlots.Add(TargetRTSlot);
            inputSlots.Add(MaterialSlot);
            outputSlots.Add(ResultRTSlot);
        }

        public override void Setup()
        {
            PassGraphManager.SetupRT(this, SourceRT, RTUsage.Read);
            PassGraphManager.SetupRT(this, TargetRT, RTUsage.Write);

            ResultRTSlot.Bind(TargetRT);
        }

        public override void Execute(ScriptableRenderContext _0, CommandBuffer cmd)
        {
            if (BlitMaterialResource == null)
            {
                Debug.Log($"Missing Blit using material");
                return;
            }

            cmd.BeginSample($"Blit:{BlitMaterialResource.name}");
            var sourceRT = SourceRT.GetRT();
            var targetRT = TargetRT.GetRT();

            if (sourceRT == targetRT)
            {
                var desc = SourceRT.desc;
                var interRT = new VirtualRenderTarget()
                {
                    desc = desc
                };
                interRT.OnAlloc(cmd);

                cmd.Blit(sourceRT, interRT.GetRT(), BlitMaterialResource, 0);
                cmd.Blit(interRT.GetRT(), targetRT, CopyMaterialResource, 0);

                interRT.OnRelease(cmd);
            }
            else
            {
                cmd.Blit(sourceRT, targetRT, BlitMaterialResource, 0);
            }
            cmd.EndSample($"Blit:{BlitMaterialResource.name}");
        }
    }
}
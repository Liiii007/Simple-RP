using FrameGraph.Serliazion;
using FrameGraph.View;
using UnityEngine;

namespace FrameGraph
{
    [GraphField("Material", typeof(Material))]
    [GraphPort(PortDirection.Output, PortCapacity.Multi, "Value", typeof(Material))]
    public class MaterialResource : ResourceNodeBase<Material>, IAdditionInit
    {
        public void InitNode(FrameGraphData data, NodeData nodeData)
        {
            if (int.TryParse(nodeData.Data, out int matID) && matID >= 0)
            {
                ValueSlot.Bind(data.Items[matID].Value as Material);
            }
            else
            {
                Debug.LogWarning("Invalid material index");
            }
        }
    }

    #if UNITY_EDITOR
    namespace View
    {
        public partial class MaterialResourceNode
        {
            public override ViewNodeBase Deserlize(string jsonData, FrameGraphData data)
            {
                if (int.TryParse(jsonData, out int index) && index >= 0)
                {
                    _MaterialField.value = data.Items[index].Value;
                }

                return this;
            }
        }
    }
    #endif
}
using System.Collections.Generic;
using FrameGraph.Node;
using UnityEngine.Rendering;

namespace FrameGraph
{
    public abstract class PassBase : NodeBase
    {
        public List<PassBase> Next = new();

        public abstract void Setup();

        public abstract void Execute(ScriptableRenderContext context, CommandBuffer cmd);
    }
}
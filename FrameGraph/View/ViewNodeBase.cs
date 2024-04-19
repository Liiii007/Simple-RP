using System;
using FrameGraph.Serliazion;
using UnityEditor.Experimental.GraphView;

namespace FrameGraph.View
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GraphPortAttribute : Attribute
    {
        public GraphPortAttribute(Direction direction, Port.Capacity capacity, string name, Type capacityType,
                                           Orientation orientation = Orientation.Horizontal) { }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GraphFieldAttribute : Attribute
    {
        public GraphFieldAttribute(string name, Type type) { }
    }

    public abstract class ViewNodeBase : UnityEditor.Experimental.GraphView.Node
    {
        public virtual string Serliaze()
        {
            return string.Empty;
        }

        public virtual ViewNodeBase Deserlize(string jsonData, FrameGraphData data)
        {
            return this;
        }
    }
}
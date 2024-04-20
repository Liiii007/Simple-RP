using System;
using FrameGraph.Serliazion;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

public enum PortCapacity
{
    Single,
    Multi,
}

public enum PortDirection
{
    /// <summary>
    ///   <para>Port is an input port.</para>
    /// </summary>
    Input,
    /// <summary>
    ///   <para>Port is an output port.</para>
    /// </summary>
    Output,
}

public enum PortOrientation
{
    /// <summary>
    ///   <para>Horizontal orientation used for nodes and connections flowing to the left or right.</para>
    /// </summary>
    Horizontal,
    /// <summary>
    ///   <para>Vertical orientation used for nodes and connections flowing up or down.</para>
    /// </summary>
    Vertical,
}

namespace FrameGraph.View
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GraphPortAttribute : Attribute
    {
        public GraphPortAttribute(PortDirection   direction, PortCapacity portCapacity, string name, Type capacityType,
                                  PortOrientation orientation = PortOrientation.Horizontal) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class GraphFieldAttribute : Attribute
    {
        public GraphFieldAttribute(string name, Type type) { }
    }

    #if UNITY_EDITOR
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
    #else
    public abstract class ViewNodeBase { }
    #endif
}
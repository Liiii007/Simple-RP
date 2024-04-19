using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrameGraph.Serliazion
{
    [Serializable]
    public class NodeData
    {
        public int    ID;
        public string Type;
        public string Data;
        public Rect   Position;
    }

    [Serializable]
    public class EdgeData
    {
        public int    RightNodeID;
        public int    LeftNodeID;
        public string LeftSlotName;
        public string RightSlotName;
    }

    [Serializable]
    public class ResourceData<T>
    {
        public int ID;
        public T   Value;
    }

    [Serializable]
    public class FrameGraphData : ScriptableObject
    {
        public List<NodeData>             Nodes = new();
        public List<EdgeData>             Edges = new();
        public List<ResourceData<Object>> Items = new();
    }
}
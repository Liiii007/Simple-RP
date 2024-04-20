using System;
using System.Collections.Generic;
using FrameGraph.Node;
using FrameGraph.Serliazion;
using SimpleRP.Runtime;
using UnityEngine;

namespace FrameGraph
{
    public class PassGraph
    {
        public        ExecRoot ExecRoot;
        public static bool     RequireUpdate { get; set; }

        public void Execute(RenderData data)
        {
            PassGraphManager.SetupPasses(this);
            PassGraphManager.ExecutePasses(this, data);
        }

        public IEnumerable<PassBase> Travel()
        {
            var queue = new Queue<PassBase>();

            if (ExecRoot != null)
            {
                queue.Enqueue(ExecRoot);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current is IfStatement ifStatementNode)
                {
                    yield return ifStatementNode.GetNextPass();
                    continue;
                }

                foreach (var next in current.Next)
                {
                    queue.Enqueue(next);
                }

                yield return current;
            }
        }

        //TODO:Generate automation
        public static Dictionary<Type, Type> NodeTypes = new()
        {
            { typeof(View.ExecRootNode), typeof(ExecRoot) },
            { typeof(View.BlitPassNode), typeof(BlitPass) },
            { typeof(View.MaterialResourceNode), typeof(MaterialResource) },
            { typeof(View.CameraOpaqueTextureNode), typeof(CameraOpaqueTexture) },
            { typeof(View.FrameBufferNode), typeof(FrameBuffer) },
            { typeof(View.GetTemporaryRTNode), typeof(GetTemporaryRT) },
            { typeof(View.FinalNode), typeof(Final) },
            { typeof(View.IfStatementNode), typeof(IfStatement) }
        };

        public static PassGraph Parse(FrameGraphData data)
        {
            PassGraph result = new();

            var nodeDict   = new Dictionary<int, NodeBase>();
            var connectSet = new HashSet<Connection>();

            foreach (var nodeData in data.Nodes)
            {
                var nodeType = Type.GetType(nodeData.Type);

                if (nodeType == null)
                {
                    Debug.LogError($"Cannot find editor type:{nodeData.Type}");
                    continue;
                }

                if (!NodeTypes.TryGetValue(nodeType, out nodeType))
                {
                    Debug.LogError($"Cannot find runtime type:{nodeData.Type}");
                    continue;
                }

                if (nodeType == null) { }

                var nodeInstance = Activator.CreateInstance(nodeType) as NodeBase;

                if (nodeInstance == null)
                {
                    Debug.LogWarning($"Cannot create {nodeType.Name}");
                    continue;
                }

                nodeDict.Add(nodeData.ID, nodeInstance);

                if (nodeInstance is ExecRoot root)
                {
                    result.ExecRoot = root;
                }

                if (nodeInstance is IAdditionInit init)
                {
                    init.InitNode(data, nodeData);
                }
            }

            foreach (var edge in data.Edges)
            {
                if (!nodeDict.TryGetValue(edge.RightNodeID, out var rightNode) ||
                    !nodeDict.TryGetValue(edge.LeftNodeID, out var leftNode))
                {
                    Debug.LogWarning("Node missing");
                    continue;
                }

                if (leftNode is IfStatement ifStatementNode)
                {
                    if (edge.LeftSlotName == "True")
                    {
                        ifStatementNode.TruePass = rightNode as PassBase;
                    }
                    else
                    {
                        ifStatementNode.FalsePass = rightNode as PassBase;
                    }

                    continue;
                }

                if (rightNode is PassBase passNext && leftNode is PassBase passPrev &&
                    (edge.RightSlotName.Contains("Execute")))
                {
                    passPrev.Next.Add(passNext);
                    continue;
                }

                var connection = new Connection();
                connection.RightViewNode = rightNode;
                connection.LeftViewNode  = leftNode;

                connection.LeftSlot = connection.LeftViewNode?.QuerySlot(edge.LeftSlotName, ConnectionDirection.Output);
                connection.RightSlot =
                    connection.RightViewNode?.QuerySlot(edge.RightSlotName, ConnectionDirection.Input);

                if (connection.RightViewNode == null)
                {
                    Debug.LogWarning("Input node is null");
                }

                if (connection.LeftViewNode == null)
                {
                    Debug.LogWarning("Output node is null");
                }

                if (connection.LeftSlot == null)
                {
                    Debug.LogWarning("Input slot is null");
                }
                else if (connection.RightSlot != null)
                {
                    connection.LeftSlot.Output.Add(connection.RightSlot);
                }

                if (connection.RightSlot == null)
                {
                    Debug.LogWarning("Output slot is null");
                }
                else if (connection.LeftSlot != null)
                {
                    connection.RightSlot.Input = connection.LeftSlot;
                }

                connectSet.Add(connection);
            }

            return result;
        }
    }
}
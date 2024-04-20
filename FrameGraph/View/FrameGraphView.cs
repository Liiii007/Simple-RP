#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FrameGraph.Serliazion;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FrameGraph.View
{
    public class FrameGraphView : GraphView
    {
        private ExecRootNode _rootView;

        public FrameGraphView(bool createRoot = true) : base()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            Insert(0, new GridBackground());

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var searchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            searchWindowProvider.Initialize(this);

            if (createRoot)
            {
                _rootView = new ExecRootNode();
                AddElement(_rootView);
            }

            nodeCreationRequest += context =>
            {
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindowProvider);
            };
        }

        public void Rebuild()
        {
            foreach (var node in nodes)
            {
                if (node is ExecRootNode root)
                {
                    _rootView = root;
                    break;
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();

            foreach (var port in ports)
            {
                if (startPort.node      == port.node      ||
                    startPort.direction == port.direction ||
                    startPort.portType  != port.portType)
                {
                    continue;
                }

                result.Add(port);
            }

            return result;
        }

        public void SaveOrUpdate(FrameGraphData data)
        {
            var origin = Resources.Load<FrameGraphData>("RGraph");
            if (origin != null)
            {
                origin.Nodes = data.Nodes;
                origin.Edges = data.Edges;
                origin.Items = data.Items;

                EditorUtility.SetDirty(origin);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                AssetDatabase.CreateAsset(data, "Assets/Resources/RGraph.asset");
            }
        }

        public void Execute()
        {
            //Save
            var so = Save(this);
            SaveOrUpdate(so);
            PassGraph.RequireUpdate = true;
        }

        public static FrameGraphData Save(FrameGraphView graph)
        {
            int gID    = 0;
            var result = ScriptableObject.CreateInstance<FrameGraphData>();
            var nodes  = graph.nodes;
            var edges  = graph.edges;

            var nodeDict    = new Dictionary<int, ViewNodeBase>();
            var nodeDictRev = new Dictionary<ViewNodeBase, int>();
            var itemList    = result.Items;

            foreach (var node in nodes)
            {
                if (node is not ViewNodeBase nodeBase)
                {
                    continue;
                }

                var id = gID++;

                nodeDict[id]          = nodeBase;
                nodeDictRev[nodeBase] = id;

                if (node is MaterialResourceNode mNode)
                {
                    int mId = -1;
                    if (mNode.Material != null)
                    {
                        mId = itemList.Count;
                        itemList.Add(new ResourceData<Object>()
                        {
                            ID    = mId,
                            Value = mNode.Material
                        });
                    }

                    result.Nodes.Add(new NodeData()
                    {
                        ID       = id,
                        Type     = nodeBase.GetType().ToString(),
                        Data     = mId.ToString(),
                        Position = node.GetPosition()
                    });
                }
                else
                {
                    result.Nodes.Add(new NodeData()
                    {
                        ID       = id,
                        Type     = nodeBase.GetType().ToString(),
                        Data     = nodeBase.Serliaze(),
                        Position = node.GetPosition()
                    });
                }
            }

            foreach (var edge in edges)
            {
                if (edge.input.node is ViewNodeBase input && edge.output.node is ViewNodeBase output)
                {
                    result.Edges.Add(new EdgeData
                    {
                        RightNodeID   = nodeDictRev[input],
                        LeftNodeID    = nodeDictRev[output],
                        LeftSlotName  = edge.output.portName,
                        RightSlotName = edge.input.portName,
                    });
                }
            }

            return result;
        }

        public static FrameGraphView Load(FrameGraphData data)
        {
            var result = new FrameGraphView(false)
            {
                style = { flexGrow = 1 }
            };

            var nodeDict = new Dictionary<int, ViewNodeBase>();

            foreach (var nodeData in data.Nodes)
            {
                Type type = Type.GetType(nodeData.Type);

                if (type == null)
                {
                    Debug.LogWarning($"Invalid Node Type:{nodeData.Type}");
                    continue;
                }

                ViewNodeBase node = Activator.CreateInstance(type) as ViewNodeBase;

                if (node == null)
                {
                    Debug.LogWarning($"Cannot cast to Node Type:{nodeData.Type}");
                    continue;
                }

                node.Deserlize(nodeData.Data, data);
                node.SetPosition(nodeData.Position);

                result.AddElement(node);

                nodeDict[nodeData.ID] = node;
            }

            foreach (var edge in data.Edges)
            {
                var inputNode  = nodeDict[edge.RightNodeID];
                var outputNode = nodeDict[edge.LeftNodeID];

                var inputPort  = GetInputPort(inputNode, edge.RightSlotName);
                var outputPort = GetOutputPort(outputNode, edge.LeftSlotName);

                if (inputPort == null || outputPort == null)
                {
                    continue;
                }

                var newEdge = new Edge() { input = inputPort, output = outputPort };

                newEdge.input.Connect(newEdge);
                newEdge.output.Connect(newEdge);
                result.Add(newEdge);
            }

            result.Rebuild();
            return result;
        }

        public static Port GetInputPort(ViewNodeBase node, string portName)
        {
            return node.inputContainer.Query<Port>().Where(p => p.portName == portName);
        }

        public static Port GetOutputPort(ViewNodeBase node, string portName)
        {
            return node.outputContainer.Query<Port>().Where(p => p.portName == portName);
        }

        public static string GetGUID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
#endif
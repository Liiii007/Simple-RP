#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FrameGraph.View
{
    public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private FrameGraphView _graphView;

        public void Initialize(FrameGraphView view)
        {
            _graphView = view;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var result = new List<SearchTreeEntry>();
            result.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass &&
                        !type.IsAbstract &&
                        (type.IsSubclassOf(typeof(ViewNodeBase))) &&
                        type != typeof(ExecRootNode))
                    {
                        result.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type });
                    }
                }
            }

            return result;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var type = searchTreeEntry.userData as System.Type;
            var node = Activator.CreateInstance(type) as ViewNodeBase;
            _graphView.AddElement(node);
            return true;
        }
    }
}
#endif
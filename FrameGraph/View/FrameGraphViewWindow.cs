#if UNITY_EDITOR
using FrameGraph.Serliazion;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FrameGraph.View
{
    public class FrameGraphViewWindow : EditorWindow
    {
        [MenuItem("Simple/FrameGraphView")]
        public static void Open()
        {
            GetWindow<FrameGraphViewWindow>().Show();
        }

        private void OnEnable()
        {
            var graphView = new FrameGraphView()
            {
                style = { flexGrow = 1 }
            };

            var data = Resources.Load<FrameGraphData>("RGraph");

            if (data != null)
            {
                graphView = FrameGraphView.Load(data);
            }

            rootVisualElement.Add(graphView);
            rootVisualElement.Add(new Button(graphView.Execute) { text = "Execute" });
        }
    }
}
#endif
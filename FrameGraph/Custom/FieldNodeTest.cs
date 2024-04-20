using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace FrameGraph.View
{
    public class MyProperty : ScriptableObject
    {
        public string   Name;
        public Material Material;
    }
    
    public partial class FieldNode : FrameGraph.View.ViewNodeBase
    {
        private Port _Value;
        
        private ObjectField         _MaterialField;
        private PropertyField       _stringField;
        private                     PropertyField _matField;
        public UnityEngine.Material Material => _MaterialField.value as UnityEngine.Material;

        public FieldNode()
        {
            title = "Material Resource";

            _Value = Port.Create<Edge>((Orientation)0, (Direction)1, (Port.Capacity)1, typeof(UnityEngine.Material));
            _Value.portName = "Value";

            _MaterialField = new ObjectField { objectType = typeof(Material)};

            var                myPror           = ScriptableObject.CreateInstance<MyProperty>();
            SerializedObject   serializedObject = new SerializedObject(myPror);
            SerializedProperty pro1             = serializedObject.FindProperty("Material");
            SerializedProperty pro2             = serializedObject.FindProperty("Name");
            _stringField = new PropertyField(pro1);
            _matField = new PropertyField(pro2);

            outputContainer.Add(_Value);

            mainContainer.Add(_MaterialField);
            mainContainer.Add(_stringField);
            mainContainer.Add(_matField);
        }
    }
}

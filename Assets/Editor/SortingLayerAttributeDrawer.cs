using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
public class SortingLayerAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            SortingLayer[] layers = SortingLayer.layers;
            string[] layerNames = new string[layers.Length];
            int currentIndex = 0;

            for (int i = 0; i < layers.Length; i++)
            {
                layerNames[i] = layers[i].name;
                if (layers[i].id == property.intValue)
                    currentIndex = i;
            }

            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, layerNames);
            property.intValue = layers[selectedIndex].id;
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "SortingLayer는 int 필드에만 사용할 수 있습니다.");
        }

        EditorGUI.EndProperty();
    }
}

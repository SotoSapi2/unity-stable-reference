using System;
using UnityEditor;
using UnityEngine;

namespace UnityStableReference.Editor
{
    [CustomPropertyDrawer(typeof(StableReference<>))]
    public class StableReferenceDrawer : AbstractStableReferenceDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            var wrapperProperty = property.FindPropertyRelative("wrapper");
            
            if (wrapperProperty is null)
            {
                throw new NullReferenceException("StableReference is missing field named 'wrapper'");
            }
            
            if (wrapperProperty.managedReferenceValue == null)
            {
                EditorGUILayout.HelpBox($"'{label.text}' property must be set!", MessageType.Error);
            }
        }
    }
}
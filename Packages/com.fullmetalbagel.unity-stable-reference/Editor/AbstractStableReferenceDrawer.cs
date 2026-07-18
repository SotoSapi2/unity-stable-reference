using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityStableReference.Editor
{
    public abstract class AbstractStableReferenceDrawer : PropertyDrawer
    {
        private const string DefaultNullItemName = "None";
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var baseType = GetUnderlyingElementType(fieldInfo.FieldType)
                .GetGenericArguments()
                .First();
            
            var wrapperProperty = property.FindPropertyRelative("wrapper");

            if (wrapperProperty is null)
            {
                throw new NullReferenceException("StableReference is missing field named 'wrapper'");
            }
            
            var newPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            DrawDropdownButton(newPosition, wrapperProperty, baseType);
            RenderExpendable(position, wrapperProperty, label);
        }

        private void DrawDropdownButton(Rect position, SerializedProperty property, Type baseType)
        {
            string displayName = GetPropertyDisplayName(property);

            if (GUI.Button(position, displayName, EditorStyles.popup))
            {
                var dropdown = new StableWrapperDropdown(new AdvancedDropdownState(), baseType, property);
                dropdown.Show(position);
            }
        }

        private string GetPropertyDisplayName(SerializedProperty property)
        {
            SerializedPropertyType type = property.propertyType;

            if (type == SerializedPropertyType.ManagedReference)
            {
                if (property.managedReferenceValue == null)
                {
                    return DefaultNullItemName;
                }
                
                return property.managedReferenceValue.GetType()
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStableWrapper<>))
                    .GetGenericArguments()[0]
                    .Name;
            }

            return property.propertyType.ToString();
        }

        private static Type GetUnderlyingElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        
        private void RenderExpendable(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null || !property.hasVisibleChildren)
            {
                property.isExpanded = false;
                return;
            }

            SerializedProperty iterator = property.FindPropertyRelative("value");

            if (iterator is null)
            {
                throw new NullReferenceException("StableWrapper is missing field named 'value'");
            }
            
            SerializedProperty iteratorEnd = iterator.GetEndProperty();

            if (!iterator.hasVisibleChildren)
            {
                return;
            }
            
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(
                foldoutRect, 
                property.isExpanded, 
                label, 
                true
            );

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, iteratorEnd))
                    {
                        break;
                    }
                    
                    EditorGUILayout.PropertyField(iterator, true);
                    enterChildren = false; 
                }
                
                EditorGUI.indentLevel--;
            }
        }
    }
}
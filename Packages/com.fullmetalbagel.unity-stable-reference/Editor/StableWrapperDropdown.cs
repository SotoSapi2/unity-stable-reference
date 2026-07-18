using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityStableReference.Editor
{
    public class StableWrapperDropdown : AdvancedDropdown
    {
        public delegate void SelectCallback([CanBeNull] Type type);

        public const string NullItemName = "None";
        public const string RootName = "Select type";

        public static IReadOnlyCollection<Type> NeededAttribute = new[]
        {
            typeof(SerializableAttribute),
            typeof(GuidAttribute),
            typeof(StableWrapperCodeGenAttribute),
        };

        public Type BaseType { get; private set; }
        
        public SerializedProperty Property { get; private set; }

        private struct QueryRecord
        {
            public Type WrapperType { get; }

            public Type ReferenceType { get; }

            public QueryRecord(Type wrapperType, Type referenceType)
            {
                WrapperType = wrapperType;
                ReferenceType = referenceType;
            }
        }

        public class Item : AdvancedDropdownItem
        {
            [CanBeNull] public Type WrapperType { get; }

            public Item(string name, [CanBeNull] Type wrapperType) : base(name)
            {
                WrapperType = wrapperType;
            }
        }

        public StableWrapperDropdown(AdvancedDropdownState state, Type baseType, SerializedProperty property) :
            base(state)
        {
            BaseType = baseType;
            Property = property;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var nullItem = new Item("None", null);
            var root = new AdvancedDropdownItem(RootName);
            root.AddChild(nullItem);
            
            foreach (var query in CreateWrapperTypeIterator())
            {
                if (query.ReferenceType.IsAbstract)
                {
                    continue;
                }
                
                if (!BaseType.IsAssignableFrom(query.ReferenceType))
                {
                    continue;
                }

                if (!IsTypeHaveRequiredAttributes(query.ReferenceType))
                {
                    continue;
                }
                
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                if (query.WrapperType.GetConstructor(Type.EmptyTypes) == null &&
                    query.WrapperType.GetConstructor(flags, null, Type.EmptyTypes, null) == null)
                {
                    continue;
                }

                root.AddChild(new Item(query.ReferenceType.Name, query.WrapperType));
            }

            return root;
        }
        
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is Item scriptItem)
            {
                var instance = scriptItem.WrapperType is not null
                    ? Activator.CreateInstance(scriptItem.WrapperType)
                    : null;
                
                Property.serializedObject.Update();
                Property.managedReferenceValue = instance;
                Property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        private static IEnumerable<QueryRecord> CreateWrapperTypeIterator()
        {
            var stableTypes = TypeCache.GetTypesDerivedFrom(typeof(StableWrapper<>));
            return stableTypes.SelectMany(wrapperType => wrapperType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStableWrapper<>))
                .Select(i => new QueryRecord(wrapperType, i.GetGenericArguments()[0])));
        }
        
        private static bool IsTypeHaveRequiredAttributes(Type type)
        {
            return NeededAttribute.All(attribute => Attribute.IsDefined(type, attribute));
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomPropertyDrawer(typeof(SerializedType), true)]
    internal class SerializedTypeDrawer : PropertyDrawer
    {
        List<Type> m_Types;
        FieldInfo m_SerializedFieldInfo;
        SerializedProperty m_Property;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Property = property;
            if (m_SerializedFieldInfo == null)
                m_SerializedFieldInfo = GetFieldInfo(property);
            if (m_Types == null)
                m_Types = GetTypes(m_SerializedFieldInfo);

            EditorGUI.BeginProperty(position, label, property);
            var smallPos = EditorGUI.PrefixLabel(position, label);
            var st = (SerializedType)m_SerializedFieldInfo.GetValue(property.serializedObject.targetObject);

            string display = AddressableAssetUtility.GetCachedTypeDisplayName(st.Value);
//            if (st.ShowMixedValue)
//            {
//                display = "—";
//            }

            if (EditorGUI.DropdownButton(smallPos, new GUIContent(display), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("<none>", "Clear the type."), false, OnSetType, null);
                for (int i = 0; i < m_Types.Count; i++)
                {
                    var type = m_Types[i];
                    menu.AddItem(new GUIContent(AddressableAssetUtility.GetCachedTypeDisplayName(type), ""), false, OnSetType, type);
                }
                menu.ShowAsContext();
            }

            EditorGUI.EndProperty();
        }

        void OnSetType(object context)
        {
            Undo.RecordObject(m_Property.serializedObject.targetObject, "Set Serialized Type");
            var type = context as Type;
            m_SerializedFieldInfo.SetValue(m_Property.serializedObject.targetObject, new SerializedType { Value = type, ValueChanged = true });
            EditorUtility.SetDirty(m_Property.serializedObject.targetObject);
        }

        static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var o = property.serializedObject.targetObject;
            var t = o.GetType();
            string propertyName = property.name;
            int i = property.propertyPath.IndexOf('.');
            if (i > 0)
                propertyName = property.propertyPath.Substring(0, i);
            return t.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        static List<Type> GetTypes(FieldInfo fieldInfo)
        {
            var attrs = fieldInfo.GetCustomAttributes(typeof(SerializedTypeRestrictionAttribute), false);
            if (attrs.Length == 0 || !(attrs[0] is SerializedTypeRestrictionAttribute))
                return null;
            return AddressableAssetUtility.GetTypes((attrs[0] as SerializedTypeRestrictionAttribute).type);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(UIPrefabPartial.PrefabInfo))]
public class UIPrefabPartialItemDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {

            //设置属性名宽度
            EditorGUIUtility.labelWidth = 60;
            position.height = EditorGUIUtility.singleLineHeight;

            var prefabRect = new Rect(position)
            {
                width = position.width * 0.4f,
            };

            var parentRect = new Rect(position)
            {
                width = position.width * 0.4f,
                x = position.x + position.width * 0.4f,
            };

            var frameRect = new Rect(position)
            {
                width = position.width * 0.2f,
                x = position.x + position.width * 0.8f,
            };

            var parentProperty = property.FindPropertyRelative("m_Parent");
            var pathProperty = property.FindPropertyRelative("m_Path");
            var frameProperty = property.FindPropertyRelative("m_Frame");

            UnityEngine.Object oldPrefab = null;
            string path = pathProperty.stringValue;
            if(!string.IsNullOrEmpty(path))
            {
                oldPrefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            }
            UnityEngine.Object prefab = EditorGUI.ObjectField(prefabRect, oldPrefab, typeof(GameObject), false);
            if(prefab != oldPrefab)
            {
                if(prefab != null)
                {
                    pathProperty.stringValue = AssetDatabase.GetAssetPath(prefab);
                }
                else
                {
                    pathProperty.stringValue = string.Empty;
                }
            }

            parentProperty.objectReferenceValue = EditorGUI.ObjectField(parentRect, parentProperty.objectReferenceValue, typeof(GameObject), true);

            frameProperty.intValue = EditorGUI.IntField(frameRect, frameProperty.intValue);
            if(frameProperty.intValue < 0)
            {
                frameProperty.intValue = 0;
            }
        }
    }
}

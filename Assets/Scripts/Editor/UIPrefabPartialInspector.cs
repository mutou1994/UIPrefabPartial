using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditorInternal;

[CustomEditor(typeof(UIPrefabPartial), true)]
public class UIPrefabPartialInspector : Editor
{
    static string m_PrefabStr = "Prafab";
    static string m_ParentStr = "Parent";
    static string m_FrameStr = "Frame";

    bool m_IsPrefab = false;
    int m_SelectedIndex = -1;
    private ReorderableList _prefabsArray;
    UIPrefabPartial m_PrefabTool;

    public static bool CheckIsPrefab(Object obj)
    {
        var assetType = PrefabUtility.GetPrefabAssetType(obj);
        var status = PrefabUtility.GetPrefabInstanceStatus(obj);
        return assetType != PrefabAssetType.NotAPrefab && status == PrefabInstanceStatus.NotAPrefab;
        //2018以前版本判断Prefab的接口
        //return PrefabUtility.GetPrefabType(obj) == PrefabType.Prefab;
    }

    void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        if (Event.current != null && selectionRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0 && (Event.current.type == EventType.MouseDown))
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                GameObject selectedGameObject = UnityEditor.EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                //选中物体筛选条件
                if (selectedGameObject.name.Equals(UIPrefabPartial.m_PreviewName))
                {
                    Event.current.type = EventType.Ignore;
                }
            }
        }
    }

    private void OnDisable()
    {
        if(!m_IsPrefab)
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
        }
    }

    private void OnEnable()
    {
        
        m_IsPrefab = CheckIsPrefab(target);
        m_PrefabTool = target as UIPrefabPartial;
        _prefabsArray = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Prefabs")
            , true, true, true, true);

        //定义元素的高度
        _prefabsArray.elementHeight = 20;

        //自定义列表名称
        _prefabsArray.drawHeaderCallback = (Rect rect) =>
        {
            GUI.Label(new Rect(rect)
            {
                x = rect.x + rect.width * 0.1f,
            }, m_PrefabStr);

            GUI.Label(new Rect(rect)
            {
                x = rect.x + rect.width * 0.5f,
            }, m_ParentStr);

            GUI.Label(new Rect(rect)
            {
                x = rect.x + rect.width * 0.8f,
            }, m_FrameStr);
        };

        //自定义绘制列表元素
        _prefabsArray.drawElementCallback = (Rect rect, int index, bool selected, bool focused) =>
        {
            if(selected || focused)
            {
                m_SelectedIndex = index;
            }
            
            //根据index获取对应元素 
            SerializedProperty item = _prefabsArray.serializedProperty.GetArrayElementAtIndex(index);

            var pathProperty = item.FindPropertyRelative("m_Path");
            var parentProperty = item.FindPropertyRelative("m_Parent");

            string oldPath = pathProperty.stringValue;
            Object oldParent = parentProperty.objectReferenceValue;
            EditorGUI.PropertyField(rect, item);
            string newPath = pathProperty.stringValue;
            Object newParent = parentProperty.objectReferenceValue;

            if(newParent != null)
            {
                if(!CheckParent(newParent as GameObject))
                {
                    parentProperty.objectReferenceValue = oldParent;
                    Debug.LogError("只能绑定子节点对象");
                }
                else
                {
                    int preIndex = m_PrefabTool.Prefabs.FindIndex(o => o.m_Parent == parentProperty.objectReferenceValue);
                    if (preIndex >= 0 && preIndex != index)
                    {
                        if(oldParent != parentProperty.objectReferenceValue)
                        {
                            parentProperty.objectReferenceValue = oldParent;
                        }
                        else
                        {
                            parentProperty.objectReferenceValue = null;
                        }
                        Debug.LogError("一个Parent只能与一个Prefab绑定");
                    }
                }
            }

            if(!m_IsPrefab && (oldParent != parentProperty.objectReferenceValue || !oldPath.Equals(newPath)))
            {
                m_PrefabTool.DestroyPreview(oldParent as GameObject);
                GameObject newGo = parentProperty.objectReferenceValue as GameObject;
                m_PrefabTool.UpdateOnePreviewInEditor(newPath, newGo);
            }
        };

        _prefabsArray.onRemoveCallback = (ReorderableList list) =>
        {
            if(m_SelectedIndex < 0 || m_SelectedIndex >= list.count)
            {
                m_SelectedIndex = list.count - 1;
            }
            SerializedProperty item = list.serializedProperty.GetArrayElementAtIndex(m_SelectedIndex);
            if (item != null)
            {
                var parentProperty = item.FindPropertyRelative("m_Parent");
                m_PrefabTool.DestroyPreview(parentProperty.objectReferenceValue as GameObject);
            }
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            
        };

        _prefabsArray.onAddCallback = (ReorderableList list) =>
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            SerializedProperty item = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
            if(item != null)
            {
                var pathProperty = item.FindPropertyRelative("m_Path");
                var parentProperty = item.FindPropertyRelative("m_Parent");
                var frameProperty = item.FindPropertyRelative("m_Frame");
                pathProperty.stringValue = null;
                parentProperty.objectReferenceValue = null;
                frameProperty.intValue = 0;
            }
        };

        if (!m_IsPrefab)
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            m_PrefabTool.PreviewAllInEditor();
        }
    }

    bool CheckParent(GameObject obj)
    {
        Transform rootTrans = m_PrefabTool.transform;
        Transform trans = obj.transform;
        while (trans != null && trans != rootTrans)
        {
            trans = trans.parent;
        }
        return trans == rootTrans;
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        serializedObject.Update();
        //自动布局绘制列表
        _prefabsArray.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}

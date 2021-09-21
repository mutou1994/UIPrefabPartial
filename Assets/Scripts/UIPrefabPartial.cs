using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class UIPrefabPartial : MonoBehaviour
{
    static WaitForEndOfFrame frameWait;

    public Action<int, GameObject> FrameLoadCallBack;

    [Serializable]
    public struct PrefabInfo
    {
        [SerializeField]
        public GameObject m_Parent;

        [SerializeField]
        public string m_Path;

        [SerializeField]
        public int m_Frame;
    }

    [SerializeField]
    List<PrefabInfo> m_Prefabs = new List<PrefabInfo>();
    public List<PrefabInfo> Prefabs
    {
        get
        {
            return m_Prefabs;
        }
    }

    private void Awake()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            AwakeInEditor();
            return;
        }
#endif
        //做个排序
        m_Prefabs.Sort((l, r) =>
        {
            if (l.m_Frame < r.m_Frame)
                return -1;
            else if (l.m_Frame == r.m_Frame)
                return 0;
            else
                return 1;
        });
        if (frameWait == null)
        {
            frameWait = new WaitForEndOfFrame();
        }
        StartCoroutine(LoadPrefabCoroutine());
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            OnDestroyInEditor();
            return;
        }
#endif
        m_Prefabs.Clear();
        m_Prefabs = null;
        FrameLoadCallBack = null;
    }

    GameObject LoadPrefab(string path, Transform parent)
    {
#if UNITY_EDITOR
        //此处可改为项目资源加载接口
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Load Prefab Failed >>> Parent:" + parent + " PrefabPath:" + path);
            return null;
        }
        GameObject go = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;// Instantiate(prefab, parent.transform);
        return go;
#endif
    }

    public GameObject LoadPartial(string path, GameObject parent)
    {
        if (parent == null || string.IsNullOrEmpty(path)) return null;
        RectTransform pTrans = parent.transform as RectTransform;
        GameObject go = LoadPrefab(path, pTrans);

        if (go == null) return null;
        
        RectTransform goTrans = go.transform as RectTransform;
        goTrans.SetSiblingIndex(0);
        go.name = pTrans.name;

        goTrans.pivot = Vector2.one * 0.5f;
        goTrans.anchorMin = Vector3.zero;
        goTrans.anchorMax = Vector3.one;
        goTrans.offsetMin = Vector2.zero;
        goTrans.offsetMax = Vector2.zero;
        goTrans.anchoredPosition = Vector2.zero;

        goTrans.localPosition = Vector3.zero;
        goTrans.localEulerAngles = Vector3.zero;
        goTrans.localScale = Vector3.one;

        return go;
    }

    IEnumerator LoadPrefabCoroutine()
    {
        int frame = 0;
        int index = 0;
        int count = Prefabs.Count;
        while(index < count)
        {
            for(int i = index; i < count; i++)
            {
                PrefabInfo info = m_Prefabs[i];
                if(frame >= info.m_Frame)
                {
                    index = i + 1;
                    //此处可再加异步加载接口
                    GameObject go = LoadPartial(info.m_Path, info.m_Parent);
                    if(go)
                    {
                        if(FrameLoadCallBack != null)
                        {
                            FrameLoadCallBack(frame, go);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            frame++;
            yield return frameWait;
        }
    }

#if UNITY_EDITOR
    public static string m_PreviewName = ">>PrefabPreview<<";
    Dictionary<GameObject, GameObject> m_InstanceMap = new Dictionary<GameObject, GameObject>();

    void AwakeInEditor()
    {
        if (CheckIsPrefab(this.gameObject)) return;
        PreviewAllInEditor(); 
        UnityEditor.PrefabUtility.prefabInstanceUpdated += OnPrefabChanged;
    }

    void OnDestroyInEditor()
    {
        if (CheckIsPrefab(this.gameObject)) return;
        ClearPreview();
        UnityEditor.PrefabUtility.prefabInstanceUpdated -= OnPrefabChanged;
    }
    public bool CheckIsPrefab(UnityEngine.Object obj)
    {
        var assetType = UnityEditor.PrefabUtility.GetPrefabAssetType(obj);
        var status = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(obj);
        return assetType != UnityEditor.PrefabAssetType.NotAPrefab && status == UnityEditor.PrefabInstanceStatus.NotAPrefab;
        //2018以前版本判断Prefab的接口
        //return PrefabUtility.GetPrefabType(obj) == PrefabType.Prefab;
    }

    public bool CheckIsPrefabInstance(UnityEngine.Object obj)
    {
        var assetType = UnityEditor.PrefabUtility.GetPrefabAssetType(obj);
        var status = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(obj);
        return assetType != UnityEditor.PrefabAssetType.NotAPrefab && status == UnityEditor.PrefabInstanceStatus.Connected;
    }

    public GameObject GetPrefabRoot(GameObject obj)
    {
        if (!CheckIsPrefabInstance(obj)) return null;
        return UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
    }

    void OnPrefabChanged(GameObject obj)
    {
        if (CheckIsPrefabInstance(this.gameObject))
        {
            if (GetPrefabRoot(this.gameObject) == GetPrefabRoot(obj))
            {
                ClearPreview();
                PreviewAllInEditor();
            }
        }
    }

    public void PreviewAllInEditor()
    {
        if (Application.isPlaying) return;
        foreach (var info in Prefabs)
        {
            UpdateOnePreviewInEditor(info.m_Path, info.m_Parent);
        }
    }

    public void UpdateOnePreviewInEditor(string path, GameObject parent)
    {
        if (parent == null || string.IsNullOrEmpty(path)) return;
        DestroyPreview(parent);
        GameObject go = LoadPartial(path, parent);
        if (go != null)
        {
            if(m_InstanceMap.ContainsKey(parent))
            {
                m_InstanceMap[parent] = go;
            }else
            {
                m_InstanceMap.Add(parent, go);
            }
            go.name = m_PreviewName;
            //需要调试的话开成注释的状态
            go.hideFlags = HideFlags.HideAndDontSave;
            //go.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
        }
    }

    public void DestroyPreview(GameObject parent)
    {
        if (parent == null) return;
        GameObject go = GetPreviewInstance(parent);
        if (go)
        {
            DestroyInEditor(go);
            m_InstanceMap.Remove(parent);
        }
    }

    public GameObject GetPreviewInstance(GameObject parent)
    {
        if (parent == null) return null;
        if(m_InstanceMap.ContainsKey(parent))
        {
            return m_InstanceMap[parent];
        }
       /* else
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Transform child = parent.transform.GetChild(i);
                if (child != null && child.name.Equals(m_PreviewName))
                {
                    if ((child.hideFlags & HideFlags.DontSave) > HideFlags.None)
                    {
                        return child.gameObject;
                    }
                }
            }
        }*/
        return null;
    }

    public void DestroyInEditor(GameObject instance)
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (instance)
            {
                DestroyImmediate(instance);
            }
        };
    }

    void ClearPreview()
    {
        foreach(var instance in m_InstanceMap)
        {
            if(instance.Value != null)
            {
                DestroyInEditor(instance.Value);
            }
        }
        m_InstanceMap.Clear();
        /*GameObject parent;
        foreach (var info in Prefabs)
        {
            parent = info.m_Parent;
            if (parent != null)
            {
                for(int i=0; i < parent.transform.childCount; i++)
                {
                    Transform child = parent.transform.GetChild(0);
                    if (child != null && child.name.Equals(">>PrefabPreview<<"))
                    {
                        if((child.hideFlags & HideFlags.DontSave) > HideFlags.None)
                        {
                            DestroyInEditor(child.gameObject);
                        }
                    }
                }
            }
        }*/
    }

    //[ContextMenu("Remove Component")]
    //void RemoveComponent()
    //{
    //    GameObject go = this.gameObject;
    //    var assetType = UnityEditor.PrefabUtility.GetPrefabAssetType(go);
    //    var status = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(go);
    //    if (assetType == UnityEditor.PrefabAssetType.NotAPrefab || status != UnityEditor.PrefabInstanceStatus.NotAPrefab)
    //    {
    //        ClearPreview();
    //    }

    //    DestroyInEditor(this, true);
    //    UnityEditor.EditorUtility.SetDirty(go);
    //}

    private void Reset()
    {
        ClearPreview();
    }
#endif

}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ResourceStack : MonoBehaviour
{
    public enum StackMode { Upward, BehindPlayer }
    [SerializeField] private StackMode mode = StackMode.Upward;
    [SerializeField] private float spacing = 0.5f; 
    [SerializeField] private Transform root;

    private class ResourceGroup
    {
        public string name;
        public List<GameObject> visuals = new List<GameObject>();
    }

    private List<ResourceGroup> _groups = new List<ResourceGroup>();

    public void Add(string resourceName)
    {
        if (ResourceDatabase.Instance == null) return;
        
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(resourceName);
        if (prefab == null) return;

        ResourceGroup group = _groups.Find(g => g.name == resourceName);
        if (group == null)
        {
            group = new ResourceGroup { name = resourceName };
            if (mode == StackMode.BehindPlayer) _groups.Insert(0, group);
            else _groups.Add(group);
        }

        GameObject obj = Instantiate(prefab, root != null ? root : transform);
        group.visuals.Add(obj);
        
        UpdateLayout();
    }

    public void Remove(string resourceName)
    {
        ResourceGroup group = _groups.Find(g => g.name == resourceName);
        if (group == null || group.visuals.Count == 0) return;

        GameObject obj = group.visuals[group.visuals.Count - 1];
        group.visuals.RemoveAt(group.visuals.Count - 1);
        if (obj != null) Destroy(obj);

        if (group.visuals.Count == 0)
        {
            _groups.Remove(group);
        }

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        for (int groupIdx = 0; groupIdx < _groups.Count; groupIdx++)
        {
            var group = _groups[groupIdx];
            for (int i = 0; i < group.visuals.Count; i++)
            {
                if (group.visuals[i] == null) continue;

                if (mode == StackMode.BehindPlayer)
                {
                    // groupIdx: 플레이어 뒤로 몇 번째 줄인지
                    // i: 위로 몇 번째인지
                    float yOffset = i * spacing;
                    float zOffset = -(groupIdx + 1) * spacing;
                    
                    group.visuals[i].transform.localPosition = new Vector3(0, yOffset, zOffset);
                    group.visuals[i].transform.localRotation = Quaternion.identity;
                }
                else
                {
                    // Upward 모드일 경우 모든 자원을 하나의 수직 기둥으로 합쳐서 표시
                    int totalBefore = 0;
                    for (int j = 0; j < groupIdx; j++) totalBefore += _groups[j].visuals.Count;
                    
                    group.visuals[i].transform.localPosition = new Vector3(0, (totalBefore + i) * spacing, 0);
                    group.visuals[i].transform.localRotation = Quaternion.identity;
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var group in _groups)
        {
            foreach (var v in group.visuals) if (v != null) Destroy(v);
        }
        _groups.Clear();
    }
}

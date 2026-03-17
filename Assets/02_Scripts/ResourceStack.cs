using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ResourceStack : MonoBehaviour
{
    public enum StackMode { Upward, BehindPlayer }
    [SerializeField] private StackMode mode = StackMode.Upward;
    
    [Header("Spacing Settings")]
    [SerializeField] private float verticalSpacing = 0.5f; // 위아래 간격 (Y축)
    [SerializeField] private float rowSpacing = 0.5f;      // 앞뒤 줄 간격 (Z축)
    
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
                    // groupIdx: 플레이어 뒤로 몇 번째 줄인지 (rowSpacing 사용)
                    // i: 위로 몇 번째인지 (verticalSpacing 사용)
                    float yOffset = i * verticalSpacing;
                    float zOffset = -(groupIdx + 1) * rowSpacing;
                    
                    group.visuals[i].transform.localPosition = new Vector3(0, yOffset, zOffset);
                }
                else
                {
                    // Upward 모드일 경우 모든 자원을 하나의 수직 기둥으로 합쳐서 표시 (verticalSpacing 사용)
                    int totalBefore = 0;
                    for (int j = 0; j < groupIdx; j++) totalBefore += _groups[j].visuals.Count;
                    
                    group.visuals[i].transform.localPosition = new Vector3(0, (totalBefore + i) * verticalSpacing, 0);
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

    // [추가] 전체 자원 개수 반환
    public int GetTotalCount()
    {
        int total = 0;
        foreach (var group in _groups) total += group.visuals.Count;
        return total;
    }
}

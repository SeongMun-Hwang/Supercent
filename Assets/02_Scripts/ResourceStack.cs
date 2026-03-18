using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ResourceStack : MonoBehaviour
{
    public enum StackMode { Upward, BehindPlayer }
    [SerializeField] private StackMode mode = StackMode.Upward;
    
    [Header("Spacing Settings")]
    [SerializeField] private float verticalSpacing = 0.5f; 
    [SerializeField] private float rowSpacing = 0.5f;      
    
    [Header("Animation Settings")]
    [SerializeField] private float jumpDuration = 0.4f;
    [SerializeField] private float jumpHeight = 1.5f;

    [SerializeField] private Transform root;

    private string _assignedResourceName; // [추가] 이 스택이 담당하는 자원 이름

    public void SetAssignedResourceName(string resName) => _assignedResourceName = resName;
    public string GetAssignedResourceName() => _assignedResourceName;

    private class ResourceGroup
    {
        public string name;
        public List<GameObject> visuals = new List<GameObject>();
    }

    private List<ResourceGroup> _groups = new List<ResourceGroup>();

    // [추가] 이름 없이 호출 시 할당된 이름을 사용
    public void AddWithAnimation(Vector3 startWorldPos)
    {
        if (string.IsNullOrEmpty(_assignedResourceName)) return;
        AddWithAnimation(_assignedResourceName, startWorldPos);
    }

    // [추가] 이름 없이 호출 시 할당된 이름을 사용
    public void RemoveOne()
    {
        if (string.IsNullOrEmpty(_assignedResourceName)) return;
        Remove(_assignedResourceName);
    }

    public void Add(string resourceName)
    {
        if (ResourceDatabase.Instance == null) return;
        
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(resourceName);
        if (prefab == null) return;

        ResourceGroup group = GetOrCreateGroup(resourceName);
        GameObject obj = Instantiate(prefab, root != null ? root : transform);
        group.visuals.Add(obj);
        
        UpdateLayout();
    }

    public void AddWithAnimation(string resourceName, Vector3 startWorldPos)
    {
        if (ResourceDatabase.Instance == null) return;
        
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(resourceName);
        if (prefab == null) return;

        ResourceGroup group = GetOrCreateGroup(resourceName);
        GameObject obj = Instantiate(prefab, root != null ? root : transform);
        group.visuals.Add(obj);
        
        Vector3 targetLocalPos = CalculateTargetLocalPos(group, group.visuals.Count - 1);
        StartCoroutine(ParabolicJumpRoutine(obj, startWorldPos, targetLocalPos));
    }

    private ResourceGroup GetOrCreateGroup(string resourceName)
    {
        ResourceGroup group = _groups.Find(g => g.name == resourceName);
        if (group == null)
        {
            group = new ResourceGroup { name = resourceName };
            if (mode == StackMode.BehindPlayer) _groups.Insert(0, group);
            else _groups.Add(group);
        }
        return group;
    }

    public void Remove(string resourceName)
    {
        ResourceGroup group = _groups.Find(g => g.name == resourceName);
        if (group == null || group.visuals.Count == 0) return;

        GameObject obj = group.visuals[group.visuals.Count - 1];
        group.visuals.RemoveAt(group.visuals.Count - 1);
        if (obj != null) Destroy(obj);

        if (group.visuals.Count == 0) _groups.Remove(group);

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
                group.visuals[i].transform.localPosition = CalculateTargetLocalPos(group, i);
            }
        }
    }

    private Vector3 CalculateTargetLocalPos(ResourceGroup group, int indexInGroup)
    {
        int groupIdx = _groups.IndexOf(group);
        if (mode == StackMode.BehindPlayer)
        {
            float yOffset = indexInGroup * verticalSpacing;
            float zOffset = -(groupIdx + 1) * rowSpacing;
            return new Vector3(0, yOffset, zOffset);
        }
        else
        {
            int totalBefore = 0;
            for (int j = 0; j < groupIdx; j++) totalBefore += _groups[j].visuals.Count;
            return new Vector3(0, (totalBefore + indexInGroup) * verticalSpacing, 0);
        }
    }

    private IEnumerator ParabolicJumpRoutine(GameObject obj, Vector3 startWorldPos, Vector3 targetLocalPos)
    {
        float elapsed = 0;
        Transform parent = root != null ? root : transform;
        
        // 생성 시점의 로컬 회전값을 저장하여 유지
        Quaternion initialLocalRot = obj.transform.localRotation;

        while (elapsed < jumpDuration)
        {
            if (obj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;

            // 이동 중에도 부모(플레이어/플랫폼)가 움직일 수 있으므로 목적지 월드 좌표 갱신
            Vector3 targetWorldPos = parent.TransformPoint(targetLocalPos);

            // 포물선 궤적 계산
            Vector3 currentPos = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            float arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            currentPos.y += arc;

            obj.transform.position = currentPos;
            
            // 부모의 회전에 맞춰 회전하되 고유 로컬 회전 유지
            obj.transform.rotation = parent.rotation * initialLocalRot;

            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localPosition = targetLocalPos;
            obj.transform.localRotation = initialLocalRot; // 원래의 로컬 회전값 유지
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

    public int GetTotalCount()
    {
        int total = 0;
        foreach (var group in _groups) total += group.visuals.Count;
        return total;
    }

    public string PopResourceName()
    {
        if (_groups.Count == 0) return null;

        // 마지막 그룹 선택 (가장 최근에 추가된 종류)
        ResourceGroup lastGroup = _groups[_groups.Count - 1];
        if (lastGroup.visuals.Count == 0)
        {
            _groups.RemoveAt(_groups.Count - 1);
            return PopResourceName(); // 재귀적으로 빈 그룹 건너뜀
        }

        string resName = lastGroup.name;
        Remove(resName); // 기존의 Remove 로직 재사용 (비주얼 제거 및 레이아웃 갱신)
        return resName;
    }
}

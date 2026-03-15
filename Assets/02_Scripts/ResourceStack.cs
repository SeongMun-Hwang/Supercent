using UnityEngine;
using System.Collections.Generic;

public class ResourceStack : MonoBehaviour
{
    public enum StackMode { Upward, BehindPlayer }
    [SerializeField] private StackMode mode = StackMode.Upward;
    [SerializeField] private float spacing = 0.5f; 
    [SerializeField] private Transform root;

    [Header("Grid Settings (for BehindPlayer)")]
    [SerializeField] private int gridWidth = 3;  // 가로 칸수
    [SerializeField] private int gridDepth = 1;  // 세로(뒤로) 칸수

    private List<GameObject> _visuals = new List<GameObject>();

    public void Add(string resourceName)
    {
        if (ResourceDatabase.Instance == null) return;
        
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(resourceName);
        if (prefab == null) 
        {
            Debug.LogWarning($"[Stack] No prefab found for resource: {resourceName}. Check ResourceDatabase!");
            return;
        }

        GameObject obj = Instantiate(prefab, root != null ? root : transform);
        obj.name = resourceName; // 디버깅용 이름 설정
        
        if (mode == StackMode.Upward)
        {
            obj.transform.localPosition = new Vector3(0, _visuals.Count * spacing, 0);
            obj.transform.localRotation = Quaternion.identity;
            _visuals.Add(obj);
        }
        else
        {
            // 신규 자원을 리스트의 맨 앞에 추가 (플레이어와 가장 가깝게)
            _visuals.Insert(0, obj);
            UpdateLayout();
        }
    }

    public void Remove()
    {
        if (_visuals.Count == 0) return;

        int index = mode == StackMode.Upward ? _visuals.Count - 1 : 0;
        GameObject obj = _visuals[index];
        _visuals.RemoveAt(index);
        if (obj != null) Destroy(obj);

        if (mode == StackMode.BehindPlayer) UpdateLayout();
    }

    private void UpdateLayout()
    {
        for (int i = 0; i < _visuals.Count; i++)
        {
            if (_visuals[i] == null) continue;

            if (mode == StackMode.BehindPlayer)
            {
                // 그리드 좌표 계산
                // x: 좌우, y: 위아래, z: 앞뒤
                int layerSize = gridWidth * gridDepth;
                int layer = i / layerSize; // 몇 층인지
                int posInLayer = i % layerSize; // 해당 층에서 몇 번째인지
                
                int xPos = posInLayer % gridWidth; // 가로 위치
                int zPos = posInLayer / gridWidth; // 세로 위치

                // 중앙 정렬을 위한 X 오프셋 계산
                float xOffset = (xPos - (gridWidth - 1) / 2f) * spacing;
                float yOffset = layer * spacing;
                float zOffset = -(zPos + 1) * spacing;

                _visuals[i].transform.localPosition = new Vector3(xOffset, yOffset, zOffset);
                _visuals[i].transform.localRotation = Quaternion.identity;
            }
            else
            {
                _visuals[i].transform.localPosition = new Vector3(0, i * spacing, 0);
                _visuals[i].transform.localRotation = Quaternion.identity;
            }
        }
    }
    
    public void Clear()
    {
        foreach (var v in _visuals)
        {
            if (v != null) Destroy(v);
        }
        _visuals.Clear();
    }
}

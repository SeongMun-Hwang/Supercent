using UnityEngine;
using System;
using System.Collections.Generic;

public class ResourceDatabase : MonoBehaviour
{
    public static ResourceDatabase Instance { get; private set; }

    [Serializable]
    public struct ResourceData
    {
        public string name;
        public GameObject prefab;
        public Sprite icon;  // 자원 아이콘
        public int maxCount; // 자원 최대 보유 개수
    }

    [SerializeField] private List<ResourceData> resourceList;

    private Dictionary<string, ResourceData> _database = new Dictionary<string, ResourceData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var data in resourceList)
        {
            if (!string.IsNullOrEmpty(data.name))
                _database[data.name] = data;
        }
    }

    public GameObject GetPrefab(string resourceName)
    {
        if (_database.TryGetValue(resourceName, out ResourceData data))
            return data.prefab;

        return null;
    }

    public Sprite GetSprite(string resourceName)
    {
        if (_database.TryGetValue(resourceName, out ResourceData data))
            return data.icon;

        return null;
    }

    public int GetMaxCount(string resourceName)
    {
        if (_database.TryGetValue(resourceName, out ResourceData data))
        {
            if (data.maxCount <= 0) return 10; // 기본값 10으로 변경
            return data.maxCount;
        }

        return 10; // 자원이 DB에 없을 때도 기본값 10 반환
    }
}

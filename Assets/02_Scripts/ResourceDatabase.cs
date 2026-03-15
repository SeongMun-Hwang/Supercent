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
    }

    [SerializeField] private List<ResourceData> resourceList;
    private Dictionary<string, GameObject> _database = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var data in resourceList)
        {
            if (data.prefab != null)
                _database[data.name] = data.prefab;
        }
    }

    public GameObject GetPrefab(string resourceName)
    {
        if (_database.TryGetValue(resourceName, out GameObject prefab)) return prefab;
        return null;
    }
}

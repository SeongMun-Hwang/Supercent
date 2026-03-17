using UnityEngine;
using System.Collections.Generic;

public class WorkerSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject workerPrefab;
    [SerializeField] private List<Transform> spawnPositions;

    [Header("Worker Task")]
    [SerializeField] private ResourceStack sourceStack; // 자원을 가져올 곳
    [SerializeField] private ResourceStack targetStack; // 자원을 가져다 놓을 곳
    [SerializeField] private Transform pointA; // 작업 위치 A (집기)
    [SerializeField] private Transform pointB; // 작업 위치 B (내려놓기)

    public void SpawnWorkers()
    {
        if (workerPrefab == null || spawnPositions == null || spawnPositions.Count == 0) 
        {
            Debug.LogWarning("[WorkerSpawner] Prefab or SpawnPositions missing!");
            return;
        }

        foreach (Transform pos in spawnPositions)
        {
            GameObject obj = Instantiate(workerPrefab, pos.position, Quaternion.identity);
            Worker worker = obj.GetComponent<Worker>();

            if (worker != null)
            {
                worker.Initialize(
                    pointA.GetComponent<ResourceStack>(),
                    pointB.GetComponent<ResourceStack>(),
                    pointA.position,
                    pointB.position
                );
            }
            else
            {
                Debug.LogError($"[WorkerSpawner] Worker.cs component missing on prefab: {workerPrefab.name}");
            }
        }

        Debug.Log($"[WorkerSpawner] Successfully spawned {spawnPositions.Count} workers.");
    }
}

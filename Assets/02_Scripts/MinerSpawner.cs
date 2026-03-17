using UnityEngine;
using System.Collections.Generic;

public class MinerSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject minerPrefab;
    [SerializeField] private List<Transform> spawnPositions;
    [SerializeField] private float moveDistance = 20f; // -Z 축으로 이동할 거리
    
    [Header("Targets (Choose One)")]
    [SerializeField] private ResourceSubmissionPlatform targetSubmission;
    [SerializeField] private EquipmentUpgradePlatform targetUpgrade;

    public void SpawnMiners()
    {
        if (minerPrefab == null || spawnPositions == null || spawnPositions.Count == 0) return;

        foreach (Transform pos in spawnPositions)
        {
            GameObject obj = Instantiate(minerPrefab, pos.position, Quaternion.identity);
            Miner miner = obj.GetComponent<Miner>();
            if (miner != null)
            {
                // 목적지 계산: 현재 위치에서 -Z 방향으로 moveDistance 만큼
                Vector3 destPos = pos.position + Vector3.back * moveDistance;
                miner.Initialize(pos.position, destPos, targetSubmission, targetUpgrade);
            }
        }
        
        Debug.Log($"[MinerSpawner] Spawned {spawnPositions.Count} miners with range {moveDistance}.");
    }
}

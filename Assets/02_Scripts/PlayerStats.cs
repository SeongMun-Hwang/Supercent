using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Combat Stats")]
    public float attackPower = 20f;
    public float attackSpeed = 1.0f; // 공격 간격 (초)

    [Header("Equipment")]
    public Transform equipmentRoot; // 장비가 부착될 위치

    [Header("Visuals")]
    [SerializeField] private ResourceStack visualStack; // 시각적 스택

    private ResourcePlatform _currentHarvestTarget; // 현재 채집 중인 단일 타겟

    public bool HasEquipment()
    {
        return equipmentRoot != null && equipmentRoot.childCount > 0;
    }

    // 채집 가능 여부 확인 및 잠금
    public bool RequestHarvestPermission(ResourcePlatform platform)
    {
        // 장비가 있으면 무조건 허용 (멀티 채집)
        if (HasEquipment()) return true;

        // 장비가 없으면:
        // 1. 이미 채집 중인 타겟이 있는데 그게 내가 아니라면 거절
        if (_currentHarvestTarget != null && _currentHarvestTarget != platform)
        {
            return false;
        }

        // 2. 채집 중인 타겟이 없거나 나라면 허용하고 잠금
        _currentHarvestTarget = platform;
        return true;
    }

    public void ReleaseHarvestPermission(ResourcePlatform platform)
    {
        // 명시적으로 해당 플랫폼이 잠금을 소유하고 있을 때만 해제
        if (_currentHarvestTarget == platform)
        {
            _currentHarvestTarget = null;
            Debug.Log($"[PlayerStats] Harvest target released: {platform.gameObject.name}");
        }
    }

    [Header("Inventory Settings")]
    [SerializeField] private int extraCapacity = 0; // 업그레이드로 추가된 용량
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetResourceLimit(string resourceName)
    {
        int baseLimit = 10;
        if (ResourceDatabase.Instance != null)
            baseLimit = ResourceDatabase.Instance.GetMaxCount(resourceName);

        return baseLimit + extraCapacity;
    }

    public void UpgradeMaxCapacity(int amount)
    {
        extraCapacity += amount;
        Debug.Log($"[PlayerStats] Max Capacity Upgraded! New Extra: {extraCapacity}");
    }

    public void AddResource(string resourceName, int amount)
    {
        int current = GetResourceCount(resourceName);
        int limit = GetResourceLimit(resourceName);

        int canAdd = Mathf.Clamp(amount, 0, limit - current);

        if (canAdd <= 0) return;

        if (_inventory.ContainsKey(resourceName))
            _inventory[resourceName] += canAdd;
        else
            _inventory.Add(resourceName, canAdd);

        if (visualStack != null)
        {
            for (int i = 0; i < canAdd; i++)
            {
                visualStack.Add(resourceName);
            }
        }

        Debug.Log($"[PlayerStats] {resourceName} = {_inventory[resourceName]} / {limit}");
    }

    public int GetResourceCount(string resourceName)
    {
        return _inventory.ContainsKey(resourceName) ? _inventory[resourceName] : 0;
    }

    public void SpendResource(string resourceName, int amount)
    {
        if (_inventory.ContainsKey(resourceName))
        {
            Debug.Log($"[PlayerStats] Spending {amount} x {resourceName}");
            _inventory[resourceName] -= amount;

            // Visual Stacking
            if (visualStack != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    visualStack.Remove(resourceName);
                }
            }
        }
    }

    // Upgrade Methods
    public void UpgradeAttackPower(float amount)
    {
        attackPower += amount;
        Debug.Log($"[PlayerStats] Attack Power Upgraded: {attackPower}");
    }

    public void UpgradeAttackSpeed(float amount)
    {
        // 공격 간격을 조정 (음수 값을 더하면 빨라짐)
        attackSpeed += amount;
        attackSpeed = Mathf.Max(0.1f, attackSpeed); // 최소 공격 간격 제한
        Debug.Log($"[PlayerStats] Attack Speed Upgraded: {attackSpeed}");
    }
}

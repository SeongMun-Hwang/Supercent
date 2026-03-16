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

    [Header("Inventory")]
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Visuals")]
    [SerializeField] private ResourceStack visualStack;

    public void AddResource(string resourceName, int amount)
    {
        int current = GetResourceCount(resourceName);

        int max = 10;
        if (ResourceDatabase.Instance != null)
            max = ResourceDatabase.Instance.GetMaxCount(resourceName);

        int canAdd = Mathf.Clamp(amount, 0, max - current);

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

        Debug.Log($"Inventory: {resourceName} = {_inventory[resourceName]} / {max}");
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
        Debug.Log($"Attack Power Upgraded: {attackPower}");
    }
}

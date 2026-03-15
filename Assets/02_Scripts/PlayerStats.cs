using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Combat Stats")]
    public float attackPower = 20f;

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
        if (_inventory.ContainsKey(resourceName))
        {
            _inventory[resourceName] += amount;
        }
        else
        {
            _inventory.Add(resourceName, amount);
        }

        // Visual Stacking
        if (visualStack != null)
        {
            for (int i = 0; i < amount; i++)
            {
                visualStack.Add(resourceName);
            }
        }
        
        Debug.Log($"Inventory: {resourceName} = {_inventory[resourceName]}");
    }

    public int GetResourceCount(string resourceName)
    {
        return _inventory.ContainsKey(resourceName) ? _inventory[resourceName] : 0;
    }

    public void SpendResource(string resourceName, int amount)
    {
        if (_inventory.ContainsKey(resourceName))
        {
            _inventory[resourceName] -= amount;

            // Visual Stacking
            if (visualStack != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    visualStack.Remove();
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

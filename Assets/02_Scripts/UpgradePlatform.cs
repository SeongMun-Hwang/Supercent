using UnityEngine;

public class UpgradePlatform : MonoBehaviour, IPlatformAction
{
    public enum UpgradeType { AttackPower }

    [Header("Upgrade Settings")]
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private string requiredResource = "Iron";
    [SerializeField] private int baseCost = 100;
    [SerializeField] private float upgradeValue = 10f;
    [SerializeField] private float costMultiplier = 1.5f;

    private int _currentLevel = 1;
    private int _currentCost;

    private void Awake()
    {
        _currentCost = baseCost;
    }

    public void OnPlayerEnter(GameObject player)
    {
        Debug.Log($"Entered {upgradeType} Upgrade Platform. Cost: {_currentCost} {requiredResource}");
    }

    public void OnPlayerStay(GameObject player)
    {
        // Simple logic: press Space to upgrade
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryUpgrade();
        }
    }

    public void OnPlayerExit(GameObject player)
    {
    }

    private void TryUpgrade()
    {
        if (PlayerStats.Instance == null) return;

        // Check if player has enough resources
        if (PlayerStats.Instance.GetResourceCount(requiredResource) >= _currentCost)
        {
            // Spend resource
            PlayerStats.Instance.SpendResource(requiredResource, _currentCost);

            // Apply Upgrade
            if (upgradeType == UpgradeType.AttackPower)
            {
                PlayerStats.Instance.UpgradeAttackPower(upgradeValue);
            }

            // Increase cost for next level
            _currentLevel++;
            _currentCost = Mathf.RoundToInt(_currentCost * costMultiplier);

            Debug.Log($"Upgraded to Level {_currentLevel}! Next cost: {_currentCost}");
        }
        else
        {
            Debug.Log("Not enough resources to upgrade!");
        }
    }
}

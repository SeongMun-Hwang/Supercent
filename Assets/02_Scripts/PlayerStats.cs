using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Combat Stats")]
    public float attackPower = 20f;
    public float attackSpeed = 1.0f;

    [Header("Equipment")]
    public Transform equipmentRoot;

    [Header("Visuals")]
    [SerializeField] private ResourceStack visualStack;

    [Header("Inventory Settings")]
    [SerializeField] private int extraCapacity = 0;
    private Dictionary<string, int> _inventory = new Dictionary<string, int>();

    [Header("UI Feedback")]
    [SerializeField] private TMP_Text fullCapacityText;
    [SerializeField] private float fadeDuration = 1.0f;

    private ResourcePlatform _currentHarvestTarget;
    private float _harvestTimer;
    private Coroutine _fadeCoroutine;
    private Vector3 _originalMaxTextPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (fullCapacityText != null)
        {
            _originalMaxTextPos = fullCapacityText.transform.localPosition;
            fullCapacityText.gameObject.SetActive(false);
        }
    }

    public bool HasEquipment() => equipmentRoot != null && equipmentRoot.childCount > 0;

    public bool RequestHarvestPermission(ResourcePlatform platform)
    {
        if (HasEquipment()) return true;
        if (_currentHarvestTarget == null)
        {
            _currentHarvestTarget = platform;
            _harvestTimer = 0;
            return true;
        }
        return _currentHarvestTarget == platform;
    }

    public void ReleaseHarvestPermission(ResourcePlatform platform)
    {
        if (_currentHarvestTarget == platform)
        {
            _currentHarvestTarget = null;
            _harvestTimer = 0;
        }
    }

    public bool CanHarvest()
    {
        _harvestTimer += Time.deltaTime;
        if (_harvestTimer >= attackSpeed)
        {
            _harvestTimer = 0;
            return true;
        }
        return false;
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
    }

    public void AddResource(string resourceName, int amount, Vector3 startWorldPos = default)
    {
        int current = GetResourceCount(resourceName);
        int limit = GetResourceLimit(resourceName);

        if (current >= limit)
        {
            ShowMaxCapacityFeedback();
            return;
        }

        int canAdd = Mathf.Clamp(amount, 0, limit - current);
        if (canAdd <= 0) 
        {
            ShowMaxCapacityFeedback();
            return;
        }

        if (_inventory.ContainsKey(resourceName)) _inventory[resourceName] += canAdd;
        else _inventory.Add(resourceName, canAdd);

        if (visualStack != null)
        {
            for (int i = 0; i < canAdd; i++)
            {
                Vector3 spawnPos = (startWorldPos == default) ? transform.position : startWorldPos;
                visualStack.AddWithAnimation(resourceName, spawnPos);
            }
        }
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
            if (visualStack != null)
            {
                for (int i = 0; i < amount; i++)
                    visualStack.Remove(resourceName);
            }
        }
    }

    public void ShowMaxCapacityFeedback()
    {
        if (fullCapacityText == null) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutTextRoutine());
    }

    private IEnumerator FadeOutTextRoutine()
    {
        fullCapacityText.gameObject.SetActive(true);
        Color color = fullCapacityText.color;
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fullCapacityText.color = new Color(color.r, color.g, color.b, 1.0f - t);
            fullCapacityText.transform.localPosition = _originalMaxTextPos + new Vector3(0, t * 1.0f, 0);
            yield return null;
        }
        fullCapacityText.gameObject.SetActive(false);
        fullCapacityText.transform.localPosition = _originalMaxTextPos;
    }

    public void UpgradeAttackPower(float amount) => attackPower += amount;
    public void UpgradeAttackSpeed(float amount)
    {
        attackSpeed += amount;
        attackSpeed = Mathf.Max(0.1f, attackSpeed);
    }
}

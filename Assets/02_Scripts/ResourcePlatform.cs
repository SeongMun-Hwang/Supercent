using UnityEngine;
using System.Collections;

public class ResourcePlatform : MonoBehaviour, IPlatformAction
{
    [Header("Harvest Settings")]
    [SerializeField] private string resourceName = "Iron";
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int harvestAmount = 10;
    [SerializeField] private float harvestInterval = 1.0f;
    
    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5f;

    [Header("Visual Stacking")]
    [SerializeField] private ResourceStack visualStack;

    private float _currentHealth;
    private bool _isHarvested = false;
    private float _timer;
    private Coroutine _respawnCoroutine;
    private MeshRenderer[] _renderers;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        InitializeVisuals();
    }

    private void InitializeVisuals()
    {
        if (visualStack != null)
        {
            visualStack.Clear();
            // Show resources based on current health / damage per hit
            // For simplicity, let's show a fixed number or based on health
            int count = Mathf.Max(1, Mathf.RoundToInt(maxHealth / 20f)); 
            for (int i = 0; i < count; i++) visualStack.Add(resourceName);
        }
    }

    public void OnPlayerEnter(GameObject player)
    {
        if (_isHarvested) return;
        _timer = 0;
        Debug.Log($"[Resource] Started harvesting {resourceName}...");
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isHarvested) return;

        _timer += Time.deltaTime;
        
        if (_timer >= harvestInterval)
        {
            _timer = 0;
            
            float damage = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackPower : 10f;
            if (damage <= 0) damage = 10f;

            _currentHealth -= damage;
            
            // Visual feedback: remove a block if health crosses a threshold
            // if (visualStack != null) visualStack.Remove(); 

            Debug.Log($"[Resource] {resourceName} Health: {_currentHealth}");

            if (_currentHealth <= 0)
            {
                CompleteHarvest();
            }
        }
    }

    public void OnPlayerExit(GameObject player)
    {
        _timer = 0;
    }

    private void CompleteHarvest()
    {
        _isHarvested = true;
        _currentHealth = 0;

        SetVisualsActive(false);
        if (visualStack != null) visualStack.Clear();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddResource(resourceName, harvestAmount);
        }

        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        _isHarvested = false;
        _currentHealth = maxHealth;

        SetVisualsActive(true);
        InitializeVisuals();
        
        Debug.Log($"[Resource] {resourceName} has respawned!");
    }

    private void SetVisualsActive(bool isActive)
    {
        if (_renderers == null) return;
        foreach (var renderer in _renderers)
        {
            renderer.enabled = isActive;
        }
    }
}

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

    [Header("Animations")]
    [SerializeField] private Animator animator; 

    private float _currentHealth;
    private bool _isHarvested = false;
    private bool _isHarvestingCleanup = false; 
    private bool _isFirstHit = true; 
    private float _timer;
    private Coroutine _respawnCoroutine;
    private MeshRenderer[] _renderers;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _renderers = GetComponentsInChildren<MeshRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
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
            int count = Mathf.Max(1, Mathf.RoundToInt(maxHealth / 20f));
            for (int i = 0; i < count; i++) visualStack.Add(resourceName);
        }
    }

    public void OnPlayerEnter(GameObject player)
    {
        if (_isHarvested || _isHarvestingCleanup) return;
        _isFirstHit = true;
        _timer = 0;
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isHarvested || _isHarvestingCleanup || PlayerStats.Instance == null) return;
        if (!PlayerStats.Instance.RequestHarvestPermission(this)) return;

        if (_isFirstHit)
        {
            DealDamage(true); // 플레이어가 주는 대미지
            _isFirstHit = false;
            _timer = 0;
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= PlayerStats.Instance.attackSpeed)
        {
            _timer = 0;
            DealDamage(true);
        }
    }

    public void OnPlayerExit(GameObject player)
    {
        if (!_isHarvestingCleanup && PlayerStats.Instance != null)
            PlayerStats.Instance.ReleaseHarvestPermission(this);
        _timer = 0;
    }

    // Miner 등이 외부에서 호출할 수 있는 대미지 메서드
    public int TakeExternalDamage(float damage)
    {
        if (_isHarvested || _isHarvestingCleanup) return 0;

        _currentHealth -= damage;
        if (animator != null) animator.SetTrigger("Collect");

        if (_currentHealth <= 0)
        {
            int amount = harvestAmount;
            CompleteHarvest(false); // 플레이어에게 직접 주지 않음
            return amount;
        }
        return 0;
    }

    private void DealDamage(bool giveToPlayer)
    {
        if (_isHarvested || _isHarvestingCleanup) return;

        float damage = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackPower : 10f;
        _currentHealth -= damage;

        if (animator != null) animator.SetTrigger("Collect");

        if (giveToPlayer && PlayerStats.Instance != null && !PlayerStats.Instance.HasEquipment())
        {
            Animator playerAnim = PlayerStats.Instance.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("Attack");
        }

        if (_currentHealth <= 0)
        {
            CompleteHarvest(giveToPlayer);
        }
    }

    private void CompleteHarvest(bool giveToPlayer)
    {
        _isHarvested = true;
        _isHarvestingCleanup = true;
        _currentHealth = 0;
        GetComponent<Collider>().enabled = false;
        if (giveToPlayer && PlayerStats.Instance != null)
            PlayerStats.Instance.ReleaseHarvestPermission(this);

        StartCoroutine(HarvestCleanupRoutine(giveToPlayer));
    }

    private IEnumerator HarvestCleanupRoutine(bool giveToPlayer)
    {
        yield return new WaitForSeconds(0.2f);
        SetVisualsActive(false);
        if (visualStack != null) visualStack.Clear();

        if (giveToPlayer && PlayerStats.Instance != null)
            PlayerStats.Instance.AddResource(resourceName, harvestAmount);

        _isHarvestingCleanup = false;
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
    }

    private void SetVisualsActive(bool isActive)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers) r.enabled = isActive;
        GetComponent<Collider>().enabled = true;
    }

    public string GetResourceName() => resourceName;

    public bool IsHarvested()
    {
        return _isHarvested || _isHarvestingCleanup;
    }
}

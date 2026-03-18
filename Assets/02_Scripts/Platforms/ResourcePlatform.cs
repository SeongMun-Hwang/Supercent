using UnityEngine;
using System.Collections;

public class ResourcePlatform : MonoBehaviour, IPlatformAction
{
    [Header("Harvest Settings")]
    [SerializeField] private string resourceName = "Iron";
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int harvestAmount = 10;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 5f;

    [Header("Visual Stacking")]
    [SerializeField] private ResourceStack visualStack;

    [Header("Animations")]
    [SerializeField] private Animator animator;

    private float _currentHealth;
    private bool _isHarvested;
    private bool _isHarvestingCleanup;
    private bool _isCurrentlyHarvestingByPlayer = false; // 추가

    private Coroutine _respawnCoroutine;
    private MeshRenderer[] _renderers;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _renderers = GetComponentsInChildren<MeshRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        InitializeVisuals();
    }

    private void InitializeVisuals()
    {
        if (visualStack != null)
        {
            visualStack.Clear();
            int count = Mathf.Max(1, Mathf.RoundToInt(maxHealth / 20f));
            for (int i = 0; i < count; i++)
                visualStack.Add(resourceName);
        }
    }

    public void OnPlayerEnter(GameObject player)
    {
        if (_isHarvested || _isHarvestingCleanup) return;
        if (PlayerStats.Instance == null) return;

        if (PlayerStats.Instance.HasEquipment())
        {
            if (PlayerStats.Instance.RequestHarvestPermission(this))
            {
                NotifyHarvestStart();
                DealDamage(true);
            }
        }
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isHarvested || _isHarvestingCleanup) return;
        if (PlayerStats.Instance == null) return;

        if (!PlayerStats.Instance.RequestHarvestPermission(this))
            return;

        NotifyHarvestStart();

        if (PlayerStats.Instance.CanHarvest())
        {
            DealDamage(true);
        }
    }

    public void OnPlayerExit(GameObject player)
    {
        NotifyHarvestEnd();
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ReleaseHarvestPermission(this);
    }

    private void NotifyHarvestStart()
    {
        if (!_isCurrentlyHarvestingByPlayer && PlayerStats.Instance != null)
        {
            _isCurrentlyHarvestingByPlayer = true;
        }
    }

    private void NotifyHarvestEnd()
    {
        if (_isCurrentlyHarvestingByPlayer && PlayerStats.Instance != null)
        {
            _isCurrentlyHarvestingByPlayer = false;
        }
    }

    public int TakeExternalDamage(float damage)
    {
        if (_isHarvested || _isHarvestingCleanup) return 0;

        _currentHealth -= damage;
        if (animator != null) animator.SetTrigger("Collect");

        if (_currentHealth <= 0)
        {
            int amount = harvestAmount;
            CompleteHarvest(false);
            return amount;
        }
        return 0;
    }

    private void DealDamage(bool giveToPlayer)
    {
        if (_isHarvested || _isHarvestingCleanup) return;

        float damage = PlayerStats.Instance.attackPower;
        _currentHealth -= damage;

        if (animator != null) animator.SetTrigger("Collect");

        if (giveToPlayer && !PlayerStats.Instance.HasEquipment())
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

        if (giveToPlayer)
        {
            NotifyHarvestEnd();
            PlayerStats.Instance.ReleaseHarvestPermission(this);
        }

        StartCoroutine(HarvestCleanupRoutine(giveToPlayer));
    }

    private IEnumerator HarvestCleanupRoutine(bool giveToPlayer)
    {
        yield return new WaitForSeconds(0.2f);

        SetVisualsActive(false);
        if (visualStack != null) visualStack.Clear();

        if (giveToPlayer && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddResource(resourceName, harvestAmount, transform.position);
        }

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
    public bool IsHarvested() => _isHarvested || _isHarvestingCleanup;
}

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
    [SerializeField] private Animator animator; // 자원 애니메이터

    private float _currentHealth;
    private bool _isHarvested = false;
    private bool _isHarvestingCleanup = false; // [추가] 정리 중인지 여부
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

        // 플레이어에게 채집 권한 요청 (장비 없을 땐 하나만, 있을 땐 모두)
        if (!PlayerStats.Instance.RequestHarvestPermission(this)) return;

        if (_isFirstHit)
        {
            DealDamage();
            _isFirstHit = false;
            _timer = 0;
            return;
        }

        _timer += Time.deltaTime;
        float currentInterval = PlayerStats.Instance.attackSpeed;

        if (_timer >= currentInterval)
        {
            _timer = 0;
            DealDamage();
        }
    }

    private void DealDamage()
    {
        if (_isHarvested || _isHarvestingCleanup) return;

        float damage = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackPower : 10f;
        if (damage <= 0) damage = 10f;

        _currentHealth -= damage;

        // 자원 애니메이션 실행 (항상)
        if (animator != null) animator.SetTrigger("Collect");

        // 플레이어 공격 애니메이션 실행 (장비가 없을 때만)
        if (PlayerStats.Instance != null && !PlayerStats.Instance.HasEquipment())
        {
            Animator playerAnim = PlayerStats.Instance.GetComponentInChildren<Animator>();
            if (playerAnim != null) playerAnim.SetTrigger("Attack");
        }

        Debug.Log($"[Resource] {resourceName} Damaged. HP: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            CompleteHarvest();
        }
    }

    public void OnPlayerExit(GameObject player)
    {
        // 채집이 완료되어 정리 중이라면 코루틴이 끝날 때까지 권한을 유지함
        if (!_isHarvestingCleanup && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ReleaseHarvestPermission(this);
        }
        _timer = 0;
    }

    private void CompleteHarvest()
    {
        _isHarvested = true;
        _isHarvestingCleanup = true;
        _currentHealth = 0;

        // 애니메이션 재생 후 정리를 위한 코루틴 시작
        StartCoroutine(HarvestCleanupRoutine());
    }

    private IEnumerator HarvestCleanupRoutine()
    {
        // 애니메이션이 눈에 보일 시간을 줍니다
        yield return new WaitForSeconds(0.2f);

        SetVisualsActive(false);
        if (visualStack != null) visualStack.Clear();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddResource(resourceName, harvestAmount);
            // 시각적 정리가 끝난 이 시점에 채집 권한을 해제
            PlayerStats.Instance.ReleaseHarvestPermission(this);
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

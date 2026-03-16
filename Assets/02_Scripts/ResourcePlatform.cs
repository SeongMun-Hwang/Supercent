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
    private float _timer;
    private Coroutine _respawnCoroutine;
    private MeshRenderer[] _renderers;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _renderers = GetComponentsInChildren<MeshRenderer>();

        // 애니메이터가 할당되지 않았다면 자식 오브젝트에서 찾기
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
        if (_isHarvested) return;
        _timer = 0;

        if (animator != null)
            animator.SetTrigger("Collect");
        Debug.Log($"[Resource] Started harvesting {resourceName}...");
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isHarvested) return;

        _timer += Time.deltaTime;

        // 플레이어의 공격 속도(간격)를 가져옵니다. 없을 경우 기본값(harvestInterval) 사용.
        float currentInterval = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackSpeed : harvestInterval;

        if (_timer >= currentInterval)
        {
            _timer = 0;

            float damage = (PlayerStats.Instance != null) ? PlayerStats.Instance.attackPower : 10f;
            if (damage <= 0) damage = 10f;

            _currentHealth -= damage;

            // 대미지 입을 때 애니메이션 실행
            if (animator != null)
            {
                animator.SetTrigger("Collect");
            }

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
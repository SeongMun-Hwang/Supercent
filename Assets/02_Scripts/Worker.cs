using UnityEngine;
using System.Collections;

public class Worker : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float reachThreshold = 0.1f;

    [Header("Work Settings")]
    [SerializeField] private float loadDelay = 0.5f;   // 자원을 집는 시간
    [SerializeField] private float unloadDelay = 0.5f; // 자원을 내려놓는 시간

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform carryRoot; // 손에 든 자원이 보일 위치

    private ResourceStack _sourceStack;
    private ResourceStack _targetStack;
    private Vector3 _posA;
    private Vector3 _posB;
    private Vector3 _spawnPos; // 소환된 초기 위치
    private Vector3 _currentTargetPos;

    private bool _hasResource = false;
    private bool _isWorking = false;
    private bool _isInitialized = false;
    private string _carriedResourceName; // 현재 들고 있는 자원의 이름
    private GameObject _carriedVisual;

    public void Initialize(ResourceStack source, ResourceStack target, Vector3 posA, Vector3 posB)
    {
        _sourceStack = source;
        _targetStack = target;
        _posA = posA;
        _posB = posB;
        _spawnPos = transform.position; // 소환 시점의 위치 저장

        // 스폰 위치는 유지하고 첫 목표만 B로 설정
        _currentTargetPos = _posB;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized || _isWorking) return;

        Move();
    }

    private void Move()
    {
        // 1. 목표 지점 결정
        Vector3 target;
        
        if (_hasResource)
        {
            // 자원을 들고 있으면 무조건 Target(B)으로 이동
            target = _posB;
        }
        else
        {
            // 자원이 없으면 Source(A)에 자원이 있는지 확인
            if (_sourceStack != null && _sourceStack.GetTotalCount() > 0)
            {
                // 자원이 있으면 가지러 가기 (A로 이동)
                target = _posA;
            }
            else
            {
                // [수정] 자원이 없으면 소환된 위치(_spawnPos)에서 대기
                target = _spawnPos;
                
                // 대기 지점 도착 시 애니메이션 정지
                if (Vector3.Distance(transform.position, _spawnPos) < reachThreshold)
                {
                    if (animator != null) animator.SetFloat("moveSpeed", 0f);
                    RotateTowards(_posA); // 다음 작업을 위해 자원 생성 위치를 주시
                    return;
                }
            }
        }
        
        // 2. 실제 이동 처리
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        RotateTowards(target);

        if (animator != null) animator.SetFloat("moveSpeed", 1f);

        // 3. 도착 시 작업 수행
        if (Vector3.Distance(transform.position, target) < reachThreshold)
        {
            if (_hasResource && target == _posB) StartCoroutine(UnloadRoutine());
            else if (!_hasResource && target == _posA) StartCoroutine(LoadRoutine());
        }
    }

    private IEnumerator LoadRoutine()
    {
        _isWorking = true;
        if (animator != null) animator.SetFloat("moveSpeed", 0f);

        yield return new WaitForSeconds(loadDelay);

        // [수정] 스택에서 실제로 어떤 자원이든 하나를 가져옴
        if (_sourceStack != null)
        {
            _carriedResourceName = _sourceStack.PopResourceName();
            if (!string.IsNullOrEmpty(_carriedResourceName))
            {
                _hasResource = true;
                CreateCarriedVisual();
            }
        }

        _isWorking = false;
    }

    private IEnumerator UnloadRoutine()
    {
        _isWorking = true;
        if (animator != null) animator.SetFloat("moveSpeed", 0f);

        yield return new WaitForSeconds(unloadDelay);

        if (_targetStack != null && !string.IsNullOrEmpty(_carriedResourceName))
        {
            // [수정] 스택에 할당된 자원 이름 확인
            string assignedName = _targetStack.GetAssignedResourceName();
            
            // 만약 스택에 할당된 이름이 있고, 내가 들고 있는 자원과 같다면 플랫폼의 수령 로직 실행
            ResourceSubmissionPlatform submissionPlatform = _targetStack.GetComponentInParent<ResourceSubmissionPlatform>();

            Debug.Log($"[Worker] platform: {submissionPlatform != null}");
            Debug.Log($"[Worker] assignedName: {assignedName}");
            Debug.Log($"[Worker] carried: {_carriedResourceName}");

            if (submissionPlatform != null && !string.IsNullOrEmpty(assignedName) &&
                assignedName.Trim() == _carriedResourceName.Trim())
            {
                submissionPlatform.AddHeldAmountDirectly(_carriedResourceName, 1, carryRoot.transform.position);
            }
            else
            {
                // 일반 스택이거나 이름이 매칭되지 않을 경우 기본 동작
                _targetStack.AddWithAnimation(_carriedResourceName, carryRoot.transform.position);
            }
        }

        _hasResource = false;
        _carriedResourceName = null;
        if (_carriedVisual != null) Destroy(_carriedVisual);

        _isWorking = false;
    }

    private void CreateCarriedVisual()
    {
        if (ResourceDatabase.Instance == null || string.IsNullOrEmpty(_carriedResourceName)) return;
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(_carriedResourceName);
        if (prefab != null && carryRoot != null)
        {
            _carriedVisual = Instantiate(prefab, carryRoot);
            _carriedVisual.transform.localPosition = Vector3.zero;
        }
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }
}

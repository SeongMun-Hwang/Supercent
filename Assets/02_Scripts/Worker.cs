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
    [SerializeField] private string resourceName = "Iron"; // 옮길 자원 이름

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform carryRoot; // 손에 든 자원이 보일 위치

    private ResourceStack _sourceStack;
    private ResourceStack _targetStack;
    private Vector3 _posA;
    private Vector3 _posB;
    private Vector3 _currentTargetPos;

    private bool _hasResource = false;
    private bool _isWorking = false;
    private bool _isInitialized = false;
    private GameObject _carriedVisual;

    public void Initialize(ResourceStack source, ResourceStack target, Vector3 posA, Vector3 posB)
    {
        _sourceStack = source;
        _targetStack = target;
        _posA = posA;
        _posB = posB;

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
                // [수정] 자원이 없으면 Target(B)에서 대기
                target = _posB;
                
                // 대기 중일 때는 이동 애니메이션 정지 및 A를 바라보게 설정
                if (Vector3.Distance(transform.position, _posB) < reachThreshold)
                {
                    if (animator != null) animator.SetFloat("moveSpeed", 0f);
                    RotateTowards(_posA); // 다음 작업을 위해 A를 주시
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

        // Source에 자원이 생길 때까지 대기 (선택 사항: 없으면 그냥 계속 대기)
        // 여기서는 간단하게 딜레이 후 시도
        yield return new WaitForSeconds(loadDelay);

        // 실제 스택에서 자원 제거 시도 (시각적 로직은 생략하거나 추가 가능)
        // 원본 Stack에 데이터 기반 제거 로직이 필요할 수 있음
        // 지금은 무조건 생성하는 방식으로 예시 구현
        _hasResource = true;
        CreateCarriedVisual();

        _isWorking = false;
    }

    private IEnumerator UnloadRoutine()
    {
        _isWorking = true;
        if (animator != null) animator.SetFloat("moveSpeed", 0f);

        yield return new WaitForSeconds(unloadDelay);

        if (_targetStack != null)
        {
            _targetStack.Add(resourceName);
        }

        _hasResource = false;
        if (_carriedVisual != null) Destroy(_carriedVisual);

        _isWorking = false;
    }

    private void CreateCarriedVisual()
    {
        if (ResourceDatabase.Instance == null) return;
        GameObject prefab = ResourceDatabase.Instance.GetPrefab(resourceName);
        if (prefab != null && carryRoot != null)
        {
            _carriedVisual = Instantiate(prefab, carryRoot);
            _carriedVisual.transform.localPosition = Vector3.zero;
            _carriedVisual.transform.localRotation = Quaternion.identity;
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

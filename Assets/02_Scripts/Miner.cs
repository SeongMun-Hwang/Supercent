using UnityEngine;

public class Miner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float reachThreshold = 0.1f;

    [Header("Mining")]
    [SerializeField] private float attackPower = 20f;
    [SerializeField] private float attackInterval = 1.0f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;

    private Vector3 _spawnPos;
    private Vector3 _destPos;
    private Vector3 _currentTargetPos;
    
    private ResourceSubmissionPlatform _targetSubmission;
    private EquipmentUpgradePlatform _targetUpgrade; 
    private ResourcePlatform _miningTarget;
    
    private float _timer;
    private bool _isMining = false;
    private bool _isInitialized = false;

    public void Initialize(Vector3 spawnPos, Vector3 destPos, ResourceSubmissionPlatform submission, EquipmentUpgradePlatform upgrade)
    {
        _spawnPos = spawnPos;
        _destPos = destPos;
        _currentTargetPos = _destPos; 
        
        _targetSubmission = submission;
        _targetUpgrade = upgrade;
        
        transform.position = spawnPos;
        RotateTowards(_currentTargetPos);
        
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        if (_isMining)
        {
            UpdateMining();
        }
        else
        {
            Move();
        }
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, _currentTargetPos, moveSpeed * Time.deltaTime);
        
        if (animator != null) animator.SetFloat("moveSpeed", 1f);

        if (Vector3.Distance(transform.position, _currentTargetPos) < reachThreshold)
        {
            _currentTargetPos = (_currentTargetPos == _destPos) ? _spawnPos : _destPos;
            RotateTowards(_currentTargetPos);
        }
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isMining) return;

        ResourcePlatform platform = other.GetComponent<ResourcePlatform>();
        if (platform != null && !platform.IsHarvested())
        {
            _miningTarget = platform;
            _isMining = true;
            _timer = 0;
            if (animator != null) animator.SetFloat("moveSpeed", 0f);
        }
    }

    private void UpdateMining()
    {
        if (_miningTarget == null || _miningTarget.IsHarvested())
        {
            _isMining = false;
            _miningTarget = null;
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= attackInterval)
        {
            _timer = 0;
            
            if (animator != null) animator.SetTrigger("Attack");

            int gained = _miningTarget.TakeExternalDamage(attackPower);
            if (gained > 0)
            {
                if (_targetSubmission != null) 
                    _targetSubmission.AddHeldAmountDirectly(_miningTarget.GetResourceName(), gained, transform.position);
                else if (_targetUpgrade != null) 
                    _targetUpgrade.AddHeldAmountDirectly(_miningTarget.GetResourceName(), gained, transform.position);
                
                _miningTarget = null;
                _isMining = false;
            }
        }
    }
}

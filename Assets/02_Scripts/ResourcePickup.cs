using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    [SerializeField] private float collectInterval = 0.1f; // 수령 간격
    private float _timer;
    private ResourceSubmissionPlatform _parentPlatform;
    private bool _isPlayerInside;

    private void Awake()
    {
        _parentPlatform = GetComponentInParent<ResourceSubmissionPlatform>();
        
        // 보장: 트리거 설정
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        Debug.Log($"[Pickup] {gameObject.name} initialized on {transform.parent.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[Pickup] Player Detected: {other.name}");
            _isPlayerInside = true;
            _timer = 0;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Tag 체크를 Stay에서도 수행 (안정성)
        if (other.CompareTag("Player") && _parentPlatform != null)
        {
            _isPlayerInside = true;
            _timer += Time.deltaTime;
            
            if (_timer >= collectInterval)
            {
                _timer = 0;
                _parentPlatform.TryCollectOneResource();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[Pickup] Player Exited");
            _isPlayerInside = false;
        }
    }
}

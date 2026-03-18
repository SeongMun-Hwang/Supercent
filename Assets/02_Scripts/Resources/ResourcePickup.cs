using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    [SerializeField] private float collectInterval = 0.1f; // 수령 간격
    [SerializeField] ResourceSubmissionPlatform resourceSubmissionPlatform;
    private float _timer;
    private bool _isPlayerInside;

    private void Awake()
    {        
        // 보장: 트리거 설정
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        Debug.Log($"[Pickup] {gameObject.name} initialized on {transform.parent.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[ResourcePickup] Player ENTERED: {other.name}");
            _isPlayerInside = true;
            _timer = 0;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && resourceSubmissionPlatform != null)
        {
            _isPlayerInside = true;
            _timer += Time.deltaTime;
            
            if (_timer >= collectInterval)
            {
                _timer = 0;
                Debug.Log($"[ResourcePickup] Attempting to collect 1 unit from {resourceSubmissionPlatform.gameObject.name}...");
                resourceSubmissionPlatform.TryCollectOneResource();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[ResourcePickup] Player EXITED: {other.name}");
            _isPlayerInside = false;
        }
    }
}

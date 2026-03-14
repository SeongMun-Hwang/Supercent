using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlatformBase : MonoBehaviour
{
    private IPlatformAction _action;

    private void Awake()
    {
        _action = GetComponent<IPlatformAction>();
        if (_action == null) Debug.LogError($"[Platform] IPlatformAction implementation missing on {gameObject.name}");
        
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        
        Debug.Log($"[Platform] {gameObject.name} initialized. Trigger: {col.isTrigger}");
    }

    private void Start()
    {
        // 다시 한 번 확인
        Debug.Log($"[Platform] {gameObject.name} is ready on Layer: {LayerMask.LayerToName(gameObject.layer)}");
    }

    private void OnTriggerEnter(Collider other)
    {
        // 어떤 로그도 안나온다면 엔진에서 충돌을 감지 못한 것
        Debug.Log($"[Platform] {gameObject.name} Trigger Enter with: {other.name} | Tag: {other.tag}");
        
        if (other.CompareTag("Player") && _action != null)
        {
            _action.OnPlayerEnter(other.gameObject);
        }
    }
    // ... (OnTriggerStay, OnTriggerExit 생략 - 기존 유지)

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && _action != null)
        {
            _action.OnPlayerStay(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[Platform] {gameObject.name} Trigger Exit: {other.name}");
        if (other.CompareTag("Player") && _action != null)
        {
            _action.OnPlayerExit(other.gameObject);
        }
    }
}

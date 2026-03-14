using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ResourceSubmissionPlatform : MonoBehaviour, IPlatformAction
{
    [Header("Submission Settings")]
    [SerializeField] private string resourceName = "Iron";
    [SerializeField] private int targetAmount = 100;
    [SerializeField] private bool isRepeatable = true;
    
    [Header("Speed Settings")]
    [SerializeField] private float playerToPlatformInterval = 0.05f; 
    [SerializeField] private float platformToTargetInterval = 0.1f;
    [SerializeField] private int transferBatchSize = 1;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text heldAmountText;      
    [SerializeField] private TMP_Text remainingAmountText; 

    [Header("Current Progress")]
    [SerializeField] private int currentAmount = 0; 
    [SerializeField] private int heldAmount = 0;    

    [Header("Events")]
    public UnityEvent OnTargetReached;

    private float _transferTimer;
    private float _processTimer;
    private bool _isCompleted = false;
    private bool _isPlayerOnPlatform = false; // 플레이어 접촉 상태 확인용

    private void Start()
    {
        UpdateProgressTexts();
    }

    private void Update()
    {
        // 플레이어가 자원을 옮길 수 있는 상태인지 체크
        bool isTransferring = _isPlayerOnPlatform && PlayerStats.Instance != null && PlayerStats.Instance.GetResourceCount(resourceName) > 0;
        
        // 반복 불가 미션인데 이미 목표치만큼 가져왔다면 전송 중이 아닌 것으로 간주
        if (!isRepeatable && (currentAmount + heldAmount) >= targetAmount) isTransferring = false;

        // "자원이 이동 중일 때"가 아닐 때만 최종 제출(Process) 진행
        if (!_isCompleted && heldAmount > 0 && !isTransferring)
        {
            _processTimer += Time.deltaTime;
            if (_processTimer >= platformToTargetInterval)
            {
                _processTimer = 0;
                TryProcessToTarget();
            }
        }
    }

    public void OnPlayerEnter(GameObject player)
    {
        if (_isCompleted && !isRepeatable) return;
        _isPlayerOnPlatform = true;
        _transferTimer = 0;
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isCompleted && !isRepeatable) return;

        // 플레이어 -> 플랫폼 저장소 (Transfer)
        _transferTimer += Time.deltaTime;
        if (_transferTimer >= playerToPlatformInterval)
        {
            _transferTimer = 0;
            TryTransferFromPlayer();
        }
    }

    public void OnPlayerExit(GameObject player) 
    {
        _isPlayerOnPlatform = false;
    }

    private void TryTransferFromPlayer()
    {
        if (PlayerStats.Instance == null) return;

        int maxCanTake = targetAmount - (currentAmount + heldAmount);
        if (!isRepeatable && maxCanTake <= 0) return;

        int playerHas = PlayerStats.Instance.GetResourceCount(resourceName);
        if (playerHas > 0)
        {
            int amountToTake = Mathf.Min(playerHas, transferBatchSize);
            if (!isRepeatable) amountToTake = Mathf.Min(amountToTake, maxCanTake);

            PlayerStats.Instance.SpendResource(resourceName, amountToTake);
            heldAmount += amountToTake;
            
            UpdateProgressTexts();
        }
    }

    private void TryProcessToTarget()
    {
        if (heldAmount > 0)
        {
            heldAmount--;
            currentAmount++;
            
            UpdateProgressTexts();

            if (currentAmount >= targetAmount)
            {
                CompleteSubmission();
            }
        }
    }

    private void CompleteSubmission()
    {
        Debug.Log($"[Submission] {gameObject.name} Target Reached!");
        OnTargetReached?.Invoke();

        if (isRepeatable)
        {
            currentAmount -= targetAmount;
            if (currentAmount < 0) currentAmount = 0;
        }
        else
        {
            _isCompleted = true;
            heldAmount = 0;
            currentAmount = targetAmount;
        }
        
        UpdateProgressTexts();
    }

    private void UpdateProgressTexts()
    {
        if (heldAmountText != null)
        {
            heldAmountText.text = heldAmount > 0 ? $"+{heldAmount}" : "";
        }

        if (remainingAmountText != null)
        {
            int remaining = targetAmount - currentAmount;
            if (remaining < 0) remaining = 0;

            if (_isCompleted && !isRepeatable)
                remainingAmountText.text = "OK";
            else
                remainingAmountText.text = $"{remaining}";
        }
    }

    public void ResetProgress()
    {
        currentAmount = 0;
        heldAmount = 0;
        _isCompleted = false;
        UpdateProgressTexts();
    }
}

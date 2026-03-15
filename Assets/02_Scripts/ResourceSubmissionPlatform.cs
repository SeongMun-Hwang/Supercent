using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class ResourceSubmissionPlatform : MonoBehaviour, IPlatformAction
{
    [Header("Submission Settings")]
    [SerializeField] private string resourceName = "Iron";
    [SerializeField] private int targetAmount = 100;
    [SerializeField] private bool isRepeatable = true;
    
    [Header("Converter Settings")]
    [SerializeField] private bool isConverter = false; // 변환기 모드 여부
    [SerializeField] private string outputResourceName = "Steel"; // 변환 결과물 이름
    [SerializeField] private GameObject outputArea; // 자원이 쌓일 자식 오브젝트
    [SerializeField] private TMP_Text outputAmountText; // 변환된 수량 표시 TMP

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
    [SerializeField] private int convertedAmount = 0; // 변환 완료되어 쌓인 양

    [Header("Events")]
    public UnityEvent OnTargetReached;

    private float _transferTimer;
    private float _processTimer;
    private bool _isCompleted = false;
    private bool _isPlayerOnPlatform = false;

    private void Awake()
    {
        // 변환기 모드가 아니면 출력 구역 비활성화
        if (outputArea != null)
        {
            outputArea.SetActive(isConverter);
        }
    }

    private void Start()
    {
        UpdateProgressTexts();
    }

    private void Update()
    {
        // 플레이어가 자원을 옮길 수 있는 상태인지 체크
        bool isTransferring = _isPlayerOnPlatform && PlayerStats.Instance != null && PlayerStats.Instance.GetResourceCount(resourceName) > 0;
        
        // 반복 불가 미션인데 이미 목표치만큼 가져왔다면 전송 중이 아닌 것으로 간주 (변환기는 제외)
        if (!isConverter && !isRepeatable && (currentAmount + heldAmount) >= targetAmount) isTransferring = false;

        // "자원이 이동 중일 때"가 아닐 때만 최종 제출/변환 진행
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
        if (!isConverter && !isRepeatable && maxCanTake <= 0) return;

        int playerHas = PlayerStats.Instance.GetResourceCount(resourceName);
        if (playerHas > 0)
        {
            int amountToTake = Mathf.Min(playerHas, transferBatchSize);
            if (!isConverter && !isRepeatable) amountToTake = Mathf.Min(amountToTake, maxCanTake);

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
            
            if (isConverter)
            {
                convertedAmount++; // 변환기로 작동 시 결과물 버퍼에 쌓음
            }
            else
            {
                currentAmount++; // 일반 제출기로 작동
                if (currentAmount >= targetAmount)
                {
                    CompleteSubmission();
                }
            }
            
            UpdateProgressTexts();
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

    public void UpdateProgressTexts()
    {
        if (heldAmountText != null)
        {
            heldAmountText.text = heldAmount > 0 ? $"+{heldAmount}" : "";
        }

        if (remainingAmountText != null)
        {
            if (isConverter)
            {
                remainingAmountText.text = "CONVERTING";
            }
            else
            {
                int remaining = targetAmount - currentAmount;
                if (remaining < 0) remaining = 0;

                if (_isCompleted && !isRepeatable)
                    remainingAmountText.text = "OK";
                else
                    remainingAmountText.text = $"{remaining}";
            }
        }

        if (outputAmountText != null)
        {
            outputAmountText.text = convertedAmount > 0 ? $"{convertedAmount}" : "EMPTY";
        }
    }

    public void CollectConvertedResources()
    {
        // 이 함수는 기존처럼 한꺼번에 수령할 때 사용하거나 제거 가능
        if (convertedAmount > 0 && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddResource(outputResourceName, convertedAmount);
            convertedAmount = 0;
            UpdateProgressTexts();
        }
    }

    public void TryCollectOneResource()
    {
        if (convertedAmount > 0 && PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddResource(outputResourceName, 1);
            convertedAmount--;
            UpdateProgressTexts();
        }
    }

    public void ResetProgress()
    {
        currentAmount = 0;
        heldAmount = 0;
        convertedAmount = 0;
        _isCompleted = false;
        UpdateProgressTexts();
    }
}

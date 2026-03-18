using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ResourceSubmissionPlatform : MonoBehaviour, IPlatformAction
{
    [Header("Submission Settings")]
    [SerializeField] private string resourceName = "Iron";
    [SerializeField] private int targetAmount = 100;
    [SerializeField] private bool isRepeatable = true;
    
    [Header("Converter Settings")]
    [SerializeField] private bool isConverter = false; 
    [SerializeField] private string outputResourceName = "Steel"; 
    [SerializeField] private GameObject outputArea; 
    [SerializeField] private TMP_Text outputAmountText; 

    [Header("Speed Settings")]
    [SerializeField] private float playerToPlatformInterval = 0.05f; 
    [SerializeField] private float platformToTargetInterval = 0.1f;
    [SerializeField] private int transferBatchSize = 1;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text heldAmountText;      
    [SerializeField] private TMP_Text remainingAmountText; 
    [SerializeField] private Image resourceIconImage;       
    [SerializeField] private Image outputResourceIconImage; 

    [Header("Visual Stacking")]
    [SerializeField] private ResourceStack heldStack;   
    [SerializeField] private ResourceStack outputStack; 

    [Header("Current Progress")]
    [SerializeField] private int currentAmount = 0; 
    [SerializeField] private int heldAmount = 0;    
    [SerializeField] private int convertedAmount = 0; 

    [Header("Events")]
    public UnityEvent OnTargetReached;

    private float _transferTimer;
    private float _processTimer;
    private bool _isCompleted = false;
    private bool _isPlayerOnPlatform = false;

    private void Awake()
    {
        if (outputArea != null) outputArea.SetActive(isConverter);
    }

    private void Start()
    {
        // [수정] 각 스택에 담당 자원 이름 할당
        if (heldStack != null) heldStack.SetAssignedResourceName(resourceName);
        if (outputStack != null) outputStack.SetAssignedResourceName(outputResourceName);

        UpdateResourceIcons();
        UpdateProgressTexts();
    }

    private void Update()
    {
        // 플레이어가 자원을 옮기고 있는 중인지 확인
        bool isPlayerTransferring = _isPlayerOnPlatform && PlayerStats.Instance != null && PlayerStats.Instance.GetResourceCount(resourceName) > 0;
        
        if (!isConverter && !isRepeatable && (currentAmount + heldAmount) >= targetAmount) isPlayerTransferring = false;

        // [수정] 플레이어가 전송 중이 아닐 때만 이미 수령된 자원(heldAmount)의 변환 프로세스를 진행함
        if (!_isCompleted && heldAmount > 0 && !isPlayerTransferring)
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
        Debug.Log($"[SubmissionPlatform] Player Entered: {player.name}");
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
        Debug.Log($"[SubmissionPlatform] Player Exited: {player.name}");
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
            
            // [수정] 직접 호출 메서드 사용으로 일관성 유지
            AddHeldAmountDirectly(resourceName, amountToTake, PlayerStats.Instance.transform.position);
        }
    }

    private void TryProcessToTarget()
    {
        if (heldAmount > 0)
        {
            heldAmount--;
            // [수정] 스택에 할당된 이름을 사용하여 제거
            if (heldStack != null) heldStack.RemoveOne();
            
            if (isConverter)
            {
                convertedAmount++; 
                // [수정] 변환 시 출력 스택에 할당된 이름을 사용하여 추가
                if (outputStack != null) outputStack.Add(outputResourceName);
            }
            else
            {
                currentAmount++;
                if (currentAmount >= targetAmount) CompleteSubmission();
            }
            UpdateProgressTexts();
        }
    }

    private void CompleteSubmission()
    {
        OnTargetReached?.Invoke();
        if (isRepeatable) currentAmount -= targetAmount;
        else 
        { 
            _isCompleted = true; 
            heldAmount = 0; 
            if (heldStack != null) heldStack.Clear(); 
            currentAmount = targetAmount; 
        }
        UpdateProgressTexts();
    }

    private void UpdateResourceIcons()
    {
        if (ResourceDatabase.Instance == null) return;
        if (resourceIconImage != null) resourceIconImage.sprite = ResourceDatabase.Instance.GetSprite(resourceName);
        if (outputResourceIconImage != null && isConverter) outputResourceIconImage.sprite = ResourceDatabase.Instance.GetSprite(outputResourceName);
    }

    public void UpdateProgressTexts()
    {
        if (heldAmountText != null) heldAmountText.text = heldAmount > 0 ? $"+{heldAmount}" : "";
        if (remainingAmountText != null)
        {
            if (isConverter) remainingAmountText.text = "CONVERTING";
            else remainingAmountText.text = (_isCompleted && !isRepeatable) ? "OK" : $"{Mathf.Max(0, targetAmount - currentAmount)}";
        }
        if (outputAmountText != null) outputAmountText.text = convertedAmount > 0 ? $"{convertedAmount}" : "EMPTY";
    }

    public void CollectConvertedResources()
    {
        if (convertedAmount <= 0 || PlayerStats.Instance == null) return;

        int current = PlayerStats.Instance.GetResourceCount(outputResourceName);
        int limit = PlayerStats.Instance.GetResourceLimit(outputResourceName);
        
        if (current >= limit)
        {
            PlayerStats.Instance.ShowMaxCapacityFeedback();
            return;
        }

        int canTake = Mathf.Min(convertedAmount, limit - current);
        
        // 시작 위치를 OutputArea의 위치로 설정
        Vector3 startPos = (outputArea != null) ? outputArea.transform.position : transform.position;
        PlayerStats.Instance.AddResource(outputResourceName, canTake, startPos);
        
        convertedAmount -= canTake;
        if (outputStack != null) for (int i = 0; i < canTake; i++) outputStack.Remove(outputResourceName);
        UpdateProgressTexts();
    }

    public void TryCollectOneResource()
    {
        if (convertedAmount <= 0 || PlayerStats.Instance == null) return;

        int current = PlayerStats.Instance.GetResourceCount(outputResourceName);
        int limit = PlayerStats.Instance.GetResourceLimit(outputResourceName);

        if (current >= limit)
        {
            PlayerStats.Instance.ShowMaxCapacityFeedback();
            return;
        }

        // 시작 위치를 OutputArea의 위치로 설정
        Vector3 startPos = (outputArea != null) ? outputArea.transform.position : transform.position;
        PlayerStats.Instance.AddResource(outputResourceName, 1, startPos);
        
        convertedAmount--;
        if (outputStack != null) outputStack.Remove(outputResourceName);
        UpdateProgressTexts();
    }

    public void ResetProgress()
    {
        currentAmount = 0; 
        heldAmount = 0; 
        convertedAmount = 0; 
        _isCompleted = false;
        if (heldStack != null) heldStack.Clear();
        if (outputStack != null) outputStack.Clear();
        UpdateProgressTexts();
    }

    public void AddHeldAmountDirectly(string rName, int amount, Vector3 startPos = default)
    {
        // [수정] 자원 이름 검사를 더 유연하게 처리
        if (string.IsNullOrEmpty(rName) || rName.Trim().ToLower() != resourceName.Trim().ToLower()) 
        {
            Debug.LogWarning($"[Submission] Rejected: {rName}. Expected: {resourceName}");
            return;
        }

        heldAmount += amount;
        if (heldStack != null)
        {
            Vector3 sPos = (startPos == default) ? transform.position : startPos;
            for (int i = 0; i < amount; i++) 
            {
                // [수정] 스택에 할당된 이름을 사용하여 애니메이션 추가
                heldStack.AddWithAnimation(sPos);
            }
        }
        UpdateProgressTexts();
    }
}

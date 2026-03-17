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
        UpdateResourceIcons();
        UpdateProgressTexts();
    }

    private void Update()
    {
        bool isTransferring = _isPlayerOnPlatform && PlayerStats.Instance != null && PlayerStats.Instance.GetResourceCount(resourceName) > 0;
        if (!isConverter && !isRepeatable && (currentAmount + heldAmount) >= targetAmount) isTransferring = false;

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

    public void OnPlayerExit(GameObject player) => _isPlayerOnPlatform = false;

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
            if (heldStack != null) for (int i = 0; i < amountToTake; i++) heldStack.Add(resourceName);
            UpdateProgressTexts();
        }
    }

    private void TryProcessToTarget()
    {
        if (heldAmount > 0)
        {
            heldAmount--;
            if (heldStack != null) heldStack.Remove(resourceName);
            
            if (isConverter)
            {
                convertedAmount++; 
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
        else { _isCompleted = true; heldAmount = 0; if (heldStack != null) heldStack.Clear(); currentAmount = targetAmount; }
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
        PlayerStats.Instance.AddResource(outputResourceName, canTake);
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

        PlayerStats.Instance.AddResource(outputResourceName, 1);
        convertedAmount--;
        if (outputStack != null) outputStack.Remove(outputResourceName);
        UpdateProgressTexts();
    }

    public void ResetProgress()
    {
        currentAmount = 0; heldAmount = 0; convertedAmount = 0; _isCompleted = false;
        if (heldStack != null) heldStack.Clear();
        if (outputStack != null) outputStack.Clear();
        UpdateProgressTexts();
    }

    // Miner 전용: 플레이어를 거치지 않고 바로 플랫폼 저장소로 추가
    public void AddHeldAmountDirectly(string rName, int amount)
    {
        if (rName != resourceName) return; // 변환기/제출기 자원과 다르면 무시

        heldAmount += amount;
        if (heldStack != null)
        {
            for (int i = 0; i < amount; i++) heldStack.Add(rName);
        }
        UpdateProgressTexts();
    }
}

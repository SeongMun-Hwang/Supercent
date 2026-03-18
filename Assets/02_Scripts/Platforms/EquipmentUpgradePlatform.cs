using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using TMPro;

public class EquipmentUpgradePlatform : MonoBehaviour, IPlatformAction
{
    [Serializable]
    public struct UpgradeStep
    {
        public string resourceName;      
        public int targetAmount;         
        public GameObject equipmentPrefab; 

        [Header("Stat Upgrades (0 = Ignore)")]
        public float addAttackPower;     
        public int addMaxCapacity;       
        public float addAttackSpeed;     
    }

    [Header("Upgrade Configuration")]
    [SerializeField] private List<UpgradeStep> upgradeSteps;
    [SerializeField] private int currentStepIndex = 0;

    [Header("Speed Settings")]
    [SerializeField] private float playerToPlatformInterval = 0.05f;
    [SerializeField] private float platformToTargetInterval = 0.1f;
    [SerializeField] private int transferBatchSize = 1;

    [Header("UI Settings")]
    [SerializeField] private TMP_Text remainingAmountText; 
    [SerializeField] private Image progressFillImage; 
    [SerializeField] private Image resourceIconImage; 

    [Header("Events")]
    public UnityEvent OnStepCompleted; 

    [Header("Current Progress")]
    [SerializeField] private int currentStepAmount = 0; 
    [SerializeField] private int heldAmount = 0;        

    private float _transferTimer;
    private float _processTimer;
    private bool _isCompleted = false;
    private bool _isPlayerOnPlatform = false;

    private void Start()
    {
        UpdateResourceIcon();
        UpdateUI();
    }

    private void Update()
    {
        if (currentStepIndex >= upgradeSteps.Count) return;

        UpgradeStep currentStep = upgradeSteps[currentStepIndex];
        
        bool canTransfer = _isPlayerOnPlatform && PlayerStats.Instance != null && 
                             PlayerStats.Instance.GetResourceCount(currentStep.resourceName) > 0;
        
        if ((currentStepAmount + heldAmount) >= currentStep.targetAmount) canTransfer = false;

        if (!_isCompleted && heldAmount > 0)
        {
            if (!canTransfer)
            {
                InstantProcessToTarget();
            }
            else
            {
                _processTimer += Time.deltaTime;
                if (_processTimer >= platformToTargetInterval)
                {
                    _processTimer = 0;
                    ProcessTowardsStep();
                }
            }
        }
    }

    private void InstantProcessToTarget()
    {
        UpgradeStep currentStep = upgradeSteps[currentStepIndex];
        int needed = currentStep.targetAmount - currentStepAmount;
        int amountToProcess = Mathf.Min(heldAmount, needed);

        heldAmount -= amountToProcess;
        currentStepAmount += amountToProcess;

        if (currentStepAmount >= currentStep.targetAmount)
        {
            CompleteStep(currentStep);
        }
        else
        {
            UpdateUI();
        }
    }

    public void OnPlayerEnter(GameObject player)
    {
        if (_isCompleted) return;
        _isPlayerOnPlatform = true;
        _transferTimer = 0;
    }

    public void OnPlayerStay(GameObject player)
    {
        if (_isCompleted || currentStepIndex >= upgradeSteps.Count) return;

        _transferTimer += Time.deltaTime;
        if (_transferTimer >= playerToPlatformInterval)
        {
            _transferTimer = 0;
            TransferFromPlayer();
        }
    }

    public void OnPlayerExit(GameObject player) 
    {
        _isPlayerOnPlatform = false;
    }

    private void TransferFromPlayer()
    {
        if (PlayerStats.Instance == null) return;
        UpgradeStep currentStep = upgradeSteps[currentStepIndex];

        int maxCanTake = currentStep.targetAmount - (currentStepAmount + heldAmount);
        if (maxCanTake <= 0) return;

        int playerHas = PlayerStats.Instance.GetResourceCount(currentStep.resourceName);
        if (playerHas > 0)
        {
            int amountToTake = Mathf.Min(playerHas, transferBatchSize, maxCanTake);
            PlayerStats.Instance.SpendResource(currentStep.resourceName, amountToTake);
            
            // 전송 위치와 함께 주입
            AddHeldAmountDirectly(currentStep.resourceName, amountToTake, PlayerStats.Instance.transform.position);
        }
    }

    private void ProcessTowardsStep()
    {
        if (heldAmount <= 0) return;

        heldAmount--;
        currentStepAmount++;
        
        UpgradeStep currentStep = upgradeSteps[currentStepIndex];
        if (currentStepAmount >= currentStep.targetAmount)
        {
            CompleteStep(currentStep);
        }
        else
        {
            UpdateUI();
        }
    }

    private void CompleteStep(UpgradeStep step)
    {
        ApplyUpgrade(step);
        OnStepCompleted?.Invoke();

        currentStepIndex++;
        currentStepAmount = 0;
        
        if (currentStepIndex >= upgradeSteps.Count)
        {
            _isCompleted = true;
            gameObject.SetActive(false);
        }
        else
        {
            UpdateResourceIcon();
            UpdateUI();
        }
    }

    private void ApplyUpgrade(UpgradeStep step)
    {
        if (PlayerStats.Instance == null) return;

        if (step.equipmentPrefab != null)
        {
            Transform root = PlayerStats.Instance.equipmentRoot;
            if (root != null)
            {
                foreach (Transform child in root) Destroy(child.gameObject);
                GameObject eq = Instantiate(step.equipmentPrefab, root);
                eq.transform.localPosition = Vector3.zero;
                eq.transform.localRotation = Quaternion.identity;
                SetTagRecursive(eq, "Player");
            }
        }

        if (step.addAttackPower != 0) PlayerStats.Instance.UpgradeAttackPower(step.addAttackPower);
        if (step.addMaxCapacity != 0) PlayerStats.Instance.UpgradeMaxCapacity(step.addMaxCapacity);
        if (step.addAttackSpeed != 0) PlayerStats.Instance.UpgradeAttackSpeed(step.addAttackSpeed);
    }

    private void SetTagRecursive(GameObject obj, string tag)
    {
        obj.tag = tag;
        foreach (Transform child in obj.transform) SetTagRecursive(child.gameObject, tag);
    }

    private void UpdateResourceIcon()
    {
        if (currentStepIndex < upgradeSteps.Count && resourceIconImage != null && ResourceDatabase.Instance != null)
        {
            string rName = upgradeSteps[currentStepIndex].resourceName;
            Sprite icon = ResourceDatabase.Instance.GetSprite(rName);
            if (icon != null) resourceIconImage.sprite = icon;
        }
    }

    private void UpdateUI()
    {
        if (currentStepIndex >= upgradeSteps.Count)
        {
            if (progressFillImage != null) progressFillImage.fillAmount = 1f;
            return;
        }

        UpgradeStep step = upgradeSteps[currentStepIndex];
        int totalProgress = currentStepAmount + heldAmount;

        if (remainingAmountText != null)
        {
            int remaining = step.targetAmount - totalProgress;
            remainingAmountText.text = $"{Mathf.Max(0, remaining)}";
        }

        if (progressFillImage != null)
        {
            float fillRatio = (float)totalProgress / step.targetAmount;
            progressFillImage.fillAmount = Mathf.Clamp01(fillRatio);
        }
    }

    public void AddHeldAmountDirectly(string rName, int amount, Vector3 startPos = default)
    {
        if (currentStepIndex >= upgradeSteps.Count) return;
        if (rName != upgradeSteps[currentStepIndex].resourceName) return;

        heldAmount += amount;
        // 나중에 이 플랫폼에 HeldStack 비주얼이 추가되면 여기서 AddWithAnimation 호출 가능
        UpdateUI();
    }
}

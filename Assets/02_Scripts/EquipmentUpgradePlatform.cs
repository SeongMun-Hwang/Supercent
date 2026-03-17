using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class EquipmentUpgradePlatform : MonoBehaviour, IPlatformAction
{
    [Serializable]
    public struct UpgradeStep
    {
        public string resourceName;      // 요구 자원
        public int targetAmount;         // 요구 수량
        public GameObject equipmentPrefab; // (선택) 교체될 장비 프리팹

        [Header("Stat Upgrades (0 = Ignore)")]
        public float addAttackPower;     // 추가 공격력
        public int addMaxCapacity;       // 추가 인벤토리 용량
        public float addAttackSpeed;     // 공격 간격 변경 (빨라지려면 음수 입력)
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
            heldAmount += amountToTake;
            UpdateUI();
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

        // 1. 장비 교체
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

        // 2. 능력치 강화 (0이 아닐 때만 적용)
        if (step.addAttackPower != 0)
            PlayerStats.Instance.UpgradeAttackPower(step.addAttackPower);
        
        if (step.addMaxCapacity != 0)
            PlayerStats.Instance.UpgradeMaxCapacity(step.addMaxCapacity);
            
        if (step.addAttackSpeed != 0)
            PlayerStats.Instance.UpgradeAttackSpeed(step.addAttackSpeed);

        Debug.Log($"[Upgrade] Step Applied. Total Progress: {currentStepIndex + 1}/{upgradeSteps.Count}");
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
}

using UnityEngine;
using UnityEngine.UI; // 추가
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
        public GameObject equipmentPrefab; // 교체될 장비 프리팹
        public float attackPower;        // 해당 단계의 공격력
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
    [SerializeField] private Image progressFillImage; // 게이지용 이미지
    [SerializeField] private Image resourceIconImage; // 추가: 요구 자원 아이콘

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
            // 플레이어가 자원을 더 이상 전송할 수 없거나 플랫폼 밖에 있을 때 즉시 처리
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
            UpdateResourceIcon(); // 다음 단계 아이콘으로 갱신
            UpdateUI();
        }
    }

    private void ApplyUpgrade(UpgradeStep step)
    {
        if (PlayerStats.Instance == null) return;

        Transform root = PlayerStats.Instance.equipmentRoot;
        if (root != null)
        {
            foreach (Transform child in root) Destroy(child.gameObject);

            if (step.equipmentPrefab != null)
            {
                GameObject eq = Instantiate(step.equipmentPrefab, root);
                eq.transform.localPosition = Vector3.zero;
                eq.transform.localRotation = Quaternion.identity;
                SetTagRecursive(eq, "Player");
            }
        }

        PlayerStats.Instance.attackPower = step.attackPower;
        Debug.Log($"[Upgrade] Applied Step. New Power: {step.attackPower}");
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
            if (icon != null)
            {
                resourceIconImage.sprite = icon;
            }
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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[Serializable]
public class MultiClickCostEntry
{
    [EnumToggleButtons]
    public UpgradeCostType costType = UpgradeCostType.CrocodileFat;
    public float amount = 1f;
}

[Serializable]
public class MultiClickUpgradeStep
{
    [Tooltip("本级消耗；可填多项 = 同时扣多种资源")]
    public MultiClickCostEntry[] costs;
    [Tooltip("升完本级后，baseTriggerCount 直接设成这个值")]
    public int triggerCountAfterUpgrade = 5;
}

/// <summary>
/// 多次点击才触发：累计按键，达到 TriggerCount 后调用 onTriggered 并清零。
/// Button OnClick 绑 OnPress()；升级按钮绑 TryUpgrade()。
/// 升级按 upgradeSteps 逐级配置消耗与升级后次数。
/// </summary>
public class MultiClickTrigger : MonoBehaviour
{
    [Header("次数配置")]
    [Tooltip("需要按几下才触发")]
    public int baseTriggerCount = 6;
    [Tooltip("次数下限，升级赋值也不会低于此值")]
    public int minTriggerCount = 1;

    [Header("升级配置（表有几项就能升几级）")]
    public MultiClickUpgradeStep[] upgradeSteps;

    [Header("进度显示（可选）")]
    public TMP_Text progressText;
    [Tooltip("例如 {0}/{1} → 3/6")]
    public string progressFormat = "{0}/{1}";

    [Header("升级消耗显示（图标 x 数量）")]
    [Tooltip("横向排布父节点，建议挂 Horizontal Layout Group")]
    public Transform upgradeCostRoot;
    [Tooltip("单条预制体：含 Image + TMP")]
    public MultiClickCostIconEntry costIconPrefab;
    public Sprite moneyIcon;
    public Sprite crocodileFatIcon;
    public Sprite crocoFurIcon;
    [Tooltip("满级时显示；未赋值则跳过")]
    public TMP_Text maxLevelCostText;
    [Tooltip("例如 x{0} → x3")]
    public string amountFormat = "x{0}";

    [Header("触发时调用")]
    public UnityEvent onTriggered;

    [Header("升级结果回调（可选）")]
    public UnityEvent onUpgradeSuccess;
    public UnityEvent onUpgradeNotEnough;
    public UnityEvent onUpgradeMaxLevel;

    private int pressCount;
    private int upgradeLevel;

    public int PressCount => pressCount;
    public int UpgradeLevel => upgradeLevel;

    /// <summary>当前实际需要按几下。</summary>
    public int TriggerCount => Mathf.Max(minTriggerCount, baseTriggerCount);

    void Start()
    {
        RefreshProgressUI();
        RefreshUpgradeCostUI();
    }

    /// <summary>绑到 Button OnClick：每按一次累加，够次数就触发并归零。</summary>
    public void OnPress()
    {
        pressCount++;
        RefreshProgressUI();

        if (pressCount >= TriggerCount)
        {
            pressCount = 0;
            RefreshProgressUI();
            onTriggered?.Invoke();
        }
    }

    /// <summary>绑到升级 Button OnClick。</summary>
    public void TryUpgrade()
    {
        UpgradeResult result = AttemptUpgrade();
        switch (result)
        {
            case UpgradeResult.Success:
                onUpgradeSuccess?.Invoke();
                break;
            case UpgradeResult.MaxLevel:
                onUpgradeMaxLevel?.Invoke();
                break;
            default:
                onUpgradeNotEnough?.Invoke();
                break;
        }

        RefreshUpgradeCostUI();
    }

    /// <summary>尝试升级并返回结果（不播回调）。</summary>
    public UpgradeResult AttemptUpgrade()
    {
        if (IsMaxUpgrade())
        {
            return UpgradeResult.MaxLevel;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MultiClickTrigger: GameManager 不存在。", this);
            return UpgradeResult.NotEnoughResource;
        }

        MultiClickUpgradeStep step = upgradeSteps[upgradeLevel];
        MultiClickCostEntry[] costs = step != null ? step.costs : null;

        if (!GameManager.Instance.TrySpendUpgradeCosts(costs))
        {
            return UpgradeResult.NotEnoughResource;
        }

        SetTriggerCount(step.triggerCountAfterUpgrade);
        upgradeLevel++;
        return UpgradeResult.Success;
    }

    public bool IsMaxUpgrade()
    {
        return upgradeSteps == null || upgradeLevel >= upgradeSteps.Length;
    }

    /// <summary>直接设置所需点击次数（受 minTriggerCount 下限约束）。</summary>
    public void SetTriggerCount(int count)
    {
        baseTriggerCount = Mathf.Max(minTriggerCount, count);
        TryFlushIfReady();
        RefreshProgressUI();
    }

    /// <summary>手动清零计数（例如切场景、重置关卡）。</summary>
    public void ResetPressCount()
    {
        pressCount = 0;
        RefreshProgressUI();
    }

    /// <summary>外部改了阈值时调用，检查是否已该触发。</summary>
    public void TryFlushIfReady()
    {
        if (pressCount >= TriggerCount && TriggerCount > 0)
        {
            pressCount = 0;
            RefreshProgressUI();
            onTriggered?.Invoke();
        }
        else
        {
            RefreshProgressUI();
        }
    }

    public void RefreshProgressUI()
    {
        if (progressText == null) return;
        progressText.text = string.Format(progressFormat, pressCount, TriggerCount);
    }

    /// <summary>按当前升级级刷新「图标 x 数量」列表。</summary>
    public void RefreshUpgradeCostUI()
    {
        ClearCostEntries();

        bool maxed = IsMaxUpgrade();
        if (maxLevelCostText != null)
        {
            maxLevelCostText.gameObject.SetActive(maxed);
            if (maxed)
            {
                maxLevelCostText.text = "已满级";
            }
        }

        if (upgradeCostRoot != null)
        {
            upgradeCostRoot.gameObject.SetActive(!maxed);
        }

        if (maxed || upgradeCostRoot == null || costIconPrefab == null)
        {
            return;
        }

        MultiClickUpgradeStep step = upgradeSteps[upgradeLevel];
        if (step == null || step.costs == null)
        {
            return;
        }

        for (int i = 0; i < step.costs.Length; i++)
        {
            MultiClickCostEntry cost = step.costs[i];
            if (cost == null) continue;

            MultiClickCostIconEntry entry = Instantiate(costIconPrefab, upgradeCostRoot);
            entry.Setup(GetIcon(cost.costType), string.Format(amountFormat, cost.amount));
        }
    }

    private void ClearCostEntries()
    {
        if (upgradeCostRoot == null) return;

        for (int i = upgradeCostRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(upgradeCostRoot.GetChild(i).gameObject);
        }
    }

    private Sprite GetIcon(UpgradeCostType costType)
    {
        switch (costType)
        {
            case UpgradeCostType.Money:
                return moneyIcon;
            case UpgradeCostType.CrocodileFat:
                return crocodileFatIcon;
            case UpgradeCostType.CrocoFur:
                return crocoFurIcon;
            default:
                return null;
        }
    }
}

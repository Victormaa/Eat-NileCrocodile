using System;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[Serializable]
public class ResourceOutputEntry
{
    [EnumToggleButtons]
    public UpgradeCostType type = UpgradeCostType.Money;
    public float amount = 1f;
}

[Serializable]
public class ResourceOutputStep
{
    [Tooltip("本等级每次转换的产出列表")]
    public ResourceOutputEntry[] outputs;
}

/// <summary>
/// 资源转换：由 MultiClickTrigger.onTriggered 调用 TryConvert。
/// 先扣 inputCosts，再按当前升级等级从 outputSteps 取产出；不够则失败、不产出。
/// </summary>
public class ResourceConverter : MonoBehaviour
{
    [Header("升级来源")]
    [Tooltip("用于读取 UpgradeLevel；为空则同物体 GetComponent")]
    public MultiClickTrigger clickTrigger;

    [Header("每次转换消耗")]
    [Tooltip("通常一项：CrocodileFat；可填多项 = 同时扣多种资源")]
    public MultiClickCostEntry[] inputCosts;

    [Header("按等级产出（优先）")]
    [Tooltip("[0]=未升级，[1]=升1级后…；长度建议 = upgradeSteps.Length + 1")]
    public ResourceOutputStep[] outputSteps;

    [Header("兼容：固定产出")]
    [Tooltip("outputSteps 为空时使用")]
    public ResourceOutputEntry[] outputs;

    [Header("结果回调（可选）")]
    public UnityEvent onConvertSuccess;
    public UnityEvent onConvertFailed;

    void Awake()
    {
        if (clickTrigger == null)
            clickTrigger = GetComponent<MultiClickTrigger>();
    }

    /// <summary>UnityEvent 入口（无返回值）。</summary>
    public void ConvertOnce()
    {
        TryConvert();
    }

    /// <summary>绑到 MultiClickTrigger.onTriggered：够资源则扣料加货，否则失败反馈。</summary>
    public bool TryConvert()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("ResourceConverter: GameManager 不存在。", this);
            onConvertFailed?.Invoke();
            return false;
        }

        if (!CanAffordInputs())
        {
            onConvertFailed?.Invoke();
            return false;
        }

        if (!GameManager.Instance.TrySpendUpgradeCosts(inputCosts))
        {
            onConvertFailed?.Invoke();
            return false;
        }

        ApplyOutputs();
        onConvertSuccess?.Invoke();
        return true;
    }

    /// <summary>当前等级对应的产出表（供调试 / 验证用）。</summary>
    public ResourceOutputEntry[] GetCurrentOutputs()
    {
        if (outputSteps != null && outputSteps.Length > 0)
        {
            int level = clickTrigger != null ? clickTrigger.UpgradeLevel : 0;
            int index = Mathf.Clamp(level, 0, outputSteps.Length - 1);
            ResourceOutputStep step = outputSteps[index];
            return step != null ? step.outputs : null;
        }

        return outputs;
    }

    private bool CanAffordInputs()
    {
        if (inputCosts == null || inputCosts.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < inputCosts.Length; i++)
        {
            MultiClickCostEntry entry = inputCosts[i];
            if (entry == null) continue;
            if (!GameManager.Instance.CanAfford(entry.costType, entry.amount))
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyOutputs()
    {
        ResourceOutputEntry[] active = GetCurrentOutputs();
        if (active == null) return;

        for (int i = 0; i < active.Length; i++)
        {
            ResourceOutputEntry entry = active[i];
            if (entry == null || entry.amount == 0f) continue;

            switch (entry.type)
            {
                case UpgradeCostType.Money:
                    GameManager.Instance.AddMoney(entry.amount);
                    break;
                case UpgradeCostType.CrocodileFat:
                    GameManager.Instance.AddCrocodileFat(entry.amount);
                    break;
                case UpgradeCostType.CrocoFur:
                    GameManager.Instance.AddCrocoFur(entry.amount);
                    break;
                default:
                    Debug.LogWarning($"ResourceConverter: 未知产出类型 {entry.type}", this);
                    break;
            }
        }
    }
}

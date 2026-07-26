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

/// <summary>
/// 资源转换：由 MultiClickTrigger.onTriggered 调用 TryConvert。
/// 先扣 inputCosts，再按 outputs 加资源；不够则失败、不产出。
/// </summary>
public class ResourceConverter : MonoBehaviour
{
    [Header("每次转换消耗")]
    [Tooltip("通常一项：CrocodileFat；可填多项 = 同时扣多种资源")]
    public MultiClickCostEntry[] inputCosts;

    [Header("每次转换产出")]
    [Tooltip("可 1 项或多项，如 [CrocoFur, Money] 或仅 [Money]")]
    public ResourceOutputEntry[] outputs;

    [Header("结果回调（可选）")]
    public UnityEvent onConvertSuccess;
    public UnityEvent onConvertFailed;

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
        if (outputs == null) return;

        for (int i = 0; i < outputs.Length; i++)
        {
            ResourceOutputEntry entry = outputs[i];
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

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
/// 资源转换：建造（UpgradeLevel 达标）后可自动按间隔 TryConvert；
/// 也可由 MultiClickTrigger.onTriggered 手动调用。
/// 先扣 inputCosts，再按当前升级等级从 outputSteps 取产出。
/// </summary>
public class ResourceConverter : MonoBehaviour
{
    [Header("升级来源")]
    [Tooltip("用于读取 UpgradeLevel；为空则同物体 GetComponent")]
    public MultiClickTrigger clickTrigger;

    [Header("建造后自动产出")]
    [Tooltip("UpgradeLevel 达到门槛后，按间隔自动 TryConvert")]
    public bool autoProduceAfterBuilt = true;
    [Tooltip("自动产出间隔（秒）")]
    public float autoInterval = 2.5f;
    [Tooltip("UpgradeLevel >= 该值视为已建造，开始自动产")]
    public int builtUpgradeLevel = 1;
    [Tooltip("加工按钮根物体；自动模式下保持隐藏")]
    public GameObject interactButtonRoot;

    [Header("每次转换消耗")]
    [Tooltip("通常一项：CrocodileFat；可填多项 = 同时扣多种资源")]
    public MultiClickCostEntry[] inputCosts;

    [Header("按等级产出（优先）")]
    [Tooltip("[0]=未升级，[1]=升1级后…；长度建议 = upgradeSteps.Length + 1")]
    public ResourceOutputStep[] outputSteps;

    [Header("兼容：固定产出")]
    [Tooltip("outputSteps 为空时使用")]
    public ResourceOutputEntry[] outputs;

    [Header("产出飘字（可选）")]
    [Tooltip("为空则同物体 GetComponent")]
    public ResourceGainFeedback gainFeedback;

    [Header("结果回调（可选）")]
    public UnityEvent onConvertSuccess;
    public UnityEvent onConvertFailed;

    private float autoTimer;
    private bool listeningUpgrade;

    public bool IsBuilt =>
        clickTrigger != null && clickTrigger.UpgradeLevel >= builtUpgradeLevel;

    void Awake()
    {
        if (clickTrigger == null)
            clickTrigger = GetComponent<MultiClickTrigger>();
        if (gainFeedback == null)
            gainFeedback = GetComponent<ResourceGainFeedback>();
    }

    void OnEnable()
    {
        BindUpgradeListener();
        RefreshInteractButton();
    }

    void OnDisable()
    {
        UnbindUpgradeListener();
    }

    void Update()
    {
        if (!autoProduceAfterBuilt || !IsBuilt)
            return;

        if (autoInterval <= 0f)
            return;

        autoTimer += Time.deltaTime;
        if (autoTimer < autoInterval)
            return;

        autoTimer = 0f;
        TryConvert();
    }

    /// <summary>UnityEvent 入口（无返回值）。</summary>
    public void ConvertOnce()
    {
        TryConvert();
    }

    /// <summary>够资源则扣料加货，否则失败反馈。</summary>
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

        ResourceOutputEntry[] produced = GetCurrentOutputs();
        ApplyOutputs(produced);
        gainFeedback?.Show(produced);
        onConvertSuccess?.Invoke();
        return true;
    }

    /// <summary>升级成功时由 MultiClickTrigger.onUpgradeSuccess 调用，或内部监听。</summary>
    public void OnUpgradeSuccess()
    {
        RefreshInteractButton();
        if (IsBuilt)
            autoTimer = 0f;
    }

    /// <summary>当前等级对应的产出表（供调试用）。</summary>
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

    public void RefreshInteractButton()
    {
        if (interactButtonRoot == null)
            return;

        // 自动产出开启时不需要加工按钮；关闭自动时仅已建造才显示。
        bool show = !autoProduceAfterBuilt && IsBuilt;
        interactButtonRoot.SetActive(show);
    }

    private void BindUpgradeListener()
    {
        if (listeningUpgrade || clickTrigger == null)
            return;

        clickTrigger.onUpgradeSuccess.AddListener(OnUpgradeSuccess);
        listeningUpgrade = true;
    }

    private void UnbindUpgradeListener()
    {
        if (!listeningUpgrade || clickTrigger == null)
            return;

        clickTrigger.onUpgradeSuccess.RemoveListener(OnUpgradeSuccess);
        listeningUpgrade = false;
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

    private void ApplyOutputs(ResourceOutputEntry[] active)
    {
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

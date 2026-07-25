using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

public enum UpgradeResult
{
    Success,
    NotEnoughResource,
    MaxLevel,
}

public enum UpgradeCostType
{
    Money,
    CrocodileFat,
    CrocoFur,
}

public class GameManager : MonoBehaviour
{
    // ---------- UI 引用 ----------
    public TMP_Text debugTimer;
    public TMP_Text moneyValue_Text;
    [FormerlySerializedAs("satiety_Text")]
    public TMP_Text crocodileFat_Text;
    public TMP_Text crocoFur_Text;
    public TMP_Text stealthLevel_Text;

    // ---------- 数值字段 ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;

    [Header("鳄鱼膘 / 鳄鱼皮 / 隐蔽升级")]
    [Tooltip("鳄鱼膘：吃角马时获得，可用于升级")]
    public float crocodileFat;
    [Tooltip("鳄鱼皮：可用于升级的消耗资源")]
    public float crocoFur;
    [Tooltip("全局隐蔽数值，所有鳄鱼共享，用于外观")]
    public int stealthLevel;
    [Tooltip("隐蔽已成功升级次数（用作 StatUpgradeButton 步骤下标）")]
    public int stealthUpgradeCount;
    [Tooltip("隐蔽数值上限；<=0 表示不限制")]
    public int maxStealthLevel = 10;

    [Header("抓取速度 / 冷却")]
    [Tooltip("全局鳄鱼扑向猎物的速度")]
    public float catchApproachSpeed = 12f;
    [Tooltip("抓取速度已升级次数")]
    public int catchSpeedUpgradeCount;
    [Tooltip("扑速上限；<=0 表示不限制")]
    public float maxCatchApproachSpeed = 30f;
    [Tooltip("全局吃完后回位前等待（秒）")]
    public float catchReturnDelay = 1f;
    [Tooltip("抓取冷却已升级次数")]
    public int catchCoolDownUpgradeCount;
    [Tooltip("回位等待下限（秒）")]
    public float minCatchReturnDelay = 0f;

    // ---------- 单例 ----------
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                    Debug.LogError("场景里没有 GameManager 实例。");
            }
            return instance;
        }
        private set { }
    }

    public int StealthLevel => stealthLevel;
    public int StealthUpgradeCount => stealthUpgradeCount;
    public float CrocodileFat => crocodileFat;
    public float CrocoFur => crocoFur;
    public float CatchApproachSpeed => catchApproachSpeed;
    public int CatchSpeedUpgradeCount => catchSpeedUpgradeCount;
    public float CatchReturnDelay => catchReturnDelay;
    public int CatchCoolDownUpgradeCount => catchCoolDownUpgradeCount;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdateAllUI();
        RefreshAllCrocodileStealth();
        RefreshAllCrocodilesCatchStats();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        UpdateTimerUI();
    }

    // ---------- UI 更新方法 ----------
    public void UpdateAllUI()
    {
        UpdateTimerUI();
        UpdateMoneyUI();
        UpdateCrocodileFatUI();
        UpdateCrocoFurUI();
        UpdateStealthLevelUI();
    }

    private void UpdateTimerUI()
    {
        if (debugTimer != null)
            debugTimer.text = "Time: " + timer.ToString("F2");
    }

    private void UpdateMoneyUI()
    {
        if (moneyValue_Text != null)
            moneyValue_Text.text = "金钱: " + moneyValue.ToString("F2");
    }

    private void UpdateCrocodileFatUI()
    {
        if (crocodileFat_Text != null)
            crocodileFat_Text.text = "鳄鱼膘: " + crocodileFat.ToString("F0");
    }

    private void UpdateCrocoFurUI()
    {
        if (crocoFur_Text != null)
            crocoFur_Text.text = "鳄鱼皮: " + crocoFur.ToString("F0");
    }

    private void UpdateStealthLevelUI()
    {
        if (stealthLevel_Text != null)
            stealthLevel_Text.text = "Stealth Lv: " + stealthLevel.ToString();
    }

    public void AddMoney(float amount)
    {
        moneyValue += amount;
        UpdateMoneyUI();
    }

    public void AddCrocodileFat(float amount)
    {
        crocodileFat += amount;
        if (crocodileFat < 0f) crocodileFat = 0f;
        UpdateCrocodileFatUI();
    }

    public void AddCrocoFur(float amount)
    {
        crocoFur += amount;
        if (crocoFur < 0f) crocoFur = 0f;
        UpdateCrocoFurUI();
    }

    /// <summary>
    /// 隐蔽升级：按 costs 扣多种资源，成功则 stealthLevel += RoundToInt(valueIncrease)。
    /// </summary>
    public UpgradeResult TryUpgradeStealth(MultiClickCostEntry[] costs, float valueIncrease)
    {
        int add = Mathf.RoundToInt(valueIncrease);
        if (add <= 0)
        {
            Debug.LogWarning("TryUpgradeStealth: valueIncrease 取整后 <= 0，忽略本次升级。");
            return UpgradeResult.NotEnoughResource;
        }

        if (maxStealthLevel > 0 && stealthLevel >= maxStealthLevel)
        {
            return UpgradeResult.MaxLevel;
        }

        if (maxStealthLevel > 0 && stealthLevel + add > maxStealthLevel)
        {
            return UpgradeResult.MaxLevel;
        }

        if (!TrySpendUpgradeCosts(costs))
        {
            return UpgradeResult.NotEnoughResource;
        }

        stealthLevel += add;
        stealthUpgradeCount++;
        UpdateStealthLevelUI();
        RefreshAllCrocodileStealth();
        return UpgradeResult.Success;
    }

    /// <summary>
    /// 抓取速度升级：增加全局 catchApproachSpeed，并同步所有鳄鱼。
    /// </summary>
    public UpgradeResult TryUpgradeCatchSpeed(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchSpeed: valueIncrease <= 0，忽略本次升级。");
            return UpgradeResult.NotEnoughResource;
        }

        if (maxCatchApproachSpeed > 0f && catchApproachSpeed >= maxCatchApproachSpeed)
        {
            return UpgradeResult.MaxLevel;
        }

        if (maxCatchApproachSpeed > 0f && catchApproachSpeed + valueIncrease > maxCatchApproachSpeed)
        {
            return UpgradeResult.MaxLevel;
        }

        if (!TrySpendUpgradeCosts(costs))
        {
            return UpgradeResult.NotEnoughResource;
        }

        catchApproachSpeed += valueIncrease;
        catchSpeedUpgradeCount++;
        RefreshAllCrocodilesCatchStats();
        return UpgradeResult.Success;
    }

    /// <summary>
    /// 抓取冷却升级：减少全局 catchReturnDelay（不低于 minCatchReturnDelay），并同步所有鳄鱼。
    /// </summary>
    public UpgradeResult TryUpgradeCatchCoolDown(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchCoolDown: valueIncrease <= 0，忽略本次升级。");
            return UpgradeResult.NotEnoughResource;
        }

        if (catchReturnDelay <= minCatchReturnDelay)
        {
            return UpgradeResult.MaxLevel;
        }

        if (!TrySpendUpgradeCosts(costs))
        {
            return UpgradeResult.NotEnoughResource;
        }

        catchReturnDelay = Mathf.Max(minCatchReturnDelay, catchReturnDelay - valueIncrease);
        catchCoolDownUpgradeCount++;
        RefreshAllCrocodilesCatchStats();
        return UpgradeResult.Success;
    }

    /// <summary>只检查资源是否足够，不扣除。</summary>
    public bool CanAfford(UpgradeCostType costType, float amount)
    {
        if (amount <= 0f) return true;

        switch (costType)
        {
            case UpgradeCostType.Money:
                return moneyValue >= amount;
            case UpgradeCostType.CrocodileFat:
                return crocodileFat >= amount;
            case UpgradeCostType.CrocoFur:
                return crocoFur >= amount;
            default:
                return false;
        }
    }

    /// <summary>按 costType 扣除升级消耗；不够则返回 false。</summary>
    public bool TrySpendUpgradeCost(float cost, UpgradeCostType costType)
    {
        if (!CanAfford(costType, cost)) return false;

        switch (costType)
        {
            case UpgradeCostType.Money:
                moneyValue -= cost;
                UpdateMoneyUI();
                return true;

            case UpgradeCostType.CrocodileFat:
                crocodileFat -= cost;
                UpdateCrocodileFatUI();
                return true;

            case UpgradeCostType.CrocoFur:
                crocoFur -= cost;
                UpdateCrocoFurUI();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 一次性扣除多项资源：先全部检查，再逐项扣除；任一不够则整单失败、不扣任何资源。
    /// </summary>
    public bool TrySpendUpgradeCosts(MultiClickCostEntry[] costs)
    {
        if (costs == null || costs.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < costs.Length; i++)
        {
            MultiClickCostEntry entry = costs[i];
            if (entry == null) continue;
            if (!CanAfford(entry.costType, entry.amount))
            {
                return false;
            }
        }

        for (int i = 0; i < costs.Length; i++)
        {
            MultiClickCostEntry entry = costs[i];
            if (entry == null) continue;
            if (entry.amount <= 0f) continue;

            if (!TrySpendUpgradeCost(entry.amount, entry.costType))
            {
                Debug.LogError("TrySpendUpgradeCosts: 扣费中途失败，资源状态可能不一致。");
                return false;
            }
        }

        return true;
    }

    /// <summary>按当前 stealthLevel 刷新场景里所有鳄鱼。</summary>
    public void RefreshAllCrocodileStealth()
    {
        Crocodile[] crocs = FindObjectsOfType<Crocodile>();
        for (int i = 0; i < crocs.Length; i++)
        {
            if (crocs[i] != null)
            {
                crocs[i].RefreshFromGlobalStealth(stealthLevel);
            }
        }
    }

    /// <summary>按全局抓取速度/冷却刷新场景里所有鳄鱼。</summary>
    public void RefreshAllCrocodilesCatchStats()
    {
        Crocodile[] crocs = FindObjectsOfType<Crocodile>();
        for (int i = 0; i < crocs.Length; i++)
        {
            if (crocs[i] != null)
            {
                crocs[i].RefreshFromGlobalCatchStats(catchApproachSpeed, catchReturnDelay);
            }
        }
    }
}

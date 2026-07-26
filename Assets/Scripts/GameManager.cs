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
    // ---------- UI ???? ----------
    public TMP_Text debugTimer;
    public TMP_Text moneyValue_Text;
    [FormerlySerializedAs("satiety_Text")]
    public TMP_Text crocodileFat_Text;
    public TMP_Text crocoFur_Text;
    public TMP_Text stealthLevel_Text;

    // ---------- ?????? ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;

    [Header("????? / ????? / ????????")]
    [Tooltip("?????????????????????????")]
    public float crocodileFat;
    [Tooltip("??????????????????????????")]
    public float crocoFur;
    [Tooltip("???????????????????????????????")]
    public int stealthLevel;
    [Tooltip("?????????????????????? StatUpgradeButton ?????±?")]
    public int stealthUpgradeCount;
    [Tooltip("????????????<=0 ?????????")]
    public int maxStealthLevel = 10;

    [Header("????? / ???")]
    [Tooltip("???????????????????")]
    public float catchApproachSpeed = 12f;
    [Tooltip("???????????????")]
    public int catchSpeedUpgradeCount;
    [Tooltip("?????????<=0 ?????????")]
    public float maxCatchApproachSpeed = 30f;
    [Tooltip("????????λ????????")]
    public float catchReturnDelay = 1f;
    [Tooltip("???????????????")]
    public int catchCoolDownUpgradeCount;
    [Tooltip("??λ??????????")]
    public float minCatchReturnDelay = 0f;

    // ---------- ???? ----------
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                    Debug.LogError("????????? GameManager ?????");
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

    [Header("满级外观（BadCroco）")]
    [Tooltip("与对应 StatUpgradeButton 的 upgradeSteps 数量对齐")]
    public int requiredStealthUpgrades = 10;
    public int requiredCatchSpeedUpgrades = 10;
    public int requiredCatchCoolDownUpgrades = 10;

    /// <summary>三项升级按钮都升满（按升级次数，与按钮满级一致）。</summary>
    public bool IsFullyUpgraded =>
        stealthUpgradeCount >= requiredStealthUpgrades
        && catchSpeedUpgradeCount >= requiredCatchSpeedUpgrades
        && catchCoolDownUpgradeCount >= requiredCatchCoolDownUpgrades;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        // ?????????????????? DontDestroyOnLoad????????????????????UI ?????Ч
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
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

    // ---------- UI ???·??? ----------
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
            moneyValue_Text.text = "???: " + moneyValue.ToString("F2");
    }

    private void UpdateCrocodileFatUI()
    {
        if (crocodileFat_Text != null)
            crocodileFat_Text.text = "?????: " + crocodileFat.ToString("F0");
    }

    private void UpdateCrocoFurUI()
    {
        if (crocoFur_Text != null)
            crocoFur_Text.text = "?????: " + crocoFur.ToString("F0");
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
    /// ???????????? costs ??????????????? stealthLevel += RoundToInt(valueIncrease)??
    /// </summary>
    public UpgradeResult TryUpgradeStealth(MultiClickCostEntry[] costs, float valueIncrease)
    {
        int add = Mathf.RoundToInt(valueIncrease);
        if (add <= 0)
        {
            Debug.LogWarning("TryUpgradeStealth: valueIncrease ????? <= 0???????????????");
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
    /// ?????????????????? catchApproachSpeed???????????????
    /// </summary>
    public UpgradeResult TryUpgradeCatchSpeed(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchSpeed: valueIncrease <= 0???????????????");
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
    /// ?????????????????? catchReturnDelay???????? minCatchReturnDelay?????????????????
    /// </summary>
    public UpgradeResult TryUpgradeCatchCoolDown(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchCoolDown: valueIncrease <= 0???????????????");
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

    /// <summary>?????????????????????</summary>
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

    /// <summary>?? costType ??????????????????? false??</summary>
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
    /// ???????????????????????飬????????????????????????????????κ??????
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
                Debug.LogError("TrySpendUpgradeCosts: ????????????????????????");
                return false;
            }
        }

        return true;
    }

    /// <summary>????? stealthLevel ????????????????</summary>
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

    /// <summary>??????????/???????????????????</summary>
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

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
    // ---------- UI references ----------
    public TMP_Text debugTimer;
    public TMP_Text moneyValue_Text;
    [FormerlySerializedAs("satiety_Text")]
    public TMP_Text crocodileFat_Text;
    public TMP_Text crocoFur_Text;
    public TMP_Text stealthLevel_Text;

    // ---------- Resource / timer fields ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;

    [Header("Crocodile Fat / Fur / Stealth")]
    [Tooltip("Crocodile fat: gained by eating wildebeest, spent on upgrades")]
    public float crocodileFat;
    [Tooltip("Crocodile fur: resource spent on upgrades")]
    public float crocoFur;
    [Tooltip("Global stealth value shared by all crocodiles (drives visuals)")]
    public int stealthLevel;
    [Tooltip("How many times stealth was upgraded (StatUpgradeButton step index)")]
    public int stealthUpgradeCount;
    [Tooltip("Stealth level cap; <=0 means unlimited")]
    public int maxStealthLevel = 10;

    [Header("Catch Speed / Cooldown")]
    [Tooltip("Global speed when crocodiles lunge at prey")]
    public float catchApproachSpeed = 12f;
    [Tooltip("How many times catch speed was upgraded")]
    public int catchSpeedUpgradeCount;
    [Tooltip("Catch approach speed cap; <=0 means unlimited")]
    public float maxCatchApproachSpeed = 30f;
    [Tooltip("Global wait (seconds) after eating before returning home")]
    public float catchReturnDelay = 1f;
    [Tooltip("How many times catch cooldown was upgraded")]
    public int catchCoolDownUpgradeCount;
    [Tooltip("Minimum return wait (seconds)")]
    public float minCatchReturnDelay = 0f;

    // ---------- Singleton ----------
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                    Debug.LogError("No GameManager instance found in the scene.");
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

    [Header("Max-level look (BadCroco)")]
    [Tooltip("Match each StatUpgradeButton upgradeSteps count")]
    public int requiredStealthUpgrades = 10;
    public int requiredCatchSpeedUpgrades = 10;
    public int requiredCatchCoolDownUpgrades = 10;

    /// <summary>All three upgrade tracks are maxed (by upgrade count, same as button max).</summary>
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
        // Intentionally not using DontDestroyOnLoad so scene UI references stay valid.
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

    // ---------- UI refresh ----------
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
            moneyValue_Text.text = "ˆ˘”„±“: " + moneyValue.ToString("F0");
    }

    private void UpdateCrocodileFatUI()
    {
        if (crocodileFat_Text != null)
            crocodileFat_Text.text = "ˆ˘”„±Ï: " + crocodileFat.ToString("F0");
    }

    private void UpdateCrocoFurUI()
    {
        if (crocoFur_Text != null)
            crocoFur_Text.text = "ˆ˘”„∆§: " + crocoFur.ToString("F0");
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
    /// Stealth upgrade: spend costs, then stealthLevel += RoundToInt(valueIncrease).
    /// </summary>
    public UpgradeResult TryUpgradeStealth(MultiClickCostEntry[] costs, float valueIncrease)
    {
        int add = Mathf.RoundToInt(valueIncrease);
        if (add <= 0)
        {
            Debug.LogWarning("TryUpgradeStealth: valueIncrease rounds to <= 0, ignoring upgrade.");
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
    /// Catch-speed upgrade: increase global catchApproachSpeed and sync all crocodiles.
    /// </summary>
    public UpgradeResult TryUpgradeCatchSpeed(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchSpeed: valueIncrease <= 0, ignoring upgrade.");
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
    /// Catch-cooldown upgrade: reduce catchReturnDelay (not below minCatchReturnDelay) and sync crocodiles.
    /// </summary>
    public UpgradeResult TryUpgradeCatchCoolDown(MultiClickCostEntry[] costs, float valueIncrease)
    {
        if (valueIncrease <= 0f)
        {
            Debug.LogWarning("TryUpgradeCatchCoolDown: valueIncrease <= 0, ignoring upgrade.");
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

    /// <summary>Check if there are enough resources; does not spend.</summary>
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

    /// <summary>Spend one upgrade cost by type; returns false if not enough.</summary>
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
    /// Spend multiple costs atomically: check all first, then deduct; fail spends nothing.
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
                Debug.LogError("TrySpendUpgradeCosts: mid-spend failed; resources may be inconsistent.");
                return false;
            }
        }

        return true;
    }

    /// <summary>Refresh all crocodiles from the current stealthLevel.</summary>
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

    /// <summary>Refresh all crocodiles from global catch speed / return delay.</summary>
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

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
    public TMP_Text wildeBeestCount_Text;
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
    [Tooltip("??????ε???????????????????????")]
    public int stealthLevel;
    [Tooltip("?????????????????????Money / CrocodileFat / CrocoFur ???????")]
    public float stealthUpgradeCost = 3f;
    [Tooltip("???ε???????<=0 ?????????")]
    public int maxStealthLevel = 10;

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
    public float CrocodileFat => crocodileFat;
    public float CrocoFur => crocoFur;

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
        UpdateWildebeestUI();
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

    private void UpdateWildebeestUI()
    {
        if (wildeBeestCount_Text != null)
            wildeBeestCount_Text.text = "Wildebeest: " + wildeBeestCount.ToString("F0");
    }

    private void UpdateMoneyUI()
    {
        if (moneyValue_Text != null)
            moneyValue_Text.text = "$ " + moneyValue.ToString("F2");
    }

    private void UpdateCrocodileFatUI()
    {
        if (crocodileFat_Text != null)
            crocodileFat_Text.text = "Fat: " + crocodileFat.ToString("F0");
    }

    private void UpdateCrocoFurUI()
    {
        if (crocoFur_Text != null)
            crocoFur_Text.text = "Fur: " + crocoFur.ToString("F0");
    }

    private void UpdateStealthLevelUI()
    {
        if (stealthLevel_Text != null)
            stealthLevel_Text.text = "Stealth Lv: " + stealthLevel.ToString();
    }

    // ---------- ?????????? UI ----------
    public void AddWildebeest(float amount)
    {
        wildeBeestCount += amount;
        UpdateWildebeestUI();
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
    /// ???????????? costType ?? Money / CrocodileFat / CrocoFur??????????? StealthLevel ?????????
    /// </summary>
    public UpgradeResult TryUpgradeStealth(UpgradeCostType costType)
    {
        if (maxStealthLevel > 0 && stealthLevel >= maxStealthLevel)
        {
            return UpgradeResult.MaxLevel;
        }

        if (!TrySpendUpgradeCost(stealthUpgradeCost, costType))
        {
            return UpgradeResult.NotEnoughResource;
        }

        stealthLevel++;
        UpdateStealthLevelUI();
        RefreshAllCrocodileStealth();
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
                // 理论上不应发生：前面已全部 CanAfford
                Debug.LogError("TrySpendUpgradeCosts: 扣费中途失败，资源状态可能不一致。");
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
}

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
}

public class GameManager : MonoBehaviour
{
    // ---------- UI 引用 ----------
    public TMP_Text debugTimer;
    public TMP_Text wildeBeestCount_Text;
    public TMP_Text moneyValue_Text;
    public TMP_Text crocodileCount_Text;
    [FormerlySerializedAs("satiety_Text")]
    public TMP_Text crocodileFat_Text;
    public TMP_Text stealthLevel_Text;

    // ---------- 数值字段 ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;
    public float crocodileCount;

    [Header("鳄鱼膘 / 隐蔽升级")]
    [Tooltip("鳄鱼膘：吃角马时获得，可用于升级")]
    public float crocodileFat;
    [Tooltip("全局隐蔽等级，所有鳄鱼共享，用于外观")]
    public int stealthLevel;
    [Tooltip("每次隐蔽升级消耗的资源（Money 或 CrocodileFat 由按钮选择）")]
    public float stealthUpgradeCost = 3f;
    [Tooltip("隐蔽等级上限；<=0 表示不限制")]
    public int maxStealthLevel = 10;

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
    public float CrocodileFat => crocodileFat;

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

    // ---------- UI 更新方法 ----------
    public void UpdateAllUI()
    {
        UpdateTimerUI();
        UpdateWildebeestUI();
        UpdateMoneyUI();
        UpdateCrocodileUI();
        UpdateCrocodileFatUI();
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

    private void UpdateCrocodileUI()
    {
        if (crocodileCount_Text != null)
            crocodileCount_Text.text = "Crocodiles: " + crocodileCount.ToString("F0");
    }

    private void UpdateCrocodileFatUI()
    {
        if (crocodileFat_Text != null)
            crocodileFat_Text.text = "Fat: " + crocodileFat.ToString("F0");
    }

    private void UpdateStealthLevelUI()
    {
        if (stealthLevel_Text != null)
            stealthLevel_Text.text = "Stealth Lv: " + stealthLevel.ToString();
    }

    // ---------- 改数值并刷新 UI ----------
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

    public void AddCrocodile(float amount)
    {
        crocodileCount += amount;
        UpdateCrocodileUI();
    }

    public void AddCrocodileFat(float amount)
    {
        crocodileFat += amount;
        if (crocodileFat < 0f) crocodileFat = 0f;
        UpdateCrocodileFatUI();
    }

    /// <summary>
    /// 隐蔽升级：按 costType 扣 Money 或 CrocodileFat；成功则提升 StealthLevel 并刷新鳄鱼。
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

    private bool TrySpendUpgradeCost(float cost, UpgradeCostType costType)
    {
        if (costType == UpgradeCostType.Money)
        {
            if (moneyValue < cost) return false;
            moneyValue -= cost;
            UpdateMoneyUI();
            return true;
        }

        if (crocodileFat < cost) return false;
        crocodileFat -= cost;
        UpdateCrocodileFatUI();
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
}

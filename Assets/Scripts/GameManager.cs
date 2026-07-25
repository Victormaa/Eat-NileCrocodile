using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ---------- UI 引用 ----------
    public TMP_Text debugTimer;
    public TMP_Text wildeBeestCount_Text;
    public TMP_Text moneyValue_Text;
    public TMP_Text crocodileCount_Text;
    public TMP_Text satiety_Text;
    public TMP_Text stealthLevel_Text;

    // ---------- 数值字段 ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;
    public float crocodileCount;

    [Header("饱腹感 / 隐蔽升级")]
    [Tooltip("饱腹感：升级隐蔽时消耗；吃角马时增加")]
    public float satiety;
    [Tooltip("全局隐蔽等级，所有鳄鱼共用升级进度")]
    public int stealthLevel;
    [Tooltip("每次升级消耗的饱腹感")]
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
                    Debug.LogError("场景中没有 GameManager 实例！");
            }
            return instance;
        }
        private set { }
    }

    public int StealthLevel => stealthLevel;
    public float Satiety => satiety;

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
        // 初始化时刷新一次 UI
        UpdateAllUI();
        RefreshAllCrocodileStealth();
    }

    private void Update()
    {
        // 计时器累加（如果游戏需要）
        timer += Time.deltaTime;

        // 如果你希望每帧都刷新 UI（比如计时器一直在变），可以调用 UpdateAllUI()
        // 但为了性能，计时器可以单独每帧更新，其他数值只有变化时更新。
        // 这里示例：每帧更新时间，其他数值只有变化时才刷新（通过外部调用）。
        UpdateTimerUI();
    }

    // ---------- UI 更新方法 ----------
    /// <summary> 刷新所有 UI 文本（耗时操作，建议只在数值变化时调用） </summary>
    public void UpdateAllUI()
    {
        UpdateTimerUI();
        UpdateWildebeestUI();
        UpdateMoneyUI();
        UpdateCrocodileUI();
        UpdateSatietyUI();
        UpdateStealthLevelUI();
    }

    private void UpdateTimerUI()
    {
        if (debugTimer != null)
            debugTimer.text = "Time: " + timer.ToString("F2"); // 显示两位小数
    }

    private void UpdateWildebeestUI()
    {
        if (wildeBeestCount_Text != null)
            wildeBeestCount_Text.text = "Wildebeest: " + wildeBeestCount.ToString("F0"); // 整数显示（无小数）
    }

    private void UpdateMoneyUI()
    {
        if (moneyValue_Text != null)
            moneyValue_Text.text = "$ " + moneyValue.ToString("F2"); // 货币显示两位小数，带 $ 符号
    }

    private void UpdateCrocodileUI()
    {
        if (crocodileCount_Text != null)
            crocodileCount_Text.text = "Crocodiles: " + crocodileCount.ToString("F0");
    }

    private void UpdateSatietyUI()
    {
        if (satiety_Text != null)
            satiety_Text.text = "Satiety: " + satiety.ToString("F0");
    }

    private void UpdateStealthLevelUI()
    {
        if (stealthLevel_Text != null)
            stealthLevel_Text.text = "Stealth Lv: " + stealthLevel.ToString();
    }

    // ---------- 便捷方法：修改数值并自动刷新对应 UI ----------
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

    public void AddSatiety(float amount)
    {
        satiety += amount;
        if (satiety < 0f) satiety = 0f;
        UpdateSatietyUI();
    }

    /// <summary>
    /// 按钮入口：消耗饱腹感，提升全局 StealthLevel，并刷新所有鳄鱼 stealthValue / 外观。
    /// </summary>
    public bool UpgradeStealth()
    {
        if (maxStealthLevel > 0 && stealthLevel >= maxStealthLevel)
        {
            Debug.Log("GameManager: StealthLevel 已达上限。");
            return false;
        }

        if (satiety < stealthUpgradeCost)
        {
            Debug.Log("GameManager: 饱腹感不足，无法升级隐蔽。");
            return false;
        }

        satiety -= stealthUpgradeCost;
        stealthLevel++;
        UpdateSatietyUI();
        UpdateStealthLevelUI();
        RefreshAllCrocodileStealth();
        return true;
    }
    public void UpgradeStealthLevel()
    {
        UpgradeStealth();
    }

    /// <summary>按当前 stealthLevel 刷新场景中所有鳄鱼。</summary>
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

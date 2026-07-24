using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ---------- UI 引用 ----------
    public TMP_Text debugTimer;
    public TMP_Text wildeBeestCount_Text;
    public TMP_Text moneyValue_Text;
    public TMP_Text crocodileCount_Text;

    // ---------- 数值字段 ----------
    public float wildeBeestCount;
    public float timer;
    public float moneyValue;
    public float crocodileCount;

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
    }

    private void Update()
    {
        // 计时器累加（如果游戏需要）
        timer += Time.deltaTime;

        // 如果你希望每帧都刷新 UI（比如计时器一直在变），可以调用 UpdateAllUI()
        // 但为了性能，计时器可以单独每帧更新，其他数值只在变化时更新。
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
    }

    public void UpdateTimerUI()
    {
        if (debugTimer != null)
            debugTimer.text = "Time: " + timer.ToString("F2"); // 显示两位小数
    }

    public void UpdateWildebeestUI()
    {
        if (wildeBeestCount_Text != null)
            wildeBeestCount_Text.text = "Wildebeest: " + wildeBeestCount.ToString("F0"); // 整数显示（无小数）
    }

    public void UpdateMoneyUI()
    {
        if (moneyValue_Text != null)
            moneyValue_Text.text = "$ " + moneyValue.ToString("F2"); // 货币显示两位小数，带 $ 符号
    }

    public void UpdateCrocodileUI()
    {
        if (crocodileCount_Text != null)
            crocodileCount_Text.text = "Crocodiles: " + crocodileCount.ToString("F0");
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

    // 你也可以增加设置方法，比如 SetCrocodileCount(float value)
}
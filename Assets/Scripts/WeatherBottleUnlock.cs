using UnityEngine;

/// <summary>
/// CloudButton 升满后显示天气瓶子。
/// 挂法：挂在 CloudButton（或同面板）上；weatherBottleRoot 开局保持隐藏。
/// 注意：满级是最后一次 onUpgradeSuccess 之后 IsMaxUpgrade()==true，不是 onUpgradeMaxLevel。
/// </summary>
public class WeatherBottleUnlock : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("CloudButton 上的 MultiClickTrigger；空则同物体 GetComponent")]
    public MultiClickTrigger cloudTrigger;
    [Tooltip("天气瓶子根物体；开局应 SetActive(false)")]
    public GameObject weatherBottleRoot;
    [Tooltip("升满后可选隐藏的云按钮 UI 根")]
    public GameObject cloudUiToHide;

    private bool unlocked;

    private void Awake()
    {
        if (cloudTrigger == null)
            cloudTrigger = GetComponent<MultiClickTrigger>();
    }

    private void OnEnable()
    {
        if (cloudTrigger != null)
            cloudTrigger.onUpgradeSuccess.AddListener(HandleUpgradeSuccess);
    }

    private void Start()
    {
        // 场景里瓶子应默认隐藏；若运行时已满级则同步显示。
        SyncUnlockState();
    }

    private void OnDisable()
    {
        if (cloudTrigger != null)
            cloudTrigger.onUpgradeSuccess.RemoveListener(HandleUpgradeSuccess);
    }

    private void HandleUpgradeSuccess()
    {
        if (cloudTrigger != null && cloudTrigger.IsMaxUpgrade())
            UnlockBottle();
    }

    private void SyncUnlockState()
    {
        if (cloudTrigger != null && cloudTrigger.IsMaxUpgrade())
            UnlockBottle();
        else if (!unlocked && weatherBottleRoot != null)
            weatherBottleRoot.SetActive(false);
    }

    /// <summary>显示瓶子；可选隐藏云 UI。可重复调用，只生效一次。</summary>
    public void UnlockBottle()
    {
        if (unlocked) return;
        unlocked = true;

        if (weatherBottleRoot != null)
            weatherBottleRoot.SetActive(true);

        if (cloudUiToHide != null)
            cloudUiToHide.SetActive(false);
    }
}

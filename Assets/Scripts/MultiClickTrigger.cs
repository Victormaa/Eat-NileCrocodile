using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 多次点击才触发：累计按键，达到 TriggerCount 后调用 onTriggered 并清零。
/// Button OnClick 绑 OnPress()。
/// 后续可用 ReduceTriggerCount() 本地减少所需次数。
/// </summary>
public class MultiClickTrigger : MonoBehaviour
{
    [Header("次数配置")]
    [Tooltip("需要按几下才触发")]
    public int baseTriggerCount = 6;
    [Tooltip("次数下限，升级也不会低于此值")]
    public int minTriggerCount = 1;

    [Header("进度显示（可选）")]
    public TMP_Text progressText;
    [Tooltip("例如 {0}/{1} → 3/6")]
    public string progressFormat = "{0}/{1}";

    [Header("触发时调用")]
    public UnityEvent onTriggered;

    private int pressCount;

    public int PressCount => pressCount;

    /// <summary>当前实际需要按几下。</summary>
    public int TriggerCount => Mathf.Max(minTriggerCount, baseTriggerCount);

    void Start()
    {
        RefreshProgressUI();
    }

    /// <summary>绑到 Button OnClick：每按一次累加，够次数就触发并归零。</summary>
    public void OnPress()
    {
        pressCount++;
        RefreshProgressUI();

        if (pressCount >= TriggerCount)
        {
            pressCount = 0;
            RefreshProgressUI();
            onTriggered?.Invoke();
        }
    }

    /// <summary>手动清零计数（例如切场景、重置关卡）。</summary>
    public void ResetPressCount()
    {
        pressCount = 0;
        RefreshProgressUI();
    }

    /// <summary>
    /// 本地减少所需次数。
    /// 若当前已按次数已达到新阈值，会立刻触发并归零。
    /// </summary>
    public void ReduceTriggerCount(int amount = 1)
    {
        if (amount <= 0) return;

        baseTriggerCount = Mathf.Max(minTriggerCount, baseTriggerCount - amount);
        TryFlushIfReady();
        RefreshProgressUI();
    }

    /// <summary>外部改了阈值时调用，检查是否已该触发。</summary>
    public void TryFlushIfReady()
    {
        if (pressCount >= TriggerCount && TriggerCount > 0)
        {
            pressCount = 0;
            RefreshProgressUI();
            onTriggered?.Invoke();
        }
        else
        {
            RefreshProgressUI();
        }
    }

    public void RefreshProgressUI()
    {
        if (progressText == null) return;
        progressText.text = string.Format(progressFormat, pressCount, TriggerCount);
    }
}

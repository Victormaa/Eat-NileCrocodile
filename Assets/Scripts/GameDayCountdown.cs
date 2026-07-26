using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 独立天数倒计时：真实时长映射到游戏天数（如 20 分钟 → 60 天降到 0）。
/// 默认不自动开始，需调用 StartCountdown()。
/// </summary>
public class GameDayCountdown : MonoBehaviour
{
    private static GameDayCountdown instance;

    public static GameDayCountdown Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameDayCountdown>();
                if (instance == null)
                    Debug.LogError("场景里没有 GameDayCountdown 实例。");
            }
            return instance;
        }
    }

    [Header("时长映射")]
    [Tooltip("真实游戏时长（分钟），例如 20")]
    public float realDurationMinutes = 20f;
    [Tooltip("对应的游戏天数跨度，开局显示该值，结束为 0")]
    public int totalDays = 60;

    [Header("显示（可选）")]
    public TMP_Text dayText;
    [Tooltip("例如 Day: {0}")]
    public string dayFormat = "Day: {0}";

    [Header("开始")]
    [Tooltip("保持 false：需手动 StartCountdown()")]
    public bool playOnStart = false;

    [Header("事件")]
    public UnityEvent<int> onDayChanged;
    public UnityEvent onTimeUp;

    /// <summary>剩余整数天变化时回调。</summary>
    public event Action<int> OnDayChanged;

    /// <summary>倒计时到 0 时回调（只触发一次）。</summary>
    public event Action OnTimeUp;

    private float elapsed;
    private bool running;
    private bool finished;
    private int displayedDays = -1;

    public float RealDurationSeconds => Mathf.Max(0.01f, realDurationMinutes * 60f);
    public float ElapsedSeconds => elapsed;
    public float RemainingSeconds => Mathf.Max(0f, RealDurationSeconds - elapsed);
    public bool IsRunning => running;
    public bool IsFinished => finished;
    public int RemainingDays => ComputeRemainingDays();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        RefreshDayUI(force: true);
        if (playOnStart)
            StartCountdown();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (!running || finished)
            return;

        elapsed += Time.deltaTime;
        if (elapsed >= RealDurationSeconds)
        {
            elapsed = RealDurationSeconds;
            RefreshDayUI(force: true);
            FinishCountdown();
            return;
        }

        RefreshDayUI(force: false);
    }

    /// <summary>从头开始倒计时。</summary>
    public void StartCountdown()
    {
        elapsed = 0f;
        finished = false;
        running = true;
        displayedDays = -1;
        RefreshDayUI(force: true);
    }

    public void PauseCountdown()
    {
        running = false;
    }

    public void ResumeCountdown()
    {
        if (finished) return;
        running = true;
    }

    private void FinishCountdown()
    {
        if (finished) return;

        finished = true;
        running = false;
        OnTimeUp?.Invoke();
        onTimeUp?.Invoke();
    }

    private int ComputeRemainingDays()
    {
        if (totalDays <= 0)
            return 0;

        if (finished || elapsed >= RealDurationSeconds)
            return 0;

        float t = Mathf.Clamp01(elapsed / RealDurationSeconds);
        int remaining = Mathf.CeilToInt(totalDays * (1f - t));
        // t==0 时 CeilToInt(totalDays) == totalDays；极小进度仍可能是 totalDays
        return Mathf.Clamp(remaining, 0, totalDays);
    }

    private void RefreshDayUI(bool force)
    {
        int days = ComputeRemainingDays();
        if (!force && days == displayedDays)
            return;

        int previous = displayedDays;
        displayedDays = days;

        if (dayText != null)
            dayText.text = string.Format(dayFormat, days);

        if (previous != days && previous >= 0)
        {
            OnDayChanged?.Invoke(days);
            onDayChanged?.Invoke(days);
        }
    }
}

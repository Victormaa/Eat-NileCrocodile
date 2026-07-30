using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 轻量游戏事件总线：用字符串 ID 发信号，剧本 WaitPass 等可等待。
/// 场景挂一份即可；也可通过 Instance / Signal 静态入口。
/// </summary>
public class GameplayEventBus : MonoBehaviour
{
    public const string RainSuccess = "RainSuccess";
    public const string EatenReached = "EatenReached";

    private static GameplayEventBus instance;

    public static GameplayEventBus Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameplayEventBus>();
                if (instance == null)
                    Debug.LogError("场景里没有 GameplayEventBus 实例。");
            }
            return instance;
        }
    }

    /// <summary>任意事件被 Signal 时触发，参数为 eventId。</summary>
    public event Action<string> OnSignaled;

    private readonly HashSet<string> signaledOnce = new HashSet<string>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("GameplayEventBus: 场景中存在多个实例，保留先创建的。", this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>广播事件。可重复 Signal 同一 id（每次都会通知等待者）。</summary>
    public void Signal(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return;

        signaledOnce.Add(eventId);
        OnSignaled?.Invoke(eventId);
    }

    public static void SignalStatic(string eventId)
    {
        if (Instance != null)
            Instance.Signal(eventId);
    }

    /// <summary>是否曾经 Signal 过该 id（本局）。</summary>
    public bool HasSignaled(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && signaledOnce.Contains(eventId);
    }

    /// <summary>协程：等到指定 eventId 被 Signal（若已 Signal 过则立刻结束）。</summary>
    public IEnumerator WaitUntil(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
            yield break;

        if (signaledOnce.Contains(eventId))
            yield break;

        bool hit = false;
        void Handler(string id)
        {
            if (id == eventId)
                hit = true;
        }

        OnSignaled += Handler;
        while (!hit)
            yield return null;
        OnSignaled -= Handler;
    }

    /// <summary>清掉「已 Signal」记录（例如重开关卡时）。</summary>
    public void ClearHistory()
    {
        signaledOnce.Clear();
    }
}

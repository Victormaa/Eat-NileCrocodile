using System;
using UnityEngine;

/// <summary>
/// 复合剧本一步：可同时 Unlock + Tips，再可选 WaitSeconds 或 WaitPass。
/// Unlock 用 unlockIds，由 CompositeGamePlaySequence 上的 unlockBindings 映射到场景物体。
/// </summary>
[Serializable]
public class CompositePlayStep
{
    [Header("即时动作（可同时）")]
    [Tooltip("解锁 ID 列表；在 Sequence 的 Unlock Bindings 里映射到场景物体；空则跳过")]
    public string[] unlockIds;

    [TextArea]
    [Tooltip("提示文案；空则跳过")]
    public string tipMessage;
    [Tooltip("Tips 淡入后停留多久再进入等待/下一步")]
    public float tipDuration = 2.5f;

    [Header("等待（优先 waitSeconds；否则 WaitPass；都空则不等）")]
    [Tooltip(">0 则等待秒数（优先于 WaitPass）")]
    public float waitSeconds;
    [Tooltip("非空则 WaitPass：等 GameplayEventBus 信号。常用：RainSuccess / EatenReached")]
    public string waitPassEventId;
    [Tooltip("仅当 waitPassEventId 为 EatenReached 时：累计吃够该数量")]
    public int waitPassRequiredEaten = 1;
}

/// <summary>
/// 场景侧：把剧本里的 unlockId 映射到具体 GameObject。
/// </summary>
[Serializable]
public class SceneUnlockBinding
{
    public string unlockId;
    public GameObject[] objects;
}

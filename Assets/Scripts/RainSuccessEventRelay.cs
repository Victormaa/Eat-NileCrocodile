using UnityEngine;

/// <summary>
/// 把 RainHerdGoSequence.PlaySucceeded 转成 GameplayEventBus.Signal("RainSuccess")。
/// </summary>
public class RainSuccessEventRelay : MonoBehaviour
{
    public RainHerdGoSequence rainHerdGoSequence;
    public GameplayEventBus eventBus;

    [Tooltip("Signal 用的事件 ID，默认 RainSuccess")]
    public string eventId = GameplayEventBus.RainSuccess;

    private void Awake()
    {
        if (rainHerdGoSequence == null)
            rainHerdGoSequence = FindObjectOfType<RainHerdGoSequence>();
        if (eventBus == null)
            eventBus = FindObjectOfType<GameplayEventBus>();
    }

    private void OnEnable()
    {
        if (rainHerdGoSequence != null)
            rainHerdGoSequence.PlaySucceeded += HandlePlaySucceeded;
    }

    private void OnDisable()
    {
        if (rainHerdGoSequence != null)
            rainHerdGoSequence.PlaySucceeded -= HandlePlaySucceeded;
    }

    private void HandlePlaySucceeded()
    {
        if (eventBus == null)
            eventBus = GameplayEventBus.Instance;
        if (eventBus == null) return;

        eventBus.Signal(string.IsNullOrEmpty(eventId) ? GameplayEventBus.RainSuccess : eventId);
    }
}

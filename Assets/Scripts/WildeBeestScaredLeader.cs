using System;
using UnityEngine;

/// <summary>
/// 惊慌头马：跑到 stopX 后停下并通知 Manager。
/// 需与 WildeBeestBehavior 挂在同一物体（或父物体能取到 Behavior）。
/// </summary>
public class WildeBeestScaredLeader : MonoBehaviour
{
    private float stopX;
    private Action onArrived;
    private WildeBeestBehavior behavior;
    private bool hasArrived;

    void Awake()
    {
        behavior = GetComponent<WildeBeestBehavior>();
        if (behavior == null)
        {
            behavior = GetComponentInParent<WildeBeestBehavior>();
        }
    }

    public void Setup(float stopXPosition, Action arrivedCallback)
    {
        stopX = stopXPosition;
        onArrived = arrivedCallback;
        hasArrived = false;
    }

    void Update()
    {
        if (hasArrived || behavior == null) return;
        if (!behavior.CanMove) return;

        if (transform.position.x >= stopX)
        {
            Vector3 pos = transform.position;
            pos.x = stopX;
            transform.position = pos;

            hasArrived = true;
            behavior.SetCanMove(false);
            onArrived?.Invoke();
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// 移向目标点；带速度差、错峰出发与路径抖动，避免过于整齐。
/// </summary>
public class WildeBeestMoveToTarget : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float arriveThreshold = 0.05f;

    [Header("自然感")]
    [Tooltip("实际速度 = moveSpeed ± 该范围")]
    public float speedVariation = 1.5f;
    [Tooltip("出发前随机延迟上限（秒）")]
    public float maxStartDelay = 0.45f;
    [Tooltip("路径抖动幅度")]
    public float pathWobbleAmount = 0.35f;
    [Tooltip("路径抖动频率")]
    public float pathWobbleFrequency = 2.5f;

    private Transform target;
    private Action onArrived;
    private bool isMoving;
    private bool hasArrived;
    private WildeBeestBehavior behavior;

    private float actualSpeed;
    private float startDelay;
    private float delayElapsed;
    private float wobblePhase;
    private float wobbleFreqMul;

    void Awake()
    {
        behavior = GetComponent<WildeBeestBehavior>();
        if (behavior == null)
        {
            behavior = GetComponentInParent<WildeBeestBehavior>();
        }
    }

    public void Setup(Transform targetTransform, Action arrivedCallback)
    {
        target = targetTransform;
        onArrived = arrivedCallback;
        hasArrived = false;
        isMoving = false;

        actualSpeed = Mathf.Max(0.1f, moveSpeed + UnityEngine.Random.Range(-speedVariation, speedVariation));
        startDelay = UnityEngine.Random.Range(0f, Mathf.Max(0f, maxStartDelay));
        delayElapsed = 0f;
        wobblePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        wobbleFreqMul = UnityEngine.Random.Range(0.7f, 1.4f);
    }

    public void StartMoving()
    {
        if (target == null || hasArrived) return;
        isMoving = true;
        delayElapsed = 0f;

        if (behavior != null)
        {
            behavior.SetCanMove(false);
        }
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    void Update()
    {
        if (!isMoving || hasArrived || target == null) return;

        if (delayElapsed < startDelay)
        {
            delayElapsed += Time.deltaTime;
            return;
        }

        Vector3 destination = target.position;
        float dist = Vector3.Distance(transform.position, destination);

        // 接近终点时减弱抖动，保证能停进阵型点
        float wobbleScale = Mathf.Clamp01(dist / 1.5f);
        Vector3 toTarget = destination - transform.position;
        Vector3 perp = Vector3.zero;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = toTarget.normalized;
            perp = new Vector3(-dir.y, dir.x, 0f);
        }

        float wobble = Mathf.Sin(Time.time * pathWobbleFrequency * wobbleFreqMul + wobblePhase)
                       * pathWobbleAmount
                       * wobbleScale;
        Vector3 seekPoint = destination + perp * wobble;

        transform.position = Vector3.MoveTowards(
            transform.position,
            seekPoint,
            actualSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, destination) <= arriveThreshold)
        {
            transform.position = destination;
            hasArrived = true;
            isMoving = false;

            if (behavior != null)
            {
                behavior.SetCanMove(false);
            }

            onArrived?.Invoke();
        }
    }
}

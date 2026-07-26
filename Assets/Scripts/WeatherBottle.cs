using System.Collections;
using UnityEngine;

/// <summary>
/// 天气瓶子（2D Sprite）：闲置轻微漂浮；TriggerRain() 震动后调 RainHerdGoSequence.Play()。
/// 解锁显示后可自动：角马前排到位（ScaredLineReady）时自动 TriggerRain。
/// </summary>
public class WeatherBottle : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("空则运行时 FindObjectOfType")]
    public RainHerdGoSequence rainSequence;
    [Tooltip("空则从 rainSequence.herdManager 或 Find")]
    public WildeBeestHerdManager herdManager;

    [Header("自动触发")]
    [Tooltip("瓶子可见时，角马前排到位自动震动并下雨")]
    public bool autoTriggerOnScaredReady = true;

    [Header("闲置漂浮")]
    public float floatAmplitude = 0.08f;
    public float floatFrequency = 1.2f;
    [Tooltip("水平漂浮相对垂直的比例，0 = 只上下漂")]
    public float floatHorizontalScale = 0.35f;

    [Header("触发震动")]
    public float shakeDuration = 0.35f;
    public float shakeAmplitude = 0.12f;
    public int shakeOscillations = 10;

    private Vector3 baseLocalPos;
    private bool hasBasePos;
    private bool floating = true;
    private bool busy;
    private bool subscribed;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        CacheBasePos();
        ResolveRefs();
    }

    private void OnEnable()
    {
        CacheBasePos();
        floating = true;
        busy = false;
        ResolveRefs();
        SubscribeHerd();

        // 升满瞬间若角马已站稳，立刻下一次雨
        if (autoTriggerOnScaredReady
            && herdManager != null
            && herdManager.IsScaredLineReady
            && (rainSequence == null || !rainSequence.IsPlaying))
        {
            TriggerRain();
        }
    }

    private void OnDisable()
    {
        UnsubscribeHerd();
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        busy = false;
        ResetToBasePos();
    }

    private void Update()
    {
        if (!floating || busy) return;

        float t = Time.time * floatFrequency;
        float y = Mathf.Sin(t) * floatAmplitude;
        float x = Mathf.Sin(t * 0.7f + 1.3f) * floatAmplitude * floatHorizontalScale;
        transform.localPosition = baseLocalPos + new Vector3(x, y, 0f);
    }

    /// <summary>
    /// 震动一下后调用 RainHerdGoSequence.Play()。
    /// 点选或自动到位均可调用。
    /// </summary>
    public void TriggerRain()
    {
        if (busy) return;
        if (rainSequence != null && rainSequence.IsPlaying) return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeThenPlay());
    }

    private void HandleScaredLineReady()
    {
        if (!autoTriggerOnScaredReady) return;
        if (!isActiveAndEnabled) return;
        TriggerRain();
    }

    private IEnumerator ShakeThenPlay()
    {
        busy = true;
        floating = false;
        CacheBasePos();

        float duration = Mathf.Max(0.01f, shakeDuration);
        int waves = Mathf.Max(1, shakeOscillations);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float damp = 1f - u;
            float angle = u * waves * Mathf.PI * 2f;
            float ox = Mathf.Sin(angle * 1.7f) * shakeAmplitude * damp;
            float oy = Mathf.Cos(angle * 2.1f) * shakeAmplitude * damp * 0.85f;
            transform.localPosition = baseLocalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }

        ResetToBasePos();

        ResolveRefs();

        if (rainSequence != null)
            rainSequence.Play();
        else
            Debug.LogWarning("WeatherBottle: RainHerdGoSequence 未找到。", this);

        shakeRoutine = null;
        busy = false;
        floating = true;
    }

    private void ResolveRefs()
    {
        if (rainSequence == null)
            rainSequence = FindObjectOfType<RainHerdGoSequence>();

        if (herdManager == null && rainSequence != null)
            herdManager = rainSequence.herdManager;

        if (herdManager == null)
            herdManager = FindObjectOfType<WildeBeestHerdManager>();
    }

    private void SubscribeHerd()
    {
        if (subscribed || herdManager == null) return;
        herdManager.ScaredLineReady += HandleScaredLineReady;
        subscribed = true;
    }

    private void UnsubscribeHerd()
    {
        if (!subscribed || herdManager == null) return;
        herdManager.ScaredLineReady -= HandleScaredLineReady;
        subscribed = false;
    }

    private void CacheBasePos()
    {
        if (!hasBasePos)
        {
            baseLocalPos = transform.localPosition;
            hasBasePos = true;
        }
    }

    private void ResetToBasePos()
    {
        if (hasBasePos)
            transform.localPosition = baseLocalPos;
    }
}

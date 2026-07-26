using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 天数切换时，将 Volume.weight 做 0→1→0 脉冲，配合 Profile 里的变暗效果闪一下夜色。
/// </summary>
public class DayChangeVolumePulse : MonoBehaviour
{
    [Header("引用")]
    public Volume volume;
    public GameDayCountdown dayCountdown;

    [Header("脉冲时长")]
    public float fadeIn = 0.25f;
    public float hold = 0.05f;
    public float fadeOut = 0.35f;
    [Range(0f, 1f)]
    public float peakWeight = 1f;

    private Coroutine pulseRoutine;
    private bool subscribed;

    private void Awake()
    {
        if (volume == null)
            volume = GetComponent<Volume>();
        if (volume != null)
            volume.weight = 0f;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        if (volume != null)
            volume.weight = 0f;
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
        if (volume != null)
            volume.weight = 0f;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        if (dayCountdown == null)
            dayCountdown = FindObjectOfType<GameDayCountdown>();

        if (dayCountdown == null) return;

        dayCountdown.OnDayChanged += HandleDayChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || dayCountdown == null) return;
        dayCountdown.OnDayChanged -= HandleDayChanged;
        subscribed = false;
    }

    private void HandleDayChanged(int remainingDays)
    {
        PlayPulse();
    }

    /// <summary>手动触发一次天黑闪烁（也可绑到 UnityEvent）。</summary>
    public void PlayPulse()
    {
        if (volume == null) return;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float startWeight = volume.weight;
        float peak = Mathf.Clamp01(peakWeight);

        yield return LerpWeight(startWeight, peak, fadeIn);

        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        yield return LerpWeight(volume.weight, 0f, fadeOut);

        volume.weight = 0f;
        pulseRoutine = null;
    }

    private IEnumerator LerpWeight(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            volume.weight = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // smoothstep
            t = t * t * (3f - 2f * t);
            volume.weight = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }

        volume.weight = to;
    }
}

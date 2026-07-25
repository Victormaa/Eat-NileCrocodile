using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表情包气泡：子物体配置固定几种表情，用 string 标识调用 Show("Scarred")。
/// 显示时激活对应子物体并关闭其他；scale 0→1 显示，1→0 隐藏；显示期间间歇轻微抖动。
/// </summary>
public class EmoteBubble : MonoBehaviour
{
    [Serializable]
    public class EmoteEntry
    {
        [Tooltip("调用 Show 时使用的标识，如 Scarred")]
        public string id;
        [Tooltip("该表情对应的子物体")]
        public GameObject root;
    }

    [Header("表情配置")]
    public List<EmoteEntry> emotes = new List<EmoteEntry>();

    [Header("缩放显示 / 隐藏")]
    public float showDuration = 0.25f;
    public float hideDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("间歇抖动")]
    [Tooltip("两次抖动之间的最短间隔（秒）")]
    public float shakeIntervalMin = 1.2f;
    [Tooltip("两次抖动之间的最长间隔（秒）")]
    public float shakeIntervalMax = 2.5f;
    public float shakeDuration = 0.18f;
    public float shakeAmount = 0.04f;
    public float shakeFrequency = 45f;

    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPos;
    private bool isVisible;
    private bool isAnimating;
    private string currentEmoteId;
    private Coroutine scaleRoutine;
    private Coroutine shakeRoutine;

    public bool IsVisible => isVisible;
    public bool IsAnimating => isAnimating;
    public string CurrentEmoteId => currentEmoteId;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale == Vector3.zero)
        {
            baseScale = Vector3.one;
        }

        baseLocalPos = transform.localPosition;
        transform.localScale = Vector3.zero;
        DeactivateAllEmotes();
        isVisible = false;
        currentEmoteId = null;
    }

    /// <summary>
    /// 按标识显示表情：激活对应子物体、关闭其他，然后 scale 0→1。
    /// </summary>
    public void Show(string emoteId)
    {
        if (string.IsNullOrEmpty(emoteId))
        {
            Debug.LogWarning($"EmoteBubble on {name}: emoteId 为空。", this);
            return;
        }

        if (!ActivateEmote(emoteId))
        {
            Debug.LogWarning($"EmoteBubble on {name}: 未找到标识 \"{emoteId}\"。", this);
            return;
        }

        currentEmoteId = emoteId;

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        StopIdleShake();
        gameObject.SetActive(true);
        scaleRoutine = StartCoroutine(ScaleRoutine(0f, 1f, showDuration, onComplete: StartIdleShake));
        isVisible = true;
    }

    /// <summary>scale 从当前缩放到 0 隐藏，并停止抖动。</summary>
    public void Hide()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        StopIdleShake();
        float from = GetCurrentScaleFactor();
        scaleRoutine = StartCoroutine(ScaleRoutine(from, 0f, hideDuration, onComplete: OnHidden));
        isVisible = false;
    }

    /// <summary>立即隐藏，无动画。</summary>
    public void HideImmediate()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        StopIdleShake();
        transform.localScale = Vector3.zero;
        transform.localPosition = baseLocalPos;
        DeactivateAllEmotes();
        isVisible = false;
        isAnimating = false;
        currentEmoteId = null;
    }

    private bool ActivateEmote(string emoteId)
    {
        EmoteEntry matched = null;
        for (int i = 0; i < emotes.Count; i++)
        {
            EmoteEntry entry = emotes[i];
            if (entry == null) continue;

            bool isMatch = string.Equals(entry.id, emoteId, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                matched = entry;
            }

            if (entry.root != null)
            {
                entry.root.SetActive(isMatch);
            }
        }

        return matched != null && matched.root != null;
    }

    private void DeactivateAllEmotes()
    {
        for (int i = 0; i < emotes.Count; i++)
        {
            EmoteEntry entry = emotes[i];
            if (entry != null && entry.root != null)
            {
                entry.root.SetActive(false);
            }
        }
    }

    private IEnumerator ScaleRoutine(float from, float to, float duration, Action onComplete)
    {
        isAnimating = true;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            ApplyScale(to);
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curved = scaleCurve != null && scaleCurve.length > 0
                    ? scaleCurve.Evaluate(t)
                    : t;
                ApplyScale(Mathf.LerpUnclamped(from, to, curved));
                yield return null;
            }
        }

        ApplyScale(to);
        isAnimating = false;
        scaleRoutine = null;
        onComplete?.Invoke();
    }

    private void StartIdleShake()
    {
        StopIdleShake();
        baseLocalPos = transform.localPosition;
        shakeRoutine = StartCoroutine(IdleShakeLoop());
    }

    private void StopIdleShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        transform.localPosition = baseLocalPos;
    }

    private IEnumerator IdleShakeLoop()
    {
        while (isVisible)
        {
            float wait = UnityEngine.Random.Range(shakeIntervalMin, shakeIntervalMax);
            yield return new WaitForSeconds(wait);

            if (!isVisible || isAnimating)
            {
                continue;
            }

            yield return ShakeOnce();
        }

        shakeRoutine = null;
    }

    private IEnumerator ShakeOnce()
    {
        Vector3 origin = baseLocalPos;
        float elapsed = 0f;

        while (elapsed < shakeDuration && isVisible && !isAnimating)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float ox = Mathf.Sin(elapsed * shakeFrequency) * shakeAmount * damper;
            float oy = Mathf.Cos(elapsed * shakeFrequency * 1.3f) * shakeAmount * damper * 0.6f;
            transform.localPosition = origin + new Vector3(ox, oy, 0f);
            yield return null;
        }

        transform.localPosition = origin;
    }

    private void OnHidden()
    {
        transform.localPosition = baseLocalPos;
        DeactivateAllEmotes();
        currentEmoteId = null;
    }

    private void ApplyScale(float factor)
    {
        transform.localScale = baseScale * factor;
    }

    private float GetCurrentScaleFactor()
    {
        float denom = Mathf.Abs(baseScale.x) > 0.0001f ? baseScale.x : 1f;
        return transform.localScale.x / denom;
    }

    void OnDisable()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        StopIdleShake();
        isAnimating = false;
    }
}

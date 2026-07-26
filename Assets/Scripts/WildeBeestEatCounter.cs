using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 独立计数：累计成功吃掉的角马数量。不进 GameManager。
/// </summary>
public class WildeBeestEatCounter : MonoBehaviour
{
    private static WildeBeestEatCounter instance;

    public static WildeBeestEatCounter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<WildeBeestEatCounter>();
                if (instance == null)
                    Debug.LogError("场景里没有 WildeBeestEatCounter 实例。");
            }
            return instance;
        }
    }

    [SerializeField]
    private int count;

    [Header("显示（可选）")]
    public TMP_Text countText;
    [Tooltip("例如 Eaten: {0}")]
    public string countFormat = "{0}";

    [Header("增加时跳动（可选）")]
    [Tooltip("峰值缩放倍数")]
    public float bounceScale = 1.2f;
    [Tooltip("上跳像素（RectTransform）；0 则只缩放")]
    public float bounceUpPixels = 10f;
    [Tooltip("整段跳动时长（秒）")]
    public float bounceDuration = 0.22f;

    /// <summary>吃掉数量变化时回调，参数为当前累计数量。</summary>
    public event Action<int> OnEatenChanged;

    public int Count => count;

    private RectTransform countTextRect;
    private Vector3 countTextBaseScale = Vector3.one;
    private Vector2 countTextBasePos;
    private bool hasCountTextBase;
    private Coroutine bounceRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        CacheCountTextTransform();
    }

    private void Start()
    {
        RefreshCountUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>成功吃掉一只角马时调用。</summary>
    public void RegisterEat()
    {
        count++;
        RefreshCountUI();
        PlayCountBounce();
        OnEatenChanged?.Invoke(count);
    }

    public void RefreshCountUI()
    {
        if (countText == null) return;
        countText.text = string.Format(countFormat, count);
    }

    private void PlayCountBounce()
    {
        if (countText == null || bounceDuration <= 0f) return;

        CacheCountTextTransform();
        if (bounceRoutine != null)
            StopCoroutine(bounceRoutine);
        bounceRoutine = StartCoroutine(CountBounceRoutine());
    }

    private IEnumerator CountBounceRoutine()
    {
        Transform t = countText.transform;
        float half = bounceDuration * 0.5f;

        // 放大 + 上跳
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / half);
            float eased = 1f - (1f - u) * (1f - u);
            ApplyBounce(t, eased);
            yield return null;
        }

        // 回落
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / half);
            float eased = u * u;
            ApplyBounce(t, 1f - eased);
            yield return null;
        }

        ResetCountTextTransform(t);
        bounceRoutine = null;
    }

    private void ApplyBounce(Transform t, float amount)
    {
        float scale = Mathf.LerpUnclamped(1f, bounceScale, amount);
        t.localScale = countTextBaseScale * scale;

        if (countTextRect != null && bounceUpPixels != 0f)
            countTextRect.anchoredPosition = countTextBasePos + Vector2.up * (bounceUpPixels * amount);
    }

    private void ResetCountTextTransform(Transform t)
    {
        t.localScale = countTextBaseScale;
        if (countTextRect != null)
            countTextRect.anchoredPosition = countTextBasePos;
    }

    private void CacheCountTextTransform()
    {
        if (countText == null) return;

        if (!hasCountTextBase)
        {
            countTextRect = countText.rectTransform;
            countTextBaseScale = countText.transform.localScale;
            countTextBasePos = countTextRect.anchoredPosition;
            hasCountTextBase = true;
        }
    }
}

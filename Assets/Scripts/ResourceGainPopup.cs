using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单条资源获得飘字：设置图标与数量后上浮淡出并自毁。
/// </summary>
public class ResourceGainPopup : MonoBehaviour
{
    public Image icon;
    public TMP_Text amountText;
    public CanvasGroup canvasGroup;
    public float duration = 1f;
    public float floatDistance = 60f;

    private RectTransform rect;

    public RectTransform Rect
    {
        get
        {
            if (rect == null)
                rect = transform as RectTransform;
            return rect;
        }
    }

    public void Play(Sprite sprite, string label)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (amountText != null)
            amountText.text = label;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        if (Rect == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector2 start = Rect.anchoredPosition;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            Rect.anchoredPosition = start + Vector2.up * (floatDistance * u);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - u;
            yield return null;
        }

        Destroy(gameObject);
    }
}

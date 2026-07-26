using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在会产资源的道具上：在锚点附近生成资源图标 + 数量飘字。
/// UI 道具拖 spawnAnchor；世界物体（如鳄鱼）拖 worldAnchor。
/// </summary>
public class ResourceGainFeedback : MonoBehaviour
{
    [Header("飘字预制体")]
    public ResourceGainPopup popupPrefab;

    [Header("生成位置")]
    [Tooltip("UI 锚点（按钮旁）；优先使用")]
    public RectTransform spawnAnchor;
    [Tooltip("世界锚点（鳄鱼头顶等）；无 UI 锚点时使用")]
    public Transform worldAnchor;
    [Tooltip("飘字父节点；空则用锚点所在 Canvas 或场景主 Canvas")]
    public RectTransform spawnParent;
    public Vector2 spawnOffset = new Vector2(0f, 20f);
    public Vector2 randomJitter = new Vector2(12f, 8f);

    [Header("图标（与 UpgradeCostDisplay 一致）")]
    public Sprite moneyIcon;
    public Sprite crocodileFatIcon;
    public Sprite crocoFurIcon;

    [Header("文案 / 节奏")]
    [Tooltip("例如 +{0} → +3")]
    public string amountFormat = "+{0}";
    public string amountNumberFormat = "0.#";
    [Tooltip("多种产出时，相邻飘字间隔")]
    public float staggerDelay = 0.12f;

    /// <summary>显示单条资源获得。</summary>
    public void Show(UpgradeCostType type, float amount)
    {
        if (amount == 0f || popupPrefab == null)
            return;

        SpawnOne(type, amount, 0f);
    }

    /// <summary>按产出列表依次飘出（可错开）。</summary>
    public void Show(ResourceOutputEntry[] outputs)
    {
        if (outputs == null || popupPrefab == null)
            return;

        int shown = 0;
        for (int i = 0; i < outputs.Length; i++)
        {
            ResourceOutputEntry entry = outputs[i];
            if (entry == null || entry.amount == 0f)
                continue;

            float delay = shown * staggerDelay;
            SpawnOne(entry.type, entry.amount, delay);
            shown++;
        }
    }

    private void SpawnOne(UpgradeCostType type, float amount, float delay)
    {
        if (delay <= 0f)
        {
            CreatePopup(type, amount);
            return;
        }

        StartCoroutine(SpawnAfterDelay(type, amount, delay));
    }

    private IEnumerator SpawnAfterDelay(UpgradeCostType type, float amount, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreatePopup(type, amount);
    }

    private void CreatePopup(UpgradeCostType type, float amount)
    {
        RectTransform parent = ResolveParent();
        if (parent == null)
        {
            Debug.LogWarning("ResourceGainFeedback: 找不到 spawnParent / Canvas。", this);
            return;
        }

        ResourceGainPopup popup = Instantiate(popupPrefab, parent);
        RectTransform popupRect = popup.Rect;
        if (popupRect != null)
        {
            popupRect.SetAsLastSibling();
            PlacePopup(popupRect, parent);
        }

        string label = string.Format(amountFormat, amount.ToString(amountNumberFormat));
        popup.Play(GetIcon(type), label);
    }

    private void PlacePopup(RectTransform popupRect, RectTransform parent)
    {
        Vector2 jitter = new Vector2(
            Random.Range(-randomJitter.x, randomJitter.x),
            Random.Range(-randomJitter.y, randomJitter.y));

        if (spawnAnchor != null)
        {
            popupRect.position = spawnAnchor.position;
            popupRect.anchoredPosition += spawnOffset + jitter;
            return;
        }

        Transform world = worldAnchor != null ? worldAnchor : transform;
        Camera cam = Camera.main;
        if (cam == null)
        {
            popupRect.anchoredPosition = spawnOffset + jitter;
            return;
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world.position);
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : cam;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCam, out Vector2 local))
        {
            popupRect.anchoredPosition = local + spawnOffset + jitter;
        }
        else
        {
            popupRect.anchoredPosition = spawnOffset + jitter;
        }
    }

    private RectTransform ResolveParent()
    {
        if (spawnParent != null)
            return spawnParent;

        if (spawnAnchor != null)
        {
            Canvas canvas = spawnAnchor.GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas.transform as RectTransform;
        }

        Canvas any = FindObjectOfType<Canvas>();
        return any != null ? any.transform as RectTransform : null;
    }

    private Sprite GetIcon(UpgradeCostType type)
    {
        switch (type)
        {
            case UpgradeCostType.Money:
                return moneyIcon;
            case UpgradeCostType.CrocodileFat:
                return crocodileFatIcon;
            case UpgradeCostType.CrocoFur:
                return crocoFurIcon;
            default:
                return null;
        }
    }
}

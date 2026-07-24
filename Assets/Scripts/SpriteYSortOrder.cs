using UnityEngine;

/// <summary>
/// 根据 Y 轴位置自动设置 SpriteRenderer 的 sortingOrder，
/// Y 越小（越靠下）越靠前显示，遮挡关系正确。
/// </summary>
[DisallowMultipleComponent]
public class SpriteYSortOrder : MonoBehaviour
{
    [Tooltip("在计算出的 order 上再加的偏移，用于同 Y 时微调前后")]
    public int sortingOrderOffset = 0;

    [Tooltip("Y 坐标乘以该值再取整。越大，微小高度差越能拉开前后层")]
    public float precision = 100f;

    [Tooltip("是否包含子物体上的 SpriteRenderer")]
    public bool includeChildren = true;

    private SpriteRenderer[] renderers;

    void Awake()
    {
        CacheRenderers();
    }

    void LateUpdate()
    {
        if (renderers == null || renderers.Length == 0)
        {
            CacheRenderers();
            if (renderers == null || renderers.Length == 0) return;
        }

        // Y 越小 → sortingOrder 越大 → 画在更前面
        int order = sortingOrderOffset - Mathf.RoundToInt(transform.position.y * precision);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sortingOrder = order;
            }
        }
    }

    private void CacheRenderers()
    {
        if (includeChildren)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            SpriteRenderer self = GetComponent<SpriteRenderer>();
            renderers = self != null ? new[] { self } : System.Array.Empty<SpriteRenderer>();
        }
    }
}

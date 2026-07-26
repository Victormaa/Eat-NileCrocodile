using UnityEngine;

/// <summary>
/// 角马可被 GameCursor 点选；点击时触发停震→慢行。
/// SortOrder 与 SpriteYSortOrder 一致：Y 越小越靠前、越优先被点到。
/// </summary>
public class WildeBeestInteractable : Interactable
{
    [Tooltip("与 SpriteYSortOrder.precision 保持一致")]
    public float sortPrecision = 100f;

    private WildeBeestBehavior behavior;

    protected override void Start()
    {
        base.Start();
        behavior = GetComponent<WildeBeestBehavior>();
        if (behavior == null)
        {
            behavior = GetComponentInParent<WildeBeestBehavior>();
        }
    }

    public override void ManagedUpdate()
    {
        // 与 SpriteYSortOrder 相同公式：Y 越小 → order 越大 → 越优先点击
        SortOrderAdjustment = -Mathf.RoundToInt(transform.position.y * sortPrecision);
    }

    protected override void OnCursorSelectStart()
    {
        if (behavior != null)
        {
            behavior.TryStunFromClick();
        }
    }
}

using UnityEngine;

/// <summary>
/// 角马可被 GameCursor 点选；点击时触发停震→慢行。
/// </summary>
public class WildeBeestInteractable : Interactable
{
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

    protected override void OnCursorSelectStart()
    {
        if (behavior != null)
        {
            behavior.TryStunFromClick();
        }
    }
}

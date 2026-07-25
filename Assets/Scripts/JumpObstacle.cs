using UnityEngine;

/// <summary>
/// 障碍物触发器：角马碰到后调用起跳。
/// 需挂 Collider2D，并勾选 Is Trigger。
/// </summary>
public class JumpObstacle : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        WildeBeestBehavior wildebeest = other.GetComponent<WildeBeestBehavior>();
        if (wildebeest == null)
        {
            wildebeest = other.GetComponentInParent<WildeBeestBehavior>();
        }

        if (wildebeest == null || wildebeest.IsCaught) return;

        wildebeest.JumpObstacle();
    }
}

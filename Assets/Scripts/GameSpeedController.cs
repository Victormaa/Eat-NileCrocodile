using UnityEngine;

/// <summary>
/// 整体加速：按住按键加速，松开恢复正常速度（Time.timeScale）。
/// 按钮可绑 SetFastSpeed / SetNormalSpeed（例如 Pointer Down / Up）。
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Tooltip("正常速度")]
    public float normalSpeed = 1f;
    [Tooltip("加速后的倍速")]
    public float fastSpeed = 2f;
    [Tooltip("按住加速的按键")]
    public KeyCode holdKey = KeyCode.Space;

    private bool isFast;

    public bool IsFast => isFast;

    void Update()
    {
        if (Input.GetKey(holdKey))
        {
            if (!isFast)
            {
                SetFastSpeed();
            }
        }
        else if (Input.GetKeyUp(holdKey))
        {
            SetNormalSpeed();
        }
    }

    /// <summary>设为正常速度。</summary>
    public void SetNormalSpeed()
    {
        isFast = false;
        Time.timeScale = normalSpeed;
    }

    /// <summary>设为加速。</summary>
    public void SetFastSpeed()
    {
        isFast = true;
        Time.timeScale = fastSpeed;
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
        isFast = false;
    }
}

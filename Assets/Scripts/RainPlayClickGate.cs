using UnityEngine;

/// <summary>
/// CloudButton 点击门禁：角马未就绪或序列播放中时播拒绝提示，不累加 MultiClickTrigger 次数。
/// Button OnClick 绑本脚本 OnPress，再转发到 MultiClickTrigger.OnPress。
/// </summary>
public class RainPlayClickGate : MonoBehaviour
{
    public RainHerdGoSequence sequence;
    public MultiClickTrigger clickTrigger;

    void Awake()
    {
        if (clickTrigger == null)
            clickTrigger = GetComponent<MultiClickTrigger>();
    }

    /// <summary>绑到 Trigger Button OnClick。</summary>
    public void OnPress()
    {
        if (sequence != null && !sequence.CanPlay)
        {
            sequence.PlayDenyFeedback();
            return;
        }

        if (clickTrigger == null)
        {
            Debug.LogWarning("RainPlayClickGate: clickTrigger 未赋值。", this);
            return;
        }

        clickTrigger.OnPress();
    }
}

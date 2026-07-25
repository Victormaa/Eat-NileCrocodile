using System;
using UnityEngine;

/// <summary>
/// 惊慌头马标记；实际跑向目标由 WildeBeestMoveToTarget 负责。
/// 保留此组件以便 HeadScared 预制体识别。
/// </summary>
public class WildeBeestScaredLeader : MonoBehaviour
{
    public EmoteBubble emote;

    public void ShowEmote(string emoteName)
    {
        if (emote == null) return;
        emote.Show(emoteName);
    }

    public void HideEmote()
    {
        if (emote == null) return;
        emote.Hide();
    }
}

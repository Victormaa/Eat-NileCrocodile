using UnityEngine;

/// <summary>
/// 惊慌头马：表情显示；跑到屏幕右侧时自动隐藏表情。
/// </summary>
public class WildeBeestScaredLeader : MonoBehaviour
{
    public EmoteBubble emote;
    [Tooltip("超过该 X 时隐藏表情")]
    public float hideEmoteX = 12f;

    private bool emoteVisible;

    public void ShowEmote(string emoteName)
    {
        if (emote == null) return;
        emote.Show(emoteName);
        emoteVisible = true;
    }

    public void HideEmote()
    {
        if (emote == null) return;
        emote.Hide();
        emoteVisible = false;
    }

    void Update()
    {
        if (!emoteVisible) return;

        if (transform.position.x > hideEmoteX)
        {
            HideEmote();
        }
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 导演：下雨 → 稍等 → 头马 Emote "GO" → 再稍等 → StartMovingOn + SpawnFollowerHerd。
/// 外部入口请调 Play()，而不是单独调 RainProp.PlayRainProp()。
/// 未就绪（Scared 前排未站稳）时只播拒绝音效与提示，不推进序列。
/// </summary>
public class RainHerdGoSequence : MonoBehaviour
{
    [Header("引用")]
    public RainProp rainProp;
    public WildeBeestHerdManager herdManager;

    [Header("延迟")]
    [Tooltip("下雨后，多久显示 GO 表情")]
    public float delayBeforeGoEmote = 1f;
    [Tooltip("显示 GO 后，多久开始羊群出发")]
    public float delayBeforeMove = 0.5f;

    [Header("音效 - 下雨开始")]
    [Tooltip("需与 Resources/Audios/SFXs 中 clip 名一致；空则跳过")]
    public string rainStartSoundId;
    public float rainStartSoundVolume = 1f;

    [Header("音效 - GO 表情")]
    public string goEmoteSoundId;
    public float goEmoteSoundVolume = 1f;

    [Header("音效 - 羊群出发")]
    public string herdStartSoundId;
    public float herdStartSoundVolume = 1f;

    [Header("拒绝反馈（未站稳 / 无 Scared 角马）")]
    [Tooltip("需与 Resources/Audios/SFXs 中 clip 名一致；空则跳过")]
    public string denySoundId;
    public float denySoundVolume = 1f;
    public TMP_Text denyHintText;
    [TextArea]
    public string denyHintMessage = "没有 Scared 角马，请等它们站稳后再试";
    [Tooltip("完全可见后停留多久")]
    public float denyHintDuration = 2f;
    public float denyHintFadeInDuration = 0.25f;
    public float denyHintFadeOutDuration = 0.25f;
    [Tooltip("上浮 / 下浮的像素距离")]
    public float denyHintFloatDistance = 30f;

    [Header("表情")]
    public string goEmoteId = "GO";

    private Coroutine runningRoutine;
    private Coroutine denyHintRoutine;
    private RectTransform denyHintRect;
    private Vector2 denyHintBasePos;
    private bool hasDenyHintBasePos;
    private Color denyHintBaseColor;

    /// <summary>真正开始下雨序列时触发（拒绝 / 已在播放时不触发）。</summary>
    public event Action PlaySucceeded;

    /// <summary>公开入口：播放完整雨 → GO → 出发序列。</summary>
    public void Play()
    {
        if (runningRoutine != null)
        {
            return;
        }

        if (herdManager == null || !herdManager.IsScaredLineReady)
        {
            PlayDenyFeedback();
            return;
        }

        runningRoutine = StartCoroutine(PlaySequence());
        PlaySucceeded?.Invoke();
    }

    private void PlayDenyFeedback()
    {
        PlayOneShot(denySoundId, denySoundVolume);
        ShowDenyHint();
    }

    private void ShowDenyHint()
    {
        if (denyHintText == null) return;

        if (denyHintRoutine != null)
        {
            StopCoroutine(denyHintRoutine);
            denyHintRoutine = null;
            ResetDenyHintVisual();
        }

        denyHintRoutine = StartCoroutine(DenyHintRoutine());
    }

    private IEnumerator DenyHintRoutine()
    {
        CacheDenyHintBase();

        denyHintText.text = denyHintMessage;
        denyHintText.gameObject.SetActive(true);

        Vector2 startPos = denyHintBasePos + Vector2.down * denyHintFloatDistance;
        Vector2 endPos = denyHintBasePos;

        // 上浮淡入
        yield return AnimateDenyHint(
            startPos,
            endPos,
            0f,
            denyHintBaseColor.a,
            denyHintFadeInDuration
        );

        if (denyHintDuration > 0f)
        {
            yield return new WaitForSeconds(denyHintDuration);
        }

        // 下浮淡出
        Vector2 exitPos = denyHintBasePos + Vector2.down * denyHintFloatDistance;
        yield return AnimateDenyHint(
            endPos,
            exitPos,
            denyHintBaseColor.a,
            0f,
            denyHintFadeOutDuration
        );

        denyHintText.gameObject.SetActive(false);
        ResetDenyHintVisual();
        denyHintRoutine = null;
    }

    private IEnumerator AnimateDenyHint(
        Vector2 fromPos,
        Vector2 toPos,
        float fromAlpha,
        float toAlpha,
        float duration
    )
    {
        if (duration <= 0f)
        {
            SetDenyHintPos(toPos);
            SetDenyHintAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDenyHintPos(Vector2.LerpUnclamped(fromPos, toPos, t));
            SetDenyHintAlpha(Mathf.LerpUnclamped(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetDenyHintPos(toPos);
        SetDenyHintAlpha(toAlpha);
    }

    private void CacheDenyHintBase()
    {
        if (denyHintRect == null)
        {
            denyHintRect = denyHintText.rectTransform;
        }

        if (!hasDenyHintBasePos)
        {
            denyHintBasePos = denyHintRect.anchoredPosition;
            denyHintBaseColor = denyHintText.color;
            hasDenyHintBasePos = true;
        }
    }

    private void ResetDenyHintVisual()
    {
        if (denyHintText == null) return;
        CacheDenyHintBase();
        SetDenyHintPos(denyHintBasePos);
        SetDenyHintAlpha(0f);
    }

    private void SetDenyHintPos(Vector2 pos)
    {
        if (denyHintRect != null)
        {
            denyHintRect.anchoredPosition = pos;
        }
    }

    private void SetDenyHintAlpha(float alpha)
    {
        Color c = denyHintBaseColor;
        c.a = alpha;
        denyHintText.color = c;
    }

    private IEnumerator PlaySequence()
    {
        if (rainProp != null)
        {
            rainProp.PlayRainProp();
        }
        else
        {
            Debug.LogWarning("RainHerdGoSequence: rainProp 未赋值。", this);
        }

        PlayOneShot(rainStartSoundId, rainStartSoundVolume);

        if (delayBeforeGoEmote > 0f)
        {
            yield return new WaitForSeconds(delayBeforeGoEmote);
        }

        ShowLeaderGoEmote();
        PlayOneShot(goEmoteSoundId, goEmoteSoundVolume);

        if (delayBeforeMove > 0f)
        {
            yield return new WaitForSeconds(delayBeforeMove);
        }

        if (herdManager != null)
        {
            herdManager.StartMovingOn();
            herdManager.SpawnFollowerHerd();
        }
        else
        {
            Debug.LogWarning("RainHerdGoSequence: herdManager 未赋值。", this);
        }

        PlayOneShot(herdStartSoundId, herdStartSoundVolume);
        runningRoutine = null;
    }

    private void ShowLeaderGoEmote()
    {
        if (herdManager == null)
        {
            Debug.LogWarning("RainHerdGoSequence: herdManager 未赋值，跳过 GO 表情。", this);
            return;
        }

        WildeBeestBehavior head = herdManager.headScaredBeest;
        if (head == null)
        {
            Debug.LogWarning("RainHerdGoSequence: headScaredBeest 为空，跳过 GO 表情。", this);
            return;
        }

        WildeBeestScaredLeader leader = head.GetComponent<WildeBeestScaredLeader>();
        if (leader == null)
        {
            leader = head.GetComponentInChildren<WildeBeestScaredLeader>();
        }

        if (leader == null)
        {
            Debug.LogWarning("RainHerdGoSequence: 头马上找不到 WildeBeestScaredLeader，跳过 GO 表情。", this);
            return;
        }

        leader.ShowEmote(goEmoteId);
    }

    private static void PlayOneShot(string soundId, float volume)
    {
        if (string.IsNullOrEmpty(soundId)) return;
        if (AudioController.Instance == null) return;

        AudioController.Instance.PlaySound2D(soundId, volume: volume);
    }

    void OnDisable()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }

        if (denyHintRoutine != null)
        {
            StopCoroutine(denyHintRoutine);
            denyHintRoutine = null;
            ResetDenyHintVisual();
            if (denyHintText != null)
            {
                denyHintText.gameObject.SetActive(false);
            }
        }
    }
}
